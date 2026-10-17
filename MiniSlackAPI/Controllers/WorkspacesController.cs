using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mini_Slack_API.models;
using Npgsql;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkspacesController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;

    public WorkspacesController(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var workspaces = new List<WorkspaceDto>();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT w.workspace_id, w.name, w.owner_id, u.name, w.created_at
            FROM workspaces AS w
            JOIN users AS u ON u.user_id = w.owner_id
            ORDER BY w.created_at
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            workspaces.Add(ReadWorkspace(reader));

        return Ok(workspaces);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT w.workspace_id, w.name, w.owner_id, u.name, w.created_at
            FROM workspaces AS w
            JOIN users AS u ON u.user_id = w.owner_id
            WHERE w.workspace_id = @workspace_id
            """, connection);
        command.Parameters.AddWithValue("workspace_id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Ok(ReadWorkspace(reader)) : NotFound();
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var workspaces = new List<WorkspaceDto>();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT w.workspace_id, w.name, w.owner_id, u.name, w.created_at
            FROM workspaces AS w
            JOIN users AS u ON u.user_id = w.owner_id
            WHERE w.owner_id = @owner_id
            ORDER BY w.created_at
            """, connection);
        command.Parameters.AddWithValue("owner_id", userId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            workspaces.Add(ReadWorkspace(reader));

        return Ok(workspaces);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkspaceDto dto)
    {
        var workspaceId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            WITH new_workspace AS (
                INSERT INTO workspaces (workspace_id, name, owner_id, created_at)
                SELECT @workspace_id, @name, @owner_id, @created_at
                WHERE EXISTS (SELECT 1 FROM users WHERE user_id = @owner_id)
                RETURNING workspace_id, name, owner_id, created_at
            )
            SELECT w.workspace_id, w.name, w.owner_id, u.name, w.created_at
            FROM new_workspace AS w
            JOIN users AS u ON u.user_id = w.owner_id
            """, connection);
        command.Parameters.AddWithValue("workspace_id", workspaceId);
        command.Parameters.AddWithValue("name", dto.Name);
        command.Parameters.AddWithValue("owner_id", dto.OwnerId);
        command.Parameters.AddWithValue("created_at", createdAt);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync()
            ? Ok(ReadWorkspace(reader))
            : BadRequest("Workspace owner was not found.");
    }

    private static WorkspaceDto ReadWorkspace(NpgsqlDataReader reader) => new()
    {
        WorkspaceId = reader.GetGuid(0),
        Name = reader.GetString(1),
        OwnerId = reader.GetGuid(2),
        OwnerName = reader.GetString(3),
        CreatedAt = reader.GetDateTime(4)
    };
}
