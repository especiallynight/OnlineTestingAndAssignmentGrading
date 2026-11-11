
using ExamService.Data;
using ExamService.DTOs;
using ExamService.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class AnswerController : ControllerBase
    {
        private readonly ExamDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AnswerController(
            ExamDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private int? GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userId, out var id))
                return id;

            return null;
        }

        [HttpGet("exam_attempts/{attemptId:int}/answers")]
        public async Task<IActionResult> GetAnswers(int attemptId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var attempt = await _context.ExamAttempts.FindAsync(attemptId);

            if (attempt == null)
                return NotFound();

            if (attempt.StudentId != userId &&
                !User.IsInRole("Teacher") &&
                !User.IsInRole("Admin"))
                return Forbid();

            var answers = await _context.Answers
                .Where(a => a.AttemptId == attemptId)
                .Select(a => new
                {
                    a.AnswerId,
                    a.AnswerData,
                    a.QuestionId,
                    a.AttemptId,
                    a.SubmittedAt,
                    a.Status,
                    a.ExecutionStatus,
                    a.PassedTests,
                    a.TotalTests,
                    a.Score,
                    a.ExecutionError
                })
                .ToListAsync();

            return Ok(answers);
        }

        [HttpPost("exam_attempts/{attemptId:int}/answers")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CreateAnswer(
            int attemptId,
            [FromBody] CreateAnswerDTO dto)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var attempt = await _context.ExamAttempts.FindAsync(attemptId);

            if (attempt == null)
                return NotFound();

            if (attempt.StudentId != userId)
                return Forbid();

            if (attempt.Status != ExamAttemptStatus.InProgress)
                return Conflict("Попытка не активна");

            if (attempt.DeadlineAt != null &&
                DateTimeOffset.UtcNow > attempt.DeadlineAt.Value)
                return Conflict("Время попытки истекло");

            var existingAnswer = await _context.Answers
                .AnyAsync(a => a.AttemptId == attemptId &&
                               a.QuestionId == dto.QuestionId);

            if (existingAnswer)
                return Conflict("Ответ на этот вопрос уже сохранён");

            var answer = new Models.Answer
            {
                AnswerData = dto.AnswerData,
                QuestionId = dto.QuestionId,
                AttemptId = attemptId,
                Status = AnswerStatusEnum.Saved
            };

            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                answer.AnswerId,
                answer.AnswerData,
                answer.QuestionId,
                answer.AttemptId,
                answer.Status
            });
        }

        [HttpPut("answers/{answerId:int}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UpdateAnswer(
            int answerId,
            [FromBody] UpdateAnswerDTO dto)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var answer = await _context.Answers
                .Include(a => a.Attempt)
                .FirstOrDefaultAsync(a => a.AnswerId == answerId);

            if (answer == null)
                return NotFound();

            if (answer.Attempt.StudentId != userId)
                return Forbid();

            if (answer.Attempt.Status != ExamAttemptStatus.InProgress)
                return Conflict("Попытка не активна");

            if (answer.Attempt.DeadlineAt != null &&
                DateTimeOffset.UtcNow > answer.Attempt.DeadlineAt.Value)
                return Conflict("Время попытки истекло");

            if (answer.Status != AnswerStatusEnum.Saved)
                return Conflict("Ответ нельзя изменить");

            answer.AnswerData = dto.AnswerData;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                answer.AnswerId,
                answer.AnswerData,
                answer.QuestionId,
                answer.AttemptId,
                answer.Status
            });
        }

        [HttpPost("answers/{answerId:int}/submit")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitCodeAnswer(int answerId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var answer = await _context.Answers
                .Include(a => a.Attempt)
                .FirstOrDefaultAsync(a => a.AnswerId == answerId);

            if (answer == null)
                return NotFound();

            if (answer.Attempt.StudentId != userId)
                return Forbid();

            if (answer.Attempt.Status != ExamAttemptStatus.InProgress)
                return Conflict("Попытка не активна");

            if (answer.Attempt.DeadlineAt != null &&
                DateTimeOffset.UtcNow > answer.Attempt.DeadlineAt.Value)
                return Conflict("Время попытки истекло");

            if (answer.Status != AnswerStatusEnum.Saved)
                return Conflict("Ответ уже отправлен");

            var courseServiceUrl =
                _configuration["Services:CourseService"];

            var codeExecutionServiceUrl =
                _configuration["Services:CodeExecutionService"];

            var internalApiKey =
                _configuration["InternalApiKey"];

            if (string.IsNullOrWhiteSpace(courseServiceUrl) ||
                string.IsNullOrWhiteSpace(codeExecutionServiceUrl) ||
                string.IsNullOrWhiteSpace(internalApiKey))
            {
                return StatusCode(500, "Не настроены адреса сервисов");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{courseServiceUrl}/internal/questions/{answer.QuestionId}/test-cases");

                request.Headers.Add("X-Internal-Api-Key", internalApiKey);

                var testsResponse = await client.SendAsync(request);

                if (!testsResponse.IsSuccessStatusCode)
                {
                    return StatusCode(502, "Не удалось получить тесты задания");
                }

                var testsData = await testsResponse.Content
                    .ReadFromJsonAsync<TestCasesResponse>();

                if (testsData?.TestCases == null ||
                    testsData.TestCases.Count == 0)
                {
                    return BadRequest("Тесты задания не найдены");
                }

                var executionResponse = await client.PostAsJsonAsync(
                    $"{codeExecutionServiceUrl}/api/code-execution/run",
                    new CodeSubmissionRequest
                    {
                        SourceCode = answer.AnswerData,
                        TestCases = testsData.TestCases
                    });

                if (!executionResponse.IsSuccessStatusCode)
                {
                    return StatusCode(502, "Ошибка сервиса проверки кода");
                }

                var result = await executionResponse.Content
                    .ReadFromJsonAsync<CodeExecutionResponse>();

                if (result == null)
                    return StatusCode(502, "Некорректный ответ сервиса проверки");

                answer.ExecutionStatus = result.Status;
                answer.PassedTests = result.PassedTests;
                answer.TotalTests = result.TotalTests;
                answer.Score = result.Score;
                answer.ExecutionError = result.ErrorMessage;
                answer.SubmittedAt = DateTimeOffset.UtcNow;
                answer.Status = AnswerStatusEnum.Graded;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    answer.AnswerId,
                    answer.ExecutionStatus,
                    answer.PassedTests,
                    answer.TotalTests,
                    answer.Score,
                    answer.ExecutionError,
                    answer.Status
                });
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    502,
                    "Не удалось связаться с сервисом проверки кода");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(
                    504,
                    "Превышено время ожидания проверки кода");
            }
            catch (JsonException)
            {
                return StatusCode(
                    502,
                    "Ошибка обработки ответа сервиса");
            }
        }

        private class TestCasesResponse
        {
            [JsonPropertyName("test_cases")]
            public List<TestCaseRequest> TestCases { get; set; } = new();
        }

        private class TestCaseRequest
        {
            [JsonPropertyName("input")]
            public string Input { get; set; } = string.Empty;

            [JsonPropertyName("expectedOutput")]
            public string ExpectedOutput { get; set; } = string.Empty;
        }

        private class CodeSubmissionRequest
        {
            [JsonPropertyName("language")]
            public string Language { get; set; } = "csharp";

            [JsonPropertyName("sourceCode")]
            public string SourceCode { get; set; } = string.Empty;

            [JsonPropertyName("testCases")]
            public List<TestCaseRequest> TestCases { get; set; } = new();
        }

        private class CodeExecutionResponse
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("passedTests")]
            public int PassedTests { get; set; }

            [JsonPropertyName("totalTests")]
            public int TotalTests { get; set; }

            [JsonPropertyName("score")]
            public decimal Score { get; set; }

            [JsonPropertyName("errorMessage")]
            public string? ErrorMessage { get; set; }
        }
    }
}
