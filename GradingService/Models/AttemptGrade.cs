using System.ComponentModel.DataAnnotations;

namespace GradingService.Models
{
    public class AttemptGrade
    {
        public int AttemptGradeId { get; set; }
        public int AttemptId { get; set; }
        [Range(0, double.MaxValue)]
        public decimal Score { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MaxScore { get; set; }
        public DateTimeOffset CheckedAt { get; set; }
        public string? Feedback { get; set; } 
    }
}