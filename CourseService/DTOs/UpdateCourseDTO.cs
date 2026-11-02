using System.ComponentModel.DataAnnotations;

namespace CourseService.DTOs
{
    public class UpdateCourseDTO
    {
        [Required(ErrorMessage = "Название курса обязательно")]

        [MinLength(3, ErrorMessage = "Название курса должно содержать минимум 3 символа")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
