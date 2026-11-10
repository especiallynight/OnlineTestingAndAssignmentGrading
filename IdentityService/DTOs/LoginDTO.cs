using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs
{
    public class LoginDTO
    {

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = string.Empty;

    }
}
