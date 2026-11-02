using System.ComponentModel.DataAnnotations;

namespace CourseService.DTOs
{
    public class UpdateAssignmentDto
    {
        [Required(ErrorMessage = "Название задания обязательно")]
        [MinLength(3, ErrorMessage = "Название задания должно содержать минимум 3 символа")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxScore { get; set; }

        [Range(1, 1440)]
        public int TimeLimitMinutes { get; set; }
    }
}
