using ExamService.Controllers;
using ExamService.Data;
using ExamService.DTOs;
using ExamService.Enums;
using ExamService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;

namespace ExamService.Tests;

public class ExamControllerTests
{
    private ExamController GetController(ExamDbContext db, string userId = "1", int timeLimitMinutes = 60)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, "Student")}, "Test"));

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"timeLimitMinutes\":" + timeLimitMinutes + ",\"maxScore\":100,\"courseId\":1,\"questionCount\":3}")
            });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
            { "Services:CourseService", "http://localhost/" },
            { "InternalApiKey", "test-key" }
            }).Build();

        return new ExamController(db, new Mock<IPublishEndpoint>().Object, factory.Object, config)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }

    private static ExamDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ExamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ExamDbContext(options);
    }

    // Проверяет успешный запуск попытки экзамена.
    // Ожидает: OkObjectResult, Status = InProgress.
    [Fact]
    public async Task StartAttempt_Success()
    {
        using var context = CreateContext();
        context.ExamAttempts.Add(new ExamAttempt { AttemptId = 1, StudentId = 1, Status = ExamAttemptStatus.InProgress });
        await context.SaveChangesAsync();

        var controller = GetController(context, "1");

        var result = await controller.StartAttempt(1, new StartAttemptDTO { TimeLimitMinutes = 60 });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ExamAttemptResponseDTO>(okResult.Value);

        Assert.NotNull(dto.StartedAt);
        Assert.Equal(ExamAttemptStatus.InProgress, dto.Status);
    }

    // Повторный запуск незавершённой попытки продолжает её, а не создаёт новую
    // и не сбрасывает дедлайн.
    [Fact]
    public async Task StartAttempt_WhenAlreadyInProgress_ContinuesWithoutResettingDeadline()
    {
        using var context = CreateContext();
        var deadline = DateTimeOffset.UtcNow.AddMinutes(20);
        context.ExamAttempts.Add(new ExamAttempt
        {
            AttemptId = 1,
            StudentId = 1,
            Status = ExamAttemptStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            DeadlineAt = deadline
        });
        await context.SaveChangesAsync();

        var controller = GetController(context, "1");

        var result = await controller.StartAttempt(1, new StartAttemptDTO { TimeLimitMinutes = 60 });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ExamAttemptResponseDTO>(okResult.Value);
        Assert.Equal(deadline, dto.DeadlineAt);
    }

    // Завершённую попытку нельзя запустить снова.
    [Fact]
    public async Task StartAttempt_WhenCompleted_ReturnsConflict()
    {
        using var context = CreateContext();
        context.ExamAttempts.Add(new ExamAttempt
        {
            AttemptId = 1,
            StudentId = 1,
            Status = ExamAttemptStatus.Completed
        });
        await context.SaveChangesAsync();

        var controller = GetController(context, "1");

        var result = await controller.StartAttempt(1, new StartAttemptDTO { TimeLimitMinutes = 60 });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // Студент не может запустить попытку другого студента.
    [Fact]
    public async Task StartAttempt_ForbiddenForAnotherStudent()
    {
        using var context = CreateContext();

        context.ExamAttempts.Add(new ExamAttempt
        {
            AttemptId = 1,
            StudentId = 2,
            AssignmentId = 10,
            Status = ExamAttemptStatus.InProgress
        });

        await context.SaveChangesAsync();

        var controller = GetController(context, "1");

        var result = await controller.StartAttempt(
            1,
            new StartAttemptDTO { TimeLimitMinutes = 60 });

        Assert.IsType<ForbidResult>(result.Result);
    }

    // Первый запрос создаёт попытку, повторный возвращает ту же самую.
    [Fact]
    public async Task CreateAttempt_CreatesOnlyOneAttemptPerAssignment()
    {
        using var context = CreateContext();
        var controller = GetController(context, "1");

        var first = await controller.CreateAttempt(new CreateExamAttemptDTO { AssignmentId = 5 });
        var second = await controller.CreateAttempt(new CreateExamAttemptDTO { AssignmentId = 5 });

        var firstDto = Assert.IsType<ExamAttemptResponseDTO>(
            Assert.IsType<OkObjectResult>(first.Result).Value);
        var secondDto = Assert.IsType<ExamAttemptResponseDTO>(
            Assert.IsType<OkObjectResult>(second.Result).Value);

        Assert.Equal(firstDto.AttemptId, secondDto.AttemptId);
        Assert.Equal(1, await context.ExamAttempts.CountAsync(a => a.AssignmentId == 5));
    }

    // Лимит времени берётся из задания CourseService.
    [Fact]
    public async Task CreateAttempt_UsesAssignmentTimeLimit()
    {
        using var context = CreateContext();
        var controller = GetController(context, "1", timeLimitMinutes: 90);

        var result = await controller.CreateAttempt(new CreateExamAttemptDTO { AssignmentId = 7 });

        var dto = Assert.IsType<ExamAttemptResponseDTO>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        var duration = dto.DeadlineAt!.Value - dto.StartedAt!.Value;
        Assert.Equal(90, Math.Round(duration.TotalMinutes));
    }

    // Завершённый тест нельзя пройти повторно.
    [Fact]
    public async Task CreateAttempt_WhenCompleted_ReturnsConflict()
    {
        using var context = CreateContext();
        context.ExamAttempts.Add(new ExamAttempt
        {
            AttemptId = 1,
            StudentId = 1,
            AssignmentId = 5,
            Status = ExamAttemptStatus.Completed
        });
        await context.SaveChangesAsync();

        var controller = GetController(context, "1");

        var result = await controller.CreateAttempt(new CreateExamAttemptDTO { AssignmentId = 5 });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, await context.ExamAttempts.CountAsync());
    }

    // Просроченный тест тоже нельзя пройти повторно.
    [Fact]
    public async Task CreateAttempt_WhenExpired_ReturnsConflict()
    {
        using var context = CreateContext();
        context.ExamAttempts.Add(new ExamAttempt
        {
            AttemptId = 1,
            StudentId = 1,
            AssignmentId = 5,
            Status = ExamAttemptStatus.Expired
        });
        await context.SaveChangesAsync();

        var controller = GetController(context, "1");

        var result = await controller.CreateAttempt(new CreateExamAttemptDTO { AssignmentId = 5 });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // Просроченная активная попытка закрывается и блокирует повторное прохождение.
    [Fact]
    public async Task CreateAttempt_WhenActiveButOverdue_ExpiresAndReturnsConflict()
    {
        using var context = CreateContext();
        context.ExamAttempts.Add(new ExamAttempt
        {
            AttemptId = 1,
            StudentId = 1,
            AssignmentId = 5,
            Status = ExamAttemptStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-90),
            DeadlineAt = DateTimeOffset.UtcNow.AddMinutes(-30)
        });
        await context.SaveChangesAsync();

        var controller = GetController(context, "1");

        var result = await controller.CreateAttempt(new CreateExamAttemptDTO { AssignmentId = 5 });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var attempt = await context.ExamAttempts.SingleAsync();
        Assert.Equal(ExamAttemptStatus.Expired, attempt.Status);
    }
}
