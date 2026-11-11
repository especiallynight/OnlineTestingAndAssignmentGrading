using AnalyticsService.Data;
using AnalyticsService.Hubs;
using AnalyticsService.Models;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace AnalyticsService.Consumers;

public class AttemptGradedConsumer : IConsumer<AttemptGradedEvent>
{
    private readonly AnalyticsDbContext _context;
    private readonly IHubContext<GradingHub> _hubContext;
    public AttemptGradedConsumer(AnalyticsDbContext context, IHubContext<GradingHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task Consume(ConsumeContext<AttemptGradedEvent> context)
    {
        var msg = context.Message;
        var scorePercent = msg.MaxScore > 0 ? (msg.Score / msg.MaxScore) * 100 : 0;

        var stats = await _context.StudentStatistics
            .FirstOrDefaultAsync(s => s.StudentId == msg.StudentId && s.CourseId == msg.CourseId);

        var is_new_result = false;

        if (stats == null)
        {
            stats = new StudentStatistics
            {
                StudentId = msg.StudentId,
                CourseId = msg.CourseId,
                CompletedAttempts = 1,
                AverageScore = scorePercent,
                HighestScore = scorePercent,
                ProcessedAttemptIds = new List<int> { msg.AttemptId },
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.StudentStatistics.Add(stats);
            is_new_result = true;
        }
        else if (!stats.ProcessedAttemptIds.Contains(msg.AttemptId))
        {
            stats.AverageScore =
                (stats.AverageScore * stats.CompletedAttempts + scorePercent)
                / (stats.CompletedAttempts + 1);

            stats.CompletedAttempts++;
            stats.HighestScore = Math.Max(stats.HighestScore, scorePercent);
            stats.ProcessedAttemptIds.Add(msg.AttemptId);
            stats.UpdatedAt = DateTimeOffset.UtcNow;

            is_new_result = true;
        }

        await _context.SaveChangesAsync();

        if (is_new_result)
        {
            await _hubContext.Clients.All.SendAsync(
                "ReceiveGradeUpdate",
                msg.AttemptId,
                msg.Score);
        }

    }
}