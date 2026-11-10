using System.ComponentModel.DataAnnotations;

namespace ExamService.DTOs
{
    public class StartAttemptDTO
    {
        [Range(1, 1440, ErrorMessage = "Лимит должен быть от 1 до 1440 минут")]
        public int TimeLimitMinutes { get; set; }
    }


}
