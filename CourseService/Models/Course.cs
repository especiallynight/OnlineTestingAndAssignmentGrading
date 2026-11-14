using System.ComponentModel;

namespace CourseService.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public string? Description { get; set; }
        public int TeacherId { get; set; }
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<CourseMaterial> Materials { get; set; } = new List<CourseMaterial>();
    }
}
