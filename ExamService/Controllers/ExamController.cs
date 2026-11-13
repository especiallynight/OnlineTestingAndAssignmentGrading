using ExamService.Data;
using ExamService.DTOs;
using ExamService.Enums;
using ExamService.Models;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Events;
using System.Security.Claims;
using System.Text.Json;

namespace ExamService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/exam_attempts")]
    public class ExamController : ControllerBase
    {
        private readonly ExamDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ExamController(
            ExamDbContext context,
            IPublishEndpoint publishEndpoint,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
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

        private static ExamAttemptResponseDTO ToDTO(ExamAttempt attempt)
        {
            return new ExamAttemptResponseDTO
            {
                AttemptId = attempt.AttemptId,
                StudentId = attempt.StudentId,
                AssignmentId = attempt.AssignmentId,
                StartedAt = attempt.StartedAt,
                DeadlineAt = attempt.DeadlineAt,
                Status = attempt.Status
            };
        }

        // Запрашивает лимит времени задания у CourseService (internal API).
        // Если сервис недоступен, вызывающий код использует значение по умолчанию.
        private async Task<int?> GetAssignmentTimeLimitAsync(int assignmentId)
        {
            try
            {
                var baseUrl = _configuration["Services:CourseService"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return null;

                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    baseUrl.TrimEnd('/') + "/internal/assignments/" + assignmentId);

                var apiKey = _configuration["InternalApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                    request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", apiKey);

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);

                if (document.RootElement.TryGetProperty("timeLimitMinutes", out var element) &&
                    element.TryGetInt32(out var minutes) &&
                    minutes > 0)
                {
                    return minutes;
                }
            }
            catch
            {
                // CourseService может быть недоступен — работаем с лимитом по умолчанию.
            }

            return null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamAttemptResponseDTO>>> GetAttempts()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var attempts = _context.ExamAttempts.AsQueryable();

            if (User.IsInRole("Student"))
            {
                attempts = attempts.Where(a => a.StudentId == userId);
            }
            else if (!User.IsInRole("Teacher") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var result = await attempts.ToListAsync();
            return Ok(result.Select(ToDTO));
        }

        [HttpGet("attempt/{attemptId:int}")]
        public async Task<ActionResult<ExamAttemptResponseDTO>> GetAttemptById(int attemptId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var attempt = await _context.ExamAttempts.FirstOrDefaultAsync(a => a.AttemptId == attemptId);
            if (attempt == null) return NotFound();

            if (attempt.StudentId != userId && !User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                return Forbid();

            return Ok(ToDTO(attempt));
        }

        // Создаёт попытку или возвращает уже начатую.
        // На пару (студент, задание) допускается ровно одна попытка:
        // завершённая или просроченная блокирует повторное прохождение.
        [HttpPost]
        public async Task<ActionResult<ExamAttemptResponseDTO>> CreateAttempt([FromBody] CreateExamAttemptDTO dto)
        {
            var studentId = GetUserId();
            if (studentId == null) return Unauthorized();
            if (!User.IsInRole("Student")) return Forbid();

            var attempts = await _context.ExamAttempts
                .Where(a => a.AssignmentId == dto.AssignmentId && a.StudentId == studentId)
                .ToListAsync();

            var finished = attempts.FirstOrDefault(a =>
                a.Status == ExamAttemptStatus.Completed ||
                a.Status == ExamAttemptStatus.Expired);

            if (finished != null)
                return Conflict(new { message = "Вы уже проходили этот тест. Повторная попытка невозможна." });

            var active = attempts.FirstOrDefault(a => a.Status == ExamAttemptStatus.InProgress);

            if (active != null)
            {
                if (active.DeadlineAt != null && DateTimeOffset.UtcNow > active.DeadlineAt.Value)
                {
                    active.Status = ExamAttemptStatus.Expired;
                    await _context.SaveChangesAsync();
                    return Conflict(new { message = "Время попытки истекло. Повторное прохождение невозможно." });
                }

                return Ok(ToDTO(active));
            }

            var timeLimit = await GetAssignmentTimeLimitAsync(dto.AssignmentId)
                ?? dto.TimeLimitMinutes
                ?? 60;

            var now = DateTimeOffset.UtcNow;
            var attempt = new ExamAttempt
            {
                AssignmentId = dto.AssignmentId,
                StudentId = studentId.Value,
                Status = ExamAttemptStatus.InProgress,
                StartedAt = now,
                DeadlineAt = now.AddMinutes(timeLimit)
            };

            _context.ExamAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return Ok(ToDTO(attempt));
        }

        // Идемпотентно возвращает начатую попытку, не сбрасывая дедлайн.
        [HttpPost("{attemptId:int}/start")]
        public async Task<ActionResult<ExamAttemptResponseDTO>> StartAttempt(
            int attemptId,
            [FromBody] StartAttemptDTO dto)
        {
            var studentId = GetUserId();
            if (studentId == null) return Unauthorized();

            var attempt = await _context.ExamAttempts.FindAsync(attemptId);
            if (attempt == null) return NotFound();

            if (attempt.StudentId != studentId && !User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                return Forbid();

            if (attempt.Status != ExamAttemptStatus.InProgress)
                return Conflict(new { message = "Попытка уже завершена. Повторное прохождение невозможно." });

            if (attempt.DeadlineAt != null && DateTimeOffset.UtcNow > attempt.DeadlineAt.Value)
            {
                attempt.Status = ExamAttemptStatus.Expired;
                await _context.SaveChangesAsync();
                return Conflict(new { message = "Время попытки истекло." });
            }

            if (attempt.StartedAt == null)
            {
                var timeLimit = dto.TimeLimitMinutes > 0 ? dto.TimeLimitMinutes : 60;
                attempt.StartedAt = DateTimeOffset.UtcNow;
                attempt.DeadlineAt = attempt.StartedAt.Value.AddMinutes(timeLimit);
                await _context.SaveChangesAsync();
            }

            return Ok(ToDTO(attempt));
        }

        [HttpPut("{attemptId:int}/stop")]
        public async Task<ActionResult<ExamAttemptResponseDTO>> StopAttempt(int attemptId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var attempt = await _context.ExamAttempts.FindAsync(attemptId);
            if (attempt == null) return NotFound();

            if (attempt.StudentId != userId && !User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                return Forbid();

            if (attempt.Status != ExamAttemptStatus.InProgress || attempt.StartedAt == null)
                return Conflict(new { message = "Попытку нельзя завершить, она уже завершена или не начата." });

            if (attempt.DeadlineAt != null && DateTimeOffset.UtcNow > attempt.DeadlineAt.Value)
            {
                attempt.Status = ExamAttemptStatus.Expired;
            }
            else
            {
                attempt.Status = ExamAttemptStatus.Completed;

                var answers = await _context.Answers
                    .Where(a => a.AttemptId == attempt.AttemptId)
                    .Select(a => new SubmittedAnswer
                    {
                        QuestionId = a.QuestionId,
                        AnswerData = a.AnswerData,
                        CodeScore = a.Score
                    })
                    .ToListAsync();

                await _publishEndpoint.Publish(new TestSubmittedEvent
                {
                    AttemptId = attempt.AttemptId,
                    StudentId = attempt.StudentId,
                    AssignmentId = attempt.AssignmentId,
                    Answers = answers
                });
            }

            await _context.SaveChangesAsync();
            return Ok(ToDTO(attempt));
        }
    }
}
