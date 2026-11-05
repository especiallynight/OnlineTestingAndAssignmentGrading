using IdentityService.Enums;
using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public class User
    {
        public int UserId { get; set; }
        [MaxLength(40)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Student;
        public ICollection<TeacherApplication> TeacherApplications { get; set; } = new List<TeacherApplication>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List <RefreshToken>();
        public ICollection<TeacherApplication> ReviewedApplications { get; set; } = new List<TeacherApplication>();
    }
}
