using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_Slack_API.models;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require JWT
public class WorkspacesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WorkspacesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ✅ Get all workspaces
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var workspaces = await _context.Workspaces
            .Include(w => w.Owner)
            .Select(w => new WorkspaceDto
            {
                WorkspaceId = w.WorkspaceId,
                Name = w.Name,
                OwnerName = w.Owner.Name,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();

        return Ok(workspaces);
    }

    // ✅ Get workspace by ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Owner)
            .Where(w => w.WorkspaceId == id)
            .Select(w => new WorkspaceDto
            {
                WorkspaceId = w.WorkspaceId,
                Name = w.Name,
                OwnerName = w.Owner.Name,
                CreatedAt = w.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (workspace == null) return NotFound();
        return Ok(workspace);
    }


    //get by user id
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var workspaces = await _context.Workspaces
            .Include(w => w.Owner)
            .Where(w => w.OwnerId == userId)
            .Select(w => new WorkspaceDto
            {
                WorkspaceId = w.WorkspaceId,
                Name = w.Name,
                OwnerName = w.Owner.Name,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();
        return Ok(workspaces);
    }
   


    // ✅ Create workspace
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkspaceDto dto)
    {
        var workspace = new Workspace
        {
            WorkspaceId = Guid.NewGuid(),
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        return Ok(new WorkspaceDto
        {
            WorkspaceId = workspace.WorkspaceId,
            Name = workspace.Name,
            OwnerName = (await _context.Users.FindAsync(workspace.OwnerId))?.Name ?? "",
            CreatedAt = workspace.CreatedAt
        });
    }
}
