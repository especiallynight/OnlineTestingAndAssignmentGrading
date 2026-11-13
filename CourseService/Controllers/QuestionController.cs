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

        // Полные данные вопросов (с правильными ответами) для редактора преподавателя.
        [Authorize(Roles = "Teacher")]
        [HttpGet("manage")]
        public async Task<ActionResult> GetQuestionsForEditing(int assignment_id)
        {
            var teacher_id = GetUserId();
            if (teacher_id == null)
            {
                return Unauthorized();
            }

            var assignment = await _context.Assignments
                .Include(item => item.Course)
                .FirstOrDefaultAsync(item => item.AssignmentId == assignment_id);

            if (assignment == null)
            {
                return NotFound("Задание не найдено");
            }

            if (assignment.Course == null)
            {
                return NotFound("Курс задания не найден");
            }

            if (assignment.Course.TeacherId != teacher_id.Value)
            {
                return Forbid();
            }

            var questions = await _context.Questions
                .Where(question => question.AssignmentId == assignment_id)
                .Select(question => new
                {
                    question_id = question.QuestionId,
                    assignment_id = question.AssignmentId,
                    text = question.QuestionText,
                    type = question.QuestionType,
                    options = question.QuestionOptions,
                    correct_answer = question.CorrectAnswer,
                    test_cases = question.TestCases
                })
                .ToListAsync();

            return Ok(questions);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public async Task<ActionResult> CreateQuestion(int assignment_id, CreateQuestionDTO dto)
        {
            var teacher_id = GetUserId();
            if (teacher_id == null)
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

            if (assignment.Course.TeacherId != teacher_id.Value)
            {
                return Forbid();
            }

            var error = ValidateQuestion(
                dto.QuestionType,
                dto.Options,
                dto.CorrectAnswer,
                dto.TestCases);

            if (error != null)
            {
                return BadRequest(error);
            }

            var question = new Question
            {
                AssignmentId = assignment_id,
                QuestionText = dto.QuestionText,
                QuestionType = dto.QuestionType,
                QuestionOptions = dto.Options!.Value.Clone(),
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

        [Authorize(Roles = "Teacher")]
        [HttpPut("~/api/questions/{question_id:int}")]
        public async Task<ActionResult> UpdateQuestion(int question_id, UpdateQuestionDTO dto)
        {
            var teacher_id = GetUserId();
            if (teacher_id == null)
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Include(item => item.Assignment)
                .ThenInclude(assignment => assignment.Course)
                .FirstOrDefaultAsync(item => item.QuestionId == question_id);

            if (question == null)
            {
                return NotFound("Вопрос не найден");
            }

            if (question.Assignment?.Course == null)
            {
                return NotFound("Курс задания не найден");
            }

            if (question.Assignment.Course.TeacherId != teacher_id.Value)
            {
                return Forbid();
            }

            var error = ValidateQuestion(
                dto.QuestionType,
                dto.Options,
                dto.CorrectAnswer,
                dto.TestCases);

            if (error != null)
            {
                return BadRequest(error);
            }

            question.QuestionText = dto.QuestionText;
            question.QuestionType = dto.QuestionType;
            question.QuestionOptions = dto.Options!.Value.Clone();
            question.CorrectAnswer = dto.CorrectAnswer?.Clone();
            question.TestCases = dto.TestCases?.Clone();

            await _context.SaveChangesAsync();
            await _cache.RemoveAsync($"assignments:{question.AssignmentId}:questions");

            return Ok(new
            {
                question_id = question.QuestionId,
                message = "Вопрос обновлён"
            });
        }

        [Authorize(Roles = "Teacher")]
        [HttpDelete("~/api/questions/{question_id:int}")]
        public async Task<ActionResult> DeleteQuestion(int question_id)
        {
            var teacher_id = GetUserId();
            if (teacher_id == null)
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Include(item => item.Assignment)
                .ThenInclude(assignment => assignment.Course)
                .FirstOrDefaultAsync(item => item.QuestionId == question_id);

            if (question == null)
            {
                return NotFound("Вопрос не найден");
            }

            if (question.Assignment?.Course == null)
            {
                return NotFound("Курс задания не найден");
            }

            if (question.Assignment.Course.TeacherId != teacher_id.Value)
            {
                return Forbid();
            }

            var assignment_id = question.AssignmentId;

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync($"assignments:{assignment_id}:questions");

            return NoContent();
        }

        private int? GetUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        private static string? ValidateQuestion(
            string question_type,
            JsonElement? options,
            JsonElement? correct_answer,
            JsonElement? test_cases)
        {
            if (options == null)
            {
                return "Необходимо указать параметры вопроса";
            }

            if (question_type == "SingleChoice" ||
                question_type == "MultipleChoice")
            {
                if (options.Value.ValueKind != JsonValueKind.Object)
                {
                    return "Варианты ответа должны быть объектом.";
                }

                if (options.Value.EnumerateObject()
                    .Any(option => option.Value.ValueKind != JsonValueKind.String))
                {
                    return "Текст вариантов ответа должен быть строкой.";
                }

                if (correct_answer == null)
                {
                    return "Необходимо указать правильный ответ.";
                }

                if (question_type == "SingleChoice")
                {
                    if (correct_answer.Value.ValueKind != JsonValueKind.String)
                    {
                        return "Для SingleChoice правильный ответ должен быть строкой.";
                    }

                    var answer = correct_answer.Value.GetString();

                    if (string.IsNullOrWhiteSpace(answer) ||
                        !options.Value.TryGetProperty(answer, out _))
                    {
                        return "Правильный ответ отсутствует среди вариантов.";
                    }
                }

                if (question_type == "MultipleChoice")
                {
                    if (correct_answer.Value.ValueKind != JsonValueKind.Array)
                    {
                        return "Для MultipleChoice правильный ответ должен быть массивом.";
                    }

                    var answers = correct_answer.Value.EnumerateArray().ToList();

                    if (answers.Count == 0 ||
                        answers.Any(answer => answer.ValueKind != JsonValueKind.String))
                    {
                        return "Укажите хотя бы один правильный вариант.";
                    }

                    foreach (var answer in answers)
                    {
                        var key = answer.GetString();

                        if (string.IsNullOrWhiteSpace(key) ||
                            !options.Value.TryGetProperty(key, out _))
                        {
                            return "Один из правильных ответов отсутствует среди вариантов.";
                        }
                    }

                    if (answers.Select(answer => answer.GetString()).Distinct().Count() != answers.Count)
                    {
                        return "Правильные ответы не должны повторяться.";
                    }
                }
            }
            else if (question_type == "Code")
            {
                if (correct_answer != null)
                {
                    return "Для вопросов типа Code правильный ответ не указывается.";
                }
            }

            if (question_type == "Code")
            {
                if (test_cases == null ||
                    test_cases.Value.ValueKind != JsonValueKind.Array ||
                    test_cases.Value.GetArrayLength() == 0)
                {
                    return "Для вопроса с кодом необходимо указать тесты.";
                }
            }
            else if (test_cases != null)
            {
                return "Тесты можно добавлять только к вопросам типа Code.";
            }

            return null;
        }
    }
}
