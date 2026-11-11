
namespace CodeExecutionService.DTOs;

public class CodeExecutionResultDTO
{
    public string Status { get; set; } = string.Empty;

    public int PassedTests { get; set; }

    public int TotalTests { get; set; }

    public decimal Score { get; set; }

    public string? ErrorMessage { get; set; }
}
