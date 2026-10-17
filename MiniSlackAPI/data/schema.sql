CREATE TABLE IF NOT EXISTS users (
    user_id uuid PRIMARY KEY,
    name varchar(100) NOT NULL,
    email varchar(320) NOT NULL UNIQUE,
    password_hash text NOT NULL,
    avatar_url varchar(2048),
    created_at timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS workspaces (
    workspace_id uuid PRIMARY KEY,
    name varchar(100) NOT NULL,
    owner_id uuid NOT NULL REFERENCES users (user_id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS channels (
    channel_id uuid PRIMARY KEY,
    name varchar(100) NOT NULL,
    workspace_id uuid NOT NULL REFERENCES workspaces (workspace_id) ON DELETE CASCADE,
    is_private boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS messages (
    message_id uuid PRIMARY KEY,
    channel_id uuid NOT NULL REFERENCES channels (channel_id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES users (user_id) ON DELETE RESTRICT,
    content text NOT NULL,
    created_at timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS user_channel_maps (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL REFERENCES users (user_id) ON DELETE CASCADE,
    channel_id uuid NOT NULL REFERENCES channels (channel_id) ON DELETE CASCADE,
    CONSTRAINT uq_user_channel_maps_user_id_channel_id UNIQUE (user_id, channel_id)
);

CREATE INDEX IF NOT EXISTS ix_workspaces_owner_id ON workspaces (owner_id);
CREATE INDEX IF NOT EXISTS ix_channels_workspace_id ON channels (workspace_id);
CREATE INDEX IF NOT EXISTS ix_messages_channel_id_created_at ON messages (channel_id, created_at);
CREATE INDEX IF NOT EXISTS ix_messages_user_id ON messages (user_id);
CREATE INDEX IF NOT EXISTS ix_user_channel_maps_channel_id ON user_channel_maps (channel_id);
