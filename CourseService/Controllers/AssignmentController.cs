using CourseService.Data;
using CourseService.DTOs;
using CourseService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CourseService.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class AssignmentController : ControllerBase
    {
        private readonly CourseDbContext _context;

        public AssignmentController(CourseDbContext context)
        {
            _context = context;
        }

        [HttpGet("courses/{course_id:int}/assignments")]
        public async Task<ActionResult<List<AssignmentResponseDTO>>> GetAssignments(int course_id)
        {
            var exists = await _context.Courses.AnyAsync(c => c.CourseId == course_id);
            if (!exists) return NotFound("Курс не найден");

            var assignments = await _context.Assignments
                .Where(assignment => assignment.CourseId == course_id)
                .Select(assignment => new AssignmentResponseDTO
                {
                    AssignmentId = assignment.AssignmentId,
                    CourseId = assignment.CourseId,
                    Name = assignment.Name,
                    Description = assignment.Description,
                    MaxScore = assignment.MaxScore,
                    TimeLimitMinutes = assignment.TimeLimitMinutes
                })
                .ToListAsync();

            return Ok(assignments);
        }

        [HttpGet("assignments/{assignment_id:int}")]
        public async Task<ActionResult<AssignmentResponseDTO>> GetAssignment(int assignment_id)
        {
            var assignment = await _context.Assignments
                .Where(assignment => assignment.AssignmentId == assignment_id)
                .Select(assignment => new AssignmentResponseDTO
                {
                    AssignmentId = assignment.AssignmentId,
                    CourseId = assignment.CourseId,
                    Name = assignment.Name,
                    Description = assignment.Description,
                    MaxScore = assignment.MaxScore,
                    TimeLimitMinutes = assignment.TimeLimitMinutes
                })
                .FirstOrDefaultAsync();

            if (assignment == null) return NotFound("Задание не найдено");

            return Ok(assignment);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost("courses/{course_id:int}/assignments")]
        public async Task<ActionResult<AssignmentResponseDTO>> CreateAssignment(int course_id, CreateAssignmentDTO dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var teacherId)) return Unauthorized();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == course_id);
            if (course == null) return NotFound("Курс не найден");

            if (course.TeacherId != teacherId) return Forbid();

            var assignment = new Assignment
            {
                CourseId = course_id,
                Name = dto.Name,
                Description = dto.Description,
                MaxScore = dto.MaxScore,
                TimeLimitMinutes = dto.TimeLimitMinutes
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            var response = new AssignmentResponseDTO
            {
                AssignmentId = assignment.AssignmentId,
                CourseId = assignment.CourseId,
                Name = assignment.Name,
                Description = assignment.Description,
                MaxScore = assignment.MaxScore,
                TimeLimitMinutes = assignment.TimeLimitMinutes
            };

            return CreatedAtAction(nameof(GetAssignment), new { assignment_id = assignment.AssignmentId }, response);
        }


        [Authorize(Roles = "Teacher")]
        [HttpPut("assignments/{assignment_id:int}")]
        public async Task<ActionResult<AssignmentResponseDTO>> UpdateAssignment(int assignment_id, UpdateAssignmentDTO dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var teacherId)) return Unauthorized();

            var assignment = await _context.Assignments
                .Include(item => item.Course)
                .FirstOrDefaultAsync(item => item.AssignmentId == assignment_id);

            if (assignment == null) return NotFound("Задание не найдено");
            if (assignment.Course == null) return NotFound("Курс задания не найден");
            if (assignment.Course.TeacherId != teacherId) return Forbid();

            assignment.Name = dto.Name;
            assignment.Description = dto.Description;
            assignment.MaxScore = dto.MaxScore;
            assignment.TimeLimitMinutes = dto.TimeLimitMinutes;

            await _context.SaveChangesAsync();

            var response = new AssignmentResponseDTO
            {
                AssignmentId = assignment.AssignmentId,
                CourseId = assignment.CourseId,
                Name = assignment.Name,
                Description = assignment.Description,
                MaxScore = assignment.MaxScore,
                TimeLimitMinutes = assignment.TimeLimitMinutes
            };

            return Ok(response);
        }

        [Authorize(Roles = "Teacher")]
        [HttpDelete("assignments/{assignment_id:int}")]
        public async Task<IActionResult> DeleteAssignment(int assignment_id)
        {
            var user_id_str = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(user_id_str, out var teacher_id))
                return Unauthorized();

            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.AssignmentId == assignment_id);

            if (assignment == null)
                return NotFound("Задание не найдено");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == assignment.CourseId);

            if (course == null)
                return NotFound("Курс не найден");

            if (course.TeacherId != teacher_id)
                return Forbid();

            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return NoContent();  
        }
    }
}