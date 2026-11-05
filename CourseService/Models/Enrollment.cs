
namespace CourseService.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }

        public int StudentId { get; set; }

        public int CourseId { get; set; }

        public DateTimeOffset EnrolledAt { get; set; } = DateTimeOffset.UtcNow;

        public Course Course { get; set; } = null!;
    }
}