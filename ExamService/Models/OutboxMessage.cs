using System.Text.Json;

namespace ExamService.Models
{
    public class OutboxMessage
    {
        public int OutboxMessageId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public JsonElement Payload { get; set; } 
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; 
        public DateTimeOffset? PublishedAt { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }

    }
}
