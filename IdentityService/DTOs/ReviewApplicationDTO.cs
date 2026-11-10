using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs
{
    public class ReviewApplicationDTO
    {
        [Required]
        [RegularExpression("^(Approved|Rejected)$", ErrorMessage = "Статус должен быть Approved или Rejected")]
        public string Status { get; set; } = string.Empty;
    }
}
