
using ExamService.Data;
using ExamService.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamService.Services;

public class AttemptExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttemptExpirationService> _logger;

    public AttemptExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<AttemptExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<ExamDbContext>();

                var now = DateTimeOffset.UtcNow;

                var expiredCount = await context.ExamAttempts
                    .Where(a =>
                        a.Status == ExamAttemptStatus.InProgress &&
                        a.DeadlineAt != null &&
                        a.DeadlineAt <= now)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(
                            a => a.Status,
                            ExamAttemptStatus.Expired),
                        stoppingToken);

                if (expiredCount > 0)
                {
                    _logger.LogInformation(
                        "Expired attempts: {Count}", expiredCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Error while expiring attempts");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }
}
