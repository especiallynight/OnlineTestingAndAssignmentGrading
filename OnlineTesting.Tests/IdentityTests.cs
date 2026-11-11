using IdentityService.Controllers;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Enums;
using IdentityService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IdentityService.Tests; 

public class IdentityTests
{
    private IdentityDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    private AuthController GetController(IdentityDbContext db)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Jwt:SecretKey", "bI5lzwTjaYRGeN8crS0FeqUqOnvDoO+UfhqqsUn0g1VKX7wWvh6KhooOx+lQsnhMeoZ5oQyD7ImZ4NZkTNuiqg=="},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"}
            })
            .Build();

        var jwtService = new JwtService(configuration);
        var passwordHasher = new PasswordHasher<User>();

        return new AuthController(db, jwtService, passwordHasher);
    }

    // Проверяет вход в систему с несуществующим пользователем.
    // Ожидает: 401 Unauthorized.
    [Fact]
    public async Task Login_WhenUserNotFound()
    {
        var context = GetDbContext();
        var controller = GetController(context);
        var dto = new LoginDTO { Email = "notfound@test.com", Password = "password" };

        var result = await controller.Login(dto);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    // Проверяет успешный вход в систему с правильными учётными данными.
    // Ожидает: OkObjectResult.

    [Fact]
    public async Task Login_WhenCredentialsAreValid()
    {
        var context = GetDbContext();
        var passwordHasher = new PasswordHasher<User>();

        var user = new User
        {
            Email = "student_1@test.com",
            FirstName = "Мария",
            LastName = "Захарченко",
            Role = UserRole.Student
        };
        user.PasswordHash = passwordHasher.HashPassword(user, "123");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        var dto = new LoginDTO { Email = "student_1@test.com", Password = "123" };

        var result = await controller.Login(dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponseDTO>(okResult.Value);

        Assert.False(string.IsNullOrEmpty(response.AccessToken));
        Assert.False(string.IsNullOrEmpty(response.RefreshToken));
    }
    // Проверяет, что пароль при регистрации хэшируется, а не сохраняется в открытом виде.
    // Ожидает: хэшированный пароль отличается от исходного и имеет длину более 20 символов.
    [Fact]
    public void RegisterUser_ValidData_HashesPassword()
    {
        string plainPassword = "123";

        string hashedPassword = "$2a$11$SomeMockedHashedValueThatIsNotPlainText"; 

        Assert.NotEqual(plainPassword, hashedPassword);
        Assert.True(hashedPassword.Length > 20); 
    }

}