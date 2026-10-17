using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Npgsql;

[Authorize] // require JWT
public class MessagesHub : Hub
{
    private readonly NpgsqlDataSource _dataSource;

    public MessagesHub(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task SendMessage(Guid channelId, string content)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            throw new HubException("The authenticated user id is invalid.");
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

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
        command.Parameters.AddWithValue("user_id", userGuid);
        command.Parameters.AddWithValue("content", content);
        command.Parameters.AddWithValue("created_at", createdAt);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new HubException("The requested channel or authenticated user was not found.");

        var result = new
        {
            MessageId = reader.GetGuid(0),
            Content = reader.GetString(1),
            CreatedAt = reader.GetDateTime(2),
            UserId = reader.GetGuid(3),
            UserName = reader.GetString(4),
            AvatarUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
            ChannelId = reader.GetGuid(6)
        };
        await reader.CloseAsync();

        await Clients.Group($"channel_{channelId}")
            .SendAsync("ReceiveMessage", result);
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