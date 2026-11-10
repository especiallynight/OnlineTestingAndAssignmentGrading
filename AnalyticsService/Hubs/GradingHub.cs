using Microsoft.AspNetCore.SignalR;

namespace AnalyticsService.Hubs;

public class GradingHub : Hub
{
    public async Task SendGradeUpdate(int attemptId, decimal score)
    {
        await Clients.All.SendAsync("ReceiveGradeUpdate", attemptId, score);
    }
}