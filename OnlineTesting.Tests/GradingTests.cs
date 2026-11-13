using System.Net;
using System.Security.Claims;
using GradingService.Controllers;
using GradingService.Data;
using GradingService.Models;
using Shared.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;

namespace GradingService.Tests
{
    public class GradesControllerTests
    {
        private GradesDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<GradesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new GradesDbContext(options);
        }

        private GradeController GetController(GradesDbContext db, string? attemptStatus = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Teacher")
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));

            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    var path = request.RequestUri?.AbsolutePath ?? "";

                    if (path.Contains("exam_attempts"))
                    {
                        var status = attemptStatus == null
                            ? string.Empty
                            : ",\"status\":\"" + attemptStatus + "\"";

                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"studentId\":1,\"assignmentId\":1" + status + "}")
                        };
                    }

                    if (path.Contains("assignments"))
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"courseId\":1}")
                        };
                    }

                    if (path.Contains("courses"))
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"teacherId\":1}")
                        };
                    }

                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                });

            var factoryMock = new Mock<IHttpClientFactory>();

            factoryMock
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns((string name) =>
                    new HttpClient(handlerMock.Object)
                    {
                        BaseAddress = new Uri("http://localhost/")
                    });

            var controller = new GradeController(
                db,
                factoryMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = user
                    }
                }
            };

            controller.HttpContext.Request.Headers.Authorization =
                "Bearer test-token";

            return controller;
        }

        //Создать оценку с негативным значением.
        //Ожидаем ошибку BadRequest с текстом "Некорректные баллы".
        
        [Fact]
        public async Task CreateGrade_WhenNegativeScore()
        {
            using var context = GetDbContext();
            var controller = GetController(context);

            var dto = new AttemptGradedEvent
            {
                AttemptId = 1,
                Score = -5,
                MaxScore = 100
            };

            var result = await controller.CreateGrade(dto);

            var response = Assert.IsType<BadRequestObjectResult>(
                result.Result);

            Assert.Equal("Некорректные баллы", response.Value);
        }

        //Создаем оценку с валидными данными.
        //Ожимдаем 201 Created.
        
        [Fact]
        public async Task CreateGrade_WhenValidData()
        {
            using var context = GetDbContext();
            var controller = GetController(context);

            var dto = new AttemptGradedEvent
            {
                AttemptId = 1,
                StudentId = 1,
                CourseId = 1,
                Score = 85,
                MaxScore = 100,
                GradedAt = DateTimeOffset.UtcNow
            };

            var result = await controller.CreateGrade(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Single(context.AttemptGrades);
        }

        // Нельзя выставить оценку, пока попытка не завершена.
        [Fact]
        public async Task CreateGrade_WhenAttemptInProgress_ReturnsConflict()
        {
            using var context = GetDbContext();
            var controller = GetController(context, "InProgress");

            var dto = new AttemptGradedEvent
            {
                AttemptId = 1,
                StudentId = 1,
                CourseId = 1,
                Score = 85,
                MaxScore = 100,
                GradedAt = DateTimeOffset.UtcNow
            };

            var result = await controller.CreateGrade(dto);

            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Empty(context.AttemptGrades);
        }

        //Пытаемся создать оценку для той же попытки.
        //Контроллер должен вернуть ошибку Conflict(409), так как оценка уже существует.      
        
        [Fact]
        public async Task CreateGrade_WhenGradeAlreadyExists()
        {
            using var context = GetDbContext();

            context.AttemptGrades.Add(new AttemptGrade
            {
                AttemptId = 1,
                Score = 80,
                MaxScore = 100,
                CheckedAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync();

            var controller = GetController(context);

            var dto = new AttemptGradedEvent
            {
                AttemptId = 1,
                StudentId = 1,
                CourseId = 1,
                Score = 85,
                MaxScore = 100,
                GradedAt = DateTimeOffset.UtcNow
            };

            var result = await controller.CreateGrade(dto);

            Assert.IsType<ConflictObjectResult>(result.Result);
        }
    }

}
