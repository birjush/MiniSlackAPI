# MiniSlackAPI

## Run in a VS Code Dev Container

Install the **Dev Containers** extension, open this repository in VS Code, and
run **Dev Containers: Reopen in Container**. The container uses the .NET 9 SDK,
restores the solution when it is created, and installs the C# and REST Client
extensions.

Start the API from the container's terminal:

```sh
dotnet watch --project MiniSlackAPI/MiniSlackAPI.csproj \
  --no-launch-profile --urls http://0.0.0.0:5113
```

Open [http://localhost:5113/swagger](http://localhost:5113/swagger) on the host.
The forwarded port and Development environment are configured by
`.devcontainer/devcontainer.json`.

### Database

The API uses PostgreSQL through parameterized ADO.NET queries with Npgsql; no
ORM or automatic schema migration runs at API startup. The schema SQL is in
`MiniSlackAPI/data/schema.sql`. Apply it to your PostgreSQL database before
starting the API. It creates:

| Table | Purpose and relationships |
| --- | --- |
| `users` | Accounts; email is unique. |
| `workspaces` | Each workspace has one owner (`owner_id` → `users`). |
| `channels` | Channels belong to a workspace (`workspace_id` → `workspaces`). |
| `messages` | Messages belong to a channel and author (`channel_id` → `channels`, `user_id` → `users`). |
| `user_channel_maps` | Channel memberships (`user_id` → `users`, `channel_id` → `channels`); a user can join a channel only once. |

For API development directly on your Mac, ensure your native PostgreSQL server
is running and configure `ConnectionStrings__MiniSlackDb` (or the
`MiniSlackDb` value in `MiniSlackAPI/appsettings.Development.json`) to connect
to it. The default host `localhost` is appropriate when the API also runs
directly on your Mac.

When running the API inside the VS Code Dev Container, `localhost` refers to
the container, not your Mac. Use `host.docker.internal` as the connection
string host to connect to your Mac's PostgreSQL, and ensure PostgreSQL accepts
connections from the container. Keep database credentials in local
configuration or environment variables; use managed secret configuration
outside local development.

Apply the schema manually before starting the API:

```sh
psql "$DATABASE_URL" -f MiniSlackAPI/data/schema.sql
```
