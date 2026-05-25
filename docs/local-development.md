# Local Development Guide

## Purpose

This guide describes how to run KnowledgeOps-AI locally for development and testing. It covers the SQL Server container, API, Worker, and Angular frontend startup, local document storage conventions, fake AI provider configuration, and safe environment setup.

The local runtime uses a hybrid approach:

- **SQL Server** runs in Docker Compose.
- **API** and **Worker** run locally via `dotnet run`.
- **Angular frontend** runs via the Angular CLI.

This approach keeps the local stack easy to start without requiring application containers.

---

## Prerequisites

Before starting, ensure the following are installed:

| Tool | Required Version | Notes |
| --- | --- | --- |
| .NET SDK | 10.0.204 (pinned in `global.json`) | Run `dotnet --version` to confirm. |
| Node.js / npm | npm 11.12.1 (pinned in `frontend/package.json`) | Run `node --version` and `npm --version`. |
| Docker Desktop or Docker Engine | 20.10 or newer | Engine 29.4.3 confirmed working. |
| Docker Compose | v2.0 or newer | `docker compose version` (note: plugin form, not `docker-compose`). |

**Windows note:** Docker Desktop must be in **Linux container mode** to run the SQL Server Linux image. Linux containers is the Docker Desktop default. If you have switched to Windows containers, switch back before starting.

---

## Local Runtime Shape

```
SQL Server    →  Docker Compose (port 1433)
API           →  dotnet run     (port 5194)
Worker        →  dotnet run     (no public port)
Frontend      →  npm start      (port 4200)
```

| Service | URL / Port | Notes |
| --- | --- | --- |
| SQL Server | `localhost:1433` | Container managed by Docker Compose. |
| API | `http://localhost:5194` | `dotnet run` from project root. |
| API base | `http://localhost:5194/api/v1` | Used by Angular frontend `environment.development.ts`. |
| Frontend | `http://localhost:4200` | Angular CLI dev server. |
| Worker | No public port | Background service; communicates via database only. |

---

## Environment Setup

### 1. Copy the environment template

```bash
cp .env.example .env
```

### 2. Edit `.env`

Open `.env` and change the SA password to a strong local value:

```
KNOWLEDGEOPS_SQL_PASSWORD=Your_strong_local_password_123!
```

SQL Server requires a password that meets complexity rules:
- Minimum 8 characters
- Mix of uppercase, lowercase, digit, and symbol

**Never commit `.env` to source control.** It is excluded by `.gitignore`. The `.env.example` file in the repository is a safe template with placeholder values only.

---

## Start SQL Server

```bash
docker compose up -d sqlserver
```

Confirm the container is running:

```bash
docker compose ps
```

SQL Server may take 15–30 seconds to become ready on first start. If needed, inspect the startup logs:

```bash
docker compose logs sqlserver
```

Look for a line similar to:

```
SQL Server is now ready for client connections.
```

---

## Apply Database Migrations

After SQL Server is running, apply EF Core migrations to create the schema and load the fictional demo personas:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=<your-local-password>;TrustServerCertificate=True;Encrypt=True"

dotnet tool run dotnet-ef database update `
  --project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj `
  --startup-project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj
```

Replace `<your-local-password>` with the SA password you set in `.env`.

This creates the `KnowledgeOpsLocal` database, applies all migrations, and inserts the fictional seed organizations and personas (two organizations, seven users, seven role assignments). No passwords are seeded.

See [docs/demo-data.md](demo-data.md) for the full list of seeded organizations, users, and roles, plus reset instructions and safety rules.

---

## Run API Locally

From the repository root:

```bash
dotnet run --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

The API starts at `http://localhost:5194`.

Environment is set to `Development` by the `launchSettings.json` HTTP profile. No connection string is required until EF Core persistence is introduced in a later sprint.

---

## Run Worker Locally

From the repository root:

```bash
dotnet run --project src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj
```

The Worker has no public port. It communicates with the database only. No database connection is required until EF Core persistence is introduced in a later sprint.

---

## Run Frontend Locally

```bash
cd frontend
npm install       # only needed on first run or after dependency changes
npm start
```

The Angular dev server starts at `http://localhost:4200`. It proxies API calls to `http://localhost:5194/api/v1` as configured in `frontend/src/environments/environment.development.ts`.

---

## Local Document Storage

Uploaded documents are stored in the local filesystem during development. The convention is:

```
.local/storage/documents/
```

This directory is at the repository root. It is excluded from git by `.gitignore` (the entire `.local/` tree is ignored). It may be created manually when needed:

