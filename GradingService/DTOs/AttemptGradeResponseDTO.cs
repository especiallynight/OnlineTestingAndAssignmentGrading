using System.ComponentModel.DataAnnotations;

namespace GradingService.DTOs
{
    public class AttemptGradeResponseDTO
    {
        public int AttemptGradeId { get; set; }
        public int AttemptId { get; set; }
        public decimal Score { get; set; }

        public decimal MaxScore { get; set; }
        public DateTimeOffset CheckedAt { get; set; }
        public string? Feedback { get; set; } 
    }
}
