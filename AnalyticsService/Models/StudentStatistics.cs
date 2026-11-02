namespace AnalyticsService.Models
{
    public class StudentStatistics
    {
        public int StudentStatisticId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int CompletedAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}