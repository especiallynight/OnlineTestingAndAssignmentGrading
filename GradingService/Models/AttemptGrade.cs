using System.ComponentModel.DataAnnotations;

namespace GradingService.Models
{
    public class AttemptGrade
    {
        public int AttemptGradeId { get; set; }
        public int AttemptId { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public DateTimeOffset CheckedAt { get; set; }
        public string? Feedback { get; set; } 
    }
}