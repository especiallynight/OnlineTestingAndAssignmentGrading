using CourseService.Data;
using CourseService.DTOs;
using CourseService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CourseService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/courses")]
    public class CourseController : ControllerBase
    {
        private readonly CourseDbContext _context;
        private readonly IDistributedCache _cache;

        public CourseController(CourseDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }
        private static readonly DistributedCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        
        [HttpGet("{course_id:int}")]
        public async Task<ActionResult<CourseResponseDTO>> GetCourse(int course_id)
        {
            var key = $"courses:{course_id}";
            var cached = await _cache.GetStringAsync(key);
            if (cached != null)
                return Ok(JsonSerializer.Deserialize<CourseResponseDTO>(cached));

            var course = await _context.Courses
                .Where(course => course.CourseId == course_id)
                .Select(course => new CourseResponseDTO
                {
                    CourseId = course.CourseId,
                    Name = course.Name,
                    CreatedAt = course.CreatedAt,
                    Description = course.Description,
                    TeacherId = course.TeacherId,
                    StudentsCount = course.Enrollments.Count,
                    AssignmentsCount = course.Assignments.Count
                })
                .FirstOrDefaultAsync();
            if (course == null)
            {
                return NotFound();
            }

            await _cache.SetStringAsync(key, JsonSerializer.Serialize(course), CacheOptions);
            return Ok(course);
        }

        [HttpGet]
        public async Task<ActionResult<List<CourseResponseDTO>>> GetCourses()
        {
            const string key = "courses:list";
            var cached = await _cache.GetStringAsync(key);
            if (cached != null)
                return Ok(JsonSerializer.Deserialize<List<CourseResponseDTO>>(cached));

            var courses = await _context.Courses
                .Select(course => new CourseResponseDTO
                {
                    CourseId = course.CourseId,
                    Name = course.Name,
                    CreatedAt = course.CreatedAt,
                    Description = course.Description,
                    TeacherId = course.TeacherId,
                    StudentsCount = course.Enrollments.Count,
                    AssignmentsCount = course.Assignments.Count
                })
                .ToListAsync();

            await _cache.SetStringAsync(key, JsonSerializer.Serialize(courses), CacheOptions);
            return Ok(courses);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public async Task<ActionResult<CourseResponseDTO>> CreateCourse(CreateCourseDTO dto)
        {
            if (!User.IsInRole("Teacher"))
            {
                return Forbid();
            }

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
            await _cache.RemoveAsync("courses:list");

            var response = new CourseResponseDTO
            {
                CourseId = course.CourseId,
                Name = course.Name,
                CreatedAt = course.CreatedAt,
                Description = course.Description,
                TeacherId = course.TeacherId,
                StudentsCount = 0,
                AssignmentsCount = 0
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
            await _cache.RemoveAsync("courses:list");
            await _cache.RemoveAsync($"courses:{course_id}");

            var response = new CourseResponseDTO
            {
                CourseId = course.CourseId,
                Name = course.Name,
                CreatedAt = course.CreatedAt,
                Description = course.Description,
                TeacherId = course.TeacherId,
                StudentsCount = await _context.Enrollments.CountAsync(enrollment => enrollment.CourseId == course_id),
                AssignmentsCount = await _context.Assignments.CountAsync(assignment => assignment.CourseId == course_id)
            };
            return Ok(response);

        }

        [Authorize(Roles = "Student")]
        [HttpGet("enrolled")]
        public async Task<ActionResult<List<CourseResponseDTO>>> GetEnrolledCourses()
        {
            var student_id = GetUserId();
            if (student_id == null)
            {
                return Unauthorized();
            }

            var courses = await _context.Enrollments
                .Where(enrollment => enrollment.StudentId == student_id.Value)
                .Select(enrollment => new CourseResponseDTO
                {
                    CourseId = enrollment.Course.CourseId,
                    Name = enrollment.Course.Name,
                    CreatedAt = enrollment.Course.CreatedAt,
                    Description = enrollment.Course.Description,
                    TeacherId = enrollment.Course.TeacherId,
                    StudentsCount = enrollment.Course.Enrollments.Count,
                    AssignmentsCount = enrollment.Course.Assignments.Count
                })
                .ToListAsync();

            return Ok(courses);
        }

        [Authorize(Roles = "Student")]
        [HttpPost("{course_id:int}/enroll")]
        public async Task<IActionResult> Enroll(int course_id)
        {
            var student_id = GetUserId();
            if (student_id == null)
            {
                return Unauthorized();
            }

            var course_exists = await _context.Courses
                .AnyAsync(course => course.CourseId == course_id);
            if (!course_exists)
            {
                return NotFound("Курс не найден");
            }

            var already_enrolled = await _context.Enrollments
                .AnyAsync(enrollment =>
                    enrollment.CourseId == course_id &&
                    enrollment.StudentId == student_id.Value);
            if (already_enrolled)
            {
                return Conflict("Вы уже записаны на этот курс");
            }

            var enrollment = new Enrollment
            {
                StudentId = student_id.Value,
                CourseId = course_id,
                EnrolledAt = DateTimeOffset.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            await _cache.RemoveAsync("courses:list");
            await _cache.RemoveAsync($"courses:{course_id}");

            return StatusCode(201, new
            {
                enrollment.EnrollmentId,
                enrollment.StudentId,
                enrollment.CourseId,
                enrollment.EnrolledAt
            });
        }

        [Authorize(Roles = "Student")]
        [HttpDelete("{course_id:int}/enroll")]
        public async Task<IActionResult> Unenroll(int course_id)
        {
            var student_id = GetUserId();
            if (student_id == null)
            {
                return Unauthorized();
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(item =>
                    item.CourseId == course_id &&
                    item.StudentId == student_id.Value);
            if (enrollment == null)
            {
                return NotFound("Запись на курс не найдена");
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            await _cache.RemoveAsync("courses:list");
            await _cache.RemoveAsync($"courses:{course_id}");

            return NoContent();
        }

        [Authorize]
        [HttpGet("{course_id:int}/enrollment")]
        public async Task<IActionResult> GetEnrollment(int course_id)
        {
            var student_id = GetUserId();
            var students_count = await _context.Enrollments
                .CountAsync(enrollment => enrollment.CourseId == course_id);

            var enrolled = student_id != null && await _context.Enrollments
                .AnyAsync(enrollment =>
                    enrollment.CourseId == course_id &&
                    enrollment.StudentId == student_id.Value);

            return Ok(new
            {
                courseId = course_id,
                enrolled,
                studentsCount = students_count
            });
        }

        private int? GetUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
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

            var assignmentIds = await _context.Assignments
                .Where(a => a.CourseId == course_id)
                .Select(a => a.AssignmentId)
                .ToListAsync();

            var enrollments = await _context.Enrollments
                .Where(enrollment => enrollment.CourseId == course_id)
                .ToListAsync();
            _context.Enrollments.RemoveRange(enrollments);

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            await _cache.RemoveAsync("courses:list");
            await _cache.RemoveAsync($"courses:{course_id}");
            foreach (var assignmentId in assignmentIds)
                await _cache.RemoveAsync($"assignments:{assignmentId}:questions");
            return NoContent();
        }
    }
}
