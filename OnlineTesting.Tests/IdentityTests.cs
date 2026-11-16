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

    // Проверяет регистрацию пользователя и хэширование пароля.
    // Ожидает: успешный ответ, пользователь сохранён в БД, пароль захэширован, роль Student.
    [Fact]
    public async Task Register_ValidData_HashesPassword()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var dto = new CreateUserDTO
        {
            Email = "newstudent@test.com",
            Password = "123456",
            FirstName = "Мария",
            LastName = "Иванова"
        };

        var result = await controller.Register(dto);

        Assert.IsType<OkObjectResult>(result);

        var savedUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == "newstudent@test.com");

        Assert.NotNull(savedUser);

        Assert.Equal("Мария", savedUser.FirstName);
        Assert.Equal("Иванова", savedUser.LastName);
        Assert.Equal(UserRole.Student, savedUser.Role);

        Assert.NotEqual("123456", savedUser.PasswordHash);

        var passwordHasher = new PasswordHasher<User>();

        var verification = passwordHasher.VerifyHashedPassword(
            savedUser,
            savedUser.PasswordHash,
            "123456");

        Assert.Equal(
            PasswordVerificationResult.Success,
            verification);
    }
}

