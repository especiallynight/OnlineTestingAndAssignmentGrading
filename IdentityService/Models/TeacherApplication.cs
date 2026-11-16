using IdentityService.Enums;
using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public class TeacherApplication
    {
        public int TeacherApplicationId { get; set; }
        public int UserId { get; set; }
        public TeacherApplicationStatus Status { get; set; } = TeacherApplicationStatus.Pending;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public User? Reviewer { get; set; }
        public User User { get; set; } = null!;
    }
}
