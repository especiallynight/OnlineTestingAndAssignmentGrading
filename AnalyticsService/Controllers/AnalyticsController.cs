using AnalyticsService.Data;
using AnalyticsService.DTOs;
using AnalyticsService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsDbContext _analyticsDbContext;

        public AnalyticsController(AnalyticsDbContext analyticsDbContext)
        {
            _analyticsDbContext = analyticsDbContext;
        }

        [HttpGet("students/{studentId:int}")]
        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> GetStudentStatistics(int studentId)
        {
            var currentUserId = User.FindFirst("sub")?.Value;

            if (!int.TryParse(currentUserId, out var userId))
                return Unauthorized();

            if (userId != studentId && !User.IsInRole("Admin"))
                return Forbid();

            var stats = await _analyticsDbContext.StudentStatistics
                .Where(s => s.StudentId == studentId)
                .ToListAsync();

            if (stats.Count == 0)
                return NotFound("Статистика для указанного студента не найдена");

            return Ok(stats.Select(s => new StudentStatisticsResponseDTO
            {
                CourseId = s.CourseId,
                CompletedAttempts = s.CompletedAttempts,
                AverageScore = s.AverageScore,
                HighestScore = s.HighestScore
            }));
        }

        [HttpGet("courses/{courseId:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> GetCourseStatistics(int courseId)
        {
            var stats = await _analyticsDbContext.StudentStatistics
                .Where(s => s.CourseId == courseId)
                .Select(s => new { s.CompletedAttempts, s.AverageScore, s.UpdatedAt })
                .ToListAsync();

            if (stats.Count == 0)
                return NotFound("Статистика по данному курсу не найдена");

            var totalAttempts = stats.Sum(s => s.CompletedAttempts);
            var averageScore = totalAttempts > 0
                ? (decimal)stats.Sum(s => s.AverageScore * s.CompletedAttempts) / totalAttempts
                : 0;
            var lastUpdate = stats.Max(s => s.UpdatedAt);

            return Ok(new CourseStatisticsResponseDTO
            {
                CourseId = courseId,
                CompletedAttempts = totalAttempts,
                AverageScore = averageScore,
                UpdatedAt = lastUpdate
            });
        }

        [HttpPost("students")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> UpdateStudentStatistics(UpdateStudentStatisticsDTO dto)
        {
            if (dto.MaxScore <= 0 || dto.Score < 0 || dto.Score > dto.MaxScore)
                return BadRequest("Некорректные баллы");

            var stats = await _analyticsDbContext.StudentStatistics
                .FirstOrDefaultAsync(s =>
                    s.StudentId == dto.StudentId &&
                    s.CourseId == dto.CourseId);

            if (stats != null && stats.ProcessedAttemptIds.Contains(dto.AttemptId))
            {
                return Ok(new { message = "Эта попытка уже учтена" });
            }

            var scoreInPercent = dto.Score / dto.MaxScore * 100;

            if (stats == null)
            {
                stats = new StudentStatistics
                {
                    ProcessedAttemptIds = new List<int> { dto.AttemptId },
                    StudentId = dto.StudentId,
                    CourseId = dto.CourseId,
                    CompletedAttempts = 1,
                    AverageScore = scoreInPercent,
                    HighestScore = scoreInPercent,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _analyticsDbContext.StudentStatistics.Add(stats);
            }
            else
            {
                stats.AverageScore = (stats.AverageScore * stats.CompletedAttempts + scoreInPercent) / (stats.CompletedAttempts + 1);
                stats.CompletedAttempts++;

                if (scoreInPercent > stats.HighestScore)
                    stats.HighestScore = scoreInPercent;

                stats.ProcessedAttemptIds.Add(dto.AttemptId);
                stats.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _analyticsDbContext.SaveChangesAsync();

            return Ok();
        }
    }
}