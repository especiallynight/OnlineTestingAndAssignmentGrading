using CourseService.Data;
using CourseService.DTOs;
using CourseService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CourseService.Controllers
{
    // Учебные материалы (теория) курса. Читают все авторизованные,
    // изменяет только преподаватель — владелец курса.
    [ApiController]
    [Route("api/courses/{course_id:int}/materials")]
    [Authorize]
    public class CourseMaterialController : ControllerBase
    {
        private readonly CourseDbContext _context;

        public CourseMaterialController(CourseDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<CourseMaterialResponseDTO>>> GetMaterials(int course_id)
        {
            var exists = await _context.Courses.AnyAsync(course => course.CourseId == course_id);
            if (!exists) return NotFound("Курс не найден");

            var materials = await _context.CourseMaterials
                .Where(material => material.CourseId == course_id)
                .OrderBy(material => material.SortOrder)
                .ThenBy(material => material.CourseMaterialId)
                .Select(material => ToResponse(material))
                .ToListAsync();

            return Ok(materials);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public async Task<ActionResult<CourseMaterialResponseDTO>> CreateMaterial(
            int course_id,
            CreateCourseMaterialDTO dto)
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var course = await _context.Courses
                .FirstOrDefaultAsync(course => course.CourseId == course_id);
            if (course == null) return NotFound("Курс не найден");
            if (course.TeacherId != teacherId.Value) return Forbid();

            var material = new CourseMaterial
            {
                CourseId = course_id,
                Title = dto.Title.Trim(),
                Content = dto.Content.Trim(),
                SortOrder = dto.SortOrder,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.CourseMaterials.Add(material);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMaterials),
                new { course_id = course_id },
                ToResponse(material));
        }

        [Authorize(Roles = "Teacher")]
        [HttpPut("{material_id:int}")]
        public async Task<ActionResult<CourseMaterialResponseDTO>> UpdateMaterial(
            int course_id,
            int material_id,
            UpdateCourseMaterialDTO dto)
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var material = await _context.CourseMaterials
                .Include(item => item.Course)
                .FirstOrDefaultAsync(item =>
                    item.CourseMaterialId == material_id &&
                    item.CourseId == course_id);

            if (material == null) return NotFound("Материал не найден");
            if (material.Course == null) return NotFound("Курс материала не найден");
            if (material.Course.TeacherId != teacherId.Value) return Forbid();

            material.Title = dto.Title.Trim();
            material.Content = dto.Content.Trim();
            material.SortOrder = dto.SortOrder;

            await _context.SaveChangesAsync();

            return Ok(ToResponse(material));
        }

        [Authorize(Roles = "Teacher")]
        [HttpDelete("{material_id:int}")]
        public async Task<IActionResult> DeleteMaterial(int course_id, int material_id)
        {
            var teacherId = GetUserId();
            if (teacherId == null) return Unauthorized();

            var material = await _context.CourseMaterials
                .Include(item => item.Course)
                .FirstOrDefaultAsync(item =>
                    item.CourseMaterialId == material_id &&
                    item.CourseId == course_id);

            if (material == null) return NotFound("Материал не найден");
            if (material.Course == null) return NotFound("Курс материала не найден");
            if (material.Course.TeacherId != teacherId.Value) return Forbid();

            _context.CourseMaterials.Remove(material);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private int? GetUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        private static CourseMaterialResponseDTO ToResponse(CourseMaterial material)
        {
            return new CourseMaterialResponseDTO
            {
                CourseMaterialId = material.CourseMaterialId,
                CourseId = material.CourseId,
                Title = material.Title,
                Content = material.Content,
                SortOrder = material.SortOrder,
                CreatedAt = material.CreatedAt
            };
        }
    }
}
