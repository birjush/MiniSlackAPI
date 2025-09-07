using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Mini_Slack_API.models;

[ApiController]
[Route("api/channels/{channelId}/[controller]")]
[Authorize] // Require JWT
public class MessagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<MessagesHub> _hubContext;

    public MessagesController(ApplicationDbContext context, IHubContext<MessagesHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // POST: api/channels/{channelId}/messages
    [HttpPost]
    public async Task<IActionResult> PostMessage(Guid channelId, [FromBody] MessageCreateDto dto)
    {
        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ChannelId = channelId,
            UserId = dto.UserId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var messageDto = new MessageDto
        {
            MessageId = message.MessageId,
            ChannelId = message.ChannelId,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            UserId = message.UserId,
            UserName = (await _context.Users.FindAsync(message.UserId))?.Name ?? "Unknown",
            AvatarUrl = (await _context.Users.FindAsync(message.UserId))?.AvatarUrl
        };

        // Broadcast to SignalR group
        await _hubContext.Clients.Group($"channel_{channelId}")
            .SendAsync("ReceiveMessage", messageDto);

        return Ok(messageDto);
    }

    // GET: api/channels/{channelId}/messages
    [HttpGet]
    public async Task<IActionResult> GetByChannel(Guid channelId)
    {
        var messages = await _context.Messages
            .Where(m => m.ChannelId == channelId)
            .Include(m => m.User)
            .Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                UserId = m.UserId,
                UserName = m.User.Name,
                AvatarUrl = m.User.AvatarUrl,
                ChannelId = m.ChannelId
            })
            .ToListAsync();

        return Ok(messages);
    }
}
