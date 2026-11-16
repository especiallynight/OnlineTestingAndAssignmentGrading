using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IdentityService.Enums;

namespace IdentityService.DTOs
{
    public class CreateUserDTO
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [StringLength(100, ErrorMessage = "Email не более 100 символов")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        [MaxLength(100, ErrorMessage = "Пароль не более 100 символов")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Имя от 2 до 40 символов")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилия от 2 до 50 символов")]
        public string LastName { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole? Role { get; set; }
    }
}
