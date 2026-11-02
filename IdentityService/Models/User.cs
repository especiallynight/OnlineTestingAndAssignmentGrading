using IdentityService.Enums;

namespace IdentityService.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Student;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
