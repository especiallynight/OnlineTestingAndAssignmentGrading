using System.ComponentModel.DataAnnotations;

namespace ExamService.DTOs
{
    public class CreateExamAttemptDTO
    {
        [Required(ErrorMessage = "ID задания обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "Некорректный ID задания")]
        public int AssignmentId { get; set; }

        [Range(1, 1440, ErrorMessage = "Лимит времени должен быть от 1 до 1440 минут")]
        public int? TimeLimitMinutes { get; set; }
    }
}
