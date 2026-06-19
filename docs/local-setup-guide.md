# Local Setup Guide

This guide walks through the complete process to run KnowledgeOps-AI locally from scratch,
including database initialization, secret configuration, and first-time admin login.
Follow each section in order.

---

## Prerequisites

| Tool | Required Version | Verify |
|---|---|---|
| .NET SDK | 10.0 (pinned in `global.json`) | `dotnet --version` |
| Node.js / npm | npm 11+ | `npm --version` |
| Docker Desktop | 20.10+ | `docker --version` |
| Docker Compose | v2+ | `docker compose version` |

> **Windows:** Docker Desktop must be in **Linux container mode**. The SQL Server image is a Linux container.
> Right-click the Docker Desktop tray icon → *Switch to Linux containers* if needed.

---

## Part 1 — One-Time Setup

Complete this section once when setting up the project for the first time.

### Step 1 — Clone and configure the environment file

```bash
git clone https://github.com/froblerom/knowledgeops-ai.git
cd knowledgeops-ai
cp .env.example .env
```

Open `.env` and set a strong local password for SQL Server:

```
KNOWLEDGEOPS_SQL_PASSWORD=YourStrong_LocalPassword_123!
```

SQL Server requires a password with at least 8 characters including uppercase, lowercase, digit, and symbol.
Never commit `.env` to source control. Only `.env.example` is committed.

### Step 2 — Generate the JWT signing key (user-secrets)

The API requires a JWT signing key of at least 32 characters. The recommended approach is
`dotnet user-secrets`, which persists the key in your local user profile and survives terminal restarts.

Run this once from the repository root in PowerShell:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:SigningKey" "$jwtKey" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

Expected output: `Successfully saved Jwt:SigningKey to the secret store.`

This stores the signing key in the .NET user secrets store, not in any file inside the repository.
You do not need to repeat this step unless you delete your user secrets or switch machines.

### Step 2b — AI provider mode (optional: OpenAI)

By default, the chat assistant uses the **Demo provider** — a deterministic extractive generator
that requires no API key and is safe for CI and portfolio demonstrations.

If you want to try the optional **OpenAI provider** for a more fluent generative answer,
set the following user-secrets:

```powershell
dotnet user-secrets set "Ai:AnswerProvider" "OpenAI" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
dotnet user-secrets set "Ai:OpenAI:ApiKey" "<your-openai-key>" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

To revert to Demo mode, either remove the user-secrets or set `Ai:AnswerProvider` back to `Demo`:

```powershell
dotnet user-secrets set "Ai:AnswerProvider" "Demo" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

**Security rules:**
- Never commit an API key to source control.
- Never put the API key in `appsettings.json` or `appsettings.Development.json`.
- The API key is never stored in the database or exposed to the Angular frontend.
- The API will fail to start if `Ai:AnswerProvider = OpenAI` but `Ai:OpenAI:ApiKey` is missing.

### Step 2c — AI provider mode (optional: Local Qwen via Ollama)

