using CourseService.Data;
using CourseService.DTOs;
using CourseService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CourseService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/assignments/{assignment_id:int}/questions")]
    public class QuestionController : ControllerBase
    {
        private readonly CourseDbContext _context;

        public QuestionController(CourseDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetQuestions(int assignment_id)
        {
            var assignment_exists = await _context.Assignments
                .AnyAsync(assignment => assignment.AssignmentId == assignment_id);

            if (!assignment_exists)
            {
                return NotFound("Задание не найдено");
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
                })
                .ToListAsync();

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
                TestCases = dto.TestCases?.Clone()
            };

            _context.Questions.Add(question);

            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {   
                question_id = question.QuestionId,
                message = "Вопрос успешно добавлен"
            });
        }
    }
}