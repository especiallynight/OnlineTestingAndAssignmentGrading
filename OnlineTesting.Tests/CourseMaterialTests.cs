using CourseService.Controllers;
using CourseService.Data;
using CourseService.DTOs;
using CourseService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace OnlineTesting.Tests
{
    public class CourseMaterialControllerTests
    {
        private static CourseDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<CourseDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new CourseDbContext(options);
        }

        private static CourseMaterialController GetController(
            CourseDbContext db,
            string userId = "2",
            string role = "Teacher")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuth"));

            return new CourseMaterialController(db)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
        }

        private static async Task<Course> SeedCourseAsync(CourseDbContext db, int teacherId = 2)
        {
            var course = new Course
            {
                Name = "Основы программирования",
                Description = "Базовый курс",
                TeacherId = teacherId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            return course;
        }

        // Преподаватель — владелец курса добавляет материал. Ожидает: 201 и сохранение в БД.
        [Fact]
        public async Task CreateMaterial_WhenTeacherOwnsCourse()
        {
            var context = GetDbContext();
            var course = await SeedCourseAsync(context, teacherId: 2);
            var controller = GetController(context, userId: "2");

            var result = await controller.CreateMaterial(course.CourseId, new CreateCourseMaterialDTO
            {
                Title = "Введение в C#",
                Content = "C# — современный объектно-ориентированный язык.",
                SortOrder = 1
            });

            var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CourseMaterialResponseDTO>(createdAt.Value);
            Assert.Equal("Введение в C#", response.Title);
            Assert.Equal(course.CourseId, response.CourseId);

            var saved = await context.CourseMaterials.SingleAsync();
            Assert.Equal("Введение в C#", saved.Title);
            Assert.Equal(1, saved.SortOrder);
        }

        // Преподаватель не может добавить материал в чужой курс. Ожидает: 403 Forbidden.
        [Fact]
        public async Task CreateMaterial_WhenTeacherDoesNotOwnCourse()
        {
            var context = GetDbContext();
            var course = await SeedCourseAsync(context, teacherId: 1);
            var controller = GetController(context, userId: "2");

            var result = await controller.CreateMaterial(course.CourseId, new CreateCourseMaterialDTO
            {
                Title = "Чужой материал",
                Content = "Этот материал не должен сохраниться.",
                SortOrder = 1
            });

            Assert.IsType<ForbidResult>(result.Result);
            Assert.Empty(context.CourseMaterials);
        }

        // Материалы отдаются в порядке SortOrder. Ожидает: [1, 2, 3].
        [Fact]
        public async Task GetMaterials_ReturnsOrderedBySortOrder()
        {
            var context = GetDbContext();
            var course = await SeedCourseAsync(context);
            context.CourseMaterials.AddRange(
                new CourseMaterial
                {
                    CourseId = course.CourseId,
                    Title = "Третий",
                    Content = "Текст третьего материала.",
                    SortOrder = 3
                },
                new CourseMaterial
                {
                    CourseId = course.CourseId,
                    Title = "Первый",
                    Content = "Текст первого материала.",
                    SortOrder = 1
                },
                new CourseMaterial
                {
                    CourseId = course.CourseId,
                    Title = "Второй",
                    Content = "Текст второго материала.",
                    SortOrder = 2
                });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            var result = await controller.GetMaterials(course.CourseId);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var materials = Assert.IsType<List<CourseMaterialResponseDTO>>(ok.Value);
            Assert.Equal(new[] { "Первый", "Второй", "Третий" }, materials.Select(item => item.Title));
        }

        // Владелец курса обновляет материал. Ожидает: изменённый заголовок и порядок.
        [Fact]
        public async Task UpdateMaterial_WhenTeacherOwnsCourse()
        {
            var context = GetDbContext();
            var course = await SeedCourseAsync(context, teacherId: 2);
            var material = new CourseMaterial
            {
                CourseId = course.CourseId,
                Title = "Старый заголовок",
                Content = "Старый текст материала.",
                SortOrder = 1
            };
            context.CourseMaterials.Add(material);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId: "2");

            var result = await controller.UpdateMaterial(course.CourseId, material.CourseMaterialId,
                new UpdateCourseMaterialDTO
                {
                    Title = "Новый заголовок",
                    Content = "Обновлённый текст материала.",
                    SortOrder = 5
                });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<CourseMaterialResponseDTO>(ok.Value);
            Assert.Equal("Новый заголовок", response.Title);
            Assert.Equal(5, response.SortOrder);

            var saved = await context.CourseMaterials.SingleAsync();
            Assert.Equal("Обновлённый текст материала.", saved.Content);
        }

        // Владелец курса удаляет материал. Ожидает: 204 и пустая таблица.
        [Fact]
        public async Task DeleteMaterial_WhenTeacherOwnsCourse()
        {
            var context = GetDbContext();
            var course = await SeedCourseAsync(context, teacherId: 2);
            var material = new CourseMaterial
            {
                CourseId = course.CourseId,
                Title = "Удаляемый материал",
                Content = "Этот материал будет удалён.",
                SortOrder = 1
            };
            context.CourseMaterials.Add(material);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId: "2");

            var result = await controller.DeleteMaterial(course.CourseId, material.CourseMaterialId);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(context.CourseMaterials);
        }
    }
}
