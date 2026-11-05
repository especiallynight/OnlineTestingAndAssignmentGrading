using ExamService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ExamDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ExamDbContext>();

    var canConnect = await db.Database.CanConnectAsync();

    Console.WriteLine($"Database connection: {canConnect}");
}
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();