using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CourseService.DTOs
{
    public class UpdateQuestionDTO
    {
        [Required(ErrorMessage = "Текст вопроса обязателен")]
        [MinLength(3, ErrorMessage = "Текст вопроса должен содержать минимум 3 символа")]
        public string Text { get; set; } = string.Empty;

        [Required(ErrorMessage = "Тип вопроса обязателен")]
        public string Type { get; set; } = string.Empty;

        public JsonDocument Options { get; set; } =
            JsonDocument.Parse("{}");
    }
}
