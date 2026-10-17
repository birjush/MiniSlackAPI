using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mini_Slack_API.models;
using Npgsql;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChannelsController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;

    public ChannelsController(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    [HttpGet("workspace/{workspaceId}")]
    public async Task<IActionResult> GetByWorkspace(Guid workspaceId)
    {
        var channels = new List<ChannelDto>();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT c.channel_id, c.name, c.workspace_id, w.name, c.created_at
            FROM channels AS c
            JOIN workspaces AS w ON w.workspace_id = c.workspace_id
            WHERE c.workspace_id = @workspace_id
            ORDER BY c.created_at
            """, connection);
        command.Parameters.AddWithValue("workspace_id", workspaceId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            channels.Add(ReadChannel(reader));

        return Ok(channels);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChannelDto channelDto)
    {
        var channelId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            WITH new_channel AS (
                INSERT INTO channels (channel_id, name, workspace_id, is_private, created_at)
                SELECT @channel_id, @name, @workspace_id, false, @created_at
                WHERE EXISTS (
                    SELECT 1 FROM workspaces WHERE workspace_id = @workspace_id
                )
                RETURNING channel_id, name, workspace_id, created_at
            )
            SELECT c.channel_id, c.name, c.workspace_id, w.name, c.created_at
            FROM new_channel AS c
            JOIN workspaces AS w ON w.workspace_id = c.workspace_id
            """, connection);
        command.Parameters.AddWithValue("channel_id", channelId);
        command.Parameters.AddWithValue("name", channelDto.Name);
        command.Parameters.AddWithValue("workspace_id", channelDto.WorkspaceId);
        command.Parameters.AddWithValue("created_at", createdAt);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync()
            ? Ok(ReadChannel(reader))
            : NotFound("Workspace was not found.");
    }

    private static ChannelDto ReadChannel(NpgsqlDataReader reader) => new()
    {
        ChannelId = reader.GetGuid(0),
        Name = reader.GetString(1),
        WorkspaceId = reader.GetGuid(2),
        WorkspaceName = reader.GetString(3),
        CreatedAt = reader.GetDateTime(4)
    };
}
