namespace AnalyticsService.DTOs
{
    public class CourseStatisticsResponseDTO
    {
        public int CourseId { get; set; }
        public int CompletedAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}