using CourseService.Controllers;
using CourseService.Data;
using CourseService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace OnlineTesting.Tests
{
    public class QuestionControllerTests
    {
        private static CourseDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<CourseDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new CourseDbContext(options);
        }

        private static QuestionController GetController(CourseDbContext db)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "3"),
                new Claim(ClaimTypes.Role, "Student")
            }, "TestAuth"));

            var cache = new Mock<IDistributedCache>();
            cache
                .Setup(item => item.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            return new QuestionController(db, cache.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
        }

        private static async Task<Question> SeedCodeQuestionAsync(CourseDbContext db)
        {
            var course = new Course
            {
                Name = "Основы программирования",
                TeacherId = 2,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var assignment = new Assignment
            {
                Course = course,
                Name = "Вводный тест",
                MaxScore = 10,
                TimeLimitMinutes = 30
            };
            var question = new Question
            {
                Assignment = assignment,
                QuestionText = "Сложите два числа",
                QuestionType = "Code",
                QuestionOptions = JsonDocument.Parse("{}").RootElement.Clone(),
                TestCases = JsonDocument.Parse("""
                    [
                      {"input":"1 2","expectedOutput":"3"},
                      {"input":"-5 8","expectedOutput":"3"},
                      {"input":"0 0","expectedOutput":"0"},
                      {"input":"10 -10","expectedOutput":"0"},
                      {"input":"1000000 2000000","expectedOutput":"3000000"}
                    ]
                    """).RootElement.Clone()
            };

            db.Questions.Add(question);
            await db.SaveChangesAsync();
            return question;
        }

        // Студент получает примеры входных и выходных данных для задачи на код.
        // Ожидает: три примера, первый — «1 2» → «3».
        [Fact]
        public async Task GetQuestions_ReturnsExamplesForCodeQuestion()
        {
            var context = GetDbContext();
            var question = await SeedCodeQuestionAsync(context);
            var controller = GetController(context);

            var result = await controller.GetQuestions(question.AssignmentId);

            var ok = Assert.IsType<OkObjectResult>(result);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            var first = document.RootElement[0];
            var examples = first.GetProperty("examples");

            Assert.Equal(3, examples.GetArrayLength());
            Assert.Equal("1 2", examples[0].GetProperty("input").GetString());
            Assert.Equal("3", examples[0].GetProperty("expectedOutput").GetString());
        }

        // Полный набор тестов не должен попадать в ответ для студента:
        // скрытые тесты остаются только у преподавателя и автопроверки.
        [Fact]
        public async Task GetQuestions_DoesNotExposeHiddenTestCases()
        {
            var context = GetDbContext();
            var question = await SeedCodeQuestionAsync(context);
            var controller = GetController(context);

            var result = await controller.GetQuestions(question.AssignmentId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);

            Assert.DoesNotContain("1000000 2000000", json);
            Assert.DoesNotContain("test_cases", json);
        }
    }
}
