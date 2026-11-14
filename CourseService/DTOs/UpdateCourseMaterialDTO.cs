using System.ComponentModel.DataAnnotations;

namespace CourseService.DTOs
{
    public class UpdateCourseMaterialDTO
    {
        [Required(ErrorMessage = "Заголовок материала обязателен")]
        [MinLength(3, ErrorMessage = "Заголовок должен содержать минимум 3 символа")]
        [MaxLength(150, ErrorMessage = "Заголовок не должен превышать 150 символов")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Текст материала обязателен")]
        [MinLength(10, ErrorMessage = "Текст материала должен содержать минимум 10 символов")]
        [MaxLength(20000, ErrorMessage = "Текст материала не должен превышать 20000 символов")]
        public string Content { get; set; } = string.Empty;

        [Range(0, 1000, ErrorMessage = "Порядковый номер должен быть от 0 до 1000")]
        public int SortOrder { get; set; }
    }
}