If you have [Ollama](https://ollama.com) installed, you can run **qwen3:8b** locally as the answer
provider. This does not require an OpenAI account or billing credits.

**Prerequisites:**

1. Install Ollama from https://ollama.com/download
2. Pull the model:

```powershell
ollama pull qwen3:8b
ollama list    # confirm qwen3:8b appears
```

**Smoke test the local endpoint before enabling the provider:**

```powershell
$body = @{
  model    = "qwen3:8b"
  messages = @(
    @{ role = "system"; content = "You are a grounded support assistant. Reply only with the user-provided context." },
    @{ role = "user";   content = "Reply with exactly: ok" }
  )
  stream      = $false
  temperature = 0
} | ConvertTo-Json -Depth 10

Invoke-RestMethod `
  -Uri     "http://localhost:11434/v1/chat/completions" `
  -Method  Post `
  -Headers @{ "Content-Type" = "application/json" } `
  -Body    $body
```

Expected: `choices[0].message.content` contains `"ok"` or a brief answer, confirming the local
endpoint is responding.

**Enable the local provider:**

```powershell
dotnet user-secrets set "Ai:AnswerProvider" "LocalOpenAICompatible" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

No API key is needed for Ollama. The default base URL (`http://localhost:11434/v1`) is already
set in `appsettings.json`. If your Ollama installation uses a different port, override it:

```powershell
dotnet user-secrets set "Ai:LocalOpenAICompatible:BaseUrl" "http://localhost:11434/v1" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

**Revert to Demo mode for stable screenshots:**

```powershell
dotnet user-secrets set "Ai:AnswerProvider" "Demo" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

**Startup confirmation:**

When `LocalOpenAICompatible` is active, the API startup log shows:

```text
AI answer provider: LocalOpenAICompatible Model=qwen3:8b OpenAiConfigured=False
```

And `/api/v1/health/details` (Admin only) shows:

```json
{
  "aiProvider": {
    "answerProvider": "LocalOpenAICompatible",
    "openAiConfigured": false,
    "model": "qwen3:8b",
    "localProviderBaseUrl": "http://localhost:11434/v1"
  }
}
```

**Troubleshooting:**

| Failure code | Cause | Fix |
|---|---|---|
| `ProviderUnavailable` | Ollama is not running | Start Ollama before starting the API |
| `ProviderModelUnavailable` | `qwen3:8b` is not pulled | Run `ollama pull qwen3:8b` |
| `ProviderTimeout` | Local inference is slow | Increase `Ai:LocalOpenAICompatible:TimeoutSeconds` (default 90) |

**Docker note:** If the KnowledgeOps-AI API is running inside Docker, `localhost:11434`
resolves to the container, not the host. Change `BaseUrl` to
`http://host.docker.internal:11434/v1` in that case.

### Step 3 — Start SQL Server

```powershell
docker compose up -d sqlserver
docker compose ps
```

Wait until the `sqlserver` container shows as healthy. First startup may take 15–30 seconds
while SQL Server initializes. If needed, inspect the logs:

```powershell
docker compose logs sqlserver
```

Look for: `SQL Server is now ready for client connections.`

### Step 4 — Apply database migrations

Run the EF Core migrations to create the schema and seed the fictional demo personas.
Set the connection string as a session environment variable first:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=YourStrong_LocalPassword_123!;TrustServerCertificate=True;Encrypt=True"

dotnet tool run dotnet-ef database update `
  --project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj `
  --startup-project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj
```

Replace the password with the value you set in `.env`.

This creates the `KnowledgeOpsLocal` database, applies all migrations, and inserts
two fictional organizations and seven demo users. No passwords are seeded at this stage.

### Step 5 — Bootstrap the Admin password

Seed users have no password by default. To log in for the first time you must generate
a BCrypt hash and write it directly to the database. This is a one-time local setup step.

**5a — Generate a BCrypt hash**

Run the following commands in PowerShell to create a temporary .NET project that
generates the hash. This avoids any external dependency:

```powershell
cd $env:TEMP
Remove-Item -Recurse -Force hashgen -ErrorAction SilentlyContinue
New-Item -ItemType Directory hashgen | Out-Null
cd hashgen

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
"@ | Out-File hashgen.csproj -Encoding utf8

@"
using System;
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("Demo1234!", workFactor: 12));
"@ | Out-File Program.cs -Encoding utf8

dotnet restore
dotnet run
```

Copy the full output. It will look like: `$2a$12$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

**5b — Write the hash to the database**

Navigate back to the repository root, then run the SQL update.
Use **single-quoted outer string** in PowerShell to prevent the `$` characters
in the BCrypt hash from being interpreted as PowerShell variables:

```powershell
cd C:\path\to\knowledgeops-ai

docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "YourStrong_LocalPassword_123!" -C `
  -d KnowledgeOpsLocal `
  -Q 'UPDATE users SET password_hash = ''PASTE_YOUR_HASH_HERE'' WHERE email = ''admin.a@asteria.example.com'''
```

Replace `PASTE_YOUR_HASH_HERE` with the hash from step 5a.
Replace the password with your local `.env` value.
The `-d KnowledgeOpsLocal` flag is required — without it, sqlcmd connects to the `master`
database and the `users` table will not be found.

Expected output: `(1 rows affected)`

---

## Part 2 — Starting the Application

After one-time setup is complete, use these steps each time you start the application.
You need three separate terminal windows.

### Terminal 1 — SQL Server (if not already running)

```powershell
docker compose up -d sqlserver
docker compose ps
```

### Terminal 2 — API

Set the connection string as a session environment variable, then start the API:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=YourStrong_LocalPassword_123!;TrustServerCertificate=True;Encrypt=True"

dotnet run --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

Wait for: `Now listening on: http://localhost:5194`

### Terminal 3 — Worker

Open a new terminal window and run:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=YourStrong_LocalPassword_123!;TrustServerCertificate=True;Encrypt=True"

