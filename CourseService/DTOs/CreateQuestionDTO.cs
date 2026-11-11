using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CourseService.DTOs
{
    public class CreateQuestionDTO
    {
        [Required(ErrorMessage = "Текст вопроса обязателен")]
        [MaxLength(100, ErrorMessage = "Название курса не должно превышать 100 символов")]
        [MinLength(3, ErrorMessage = "Текст вопроса должен содержать минимум 3 символа")]
        public string QuestionText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Тип вопроса обязателен")]
        [RegularExpression("^(SingleChoice|MultipleChoice|Code)$")]
        public string QuestionType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Параметры вопроса обязательны")]
        public JsonElement? Options { get; set; }
        public JsonElement? TestCases { get; set; }
    }
}