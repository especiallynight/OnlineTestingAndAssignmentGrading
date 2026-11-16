using System.ComponentModel.DataAnnotations;

namespace CourseService.DTOs
{
    public class UpdateAssignmentDTO
    {
        [Required(ErrorMessage = "Название задания обязательно")]
        [MinLength(3, ErrorMessage = "Название задания должно содержать минимум 3 символа")]
        [MaxLength(100, ErrorMessage = "Название задания не должно превышать 100 символов")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(170, ErrorMessage = "Описание не должно превышать 170 символов")]
        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Максимальный балл должен быть больше нуля")]
        public int MaxScore { get; set; }

        [Range(1, 1440, ErrorMessage = "Лимит времени должен быть от 1 до 1440 минут")]
        public int TimeLimitMinutes { get; set; }
    }
}
