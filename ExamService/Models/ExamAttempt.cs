using ExamService.Enums;

namespace ExamService.Models
{
    public class ExamAttempt
    {
        public int AttemptId { get; set; }
        public int StudentId { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset DeadlineAt { get; set; }
        public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.InProgress;
        public int AssignmentId { get; set; }
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();

    }
}
