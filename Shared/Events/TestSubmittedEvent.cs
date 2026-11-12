namespace Shared.Events;

public record SubmittedAnswer
{
    public int QuestionId { get; init; }
    public string AnswerData { get; init; } = string.Empty;
    public decimal? CodeScore { get; init; }
}

public record TestSubmittedEvent
{
    public int AttemptId { get; init; }
    public int StudentId { get; init; }
    public int AssignmentId { get; init; }
    public List<SubmittedAnswer> Answers { get; init; } = new();
}
