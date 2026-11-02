namespace CourseService.Models
{
    public class Assignment
    {
        public int AssignmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxScore { get; set; }
        public int CourseId { get; set; }
        public int TeacherId { get; set; }
        public int TimeLimitMinutes { get; set; }
        public Course Course { get; set; } = null!;
        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
