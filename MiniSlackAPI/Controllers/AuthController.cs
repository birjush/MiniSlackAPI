using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Mini_Slack_API.models;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IConfiguration _config;

    public AuthController(NpgsqlDataSource dataSource, IConfiguration config)
    {
        _dataSource = dataSource;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        user.UserId = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;

        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO users (user_id, name, email, password_hash, avatar_url, created_at)
            VALUES (@user_id, @name, @email, @password_hash, @avatar_url, @created_at)
            ON CONFLICT (email) DO NOTHING
            RETURNING user_id
            """, connection);
        command.Parameters.AddWithValue("user_id", user.UserId);
        command.Parameters.AddWithValue("name", user.Name);
        command.Parameters.AddWithValue("email", user.Email);
        command.Parameters.AddWithValue("password_hash", user.PasswordHash);
        command.Parameters.AddWithValue("avatar_url", (object?)user.AvatarUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", user.CreatedAt);

        if (await command.ExecuteScalarAsync() is null)
            return BadRequest("Email already exists.");

        return Ok(new UserDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT user_id, name, email, password_hash, avatar_url, created_at
            FROM users
            WHERE email = @email
            """, connection);
        command.Parameters.AddWithValue("email", req.Email);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Unauthorized("Invalid email or password");

        var user = new User
        {
            UserId = reader.GetGuid(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            AvatarUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt = reader.GetDateTime(5)
        };

        if (user.PasswordHash != req.Password)
            return Unauthorized("Invalid email or password");

        return Ok(new
        {
            token = GenerateJwtToken(user),
            user = new UserDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt
            }
        });
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name)
        };

        var jwtSettings = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            jwtSettings["Key"] ?? throw new InvalidOperationException("JWT key is not configured.")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            jwtSettings["Issuer"],
            jwtSettings["Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
