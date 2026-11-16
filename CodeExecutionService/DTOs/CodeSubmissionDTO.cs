
namespace CodeExecutionService.DTOs;

public class CodeSubmissionDTO
{
    public string Language { get; set; } = "csharp";

    public string SourceCode { get; set; } = string.Empty;

    public List<TestCaseDTO> TestCases { get; set; } = new();
}

public class TestCaseDTO
{
    public string Input { get; set; } = string.Empty;

    public string ExpectedOutput { get; set; } = string.Empty;
}
