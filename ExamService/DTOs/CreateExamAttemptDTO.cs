
using System.ComponentModel.DataAnnotations;

namespace ExamService.DTOs
{
    public class CreateExamAttemptDTO
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int AssignmentId { get; set; }
    }
}
