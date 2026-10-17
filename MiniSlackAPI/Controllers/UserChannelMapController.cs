using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mini_Slack_API.models;
using Npgsql;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserChannelMapController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;

    public UserChannelMapController(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinChannel(Guid userId, Guid channelId)
    {
        var id = Guid.NewGuid();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO user_channel_maps (id, user_id, channel_id)
            SELECT @id, @user_id, @channel_id
            WHERE EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
              AND EXISTS (SELECT 1 FROM channels WHERE channel_id = @channel_id)
            ON CONFLICT (user_id, channel_id) DO NOTHING
            RETURNING id, user_id, channel_id
            """, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("channel_id", channelId);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new UserChannelMap
            {
                Id = reader.GetGuid(0),
                UserId = reader.GetGuid(1),
                ChannelId = reader.GetGuid(2)
            });
        await reader.CloseAsync();

        await using var check = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM user_channel_maps
                WHERE user_id = @user_id AND channel_id = @channel_id
            )
            """, connection);
        check.Parameters.AddWithValue("user_id", userId);
        check.Parameters.AddWithValue("channel_id", channelId);
        return (bool)(await check.ExecuteScalarAsync() ?? false)
            ? BadRequest("User already in channel")
            : NotFound("User or channel was not found.");
    }

    [HttpDelete("leave")]
    public async Task<IActionResult> LeaveChannel(Guid userId, Guid channelId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            DELETE FROM user_channel_maps
            WHERE user_id = @user_id AND channel_id = @channel_id
            RETURNING id
            """, connection);
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("channel_id", channelId);

        return await command.ExecuteScalarAsync() is null
            ? NotFound()
            : Ok("User removed from channel");
    }

    [HttpGet("members/{channelId}")]
    public async Task<IActionResult> GetMembers(Guid channelId)
    {
        var members = new List<object>();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT u.user_id, u.name, u.avatar_url
            FROM user_channel_maps AS m
            JOIN users AS u ON u.user_id = m.user_id
            WHERE m.channel_id = @channel_id
            ORDER BY u.name
            """, connection);
        command.Parameters.AddWithValue("channel_id", channelId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            members.Add(new
            {
                UserId = reader.GetGuid(0),
                Name = reader.GetString(1),
                AvatarUrl = reader.IsDBNull(2) ? null : reader.GetString(2)
            });

        return Ok(members);
    }

    [HttpGet("{channelId}")]
    public async Task<IActionResult> GetByChannel(Guid channelId)
    {
        var userMaps = new List<UserChannelMapDto>();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT m.id, m.user_id, u.name, m.channel_id, c.name
            FROM user_channel_maps AS m
            JOIN users AS u ON u.user_id = m.user_id
            JOIN channels AS c ON c.channel_id = m.channel_id
            WHERE m.channel_id = @channel_id
            ORDER BY u.name
            """, connection);
        command.Parameters.AddWithValue("channel_id", channelId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            userMaps.Add(new UserChannelMapDto
            {
                UserChannelMapId = reader.GetGuid(0),
                UserId = reader.GetGuid(1),
                UserName = reader.GetString(2),
                ChannelId = reader.GetGuid(3),
                ChannelName = reader.GetString(4)
            });

        return Ok(userMaps);
    }
}
