using IdentityService.Enums;
using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs
{
    public class UserResponseDTO
    {

        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }
}

