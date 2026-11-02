namespace AnalyticsService.DTOs
{
    public class StudentStatisticsResponseDTO
    {
        public int CourseId { get; set; }
        public int CompletedAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
    }
}
