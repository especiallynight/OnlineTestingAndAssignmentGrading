using System.ComponentModel.DataAnnotations;

namespace CourseService.DTOs
{
    public class UpdateCourseDTO
    {
        [Required(ErrorMessage = "Название курса обязательно")]
        [MaxLength(100, ErrorMessage = "Название курса не должно превышать 100 символов")]
        [MinLength(3, ErrorMessage = "Название курса должно содержать минимум 3 символа")]
        public string Name { get; set; } = string.Empty;
        [MaxLength(150, ErrorMessage = "Описание курса не должно превышать 100 символов")]
        public string? Description { get; set; }
    }
}
