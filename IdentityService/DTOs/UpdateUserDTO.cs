using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs
{
    public class UpdateUserDTO
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [StringLength(100, ErrorMessage = "Email не более 100 символов")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Имя от 2 до 40 символов")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилия от 2 до 50 символов")]
        public string LastName { get; set; } = string.Empty;
    }
}
