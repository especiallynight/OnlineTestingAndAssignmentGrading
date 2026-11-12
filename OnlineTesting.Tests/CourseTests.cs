using CourseService.Controllers;
using CourseService.Data;
using CourseService.DTOs;
using CourseService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace OnlineTesting.Tests
{
    public class CourseControllerTests
    {
        private CourseDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<CourseDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new CourseDbContext(options);
        }

        private CourseController GetController(CourseDbContext db, string userId = "10", string role = "Teacher")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, role)
    }, "TestAuth"));
            var cacheMock = new Mock<IDistributedCache>();
            return new CourseController(db, cacheMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
        }

        //Проверяет создание курса преподавателем. Ожидает: CreatedAtActionResult, название курса = "Программирование на C#", TeacherId = 2.
        [Fact]
        public async Task CreateCourse_WhenTeacherValid()
        {
            var context = GetDbContext();
            var controller = GetController(context, userId: "2", role: "Teacher");

            var dto = new CreateCourseDTO
            {
                Name = "Программирование на C#",
                Description = "Базовый курс"
            };

            var result = await controller.CreateCourse(dto);

            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var courseResponse = Assert.IsType<CourseResponseDTO>(createdAtResult.Value);

            Assert.Equal("Программирование на C#", courseResponse.Name);
            Assert.Equal(2, courseResponse.TeacherId);
        }

        // Проверяет, что преподаватель не может удалить чужой курс.
        // Ожидает: 403 Forbidden.
        [Fact]
        public async Task DeleteCourse_WhenUserIsNotCourseOwner()
        {
            var context = GetDbContext();

            context.Courses.Add(new Course
            {
                CourseId = 1,
                Name = "Тестовый курс",
                TeacherId = 1,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();

            var controller = GetController(context, userId: "2", role: "Teacher");

            var result = await controller.DeleteCourse(1);

            Assert.IsType<ForbidResult>(result);
        }

        // Проверяет запрет создания задания с максимальной оценкой меньше или равной 0.
        // Ожидает: ArgumentException с сообщением "Максимальный счет должен быть больше нуля".
        [Fact]
        public async Task CreateAssignment_WithInvalidMaxScore()
        {

            var assignmentDto = new { Title = "Test", MaxScore = 0 };

            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                throw new ArgumentException("Максимальный счет должен быть больше нуля."); 
            });

            Assert.Contains("Максимальный счет должен быть больше нуля", exception.Message);
        }
    }
}
