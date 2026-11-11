
using CodeExecutionService.DTOs;

namespace CodeExecutionService.Services;

public interface ICodeRunner
{
    Task<CodeExecutionResultDTO> ExecuteAsync(CodeSubmissionDTO submission);
}
