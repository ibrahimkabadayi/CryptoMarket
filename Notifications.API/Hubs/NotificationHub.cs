using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Notifications.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Client connected: {Context.ConnectionId}, User: {Context.User?.FindFirst("nameid")?.Value}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
    }

    public async Task SendPriceAlertNotification(string userId, object priceAlert)
    {
        await Clients.User(userId).SendAsync("ReceivePriceAlert", priceAlert);
    }

    public async Task DeactivatePriceAlertNotification(string userId, string priceAlertId)
    {
        await Clients.User(userId).SendAsync("DeactivatePriceAlert", priceAlertId);
    }

    public async Task SendNotification(string userId, object notification)
    {
        await Clients.User(userId).SendAsync("SendNotification", notification);
    }

    public async Task MarkNotificationAsRead(string userId, string notificationId)
    {
        await Clients.User(userId).SendAsync("DeactivateNotification", notificationId);
    }
}