dotnet run --project src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj
```

The Worker has no public port. It processes uploaded documents in the background.

### Terminal 4 — Frontend

```powershell
cd frontend
npm install    # only required on first run or after dependency changes
npm start
```

Wait for: `Application bundle generation complete.`

Open **http://localhost:4200** in your browser. The login screen should appear.

---

## Part 3 — Logging In

Use the Admin account to log in for the first time:

| Field | Value |
|---|---|
| Email | `admin.a@asteria.example.com` |
| Password | `Demo1234!` (or the password you chose in Step 5) |

Once logged in as Admin, you can create additional demo users from the Admin panel (`/admin`)
using the UI without touching the database again.

See [docs/demo-data.md](demo-data.md) for the full list of seeded organizations, users, and roles.

---

## Considerations

The following issues were encountered during setup and are documented here
so future contributors do not repeat them.

### 1. The `.env` file is not loaded automatically by `dotnet run`

.NET does not read `.env` files natively. `docker compose` reads `.env` for container
configuration, but the API and Worker processes started with `dotnet run` receive
configuration only from environment variables, `appsettings.json`, and `dotnet user-secrets`.

**Consequence:** Any value you place in `.env` (such as connection strings or JWT keys)
will not reach the API unless you also set it as an environment variable in the same
PowerShell session, or store it via `dotnet user-secrets`.

**Solution:** Set `ConnectionStrings__DefaultConnection` as a PowerShell session variable
before each `dotnet run` call (as shown above). Store `Jwt:SigningKey` permanently via
`dotnet user-secrets` so it does not need to be re-set each session.

### 2. PowerShell environment variables are session-scoped

Variables set with `$env:KEY = "value"` in PowerShell exist only for the lifetime of
that terminal session. Opening a new terminal window or restarting PowerShell clears them.

**Consequence:** If you open a new terminal to restart the API, the connection string
variable will be gone and the API will fail with a missing configuration error, even if
an earlier request in the same session worked (because EF Core creates the `DbContext`
lazily per request).

**Solution:** Re-set `$env:ConnectionStrings__DefaultConnection` each time you open
a new terminal window before starting the API or Worker.

### 3. BCrypt hash `$` characters are expanded by PowerShell double-quoted strings

BCrypt hashes start with `$2a$12$` and contain multiple `$` characters throughout.
When this string is placed inside a PowerShell double-quoted string, PowerShell interprets
each `$` as the start of a variable name, silently replacing it with an empty string.

**Consequence:** The hash written to the database is corrupted (e.g., `2a12...` instead
of `$2a$12$...`), causing `BCrypt.Verify` to throw a `SaltParseException` at login.

**Solution:** Use a **single-quoted string** in PowerShell for the `sqlcmd -Q` argument,
and escape internal single quotes with two consecutive single quotes (`''`):

```powershell
# Correct — single-quoted outer string, '' for inner escaping
-Q 'UPDATE users SET password_hash = ''$2a$12$...' WHERE ...'

# Incorrect — $ signs will be expanded to empty strings
-Q "UPDATE users SET password_hash = '$2a$12$...' WHERE ..."
```

### 4. `sqlcmd` connects to `master` by default — specify the database explicitly

When running `sqlcmd` without the `-d` flag, the connection lands on the `master` system
database. The application tables (`users`, `user_roles`, etc.) live in `KnowledgeOpsLocal`.

**Consequence:** SQL commands against `users` fail with `Invalid object name 'users'`
even though the table exists in the correct database.

**Solution:** Always include `-d KnowledgeOpsLocal` in `sqlcmd` commands:

```powershell
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "..." -C -d KnowledgeOpsLocal `
  -Q "..."
```

### 5. The JWT signing key must meet a minimum length requirement

The API validates `Jwt:SigningKey` on startup and rejects values shorter than 32 characters.
The error is thrown lazily on the first request that resolves the `DbContext` (not always
at startup), which can make it appear as an unrelated database error.

**Solution:** Use `dotnet user-secrets` to generate and store a cryptographically random key
as documented in Step 2. This persists across sessions and does not require env vars.

### 6. Document storage path is relative to the project directory, not the repository root

`dotnet run --project <path>` sets the application's working directory to the **project file's
directory** (e.g. `src/KnowledgeOps.Api/`), not the directory from which you run the command.
`Storage:LocalDocumentsPath` is resolved with `Path.GetFullPath()` at runtime, so a relative
path like `.local/storage/documents` would create the storage folder inside
`src/KnowledgeOps.Api/` for the API and inside `src/KnowledgeOps.Worker/` for the Worker —
two different locations on disk.

**Consequence:** Uploaded files are stored by the API in one directory while the Worker looks
for them in a different directory. The Worker would still process (claim and attempt to open
the document) but every document would reach `Failed` status with
`"Document file could not be opened for reading."` instead of `Processed`.

**Solution:** Both config files use `../../.local/storage/documents`, which navigates from
`src/<ProjectName>/` up two levels to the repository root, giving both processes the same
absolute path `<repo-root>/.local/storage/documents/`. This matches the documented convention.
This folder is covered by the `.gitignore` rule `.local/` and is never committed.

If you uploaded documents with the old `.local/storage/documents` path (stored in
`src/KnowledgeOps.Api/.local/`), those files are in the wrong location. Re-upload them after
applying this config fix so the Worker can find them.