```bash
mkdir -p .local/storage/documents
```

**No real documents, confidential content, or production data should be placed in this directory.**

Azure Blob Storage will replace this path in a future production sprint.

---

## Fake Provider Configuration

For local development and CI, the AI provider is set to `Fake` mode. No real Azure OpenAI or OpenAI API keys are required.

The `.env.example` template sets:

```
KNOWLEDGEOPS_AI_PROVIDER_MODE=Fake
```

With `Fake` mode:
- Embedding generation returns deterministic fake vectors.
- AI answer generation returns deterministic fake responses.
- No external API calls are made.
- No API keys or provider credentials are needed.

To test with a real AI provider, add your credentials to `.env` only (never to `.env.example`) and change:

```
KNOWLEDGEOPS_AI_PROVIDER_MODE=AzureOpenAI
# or
KNOWLEDGEOPS_AI_PROVIDER_MODE=OpenAI
```

Real provider configuration is optional and is not required for local smoke validation.

---

## Smoke Validation

Use these commands to confirm the local stack is working:

```bash
# 1. Validate docker-compose.yml syntax
docker compose config

# 2. Start SQL Server
docker compose up -d sqlserver

# 3. Confirm container is running
docker compose ps

# 4. Check SQL Server startup logs (if needed)
docker compose logs sqlserver

# 5. Optionally test SQL Server port (Windows PowerShell)
Test-NetConnection -ComputerName localhost -Port 1433

# 6. (Optional) Start API — expect http://localhost:5194 to respond
dotnet run --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj

# 7. (Optional) Start Worker — expect it to start without errors
dotnet run --project src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj

# 8. (Optional) Start Frontend — expect http://localhost:4200 to serve the app
cd frontend && npm start
```

**Note:** API and Worker may show connection errors until EF Core and the database schema are introduced in a later sprint. SQL Server container startup is the primary validation goal for Sprint 3.

---

## Shutdown

Stop the SQL Server container:

```bash
docker compose down
```

This stops and removes the container but **preserves the `sqlserver-data` named volume** and its data.

---

## Reset Local Runtime

To stop the container and **delete all local SQL Server data**:

```bash
docker compose down -v
```

This removes the `sqlserver-data` named volume. All local database content is permanently deleted. Use this when you want a clean database state.

To reset local document storage:

```bash
rm -rf .local/storage/documents/*
```

---

## Troubleshooting

### Port 1433 already in use

If SQL Server Developer Edition is already running locally on port 1433, the container cannot bind the port.

**Fix:** Change `KNOWLEDGEOPS_SQL_PORT` in your `.env`:

```
KNOWLEDGEOPS_SQL_PORT=14330
```

Docker Compose will then map `14330:1433` on your machine. Update any local connection string accordingly.

### SA password complexity error

SQL Server rejects weak passwords at startup. The container will exit immediately if the password in `.env` does not meet complexity requirements.

**Fix:** Edit `.env` and set a password with at least 8 characters including uppercase, lowercase, digit, and symbol. Then run:

```bash
docker compose down -v
docker compose up -d sqlserver
```

The `-v` flag removes the old volume so SQL Server reinitializes with the new password.

### Docker Desktop is in Windows container mode

The SQL Server image is a Linux container. On Windows, Docker Desktop must be in Linux container mode.

**Fix:** Right-click the Docker Desktop tray icon → "Switch to Linux containers".

### SQL Server takes time to start

On first startup, SQL Server initializes storage which can take 15–30 seconds.

**Fix:** Wait and then re-run `docker compose ps`. If the container exits, check `docker compose logs sqlserver` for the error.

### API port may change

Port 5194 comes from `src/KnowledgeOps.Api/Properties/launchSettings.json`. If this changes in a future sprint (e.g., when Docker Compose profiles are introduced), update `frontend/src/environments/environment.development.ts` accordingly.

---

## Security And Data Rules

- **Never commit `.env`** to source control. It is excluded by `.gitignore`.
- **`.env.example`** contains safe placeholder values only. It may be committed. It contains no real secrets.
- **Never store real passwords**, API keys, Azure credentials, JWT signing keys, or production connection strings in any committed file.
- **Never store real customer, employee, or client data** in `.local/` or in the local SQL Server container.
- **AI provider keys are not required** for local development. Use `KNOWLEDGEOPS_AI_PROVIDER_MODE=Fake`.
- **Local SQL Server data** is for development only. Do not use it to process or store real documents or personal information.
- If you suspect a secret has been committed, rotate it immediately and contact the repository owner.
