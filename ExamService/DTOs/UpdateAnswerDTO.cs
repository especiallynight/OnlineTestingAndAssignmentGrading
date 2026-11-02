using System.ComponentModel.DataAnnotations;

namespace ExamService.DTOs
{
    public class UpdateAnswerDTO
    {
        [Required(ErrorMessage = "Ответ обязателен")]
        public string AnswerData { get; set; } = string.Empty;
    }
}