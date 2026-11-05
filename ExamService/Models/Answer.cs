using ExamService.Enums;
using System.ComponentModel.DataAnnotations;

namespace ExamService.Models
{
    public class Answer
    {
        public int AnswerId { get; set; }
        public string AnswerData { get; set; } = string.Empty;
        public DateTimeOffset? SubmittedAt { get; set; }
        [MaxLength(30)]
        public AnswerStatusEnum Status { get; set; }
        public int QuestionId { get; set; }
        public int AttemptId { get; set; }
        public ExamAttempt Attempt { get; set; } = null!;

    }
}
