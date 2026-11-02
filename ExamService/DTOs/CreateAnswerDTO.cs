using System.ComponentModel.DataAnnotations;

namespace ExamService.DTOs
{
    public class CreateAnswerDTO
    {
        [Required(ErrorMessage = "Ответ обязателен")]
        public string AnswerData { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Некорректный ID вопроса")]
        public int QuestionId { get; set; }
    }
}
