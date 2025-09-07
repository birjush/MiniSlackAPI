namespace Mini_Slack_API.models
{
    public class WorkspaceDto
    {
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UserDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChannelDto
    {
        public Guid ChannelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class MessageDto
    {
        public Guid MessageId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public Guid ChannelId { get; set; }
    }

    public class UserChannelMapDto
    {
        public Guid UserChannelMapId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid ChannelId { get; set; }
        public string ChannelName { get; set; } = string.Empty;
    }

    public class MessageCreateDto
    {
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

}