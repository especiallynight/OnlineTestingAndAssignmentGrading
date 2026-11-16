
using CodeExecutionService.DTOs;
using CodeExecutionService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeExecutionService.Controllers;

[ApiController]
[Route("api/code-execution")]
public class CodeExecutionController : ControllerBase
{
    private readonly ICodeRunner _codeRunner;
    private readonly ILogger<CodeExecutionController> _logger;

    public CodeExecutionController(
        ICodeRunner codeRunner,
        ILogger<CodeExecutionController> logger)
    {
        _codeRunner = codeRunner;
        _logger = logger;
    }

    [HttpPost("run")]
    [ProducesResponseType(typeof(CodeExecutionResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CodeExecutionResultDTO>> RunCode(
        [FromBody] CodeSubmissionDTO submission)
    {
        if (submission.TestCases == null ||
            submission.TestCases.Count == 0 ||
            submission.TestCases.Count > 50)
        {
            return BadRequest("Количество тестов должно быть от 1 до 50.");
        }

        if (string.IsNullOrWhiteSpace(submission.SourceCode))
        {
            return BadRequest("Исходный код не может быть пустым.");
        }

        if (submission.Language != "csharp")
        {
            return BadRequest("Поддерживается только C#.");
        }

        try
        {
            var result = await _codeRunner.ExecuteAsync(submission);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении кода студента.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "Не удалось выполнить проверку кода.");
        }
    }
}
