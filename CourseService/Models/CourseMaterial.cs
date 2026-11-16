namespace CourseService.Models
{
    // Учебный материал курса: краткая теория, которую студент читает до теста.
    public class CourseMaterial
    {
        public int CourseMaterialId { get; set; }
        public int CourseId { get; set; }
        public Course? Course { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
