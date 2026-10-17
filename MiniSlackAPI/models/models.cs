namespace Mini_Slack_API.models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Workspace
    {
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = null!;
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Channel
    {
        public Guid ChannelId { get; set; }
        public string Name { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public bool IsPrivate { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }

    public class Message
    {
        public Guid MessageId { get; set; }
        public Guid ChannelId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }

    public class UserChannelMap
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ChannelId { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }


}