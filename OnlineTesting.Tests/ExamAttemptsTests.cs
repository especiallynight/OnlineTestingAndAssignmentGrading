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
    private ExamController GetController(ExamDbContext db, string userId = "1")
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
                    "{\"timeLimitMinutes\":60,\"maxScore\":100,\"courseId\":1,\"questionCount\":3}")
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


    // Проверяет успешный запуск попытки экзамена.
    // Ожидает: OkObjectResult, Status = InProgress.
    [Fact]
    public async Task StartAttempt_Success()
    {
        var options = new DbContextOptionsBuilder<ExamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ExamDbContext(options);
        context.ExamAttempts.Add(new ExamAttempt { AttemptId = 1, StudentId = 1, Status = ExamAttemptStatus.InProgress });
        await context.SaveChangesAsync();

        var controller = GetController(context, "1");

        var result = await controller.StartAttempt(1, new StartAttemptDTO { TimeLimitMinutes = 60 });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ExamAttemptResponseDTO>(okResult.Value);

        Assert.NotNull(dto.StartedAt); 
        Assert.Equal(ExamAttemptStatus.InProgress, dto.Status);
    }
    
    // Проверяет запрет повторного запуска уже начатой попытки экзамена.
    // Ожидает: 409 Conflict.
    [Fact]
    public async Task StartAttempt_WhenAlreadyInProgress()
    {
        var options = new DbContextOptionsBuilder<ExamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ExamDbContext(options);
        context.ExamAttempts.Add(new ExamAttempt
        {
            AttemptId = 1,
            StudentId = 1,
            Status = ExamAttemptStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
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
        var options = new DbContextOptionsBuilder<ExamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ExamDbContext(options);

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
}

