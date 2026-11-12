
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


namespace ExamService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/exam_attempts")]
    public class ExamController : ControllerBase
    {
        private readonly ExamDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;

        public ExamController(
            ExamDbContext context,
            IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
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

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<ExamAttemptResponseDTO>> CreateAttempt([FromBody] CreateExamAttemptDTO dto)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            if (dto.AssignmentId <= 0)
                return BadRequest("Некорректный ID задания");

            var attempt = new ExamAttempt
            {
                StudentId = userId.Value,
                AssignmentId = dto.AssignmentId,
                Status = ExamAttemptStatus.InProgress
            };

            _context.ExamAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetAttemptById),
                new { attemptId = attempt.AttemptId },
                ToDTO(attempt));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamAttemptResponseDTO>>> GetAttempts()
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

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

            if (userId == null)
                return Unauthorized();

            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId);

            if (attempt == null)
                return NotFound();

            if (attempt.StudentId != userId &&
                !User.IsInRole("Teacher") &&
                !User.IsInRole("Admin"))
                return Forbid();

            return Ok(ToDTO(attempt));
        }

        [HttpPost("{attemptId:int}/start")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<ExamAttemptResponseDTO>> StartAttempt( int attemptId,[FromBody] StartAttemptDTO dto)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var attempt = await _context.ExamAttempts.FindAsync(attemptId);

            if (attempt == null)
                return NotFound();

            if (attempt.StudentId != userId)
                return Forbid();

            if (attempt.StartedAt != null)
                return Conflict("Попытка уже начата");

            attempt.Status = ExamAttemptStatus.InProgress;
            attempt.StartedAt = DateTimeOffset.UtcNow;
            attempt.DeadlineAt = attempt.StartedAt.Value
                .AddMinutes(dto.TimeLimitMinutes);

            await _context.SaveChangesAsync();

            return Ok(ToDTO(attempt));
        }

        [HttpPut("{attemptId:int}/stop")]
        public async Task<ActionResult<ExamAttemptResponseDTO>> StopAttempt(int attemptId)
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

            if (attempt.Status != ExamAttemptStatus.InProgress ||
                attempt.StartedAt == null)
                return Conflict("Попытку нельзя завершить");

            if (attempt.DeadlineAt != null &&
                DateTimeOffset.UtcNow > attempt.DeadlineAt.Value)
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
