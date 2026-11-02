using System.ComponentModel.DataAnnotations;

namespace GradingService.DTOs
{
    public class AttemptGradedEventDTO
    {
        public int AttemptId { get; set; }
        public decimal TotalScore { get; set; }
        public decimal MaxScore { get; set; }
        public DateTimeOffset CheckedAt { get; set; }
    }
}
