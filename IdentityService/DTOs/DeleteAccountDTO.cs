using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs
{
    public class DeleteAccountDTO
    {
        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = string.Empty;
    }
}
