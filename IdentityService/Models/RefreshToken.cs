using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public class RefreshToken
    {
        public int RefreshTokenId { get; set; }
        public int UserId { get; set; }
        [MaxLength(256, ErrorMessage = "Хэш токена не должен превышать 256 символов")] 
        public string TokenHash { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public User User { get; set; } = null!;
    }
}