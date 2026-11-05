using AnalyticsService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Controller
{
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {

        private readonly AnalyticsDbContext _analyticsDbContext;
        public AnalyticsController(AnalyticsDbContext analyticsDbContext)
        {
            _analyticsDbContext = analyticsDbContext;

        }

        [HttpGet("{studentId}/{courseId}")]
        public async Task<IActionResult> GetStudentStatistics(int studentId, int courseId)
        {
            var statistics = await _analyticsDbContext.StudentStatistics
                .FirstOrDefaultAsync(student => student.StudentId == studentId
                && student.CourseId == courseId);

            if (statistics == null)
            {
                return NotFound("Статистика не найдена");
            }
            return Ok(statistics);
        }
      
    }
}
