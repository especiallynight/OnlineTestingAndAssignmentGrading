
using GradingService.Data;
using GradingService.DTOs;
using GradingService.Models;
using Shared.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace GradingService.Controllers
{
    [ApiController]
    [Route("api/grades")]
    [Authorize(Roles = "Student,Teacher,Admin")]
    public class GradeController : ControllerBase
    {
        private readonly GradesDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public GradeController(
            GradesDbContext context,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        private int? GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(userId, out var id) ? id : null;
        }

        private async Task<JsonDocument?> GetFromService(
            string serviceName,
            string path)
        {
            var client = _httpClientFactory.CreateClient(serviceName);

            var authorization = Request.Headers.Authorization.ToString();

            if (string.IsNullOrWhiteSpace(authorization))
                return null;

            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Authorization",
                authorization);

            using var response = await client.GetAsync(path);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(json);
        }

        private async Task<bool> CanAccessGrade(
            AttemptGrade grade,
            int userId)
        {
            if (User.IsInRole("Admin"))
                return true;

            using var attempt = await GetFromService(
                "ExamService",
                $"api/exam_attempts/attempt/{grade.AttemptId}");

            if (attempt == null)
                return false;

            var attemptData = attempt.RootElement;

            var studentId = attemptData
                .GetProperty("studentId")
                .GetInt32();

            var assignmentId = attemptData
                .GetProperty("assignmentId")
                .GetInt32();

            if (User.IsInRole("Student"))
                return studentId == userId;

            if (!User.IsInRole("Teacher"))
                return false;

            using var assignment = await GetFromService(
                "CourseService",
                $"api/assignments/{assignmentId}");

            if (assignment == null)
                return false;

            var courseId = assignment.RootElement
                .GetProperty("courseId")
                .GetInt32();

            using var course = await GetFromService(
                "CourseService",
                $"api/courses/{courseId}");

            if (course == null)
                return false;

            var teacherId = course.RootElement
                .GetProperty("teacherId")
                .GetInt32();

            return teacherId == userId;
        }

        [HttpGet("attempt/{attempt_id:int}")]
        public async Task<ActionResult<AttemptGradeResponseDTO>>
            GetGradeByAttempt(int attempt_id)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var grade = await _context.AttemptGrades
                .FirstOrDefaultAsync(g => g.AttemptId == attempt_id);

            if (grade == null)
                return NotFound();

            if (!await CanAccessGrade(grade, userId.Value))
                return NotFound();

            var result = new AttemptGradeResponseDTO
            {
                AttemptGradeId = grade.AttemptGradeId,
                AttemptId = grade.AttemptId,
                Score = grade.Score,
                MaxScore = grade.MaxScore,
                CheckedAt = grade.CheckedAt,
                Feedback = grade.Feedback
            };

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<ActionResult<AttemptGradeResponseDTO>>
            CreateGrade(AttemptGradedEvent dto)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            if (dto.MaxScore <= 0 ||
                dto.Score < 0 ||
                dto.Score > dto.MaxScore)
            {
                return BadRequest("Некорректные баллы");
            }

            var existingGrade = await _context.AttemptGrades
                .AnyAsync(g => g.AttemptId == dto.AttemptId);

            if (existingGrade)
                return Conflict("Оценка уже существует");

            using var attempt = await GetFromService(
                "ExamService",
                $"api/exam_attempts/attempt/{dto.AttemptId}");

            if (attempt == null)
                return NotFound("Попытка не найдена");

            var assignmentId = attempt.RootElement
                .GetProperty("assignmentId")
                .GetInt32();

            using var assignment = await GetFromService(
                "CourseService",
                $"api/assignments/{assignmentId}");

            if (assignment == null)
                return NotFound("Задание не найдено");

            var accessGrade = new AttemptGrade
            {
                AttemptId = dto.AttemptId
            };

            if (!await CanAccessGrade(accessGrade, userId.Value))
                return Forbid();

            var grade = new AttemptGrade
            {
                AttemptId = dto.AttemptId,
                Score = dto.Score,
                MaxScore = dto.MaxScore,
                CheckedAt = dto.GradedAt == default
                    ? DateTimeOffset.UtcNow
                    : dto.GradedAt
            };

            _context.AttemptGrades.Add(grade);

            await _context.SaveChangesAsync();

            var result = new AttemptGradeResponseDTO
            {
                AttemptGradeId = grade.AttemptGradeId,
                AttemptId = grade.AttemptId,
                Score = grade.Score,
                MaxScore = grade.MaxScore,
                CheckedAt = grade.CheckedAt,
                Feedback = grade.Feedback
            };

            return CreatedAtAction(
                nameof(GetGradeByAttempt),
                new { attempt_id = grade.AttemptId },
                result);
        }
    }
}
