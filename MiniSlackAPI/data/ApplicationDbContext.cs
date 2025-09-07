using Microsoft.EntityFrameworkCore;
using Mini_Slack_API.models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Workspace> Workspaces { get; set; } = null!;
    public DbSet<Channel> Channels { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<UserChannelMap> UserChannelMaps { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User ↔ Workspace (1 → many)
        modelBuilder.Entity<Workspace>()
            .HasOne(w => w.Owner)
            .WithMany(u => u.Workspaces)
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Channel ↔ Workspace (1 → many)
        modelBuilder.Entity<Channel>()
            .HasOne(c => c.Workspace)
            .WithMany(w => w.Channels)
            .HasForeignKey(c => c.WorkspaceId);

        // Message ↔ Channel/User (many → 1)
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Channel)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChannelId);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.User)
            .WithMany(u => u.Messages)
            .HasForeignKey(m => m.UserId);

        // UserChannelMap (many-to-many)
        modelBuilder.Entity<UserChannelMap>()
            .HasOne(uc => uc.User)
            .WithMany(u => u.UserChannelMaps)
            .HasForeignKey(uc => uc.UserId);

        modelBuilder.Entity<UserChannelMap>()
            .HasOne(uc => uc.Channel)
            .WithMany(c => c.UserChannelMaps)
            .HasForeignKey(uc => uc.ChannelId);
    }
}