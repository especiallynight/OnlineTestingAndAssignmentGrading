using System.Text.Json;

namespace CourseService.Models
{
    public class Question
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public JsonDocument Options { get; set; } = JsonDocument.Parse("{}");
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

    }
}
