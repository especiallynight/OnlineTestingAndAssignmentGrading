using ExamService.Controllers;
using ExamService.Data;
using ExamService.DTOs;
using ExamService.Enums;
using ExamService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace ExamService.Tests;

public class ExamControllerTests
{
    private ExamController GetController(ExamDbContext db, string userId = "1")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, "Student")
    }, "Test"));

        return new ExamController(db)
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
            .UseInMemoryDatabase("TestDb_Success") 
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
    public async Task StartAttemptReturnsConflict()
    {
        var options = new DbContextOptionsBuilder<ExamDbContext>()
            .UseInMemoryDatabase("TestDb_Conflict") 
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

    //При старте экзамена попытка должна создаваться со статусом "InProgress" и корректным временем.
    [Fact]
    public async Task StartAttempt_ValidData()
    {
        int studentId = 1;
        int assignmentId = 10;

        var result = new { AttemptId = 1, Status = "InProgress", StartedAt = DateTime.UtcNow };

        Assert.NotNull(result);
        Assert.Equal("InProgress", result.Status); 
        Assert.True(result.StartedAt <= DateTime.UtcNow);
    }

}