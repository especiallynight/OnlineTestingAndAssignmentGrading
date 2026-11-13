using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
                 .WriteTo.Console());

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration);

// Клиент обращается к gateway с другого origin: локально это порт 5238,
// в Docker — 5000. Без этой политики браузер блокирует запросы.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(
                "http://localhost:5000",
                "http://localhost:5238",
                "http://localhost:5130",
                "http://localhost:8080",
                "https://localhost:7261",
                "https://localhost:7114")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseSerilogRequestLogging();

// Ocelot обрабатывает все запросы, поэтому health-check регистрируем
// отдельным middleware до него.
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"status\":\"healthy\"}");
        return;
    }

    await next();
});

app.UseCors();
await app.UseOcelot();

app.Run();