using ExamService.Enums;
using System.ComponentModel.DataAnnotations;

namespace ExamService.Models
{
    public class Answer
    {
        public int AnswerId { get; set; }
        public string AnswerData { get; set; } = string.Empty;
        public DateTimeOffset? SubmittedAt { get; set; }
        public AnswerStatusEnum Status { get; set; }
        public int QuestionId { get; set; }
        public int AttemptId { get; set; }
        public ExamAttempt Attempt { get; set; } = null!;
        public string? ExecutionStatus { get; set; }
        public int? PassedTests { get; set; }
        public int? TotalTests { get; set; }
        public decimal? Score { get; set; }
        public string? ExecutionError { get; set; }
    }
}