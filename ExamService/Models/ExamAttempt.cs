using ExamService.Models;

namespace ExamService.Models
{
    public class ExamAttempt
    {
        public int AttemptId { get; set; }
        public int StudentId { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset DeadlineAt { get; set; }
        public string Status { get; set; } = "InProgress";
        public int AssignmentId { get; set; }
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();

    }
}
