using GradingService.Data;
using GradingService.DTOs;
using GradingService.Models;
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
    public class GradesController : ControllerBase
    {
        private readonly GradesDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public GradesController(GradesDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        private int? GetUserId()
        {
            var user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(user_id, out var id) ? id : null;
        }

        private async Task<JsonDocument?> GetFromService(string service_name, string path)
        {
            var client = _httpClientFactory.CreateClient(service_name);

            var authorization = Request.Headers.Authorization.ToString();

            if (string.IsNullOrWhiteSpace(authorization))
                return null;

            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Authorization",
                authorization
            );

            using var response = await client.GetAsync(path);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(json);
        }

        private async Task<bool> CanAccessGrade(AttemptGrade grade, int user_id)
        {
            if (User.IsInRole("Admin"))
                return true;

            using var attempt = await GetFromService(
                "ExamService",
                $"api/exam_attempts/attempt/{grade.AttemptId}"
            );

            if (attempt == null)
                return false;

            var attempt_data = attempt.RootElement;

            var student_id = attempt_data
                .GetProperty("studentId")
                .GetInt32();

            var assignment_id = attempt_data
                .GetProperty("assignmentId")
                .GetInt32();

            if (User.IsInRole("Student"))
                return student_id == user_id;

            if (!User.IsInRole("Teacher"))
                return false;

            using var assignment = await GetFromService(
                "CourseService",
                $"api/assignments/{assignment_id}"
            );

            if (assignment == null)
                return false;

            var course_id = assignment.RootElement
                .GetProperty("courseId")
                .GetInt32();

            using var course = await GetFromService(
                "CourseService",
                $"api/courses/{course_id}"
            );

            if (course == null)
                return false;

            var teacher_id = course.RootElement
                .GetProperty("teacherId")
                .GetInt32();

            return teacher_id == user_id;
        }


        [HttpGet("attempt/{attempt_id:int}")]
        public async Task<ActionResult<AttemptGradeResponseDTO>>
            GetGradeByAttempt(int attempt_id)
        {
            var user_id = GetUserId();

            if (user_id == null)
                return Unauthorized();

            var grade = await _context.AttemptGrades
                .FirstOrDefaultAsync(g => g.AttemptId == attempt_id);

            if (grade == null)
                return NotFound();

            if (!await CanAccessGrade(grade, user_id.Value))
                return NotFound();

            var response = new AttemptGradeResponseDTO
            {
                AttemptGradeId = grade.AttemptGradeId,
                AttemptId = grade.AttemptId,
                Score = grade.Score,
                MaxScore = grade.MaxScore,
                CheckedAt = grade.CheckedAt,
                Feedback = grade.Feedback
            };

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<ActionResult<AttemptGradeResponseDTO>> CreateGrade(AttemptGradedEventDTO dto)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            if (dto.MaxScore <= 0 || dto.Score < 0 || dto.Score > dto.MaxScore)
                return BadRequest("Некорректные баллы");

            var existingGrade = await _context.AttemptGrades
                .AnyAsync(g => g.AttemptId == dto.AttemptId);

            if (existingGrade)
                return Conflict("Оценка уже существует");

            using var attempt = await GetFromService(
                "ExamService",
                $"api/exam_attempts/attempt/{dto.AttemptId}"
            );

            if (attempt == null)
                return NotFound("Попытка не найдена");

            var attemptData = attempt.RootElement;

            var studentId = attemptData
                .GetProperty("studentId")
                .GetInt32();

            var assignmentId = attemptData
                .GetProperty("assignmentId")
                .GetInt32();

            using var assignment = await GetFromService(
                "CourseService",
                $"api/assignments/{assignmentId}"
            );

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
                CheckedAt = DateTimeOffset.UtcNow
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
                result
            );
        }
    }
}