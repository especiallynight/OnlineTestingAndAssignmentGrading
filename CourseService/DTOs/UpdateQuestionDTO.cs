using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CourseService.DTOs
{
    public class UpdateQuestionDTO
    {
        [Required(ErrorMessage = "Текст вопроса обязателен")]
        [MinLength(3, ErrorMessage = "Текст вопроса должен содержать минимум 3 символа")]
        [MaxLength(100, ErrorMessage = "Текст вопроса не должен превышать 100 символов")]
        public string QuestionText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Тип вопроса обязателен")]
        [RegularExpression("^(SingleChoice|MultipleChoice|Code)$", ErrorMessage = "Недопустимый тип вопроса")]
        public string QuestionType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Параметры вопроса обязательны")]
        public JsonElement? Options { get; set; }
    }
}
