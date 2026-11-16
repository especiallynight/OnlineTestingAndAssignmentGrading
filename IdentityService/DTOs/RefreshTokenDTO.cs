using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs
{
    public class RefreshTokenDTO
    {
        [Required(ErrorMessage = "Refresh token обязателен")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
