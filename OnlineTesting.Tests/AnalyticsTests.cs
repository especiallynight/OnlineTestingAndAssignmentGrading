using AnalyticsService.Consumers;
using AnalyticsService.Controllers;
using AnalyticsService.Data;
using AnalyticsService.DTOs;
using AnalyticsService.Hubs;
using AnalyticsService.Models;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Shared.Events;
using System.Security.Claims;

namespace AnalyticsService.Tests;

public class AnalyticsControllerTests
{
    private AnalyticsDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AnalyticsDbContext(options);
    }

    private AnalyticsController GetController(AnalyticsDbContext db, string userId = "1", string role = "Student")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
{
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth"));
        return new AnalyticsController(db, new Mock<IDistributedCache>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };
    }


    //Проверяет, что студент не может получить статистику другого студента.
    //Ожидаемый результат: Forbid (403).
    [Fact]
    public async Task GetStudentStatisticsForOtherStudent()
    {
        var context = GetDbContext();
        var controller = GetController(context, userId: "1", role: "Student");

        var result = await controller.GetStudentStatistics(2);

        Assert.IsType<ForbidResult>(result);
    }

    //Проверяет корректность обновления статистики после новой попытки.
    //Ожидает: CompletedAttempts = 2, AverageScore = 75, HighestScore = 100.
    [Fact]
    public async Task UpdateStudentStatistics_CalculatesAverageCorrectly()
    {
        var context = GetDbContext();
        context.StudentStatistics.Add(new StudentStatistics
        {
            StudentId = 1,
            CourseId = 1,
            CompletedAttempts = 1,
            AverageScore = 50,
            HighestScore = 50,
            ProcessedAttemptIds = new List<int> { 10 }
        });
        await context.SaveChangesAsync();

        var controller = GetController(context, userId: "5", role: "Teacher");
        var dto = new UpdateStudentStatisticsDTO { StudentId = 1, CourseId = 1, AttemptId = 11, Score = 100, MaxScore = 100 };

        await controller.UpdateStudentStatistics(dto);

        var updated = await context.StudentStatistics.FirstAsync();
        Assert.Equal(2, updated.CompletedAttempts);
        Assert.Equal(75, updated.AverageScore);
        Assert.Equal(100, updated.HighestScore);
    }

    // Проверяет, что повторная доставка события не создаёт дубль статистики
    [Fact]
    public async Task Consume_DuplicateEvent()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new AnalyticsDbContext(options);

        var clientProxy = new Mock<IClientProxy>();
        var hubClients = new Mock<IHubClients>();

        hubClients
            .Setup(c => c.All)
            .Returns(clientProxy.Object);

        var hub = new Mock<IHubContext<GradingHub>>();
        hub.Setup(h => h.Clients).Returns(hubClients.Object);

        clientProxy
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cacheMock = new Mock<IDistributedCache>();
        var consumer = new AttemptGradedConsumer(db, hub.Object, cacheMock.Object);

        var evt = new AttemptGradedEvent
        {
            AttemptId = 1,
            StudentId = 1,
            CourseId = 1,
            Score = 80,
            MaxScore = 100,
            GradedAt = DateTimeOffset.UtcNow
        };

        var ctx = new Mock<ConsumeContext<AttemptGradedEvent>>();
        ctx.Setup(c => c.Message).Returns(evt);

        // Отправляем событие дважды
        await consumer.Consume(ctx.Object);
        await consumer.Consume(ctx.Object);

        // Проверяем, что статистика посчитана только один раз
        var stats = await db.StudentStatistics.SingleAsync();
        Assert.Equal(1, stats.CompletedAttempts);
        Assert.Equal(80, stats.AverageScore);
    }

}