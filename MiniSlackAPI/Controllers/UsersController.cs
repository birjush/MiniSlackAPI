using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mini_Slack_API.models;
using Npgsql;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;

    public UsersController(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = new List<UserDto>();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT user_id, name, email, avatar_url, created_at
            FROM users
            ORDER BY created_at
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            users.Add(ReadUser(reader));

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT user_id, name, email, avatar_url, created_at
            FROM users
            WHERE user_id = @user_id
            """, connection);
        command.Parameters.AddWithValue("user_id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Ok(ReadUser(reader)) : NotFound();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, User updatedUser)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            UPDATE users
            SET name = @name, avatar_url = @avatar_url
            WHERE user_id = @user_id
            RETURNING user_id, name, email, avatar_url, created_at
            """, connection);
        command.Parameters.AddWithValue("user_id", id);
        command.Parameters.AddWithValue("name", updatedUser.Name);
        command.Parameters.AddWithValue("avatar_url", (object?)updatedUser.AvatarUrl ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Ok(ReadUser(reader)) : NotFound();
    }

    private static UserDto ReadUser(NpgsqlDataReader reader) => new()
    {
        UserId = reader.GetGuid(0),
        Name = reader.GetString(1),
        Email = reader.GetString(2),
        AvatarUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
        CreatedAt = reader.GetDateTime(4)
    };
}
