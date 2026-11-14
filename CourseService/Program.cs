using CourseService.Data;
using CourseService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddDbContext<CourseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    options.InstanceName = "CourseService:";
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Введите access token"
    });
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddCors();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    c.RoutePrefix = string.Empty;
});

app.UseAuthentication();
app.UseAuthorization();

app.UseCors(p => p.WithOrigins("http://localhost:5238", "http://localhost:5000", "http://localhost:8080").AllowAnyMethod().AllowAnyHeader());

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Courses.AnyAsync())
    {
        var demoTeacherId = builder.Configuration.GetValue("Demo:TeacherId", 2);
        var demoStudentId = builder.Configuration.GetValue("Demo:StudentId", 3);

        static JsonElement Json(string raw) =>
            JsonDocument.Parse(raw).RootElement.Clone();

        var csharpCourse = new Course
        {
            Name = "Основы программирования на C#",
            Description = "Базовый курс по языку C#: типы данных, управляющие конструкции, ООП и работа с коллекциями.",
            TeacherId = demoTeacherId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var webCourse = new Course
        {
            Name = "Веб-разработка: HTML, CSS, JavaScript",
            Description = "Создание адаптивных интерфейсов, основы DOM, события и работа с сетевыми запросами.",
            TeacherId = demoTeacherId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Courses.AddRange(csharpCourse, webCourse);

        var csharpTest = new Assignment
        {
            Course = csharpCourse,
            Name = "Вводный тест по C#",
            Description = "Проверка базовых знаний платформы .NET и синтаксиса языка.",
            MaxScore = 10,
            TimeLimitMinutes = 30
        };

        var webTest = new Assignment
        {
            Course = webCourse,
            Name = "JavaScript: основы",
            Description = "Типы данных, массивы, функции и работа с DOM.",
            MaxScore = 10,
            TimeLimitMinutes = 40
        };

        db.Assignments.AddRange(csharpTest, webTest);

        db.Questions.AddRange(
            new Question
            {
                Assignment = csharpTest,
                QuestionText = "Что такое CLR?",
                QuestionType = "SingleChoice",
                QuestionOptions = Json("""{"a":"Среда выполнения общего языка","b":"Компилятор языка C#","c":"Графическая библиотека"}"""),
                CorrectAnswer = Json("\"a\"")
            },
            new Question
            {
                Assignment = csharpTest,
                QuestionText = "Какие типы являются значимыми?",
                QuestionType = "MultipleChoice",
                QuestionOptions = Json("""{"a":"int","b":"string","c":"double","d":"bool"}"""),
                CorrectAnswer = Json("""["a","c","d"]""")
            },
            new Question
            {
                Assignment = csharpTest,
                QuestionText = "Напишите метод Sum для двух чисел",
                QuestionType = "Code",
                QuestionOptions = Json("{}"),
                TestCases = Json("""[{"input":"1 2","expectedOutput":"3"}]""")
            },
            new Question
            {
                Assignment = webTest,
                QuestionText = "Какой метод создаёт новый массив?",
                QuestionType = "SingleChoice",
                QuestionOptions = Json("""{"a":"map","b":"push","c":"pop"}"""),
                CorrectAnswer = Json("\"a\"")
            },
            new Question
            {
                Assignment = webTest,
                QuestionText = "Какие методы не меняют массив?",
                QuestionType = "MultipleChoice",
                QuestionOptions = Json("""{"a":"map","b":"filter","c":"push","d":"slice"}"""),
                CorrectAnswer = Json("""["a","b","d"]""")
            });

        db.Enrollments.Add(new Enrollment
        {
            Course = csharpCourse,
            StudentId = demoStudentId,
            EnrolledAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }

    // Демонстрационная теория. Добавляется отдельно от курсов, чтобы
    // материалы появились и в уже существующей базе.
    if (!await db.CourseMaterials.AnyAsync())
    {
        var csharpCourse = await db.Courses
            .FirstOrDefaultAsync(course => course.Name == "Основы программирования на C#");
        var webCourse = await db.Courses
            .FirstOrDefaultAsync(course => course.Name == "Веб-разработка: HTML, CSS, JavaScript");

        var materials = new List<CourseMaterial>();

        if (csharpCourse != null)
        {
            materials.AddRange(new[]
            {
                new CourseMaterial
                {
                    CourseId = csharpCourse.CourseId,
                    SortOrder = 1,
                    Title = "Введение в .NET и C#",
                    Content = """
                        .NET — платформа для разработки приложений. Программа на C# компилируется в промежуточный язык IL, который выполняет CLR.

                        Основные компоненты:
                        • CLR — среда выполнения: управляет памятью, потоками и исключениями.
                        • JIT-компилятор — превращает IL в машинный код при первом обращении к методу.
                        • BCL — базовая библиотека классов .NET.

                        Минимальная программа:
                        using System;
                        Console.WriteLine("Hello, world!");
                        """,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new CourseMaterial
                {
                    CourseId = csharpCourse.CourseId,
                    SortOrder = 2,
                    Title = "Типы данных и переменные",
                    Content = """
                        C# — статически типизированный язык: тип переменной известен на этапе компиляции.

                        • int, long — целые числа.
                        • double, decimal — числа с дробной частью (decimal — для денег).
                        • bool — логическое значение true/false.
                        • string — строка, ссылочный тип.
                        • var — вывод типа компилятором, тип всё равно фиксируется.

                        int count = 3;
                        double price = 99.90;
                        string name = "Анна";
                        var total = count * price;
                        """,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new CourseMaterial
                {
                    CourseId = csharpCourse.CourseId,
                    SortOrder = 3,
                    Title = "Управляющие конструкции",
                    Content = """
                        Условия:
                        if (score >= 60) { ... } else { ... }

                        switch (role)
                        {
                            case "teacher": ...; break;
                            default: ...; break;
                        }

                        Циклы:
                        for (int i = 0; i < 5; i++) { ... }
                        foreach (var item in items) { ... }
                        while (condition) { ... }
                        """,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new CourseMaterial
                {
                    CourseId = csharpCourse.CourseId,
                    SortOrder = 4,
                    Title = "Основы ООП",
                    Content = """
                        Класс описывает состояние (поля) и поведение (методы) объекта.

                        public class Student
                        {
                            public string Name { get; set; } = string.Empty;
                            public int Score { get; private set; }

                            public void AddScore(int points) => Score += points;
                        }

                        Инкапсуляция скрывает детали через модификаторы доступа: public, private, protected.
                        Наследование позволяет вынести общее поведение в базовый класс, полиморфизм — работать с наследниками через общий тип.
                        """,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            });
        }

        if (webCourse != null)
        {
            materials.AddRange(new[]
            {
                new CourseMaterial
                {
                    CourseId = webCourse.CourseId,
                    SortOrder = 1,
                    Title = "Как устроен веб",
                    Content = """
                        Браузер (клиент) отправляет HTTP-запрос на сервер и получает HTTP-ответ.

                        • URL — адрес ресурса: схема, домен, путь, параметры.
                        • Методы: GET — получить, POST — создать, PUT/PATCH — изменить, DELETE — удалить.
                        • Коды ответа: 200 OK, 201 Created, 400 Bad Request, 401 Unauthorized, 404 Not Found, 500 Internal Server Error.

                        Статический сайт отдаёт готовые HTML, CSS и JS; динамический — формирует ответ на сервере.
                        """,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new CourseMaterial
                {
                    CourseId = webCourse.CourseId,
                    SortOrder = 2,
                    Title = "HTML: структура страницы",
                    Content = """
                        HTML описывает смысл и структуру документа.

                        <!DOCTYPE html>
                        <html lang="ru">
                        <head>
                          <meta charset="UTF-8">
                          <title>Страница</title>
                        </head>
                        <body>
                          <header>...</header>
                          <main>
                            <h1>Заголовок</h1>
                            <p>Абзац текста</p>
                          </main>
                        </body>
                        </html>

                        Семантические теги header, nav, main, section, article и footer помогают поисковым системам и экранным читалкам.
                        """,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new CourseMaterial
                {
                    CourseId = webCourse.CourseId,
                    SortOrder = 3,
                    Title = "CSS: оформление и адаптивность",
                    Content = """
                        CSS подключается через <link rel="stylesheet" href="style.css"> или тег <style>.

                        • Селекторы: .class, #id, tag, [attribute].
                        • Блочная модель: content + padding + border + margin.
                        • Flexbox выстраивает элементы в ряд или колонку: display: flex; gap: 16px.
                        • Медиазапросы адаптируют вёрстку:
                        @media (max-width: 768px) { .columns { flex-direction: column; } }
                        """,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new CourseMaterial
                {
                    CourseId = webCourse.CourseId,
                    SortOrder = 4,
                    Title = "JavaScript и DOM",
                    Content = """
                        JavaScript делает страницу интерактивной.

                        • let и const — переменные с блочной областью видимости.
                        • Функции: function name() {} или const name = () => {}.
                        • DOM — дерево элементов страницы. Поиск: document.querySelector('.card').
                        • События: element.addEventListener('click', handler).

                        Пример:
                        const button = document.querySelector('#save');
                        button.addEventListener('click', () => {
                          document.querySelector('#status').textContent = 'Сохранено';
                        });

                        Запросы к серверу выполняются через fetch:
                        const response = await fetch('/api/courses');
                        const courses = await response.json();
                        """,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            });
        }

        if (materials.Count > 0)
        {
            db.CourseMaterials.AddRange(materials);
            await db.SaveChangesAsync();
        }
    }
}
app.Run();