using System.Threading.Channels;

namespace Mini_Slack_API.models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class User
    {
        public Guid UserId { get; set; }   // changed from int → Guid
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<UserChannelMap> UserChannelMaps { get; set; } = new List<UserChannelMap>();
    }


    public class Workspace
    {
        public Guid WorkspaceId { get; set; }   // changed to Guid
        public string Name { get; set; } = null!;
        public Guid OwnerId { get; set; }       // foreign key to User

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User Owner { get; set; } = null!;
        public ICollection<Channel> Channels { get; set; } = new List<Channel>();
    }

    public class Channel
    {
        public Guid ChannelId { get; set; }
        public string Name { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public bool IsPrivate { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Workspace Workspace { get; set; } = null!;
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<UserChannelMap> UserChannelMaps { get; set; } = new List<UserChannelMap>();
    }

    public class Message
    {
        public Guid MessageId { get; set; }
        public Guid ChannelId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Channel Channel { get; set; } = null!;
        public User User { get; set; } = null!;
    }

    public class UserChannelMap
    {
        public Guid Id { get; set; }  // PK
        public Guid UserId { get; set; }
        public Guid ChannelId { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public Channel Channel { get; set; } = null!;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }


}