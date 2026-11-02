namespace GradingService.Models
{
    public class AttemptGrade
    {
        public int AttemptGradeId { get; set; }
        public int AttemptId { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public DateTimeOffset CheckedAt { get; set; }
        public string? Feedback { get; set; } 
    }
}