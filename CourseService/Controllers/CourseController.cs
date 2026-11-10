using CourseService.Data;
using CourseService.DTOs;
using CourseService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CourseService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/courses")]
    public class CourseController : ControllerBase
    {
        private readonly CourseDbContext _context;
        public CourseController(CourseDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<List<CourseResponseDTO>>> GetCourses()
        {
            var courses = await _context.Courses
                .Select(course => new CourseResponseDTO
                {
                    CourseId = course.CourseId,
                    Name = course.Name,
                    CreatedAt = course.CreatedAt,
                    Description = course.Description,
                    TeacherId = course.TeacherId


                })
                .ToListAsync();
            return Ok(courses);
        }

        [HttpGet("{course_id:int}")]
        public async Task<ActionResult<CourseResponseDTO>> GetCourse(int course_id)
        {
            var course = await _context.Courses
                .Where(course => course.CourseId == course_id)
                .Select(course => new CourseResponseDTO
                {
                    CourseId = course.CourseId,
                    Name = course.Name,
                    CreatedAt = course.CreatedAt,
                    Description = course.Description,
                    TeacherId = course.TeacherId


                })
                .FirstOrDefaultAsync();
            if (course == null)
            {
                return NotFound();
            }
            return Ok(course);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]

        public async Task<ActionResult<CourseResponseDTO>> CreateCourse(CreateCourseDTO dto)
        {
            var user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            if (!int.TryParse(user_id, out var teacher_id))
            {
                return Unauthorized();
            }

            var course = new Course
            {
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow,
                Description = dto.Description,
                TeacherId = teacher_id
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var response = new CourseResponseDTO
            {
                CourseId = course.CourseId,
                Name = course.Name,
                CreatedAt = course.CreatedAt,
                Description = course.Description,
                TeacherId = course.TeacherId
            };
            return CreatedAtAction(
                nameof(GetCourse),
                new { course_id = course.CourseId },
                response);
                
        }
        [Authorize(Roles = "Teacher")]
        [HttpPut("{course_id:int}")]
        public async Task<ActionResult<CourseResponseDTO>> UpdateCourse(int course_id, UpdateCourseDTO dto)
        {

            var user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            if (!int.TryParse(user_id, out var teacher_id))
            {
                return Unauthorized();
            }
            
            var course = await _context.Courses
                .FirstOrDefaultAsync(course => course.CourseId == course_id);

            if (course == null)
            {
                return NotFound();
            }
            if (course.TeacherId != teacher_id)
            {
                return Forbid();
            }
            
            course.Name = dto.Name;
            course.Description = dto.Description;
            
            await _context.SaveChangesAsync();

            var response = new CourseResponseDTO
            {
                CourseId = course.CourseId,
                Name = course.Name,
                CreatedAt = course.CreatedAt,
                Description = course.Description,
                TeacherId = course.TeacherId
            };
            return Ok(response);

        }

        [Authorize(Roles = "Teacher")]
        [HttpDelete("{course_id:int}")]
        
        public async Task<ActionResult> DeleteCourse(int course_id)
        {
            var user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(user_id, out var teacher_id))
            {
                return Unauthorized();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(course => course.CourseId == course_id);

            if (course == null)
            {
                return NotFound();
            }

            if (course.TeacherId != teacher_id)
            {
                return Forbid();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
