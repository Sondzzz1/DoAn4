using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace RoomRental.BackEnd.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private static readonly Dictionary<int, HashSet<string>> UserConnections = new();

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            lock (UserConnections)
            {
                if (!UserConnections.ContainsKey(userId))
                {
                    UserConnections[userId] = new HashSet<string>();
                }
                UserConnections[userId].Add(Context.ConnectionId);
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            lock (UserConnections)
            {
                if (UserConnections.ContainsKey(userId))
                {
                    UserConnections[userId].Remove(Context.ConnectionId);
                    if (UserConnections[userId].Count == 0)
                    {
                        UserConnections.Remove(userId);
                    }
                }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public static List<string> GetConnections(int userId)
    {
        lock (UserConnections)
        {
            return UserConnections.TryGetValue(userId, out var list) ? list.ToList() : new List<string>();
        }
    }

    private int GetUserId()
    {
        var claim = Context.User?.FindFirst("UserId")?.Value ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
