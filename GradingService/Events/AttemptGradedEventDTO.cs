namespace Shared.Events;

public record AttemptGradedEvent
{
    public int AttemptId { get; init; }
    public int StudentId { get; init; }
    public int CourseId { get; init; }
    public decimal Score { get; init; }
    public decimal MaxScore { get; init; }
    public DateTimeOffset GradedAt { get; init; }
}