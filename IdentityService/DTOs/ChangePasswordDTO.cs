using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs
{
    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "Текущий пароль обязателен")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        [MaxLength(100, ErrorMessage = "Пароль не более 100 символов")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
