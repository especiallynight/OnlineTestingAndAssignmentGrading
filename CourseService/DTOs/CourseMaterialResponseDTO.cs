namespace CourseService.DTOs
{
    public class CourseMaterialResponseDTO
    {
        public int CourseMaterialId { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
