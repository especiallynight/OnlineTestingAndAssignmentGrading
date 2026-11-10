using ExamService.Data;
using ExamService.DTOs;
using ExamService.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExamService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class AnswerController : ControllerBase
    {
        private readonly ExamDbContext _context;

        public AnswerController(ExamDbContext context)
        {
            _context = context;
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
                    a.Status
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
    }
}