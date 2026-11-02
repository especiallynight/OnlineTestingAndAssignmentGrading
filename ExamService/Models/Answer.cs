using ExamService.Models;

namespace ExamService.Models
{
    public class Answer
    {
        public int AnswerId { get; set; }
        public string AnswerData { get; set; } = string.Empty;
        public DateTimeOffset SubmittedAt { get; set; }
        public int AttemptId { get; set; }
        public string Status { get; set; } = "Saved";
        public int QuestionId { get; set; }
        public ExamAttempt Attempt { get; set; } = null!;

    }
}
