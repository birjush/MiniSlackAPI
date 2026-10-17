using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Mini_Slack_API.models;
using Npgsql;

[ApiController]
[Route("api/channels/{channelId}/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IHubContext<MessagesHub> _hubContext;

    public MessagesController(NpgsqlDataSource dataSource, IHubContext<MessagesHub> hubContext)
    {
        _dataSource = dataSource;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage(Guid channelId, [FromBody] MessageCreateDto dto)
    {
        var messageId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            WITH new_message AS (
                INSERT INTO messages (message_id, channel_id, user_id, content, created_at)
                SELECT @message_id, @channel_id, @user_id, @content, @created_at
                WHERE EXISTS (SELECT 1 FROM channels WHERE channel_id = @channel_id)
                  AND EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
                RETURNING message_id, channel_id, user_id, content, created_at
            )
            SELECT m.message_id, m.content, m.created_at, m.user_id, u.name, u.avatar_url, m.channel_id
            FROM new_message AS m
            JOIN users AS u ON u.user_id = m.user_id
            """, connection);
        command.Parameters.AddWithValue("message_id", messageId);
        command.Parameters.AddWithValue("channel_id", channelId);
        command.Parameters.AddWithValue("user_id", dto.UserId);
        command.Parameters.AddWithValue("content", dto.Content);
        command.Parameters.AddWithValue("created_at", createdAt);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return BadRequest("Channel or user was not found.");

        var messageDto = ReadMessage(reader);
        await reader.CloseAsync();

        await _hubContext.Clients.Group($"channel_{channelId}")
            .SendAsync("ReceiveMessage", messageDto);

        return Ok(messageDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetByChannel(Guid channelId)
    {
        var messages = new List<MessageDto>();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT m.message_id, m.content, m.created_at, m.user_id, u.name, u.avatar_url, m.channel_id
            FROM messages AS m
            JOIN users AS u ON u.user_id = m.user_id
            WHERE m.channel_id = @channel_id
            ORDER BY m.created_at
            """, connection);
        command.Parameters.AddWithValue("channel_id", channelId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            messages.Add(ReadMessage(reader));

        return Ok(messages);
    }

    private static MessageDto ReadMessage(NpgsqlDataReader reader) => new()
    {
        MessageId = reader.GetGuid(0),
        Content = reader.GetString(1),
        CreatedAt = reader.GetDateTime(2),
        UserId = reader.GetGuid(3),
        UserName = reader.GetString(4),
        AvatarUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
        ChannelId = reader.GetGuid(6)
    };
}
