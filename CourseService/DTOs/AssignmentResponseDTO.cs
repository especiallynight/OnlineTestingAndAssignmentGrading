namespace CourseService.DTOs
{
    public class AssignmentResponseDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxScore { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int AssignmentId { get; set; }
        public int CourseId { get; set; }
    }
}
