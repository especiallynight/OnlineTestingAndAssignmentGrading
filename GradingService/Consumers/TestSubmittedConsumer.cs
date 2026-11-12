using GradingService.Data;
using GradingService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;
using System.Text.Json;

namespace GradingService.Consumers;

public class TestSubmittedConsumer : IConsumer<TestSubmittedEvent>
{
    private readonly GradesDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IPublishEndpoint _publishEndpoint;

    public TestSubmittedConsumer(
        GradesDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<TestSubmittedEvent> context)
    {
        var message = context.Message;

        var existingGrade = await _context.AttemptGrades
            .AnyAsync(g => g.AttemptId == message.AttemptId);

        if (existingGrade)
            return;

        var internalApiKey = _configuration["InternalApiKey"];

        if (string.IsNullOrWhiteSpace(internalApiKey))
            throw new InvalidOperationException(
                "InternalApiKey не настроен");

        var courseClient = _httpClientFactory.CreateClient("CourseService");

        using var assignmentRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/internal/assignments/{message.AssignmentId}");

        assignmentRequest.Headers.Add(
            "X-Internal-Api-Key",
            internalApiKey);

        using var assignmentResponse =
            await courseClient.SendAsync(assignmentRequest);

        assignmentResponse.EnsureSuccessStatusCode();

        using var assignmentJson = JsonDocument.Parse(
            await assignmentResponse.Content.ReadAsStringAsync());

        var assignment = assignmentJson.RootElement;

        var courseId = assignment.GetProperty("courseId").GetInt32();
        var maxScore = assignment.GetProperty("maxScore").GetDecimal();

        if (message.Answers.Count == 0)
        {
            var emptyGrade = new AttemptGrade
            {
                AttemptId = message.AttemptId,
                Score = 0,
                MaxScore = maxScore,
                CheckedAt = DateTimeOffset.UtcNow,
                Feedback = "Ответы отсутствуют"
            };

            _context.AttemptGrades.Add(emptyGrade);

            await _publishEndpoint.Publish(new AttemptGradedEvent
            {
                AttemptId = message.AttemptId,
                StudentId = message.StudentId,
                CourseId = courseId,
                Score = 0,
                MaxScore = maxScore,
                GradedAt = emptyGrade.CheckedAt
            });

            await _context.SaveChangesAsync();
            return;
        }

        var questionCount = assignment.GetProperty("questionCount").GetInt32();
        var questionWeight = questionCount > 0 ? maxScore / questionCount : 0m;
        
        decimal totalScore = 0;
        var feedback = new List<string>();

        foreach (var answer in message.Answers)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/internal/questions/{answer.QuestionId}/correct-answer");

            request.Headers.Add("X-Internal-Api-Key", internalApiKey);

            using var response = await courseClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
                {
                    continue;
                }

                response.EnsureSuccessStatusCode();
            }

            using var questionJson = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

            var root = questionJson.RootElement;
            var questionType = root.GetProperty("question_type").GetString();

            decimal fraction;

            if (questionType == "SingleChoice" ||
                questionType == "MultipleChoice")
            {
                var correctAnswer = root.GetProperty("correct_answer");

                fraction = NormalizeAnswer(answer.AnswerData) ==
                           NormalizeAnswer(correctAnswer)
                    ? 1m
                    : 0m;
            }
            else
            {
                fraction = (answer.CodeScore ?? 0m) / 100m;
                fraction = Math.Clamp(fraction, 0m, 1m);
            }

            var earned = questionWeight * fraction;
            totalScore += earned;

            feedback.Add(
                $"Вопрос {answer.QuestionId}: {earned:F2} из {questionWeight:F2}");
        }

        totalScore = Math.Round(
            totalScore,
            2,
            MidpointRounding.AwayFromZero);

        var checkedAt = DateTimeOffset.UtcNow;

        var grade = new AttemptGrade
        {
            AttemptId = message.AttemptId,
            Score = totalScore,
            MaxScore = maxScore,
            CheckedAt = checkedAt,
            Feedback = string.Join("; ", feedback)
        };

        _context.AttemptGrades.Add(grade);

        await _publishEndpoint.Publish(new AttemptGradedEvent
        {
            AttemptId = message.AttemptId,
            StudentId = message.StudentId,
            CourseId = courseId,
            Score = totalScore,
            MaxScore = maxScore,
            GradedAt = checkedAt
        });

        await _context.SaveChangesAsync();
    }

    private static string NormalizeAnswer(string answer)
    {
        try
        {
            using var document = JsonDocument.Parse(answer);
            return NormalizeAnswer(document.RootElement);
        }
        catch (JsonException)
        {
            return answer.Trim();
        }
    }

    private static string NormalizeAnswer(JsonElement answer)
    {
        if (answer.ValueKind == JsonValueKind.Array)
        {
            return string.Join(
                ",",
                answer.EnumerateArray()
                    .Select(GetValue)
                    .OrderBy(x => x, StringComparer.Ordinal));
        }

        return GetValue(answer);
    }

    private static string GetValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => element.GetRawText()
        };
    }
}
