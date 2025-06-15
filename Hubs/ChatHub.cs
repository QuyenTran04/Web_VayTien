using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace VAYTIEN.Hubs
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, string> UserConnections = new();

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
                UserConnections[userId] = Context.ConnectionId;
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
                UserConnections.TryRemove(userId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        // Gửi tin nhắn tới user cụ thể
        public async Task SendMessage(string receiverId, string message)
        {
            var senderId = Context.UserIdentifier;
            var senderName = Context.User?.Identity?.Name ?? "Unknown";
            if (UserConnections.TryGetValue(receiverId, out var connId))
            {
                await Clients.Client(connId).SendAsync("ReceiveMessage", senderId, senderName, message);
            }
            await Clients.Caller.SendAsync("ReceiveMessage", senderId, senderName, message);
        }
    }
}
