
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

        private bool IsInternalRequestAuthorized()
        {
            var configuredKey = _configuration["InternalApiKey"];
            var providedKey = Request.Headers["X-Internal-Api-Key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(configuredKey) ||
                string.IsNullOrWhiteSpace(providedKey))
            {
                return false;
            }

            var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
            var providedBytes = Encoding.UTF8.GetBytes(providedKey);

            return configuredBytes.Length == providedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(
                    configuredBytes,
                    providedBytes);
        }

        [HttpGet("{questionId:int}/test-cases")]
        public async Task<IActionResult> GetTestCases(int questionId)
        {
            if (!IsInternalRequestAuthorized())
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
            {
                return NotFound("Вопрос не найден");
            }

            if (question.QuestionType != "Code")
            {
                return BadRequest("Вопрос не является вопросом на программирование");
            }

            if (question.TestCases == null)
            {
                return NotFound("Тесты для вопроса не найдены");
            }

            return Ok(new
            {
                question_id = question.QuestionId,
                test_cases = question.TestCases
            });
        }

        [HttpGet("{questionId:int}/correct-answer")]
        public async Task<IActionResult> GetCorrectAnswer(int questionId)
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

            if (configuredBytes.Length != providedBytes.Length ||
                !CryptographicOperations.FixedTimeEquals(configuredBytes, providedBytes))
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Where(q => q.QuestionId == questionId)
                .Select(q => new
                {
                    q.QuestionId,
                    q.QuestionType,
                    q.CorrectAnswer
                })
                .FirstOrDefaultAsync();

            if (question == null)
                return NotFound("Вопрос не найден");

            if (question.QuestionType != "SingleChoice" &&
                question.QuestionType != "MultipleChoice")
            {
                return BadRequest("Вопрос не является вопросом с выбором ответа");
            }

            if (question.CorrectAnswer == null)
                return NotFound("Правильный ответ не найден");

            return Ok(new
            {
                question_id = question.QuestionId,
                question_type = question.QuestionType,
                correct_answer = question.CorrectAnswer
            });
        
        }

        [HttpGet("~/internal/assignments/{assignmentId:int}")]
        public async Task<IActionResult> GetAssignment(int assignmentId)
        {
            if (!IsInternalRequestAuthorized())
                return Unauthorized();

            var assignment = await _context.Assignments
                .Where(a => a.AssignmentId == assignmentId)
                .Select(a => new
                {
                    a.AssignmentId,
                    a.CourseId,
                    a.MaxScore,
                    a.TimeLimitMinutes,
                    QuestionCount = a.Questions.Count
                })
                .FirstOrDefaultAsync();

            if (assignment == null)
                return NotFound("Задание не найдено");

            return Ok(assignment);
        }
    }
}
