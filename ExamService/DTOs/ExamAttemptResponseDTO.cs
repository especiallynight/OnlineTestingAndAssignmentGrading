using ExamService.Enums;

namespace ExamService.DTOs
{
    public class ExamAttemptResponseDTO
    {
        public int AttemptId { get; set; }
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? DeadlineAt { get; set; }
        public ExamAttemptStatus Status { get; set; }
    }
}
