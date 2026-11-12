
using CourseService.Data;
using CourseService.DTOs;
using CourseService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace CourseService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/assignments/{assignment_id:int}/questions")]
    public class QuestionController : ControllerBase
    {
        private readonly CourseDbContext _context;
        private readonly IDistributedCache _cache;

        public QuestionController(CourseDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }
        private sealed record QuestionCacheItem(
            int question_id,
            int assignment_id,
            string text,
            string type,
            JsonElement? options);

        private static readonly DistributedCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        [HttpGet]
        public async Task<ActionResult> GetQuestions(int assignment_id)
        {
            var assignment_exists = await _context.Assignments
                .AnyAsync(assignment => assignment.AssignmentId == assignment_id);
            if (!assignment_exists)
            {
                return NotFound("Задание не найдено");
            }

            var key = $"assignments:{assignment_id}:questions";
            var cached = await _cache.GetStringAsync(key);
            if (cached != null)
                return Ok(JsonSerializer.Deserialize<List<QuestionCacheItem>>(cached));

            var questions = await _context.Questions
                .Where(question => question.AssignmentId == assignment_id)
                .Select(question => new
                {
                    question_id = question.QuestionId,
                    assignment_id = question.AssignmentId,
                    text = question.QuestionText,
                    type = question.QuestionType,
                    options = question.QuestionOptions
                })
                .ToListAsync();

            await _cache.SetStringAsync(key, JsonSerializer.Serialize(questions), CacheOptions);
            return Ok(questions);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public async Task<ActionResult> CreateQuestion(int assignment_id, CreateQuestionDTO dto)
        {
            var user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(user_id, out var teacher_id))
            {
                return Unauthorized();
            }

            var assignment = await _context.Assignments
                .Include(assignment => assignment.Course)
                .FirstOrDefaultAsync(assignment => assignment.AssignmentId == assignment_id);

            if (assignment == null)
            {
                return NotFound("Задание не найдено");
            }

            if (assignment.Course == null)
            {
                return NotFound("Курс задания не найден");
            }

            if (assignment.Course.TeacherId != teacher_id)
            {
                return Forbid();
            }

            if (dto.Options == null)
            {
                return BadRequest("Необходимо указать параметры вопроса");
            }

            if (dto.QuestionType == "SingleChoice" ||
                dto.QuestionType == "MultipleChoice")
            {
                if (dto.Options.Value.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest("Варианты ответа должны быть объектом.");
                }

                if (dto.Options.Value.EnumerateObject()
                    .Any(option => option.Value.ValueKind != JsonValueKind.String))
                {
                    return BadRequest("Текст вариантов ответа должен быть строкой.");
                }

                if (dto.CorrectAnswer == null)
                {
                    return BadRequest("Необходимо указать правильный ответ.");
                }

                if (dto.QuestionType == "SingleChoice")
                {
                    if (dto.CorrectAnswer.Value.ValueKind != JsonValueKind.String)
                    {
                        return BadRequest("Для SingleChoice правильный ответ должен быть строкой.");
                    }

                    var answer = dto.CorrectAnswer.Value.GetString();

                    if (string.IsNullOrWhiteSpace(answer) ||
                        !dto.Options.Value.TryGetProperty(answer, out _))
                    {
                        return BadRequest("Правильный ответ отсутствует среди вариантов.");
                    }
                }

                if (dto.QuestionType == "MultipleChoice")
                {
                    if (dto.CorrectAnswer.Value.ValueKind != JsonValueKind.Array)
                    {
                        return BadRequest("Для MultipleChoice правильный ответ должен быть массивом.");
                    }

                    var answers = dto.CorrectAnswer.Value.EnumerateArray().ToList();

                    if (answers.Count == 0 ||
                        answers.Any(answer => answer.ValueKind != JsonValueKind.String))
                    {
                        return BadRequest("Укажите хотя бы один правильный вариант.");
                    }

                    foreach (var answer in answers)
                    {
                        var key = answer.GetString();

                        if (string.IsNullOrWhiteSpace(key) ||
                            !dto.Options.Value.TryGetProperty(key, out _))
                        {
                            return BadRequest("Один из правильных ответов отсутствует среди вариантов.");
                        }
                    }

                    if (answers.Select(answer => answer.GetString()).Distinct().Count() != answers.Count)
                    {
                        return BadRequest("Правильные ответы не должны повторяться.");
                    }
                }
            }
            else if (dto.QuestionType == "Code")
            {
                if (dto.CorrectAnswer != null)
                {
                    return BadRequest("Для вопросов типа Code правильный ответ не указывается.");
                }
            }

            if (dto.QuestionType == "Code")
            {
                if (dto.TestCases == null ||
                    dto.TestCases.Value.ValueKind != JsonValueKind.Array ||
                    dto.TestCases.Value.GetArrayLength() == 0)
                {
                    return BadRequest("Для вопроса с кодом необходимо указать тесты.");
                }
            }
            else if (dto.TestCases != null)
            {
                return BadRequest("Тесты можно добавлять только к вопросам типа Code.");
            }

            var question = new Question
            {
                AssignmentId = assignment_id,
                QuestionText = dto.QuestionText,
                QuestionType = dto.QuestionType,
                QuestionOptions = dto.Options.Value.Clone(),
                CorrectAnswer = dto.CorrectAnswer?.Clone(),
                TestCases = dto.TestCases?.Clone()
            };

            _context.Questions.Add(question);

            await _context.SaveChangesAsync();
            await _cache.RemoveAsync($"assignments:{assignment_id}:questions");
            return StatusCode(201, new
            {
                question_id = question.QuestionId,
                message = "Вопрос успешно добавлен"
            });
        }
    }
}
