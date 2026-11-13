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
}
app.Run();