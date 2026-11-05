using CourseService.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.ComponentModel.DataAnnotations;

namespace OnlineTesting.Tests;

public class CourseServiceHealthTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CourseServiceHealthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("healthy", content);
    }

    [Fact]
    public async Task CreateAssignmentDtoWithNegativeMaxScore()
    {
        var dto = new CreateAssignmentDto
        {
            Name = "Test assignment",
            MaxScore = -5,
            TimeLimitMinutes = 30
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(
            dto,
            context,
            results,
            validateAllProperties: true
        );

        Assert.False(isValid);
    }
}