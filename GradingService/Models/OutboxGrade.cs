using System.Text.Json;

namespace GradingService.Models
{
    public class OutboxGrade
    {
        public int OutboxMessageId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
