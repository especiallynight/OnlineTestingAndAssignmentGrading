using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CourseService.Models
{
    public class Question
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public JsonElement? QuestionOptions { get; set; }
        public JsonElement? CorrectAnswer { get; set; }
        public JsonElement? TestCases { get; set; }
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

    }
}

