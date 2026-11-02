namespace IdentityService.Models
{
    public class TeacherApplication
    {
        public int TeacherApplicationId { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public int? ReviewedBy { get; set; }
    }
}
