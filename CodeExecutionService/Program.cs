using CodeExecutionService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
                 .WriteTo.Console());

builder.Services.AddControllers();

var codeRunner = builder.Configuration["CodeRunner"] ?? "docker";
if (string.Equals(codeRunner, "process", StringComparison.OrdinalIgnoreCase))
{
    // Контейнерный режим: Docker-демон недоступен, код собирается
    // и запускается внутри контейнера CodeExecutionService.
    builder.Services.AddScoped<ICodeRunner, ProcessCodeRunner>();
}
else
{
    builder.Services.AddScoped<ICodeRunner, DockerCodeRunner>();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.Use(async (ctx, next) =>
{
    var key = builder.Configuration["InternalApiKey"];
    if (!string.IsNullOrEmpty(key) && ctx.Request.Path.StartsWithSegments("/api"))
    {
        if (ctx.Request.Headers["X-Internal-Api-Key"] != key)
        {
            ctx.Response.StatusCode = 401;
            return;
        }
    }
    await next();
});
app.MapControllers();

app.Run();