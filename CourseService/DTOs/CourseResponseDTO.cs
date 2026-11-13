namespace CourseService.DTOs
{
    public class CourseResponseDTO
    {
        public int CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public string? Description { get; set; }
        public int TeacherId { get; set; }
        public int StudentsCount { get; set; }
        public int AssignmentsCount { get; set; }
    }
}
