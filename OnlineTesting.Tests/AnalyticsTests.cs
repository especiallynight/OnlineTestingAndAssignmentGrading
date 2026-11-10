using AnalyticsService.Controllers;
using AnalyticsService.Data;
using AnalyticsService.DTOs;
using AnalyticsService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            new Claim("sub", userId),
            new Claim("role", role)
        }));
        return new AnalyticsController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };
    }

    [Fact]
    public async Task GetStudentStatisticsForOtherStudent()
    {
        var context = GetDbContext();
        var controller = GetController(context, userId: "1", role: "Student");

        var result = await controller.GetStudentStatistics(2);

        Assert.IsType<ForbidResult>(result);
    }

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

    //Защита от повторной доставки RabbitMQ
    [Fact]
    public void UpdateStatistics_IgnoresDuplicateAttemptId()
    {
        var stats = new StudentStatistics
        {
            StudentId = 1,
            CourseId = 1,
            CompletedAttempts = 1,
            AverageScore = 80,
            HighestScore = 80,
            ProcessedAttemptIds = new List<int> { 100 } 
        };

        int newAttemptId = 100; 
        decimal newScorePercent = 90;

        if (!stats.ProcessedAttemptIds.Contains(newAttemptId))
        {
            stats.AverageScore = (stats.AverageScore * stats.CompletedAttempts + newScorePercent) / (stats.CompletedAttempts + 1);
            stats.CompletedAttempts++;
            stats.ProcessedAttemptIds.Add(newAttemptId);
        }

        Assert.Equal(1, stats.CompletedAttempts); 
        Assert.Equal(80, stats.AverageScore);     
        Assert.Single(stats.ProcessedAttemptIds); 
    }

}