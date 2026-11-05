using CourseService.Data;
using CourseService.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseService.Controllers
{
    public class CourseController : ControllerBase
    {
        private readonly CourseDbContext _context;
        public CourseController(CourseDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task <ActionResult<List<CourseResponseDTO>>> GetCourses()
        {
            var courses = await _context.Courses
                .Select(course => new CourseResponseDTO
                {
                    CourseId = course.CourseId,
                    Name = course.Name,
                    CreatedAt = DateTime.UtcNow,
                    Description = course.Description,
                    TeacherId = course.TeacherId


                })
                .ToListAsync();
            
            return Ok(courses);
        }
    }
}
