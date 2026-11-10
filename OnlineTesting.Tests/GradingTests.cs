using GradingService.Controllers;
using GradingService.Data;
using GradingService.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Moq;
using System.Net.Http;
using System.Security.Claims;
using Xunit;

namespace GradingService.Tests;

public class GradingControllerTests
{
    private GradesDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<GradesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new GradesDbContext(options);
    }

    private HttpClient CreateMockHttpClient()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        return new HttpClient(handlerMock.Object);
    }

    private GradesController GetController(GradesDbContext db, string userId = "5", string role = "Teacher")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
    
        }, "TestAuth"));

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(CreateMockHttpClient());

        return new GradesController(db, factoryMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }


    [Fact]
    public async Task CreateGrade_WhenScoreExceedsMax()
    {
        var context = GetDbContext();
        var controller = GetController(context);
        var dto = new AttemptGradedEventDTO { AttemptId = 1, Score = 120, MaxScore = 100 };

        var result = await controller.CreateGrade(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateGrade_WhenNegativeScore()
    {
        var context = GetDbContext();
        var controller = GetController(context);
        var dto = new AttemptGradedEventDTO { AttemptId = 1, Score = -5, MaxScore = 100 };

        var result = await controller.CreateGrade(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}