using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_Slack_API.models;

[ApiController]
[Route("api/[controller]")]
[Authorize] // ⬅️ Require JWT token
public class UserChannelMapController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserChannelMapController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinChannel(Guid userId, Guid channelId)
    {
        if (await _context.UserChannelMaps.AnyAsync(m => m.UserId == userId && m.ChannelId == channelId))
            return BadRequest("User already in channel");

        var map = new UserChannelMap
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChannelId = channelId
        };

        _context.UserChannelMaps.Add(map);
        await _context.SaveChangesAsync();

        return Ok(map);
    }

    [HttpDelete("leave")]
    public async Task<IActionResult> LeaveChannel(Guid userId, Guid channelId)
    {
        var map = await _context.UserChannelMaps
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ChannelId == channelId);

        if (map == null) return NotFound();

        _context.UserChannelMaps.Remove(map);
        await _context.SaveChangesAsync();

        return Ok("User removed from channel");
    }

    [HttpGet("members/{channelId}")]
    public async Task<IActionResult> GetMembers(Guid channelId)
    {
        var members = await _context.UserChannelMaps
            .Where(m => m.ChannelId == channelId)
            .Include(m => m.User)
            .Select(m => new { m.User.UserId, m.User.Name, m.User.AvatarUrl })
            .ToListAsync();

        return Ok(members);
    }

    [HttpGet("{channelId}")]
    public async Task<IActionResult> GetByChannel(Guid channelId)
    {
        var userMaps = await _context.UserChannelMaps
            .Where(map => map.ChannelId == channelId)
            .Include(map => map.User)
            .Include(map => map.Channel)
            .Select(map => new UserChannelMapDto
            {
                UserChannelMapId = map.Id,
                UserId = map.UserId,
                UserName = map.User.Name,
                ChannelId = map.ChannelId,
                ChannelName = map.Channel.Name
            })
            .ToListAsync();

        return Ok(userMaps);
    }
}
