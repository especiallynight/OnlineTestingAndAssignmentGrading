
using CourseService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CourseService.Controllers
{
    [ApiController]
    [Route("internal/questions")]
    public class InternalQuestionController : ControllerBase
    {
        private readonly CourseDbContext _context;
        private readonly IConfiguration _configuration;

        public InternalQuestionController(
            CourseDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("{questionId:int}/test-cases")]
        public async Task<IActionResult> GetTestCases(int questionId)
        {
            var configuredKey = _configuration["InternalApiKey"];
            var providedKey = Request.Headers["X-Internal-Api-Key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(configuredKey) ||
                string.IsNullOrWhiteSpace(providedKey))
            {
                return Unauthorized();
            }

            var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
            var providedBytes = Encoding.UTF8.GetBytes(providedKey);

            if (configuredBytes.Length != providedBytes.Length || !CryptographicOperations.FixedTimeEquals(
                configuredBytes, providedBytes))
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Where(q => q.QuestionId == questionId)
                .Select(q => new
                {
                    q.QuestionId,
                    q.QuestionType,
                    q.TestCases
                })
                .FirstOrDefaultAsync();

            if (question == null)
                return NotFound("Вопрос не найден");

            if (question.QuestionType != "Code")
                return BadRequest("Вопрос не является вопросом на программирование");

            if (question.TestCases == null)
                return NotFound("Тесты для вопроса не найдены");

            return Ok(new
            {
                question_id = question.QuestionId,
                test_cases = question.TestCases
            });
        }
    }
}
