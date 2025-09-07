using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Mini_Slack_API.models;

[Authorize] // require JWT
public class MessagesHub : Hub
{
    private readonly ApplicationDbContext _context;

    public MessagesHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SendMessage(Guid channelId, string content)
    {
        var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return;

        var userGuid = Guid.Parse(userId);
        var user = await _context.Users.FindAsync(userGuid);
        if (user == null) return;

        // Optional: save message to DB
        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ChannelId = channelId,
            UserId = user.UserId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Broadcast to channel group
        await Clients.Group($"channel_{channelId}")
            .SendAsync("ReceiveMessage", new
            {
                message.MessageId,
                message.Content,
                message.CreatedAt,
                message.UserId,
                UserName = user.Name,
                user.AvatarUrl,
                message.ChannelId
            });
    }

    public async Task JoinChannel(Guid channelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel_{channelId}");
    }

    public async Task LeaveChannel(Guid channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");
    }
}