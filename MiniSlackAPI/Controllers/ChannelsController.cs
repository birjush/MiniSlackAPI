using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_Slack_API.models;

[ApiController]
[Route("api/[controller]")]
[Authorize] // ⬅️ Require JWT token
public class ChannelsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ChannelsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ✅ Get all channels in a workspace
    // GET: api/channels/workspace/{workspaceId}
    [HttpGet("workspace/{workspaceId}")]
    public async Task<IActionResult> GetByWorkspace(Guid workspaceId)
    {
        var channels = await _context.Channels
            .Where(c => c.WorkspaceId == workspaceId)
            .Select(c => new ChannelDto
            {
                ChannelId = c.ChannelId,
                Name = c.Name,
                WorkspaceId = c.WorkspaceId,
                WorkspaceName = c.Workspace.Name,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(channels);
    }

    // ✅ Create a new channel
    // POST: api/channels
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChannelDto channelDto)
    {
        var channel = new Channel
        {
            ChannelId = Guid.NewGuid(),
            Name = channelDto.Name,
            WorkspaceId = channelDto.WorkspaceId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Channels.Add(channel);
        await _context.SaveChangesAsync();

        // return the created channel in DTO form
        return Ok(new ChannelDto
        {
            ChannelId = channel.ChannelId,
            Name = channel.Name,
            WorkspaceId = channel.WorkspaceId,
            WorkspaceName = (await _context.Workspaces.FindAsync(channel.WorkspaceId))?.Name ?? "",
            CreatedAt = channel.CreatedAt
        });
    }
}
