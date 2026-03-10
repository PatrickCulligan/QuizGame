using Microsoft.AspNetCore.SignalR;

namespace QuizGame.Hubs;

public class GameHub : Hub
{
    public async Task JoinSessionGroup(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }
}
