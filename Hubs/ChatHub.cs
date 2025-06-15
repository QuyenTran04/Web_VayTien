using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using VAYTIEN.Models;

namespace VAYTIEN.Hubs
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, string> UserConnections = new();
        private readonly QlvayTienContext _context;

        public ChatHub(QlvayTienContext context)
        {
            _context = context;
        }

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

        // Gửi và lưu message riêng từng cặp
        public async Task SendMessage(string receiverId, string message)
        {
            var senderId = Context.UserIdentifier;
            var senderName = Context.User?.Identity?.Name ?? "Unknown";

            // Lưu DB
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                SenderName = senderName,
                Message = message,
                SentAt = DateTime.Now
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Gửi cho người nhận (nếu đang online)
            if (UserConnections.TryGetValue(receiverId, out var connId))
            {
                await Clients.Client(connId).SendAsync("ReceiveMessage", senderId, senderName, message);
            }
            // Echo lại cho sender
            await Clients.Caller.SendAsync("ReceiveMessage", senderId, senderName, message);
        }
    }
}
