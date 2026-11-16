namespace AnalyticsService.DTOs
{
    public class UpdateStudentStatisticsDTO
    {
        public int AttemptId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
    }
}