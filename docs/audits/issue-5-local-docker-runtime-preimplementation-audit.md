# Issue #5 Local Docker Runtime Pre-Implementation Audit

## 1. Purpose

This audit verifies repository and environment readiness before adding local Docker runtime and SQL Server support for KnowledgeOps-AI Sprint 3. It confirms that Docker and Docker Compose are available, documents current repository state, defines the planned implementation structure, and identifies any risks before implementation begins.

No Docker files, compose files, .env files, .gitignore modifications, source code changes, package installations, migrations, or diagram artifacts are produced by this audit.

---

## 2. Classification

```text
Classification
- Task type: Pre-implementation audit
- Prompt level: Level 3
- Related sprint/issue: Sprint 3 / Issue #5
- Scope: Audit-only / Local runtime readiness
- Primary affected area: Docker Compose, SQL Server local runtime,
  safe environment configuration, local development documentation
- Security or organization-scope impact: Configuration and secret-handling only;
  no real credentials may be committed; .env must remain gitignored; .env.example
  must contain safe placeholder values only
- AI/RAG impact: None directly; fake-provider configuration path documented
  but not implemented; no live provider requirement
- Data or migration impact: SQL Server container only; no EF Core schema,
  migrations, or database initialization scripts

Reason
- Issue #5 crosses Docker configuration, environment variable conventions,
  gitignore behavior, local runtime documentation, and SQL Server startup —
  all of which interact with the accepted decisions in ADR-002, ADR-005,
  ADR-006, ADR-008, and the DevOps strategy in docs/18-deployment-and-devops.md.
  Level 3 is required because secret-handling rules, the fake-provider path,
  and multi-layer local runtime documentation all demand cross-context review.

Required Context
- Agent context files: 00-agent-operating-protocol.md, 10-issue-execution-template.md,
  12-prompt-levels.md, 13-prompt-classifier.md, 08-devops-context.md
- Canonical documents: docs/18-deployment-and-devops.md, docs/11-architecture-overview.md,
  docs/21-implementation-roadmap.md, docs/22-implementation-guardrails.md
- ADRs: ADR-002-use-sql-server.md, ADR-005-use-entity-framework-core.md,
  ADR-006-use-azure-openai-compatible-provider-abstraction.md,
  ADR-008-use-asynchronous-document-processing.md
- Progress files: current-state.md, decisions-log.md, open-risks.md, completed-issues.md
- Source/config/test files inspected: .gitignore, frontend/.gitignore,
  src/KnowledgeOps.Api/Properties/launchSettings.json,
  src/KnowledgeOps.Api/appsettings.json,
  src/KnowledgeOps.Worker/appsettings.json,
  frontend/package.json, repository root listing

Recommended Subagents
- architecture-auditor: Confirm Docker Compose structure is consistent
  with Clean Architecture layering and ADR decisions.
- backend-implementation-agent: Implement docker-compose.yml and .env.example.
- frontend-implementation-agent: Confirm frontend local startup conventions.
- testing-agent: Define smoke validation procedure.
- verification-agent: Final validation pass.
- database-agent: Not required; SQL Server container setup is straightforward;
  no schema, migrations, or volume complexity discovered.

Validation
- docker compose config (validate docker-compose.yml syntax)
- docker compose up -d sqlserver (container start)
- docker compose ps (confirm running)
- docker compose logs sqlserver (confirm healthy)
- docker compose down (clean stop)
- Confirm .env not committed (git status)
- Confirm .env.example committed safely (git diff --check)
- Confirm .local/ ignored and not committed

Escalation Or Blockers
- No blockers found. Docker 29.4.3 and Docker Compose v5.1.3 confirmed available.
  Docker daemon confirmed running. See Section 5.
```

---

## 3. Files And Context Reviewed

### Agent Harness Files Reviewed

- `docs/agents/00-agent-operating-protocol.md`
- `docs/agents/10-issue-execution-template.md`
- `docs/agents/12-prompt-levels.md`
- `docs/agents/13-prompt-classifier.md`
- `docs/agents/08-devops-context.md`

### Progress Files Reviewed

- `docs/agents/progress/current-state.md` — Sprint 2 complete; Issue #4 Angular scaffold verified.
- `docs/agents/progress/decisions-log.md` — 14 entries; Angular 21 Vitest, port 5194, environment file conventions recorded.
- `docs/agents/progress/open-risks.md` — 5 open risks; Sprint 2 disposition added.
- `docs/agents/progress/completed-issues.md` — Issues #2, #3, #4 complete; Issue #5 not started.

### Canonical Docs Reviewed

- `docs/18-deployment-and-devops.md` — Docker strategy (Section 7), secrets strategy (Section 11), local environment (Section 3).
- `docs/21-implementation-roadmap.md` — Sprint 3 scope (lines 212–225 per user selection).
- `docs/22-implementation-guardrails.md` — Implementation workflow, canonical sources.
- `docs/11-architecture-overview.md` — Architecture constraints and goals.

### ADRs Reviewed

- `docs/decisions/ADR-002-use-sql-server.md` — Accepted; SQL Server is the required database.
- `docs/decisions/ADR-005-use-entity-framework-core.md` — Accepted; EF Core for persistence. Infrastructure layer only.
- `docs/decisions/ADR-006-use-azure-openai-compatible-provider-abstraction.md` — Accepted; fake providers for tests; no live AI provider required.
- `docs/decisions/ADR-008-use-asynchronous-document-processing.md` — Accepted; background worker required for document processing.

### Repository Files / Folders Inspected

- Repository root listing (no Docker files, no .env files, no .local/ found)
- `.gitignore` (root, 430 lines — Visual Studio template)
- `frontend/.gitignore` (Angular template)
- `src/KnowledgeOps.Api/Properties/launchSettings.json`
- `src/KnowledgeOps.Api/appsettings.json`
- `src/KnowledgeOps.Worker/appsettings.json`
- `frontend/package.json` (scripts section)

---

## 4. Repository State

| Item | Finding |
| --- | --- |
| `docker-compose.yml` | Not present — clean slate. |
| `compose.yaml` | Not present. |
| `Dockerfile*` (any) | Not present — clean slate. |
| `.env` | Not present. |
| `.env.example` | Not present. |
| `.dockerignore` | Not present. |
| `.gitignore` (root) | Present (430 lines). Visual Studio template. See Section 11 for gap analysis. |
| `frontend/.gitignore` | Present. Angular template. Covers `/dist`, `/node_modules`, `/.angular/cache`. |
| `.local/` folder | Not present. |
| Local storage folders | None exist yet. |
| Backend scaffold (Issue #3) | Present and validated. `KnowledgeOpsAI.sln`, all `src/KnowledgeOps.*` projects, all `tests/KnowledgeOps.*` projects. `appsettings.json` and `appsettings.Development.json` exist in both API and Worker. |
| Frontend scaffold (Issue #4) | Present and validated. `frontend/` directory with Angular 21 shell, environment files, placeholder routes, core services/guards/interceptor. |
| Conflicts | None. Backend and frontend scaffolds are clean and do not introduce Docker or .env conventions that would conflict with Issue #5 implementation. |

---

## 5. Docker Readiness

| Check | Result |
| --- | --- |
| `docker --version` | `Docker version 29.4.3, build 055a478` |
| `docker compose version` | `Docker Compose version v5.1.3` |
| Docker daemon reachable | Yes — `docker info` reported server version `29.4.3` |
| SQL Server 2022 image support | Confirmed: `mcr.microsoft.com/mssql/server:2022-latest` requires Docker Engine 20.10+. Engine 29.4.3 satisfies this. |
| Linux containers required | SQL Server Linux container requires Linux container mode. Docker Desktop on Windows must be in Linux container mode. This is the standard default. |
| Implementation readiness | **READY FOR IMPLEMENTATION** |
| Blockers | None. |
| Cautions | None. SQL Server container on Windows requires Docker Desktop in Linux container mode. This is the default Docker Desktop configuration; no additional setup is needed unless the developer has switched to Windows containers. Document this assumption in `docs/local-development.md`. |

---

## 6. Local Port And Runtime Conventions

| Service | Port | Source | Notes |
| --- | --- | --- | --- |
| SQL Server | `localhost:1433` | Standard SQL Server port; no conflict detected in repository. | Docker Compose maps `1433:1433`. If a SQL Server Developer Edition is running locally on port 1433, a conflict may occur — document the `KNOWLEDGEOPS_SQL_PORT` override in `.env.example`. |
| API (HTTP) | `http://localhost:5194` | `src/KnowledgeOps.Api/Properties/launchSettings.json` → `http` profile | Confirmed. Matches `environment.development.ts` `apiBaseUrl`. |
| API (HTTPS) | `https://localhost:7136` | `launchSettings.json` → `https` profile | Optional; HTTP is the default local profile. |
| Frontend | `http://localhost:4200` | Angular CLI `ng serve` default; `frontend/package.json` `start` → `ng serve` | Confirmed. No custom port configured. |
| Worker | No public port | Background service; no HTTP listener in Issue #3 scaffold. | Worker communicates via database only. |
| Conflicts | None detected. | All four services use distinct ports. | If a native SQL Server instance runs on port 1433, document override steps. |

---

## 7. Scaffold Decisions Confirmed

All of the following decisions have been accepted per the Issue #5 task specification and are confirmed consistent with the canonical documents reviewed:

| Decision | Status |
| --- | --- |
| Hybrid runtime is primary: SQL Server via Docker Compose; API/Worker via `dotnet run`; frontend via Angular CLI. | Confirmed — aligns with `docs/18-deployment-and-devops.md` Section 3.5 Option A. |
| `docker-compose.yml` contains SQL Server service only for Issue #5. | Confirmed — no app container justification found; deferred to a later sprint. |
| No API/Worker/Frontend Dockerfiles in Issue #5. | Confirmed — no Dockerfile scaffold exists; creation is explicitly out of scope. |
| SQL Server image: `mcr.microsoft.com/mssql/server:2022-latest`. | Confirmed — matches `docs/18-deployment-and-devops.md` Section 7.3 and ADR-002. |
| `.env.example` only — no `.env` committed. | Confirmed — aligns with `docs/18-deployment-and-devops.md` Section 11.2. |
| `.env` must remain gitignored. | Confirmed — root `.gitignore` line 12 has `*.env` which covers `.env`. See Section 11. |
| Local document storage convention: `.local/storage/documents/`. | Confirmed — not present in repository; must be gitignored during implementation. |
| Local development documentation: `docs/local-development.md`. | Confirmed — not present; will be created during implementation. |
| No EF Core, no migrations, no init SQL scripts. | Confirmed — SQL Server container starts empty; EF/migrations deferred to later sprint. |
| No CI/GitHub Actions. | Confirmed — out of scope. |
| No Azure deployment. | Confirmed — out of scope. |
| Fake-provider path documented, not implemented. | Confirmed — ADR-006 supports fake providers; documentation of `KNOWLEDGEOPS_AI_PROVIDER_MODE=Fake` variable is appropriate. |
| No real secrets committed. | Confirmed — placeholder values only in `.env.example`. |

---

## 8. Planned Files And Changes

| File | Action | Notes |
| --- | --- | --- |
| `docker-compose.yml` | Create | SQL Server service only. See Section 10. |
| `.env.example` | Create | Safe placeholder values only. See Section 9. |
| `docs/local-development.md` | Create | Full local startup/shutdown/reset documentation. See Section 13. |
| `.gitignore` (root) | Update | Add `.env.local`, `.env.*.local`, `.local/`, and `docker-data/` entries; confirm `.env` is covered. See Section 11. |
| `docs/agents/progress/current-state.md` | Update | During implementation: reflect Sprint 3 start. |
| `docs/agents/progress/decisions-log.md` | Update | During implementation: record Docker Compose and .env.example decisions. |
| `docs/agents/progress/open-risks.md` | Update | During implementation: note port 1433 conflict caution if applicable. |
| `docs/agents/progress/completed-issues.md` | Update | After implementation: record Issue #5 as complete. |

Files that must NOT be created during implementation:
- Any `Dockerfile.*` or `src/*/Dockerfile`
- `docker-compose.override.yml` (not needed for SQL-only scope)
- Any EF Core migration files
- Any SQL initialization scripts
- GitHub Actions workflows
- `.env` (the live file — only `.env.example` is committed)

---

## 9. .env.example Plan

The following variables are proposed for `.env.example`. All values are safe, clearly marked as local placeholders, and contain no real secrets.

```dotenv
# KnowledgeOps-AI Local Development Environment
# Copy this file to .env before starting the local stack:
#   cp .env.example .env
# Edit .env to set a strong local password before first use.
# NEVER commit .env to source control.

# ----------------------------------------------------------
# SQL Server (Docker Compose)
# ----------------------------------------------------------

# SA password for the local SQL Server container.
# Must meet SQL Server complexity requirements (>=8 chars,
# uppercase, lowercase, digit, symbol).
# This placeholder is NOT a real password.
KNOWLEDGEOPS_SQL_PASSWORD=Change_this_local_password_123!

# SQL Server port. Change if port 1433 is already in use locally.
KNOWLEDGEOPS_SQL_PORT=1433

# Local database name created during EF migrations (future sprint).
KNOWLEDGEOPS_SQL_DATABASE=KnowledgeOpsLocal

# ----------------------------------------------------------
# Connection String (for future EF Core migration sprint)
# ----------------------------------------------------------

# Full local connection string using the variables above.
# Not required until EF Core migrations are introduced.
# ConnectionStrings__DefaultConnection=Server=localhost,${KNOWLEDGEOPS_SQL_PORT};Database=${KNOWLEDGEOPS_SQL_DATABASE};User Id=sa;Password=${KNOWLEDGEOPS_SQL_PASSWORD};TrustServerCertificate=True;

# ----------------------------------------------------------
# AI Provider (fake mode for local development and CI)
# ----------------------------------------------------------

# Set to "Fake" for local development without a real AI provider.
# Set to "AzureOpenAI" or "OpenAI" only when a live provider is
# manually configured for optional end-to-end testing.
KNOWLEDGEOPS_AI_PROVIDER_MODE=Fake

# Azure OpenAI or OpenAI keys are NOT required for Fake mode.
# Do not add real keys to this file.
# KNOWLEDGEOPS_AZURE_OPENAI_ENDPOINT=
# KNOWLEDGEOPS_AZURE_OPENAI_API_KEY=
# KNOWLEDGEOPS_OPENAI_API_KEY=
```

Notes:
- The connection string line is commented out because EF Core is not implemented in Issue #5. It serves as a forward reference for future sprints.
- The AI provider lines are commented to emphasize that real credentials must never be committed.
- The strong-password requirement for SQL Server SA accounts (minimum 8 characters, mixed case, digit, symbol) is met by the placeholder. Developers must replace this with their own local value.

---

## 10. Docker Compose Plan

The `docker-compose.yml` should define one service only: `sqlserver`.

Planned content (prose description — do not create during audit):

**Service: `sqlserver`**
- Image: `mcr.microsoft.com/mssql/server:2022-latest`
- Container name: optional, omit unless useful for local tooling
- Environment:
  - `ACCEPT_EULA: "Y"` (required by SQL Server licensing)
  - `MSSQL_SA_PASSWORD: ${KNOWLEDGEOPS_SQL_PASSWORD}` (from `.env`)
  - `MSSQL_PID: Developer` (SQL Server Developer Edition — free for dev/test)
- Ports: `"${KNOWLEDGEOPS_SQL_PORT:-1433}:1433"` (allows override via env var)
- Volumes: `sqlserver-data:/var/opt/mssql` (named volume for data persistence across restarts)
- Restart: `unless-stopped` (appropriate for local development; ensures SQL Server restarts automatically after Docker restarts)
- Health check: optional but recommended — use `sqlcmd` probe to confirm the container is ready before dependent services start. Since no app containers are in scope, a health check is informational only in Issue #5.

**Named volumes:**
```yaml
volumes:
  sqlserver-data:
```

**No application services** (API, Worker, Frontend) in this `docker-compose.yml`. Hybrid mode (Option A from `docs/18-deployment-and-devops.md`) is the primary local runtime approach for Sprint 3.

**Version field:** Omit the `version:` key. Docker Compose v2+ (v5.1.3 is confirmed) does not require it and emits a deprecation warning when present.

Planned YAML sketch (safe, no credentials — for audit reference only):

```yaml
# docker-compose.yml
# Start with: docker compose up -d sqlserver
# Stop with:  docker compose down
# Reset data: docker compose down -v  (removes the sqlserver-data volume)

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${KNOWLEDGEOPS_SQL_PASSWORD}"
      MSSQL_PID: "Developer"
    ports:
      - "${KNOWLEDGEOPS_SQL_PORT:-1433}:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    restart: unless-stopped

volumes:
  sqlserver-data:
```

---

## 11. .gitignore Plan

### Root `.gitignore` Analysis

| Pattern Needed | Current State | Action Required |
| --- | --- | --- |
| `.env` | Covered by `*.env` (line 12). `*` matches zero chars in git glob, so `.env` is matched. | Recommend adding explicit `.env` entry for clarity during implementation, alongside the existing `*.env`. |
| `.env.local` | NOT covered. `.env.local` ends in `.local`, not `.env`. | Add `.env.local` during implementation. |
| `.env.*.local` | NOT covered. | Add `.env.*.local` during implementation. |
| `.local/` | NOT covered. No `.local` directory entry exists. | Add `.local/` during implementation. |
| `docker-data/` | NOT covered. | Add `docker-data/` during implementation (future-proofing for local Docker volume bind mounts). |
| `.env.example` | NOT ignored (ends in `.example`, not `.env`). | No action needed — `.env.example` must be tracked by git. No exception needed. |
| `node_modules/` | Covered (line 317). | No action needed. |
| `dist/` | NOT explicitly present in root `.gitignore`, but frontend `/dist` is covered by `frontend/.gitignore`. | No action needed at root level. |
| `/.angular/` | Covered by `frontend/.gitignore` (`/.angular/cache`). | No action needed at root level. |

### Entries To Add During Implementation

Add these lines to the root `.gitignore` in a clearly labeled block:

```gitignore
# KnowledgeOps-AI local runtime
.env
.env.local
.env.*.local
.local/
docker-data/
```

Note: `.env.example` must NOT be in `.gitignore` — it is intentionally committed as a safe template.

---

## 12. Local Document Storage Plan

- Convention: `.local/storage/documents/`
- Location: repository root (`.local/` at root level)
- Purpose: local filesystem path for uploaded document files during development (before Azure Blob Storage is configured)
- Creation: created manually by the developer, or automatically by the API when running locally
- Git behavior: the entire `.local/` tree will be ignored by `.gitignore` once the entry is added during implementation
- Content rules: no real documents, no confidential content, no production data
- Reset behavior: delete `.local/storage/documents/` contents manually; this does not affect the SQL Server container
- Future: when Azure Blob Storage is configured, this path becomes the local fallback

Documentation in `docs/local-development.md` should include:
- How to create `.local/storage/documents/` manually if needed
- Confirmation that this path is gitignored
- Warning that the directory is not automatically cleaned up

---

## 13. Local Development Documentation Plan

`docs/local-development.md` should be structured as follows:

### Sections

1. **Prerequisites** — Docker Desktop (Linux container mode), .NET 10 SDK (`10.0.204`), Node.js / npm (version from `frontend/package.json` `packageManager` field: `npm@11.12.1`), Angular CLI (project-local via `npx` or `npm`), git.
2. **First-time setup** — Clone repo, copy `.env.example` to `.env`, edit `.env` to set a strong local SA password.
3. **Start SQL Server** — `docker compose up -d sqlserver`, then `docker compose ps` to confirm healthy.
4. **Verify SQL Server** — Port `localhost:1433` reachable; optional `sqlcmd` or SSMS connection test.
5. **Apply database migrations (future sprint)** — Placeholder section noting that EF Core migrations will be added in a later sprint.
6. **Run the API** — `dotnet run --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj`. Confirm API is reachable at `http://localhost:5194/`.
7. **Run the Worker** — `dotnet run --project src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj`. No public port; confirm it starts without errors.
8. **Run the Frontend** — `cd frontend && npm install && npm start`. Confirm Angular dev server at `http://localhost:4200`.
9. **Local port reference table** — SQL Server: 1433, API: 5194, Frontend: 4200, Worker: none.
10. **Stop SQL Server** — `docker compose down`.
11. **Reset SQL Server data** — `docker compose down -v` (removes the `sqlserver-data` named volume; all local data is lost).
12. **Local document storage** — Create `.local/storage/documents/` if needed; contents are gitignored; delete to reset.
13. **Fake AI provider configuration** — Set `KNOWLEDGEOPS_AI_PROVIDER_MODE=Fake` in `.env` (already the default). Real AI provider keys are not required for local development; add them to `.env` only if manually testing live providers.
14. **Troubleshooting** — Port 1433 already in use (change `KNOWLEDGEOPS_SQL_PORT`); Docker daemon not running; SA password complexity error; Angular CLI not found.
15. **Security reminder** — Never commit `.env`. Never commit real passwords, API keys, connection strings, or production URLs. The `.env.example` file contains safe placeholder values only.

---

## 14. Validation Plan For Implementation

Run these commands after implementation to confirm the local stack is functional:

```text
# 1. Validate docker-compose.yml syntax
docker compose config

# 2. Start SQL Server container
docker compose up -d sqlserver

# 3. Confirm container running
docker compose ps

# 4. Confirm SQL Server logs show ready state
docker compose logs sqlserver

# 5. Confirm port is reachable (optional — requires sqlcmd or netcat)
# On Windows: Test-NetConnection -ComputerName localhost -Port 1433

# 6. Stop container
docker compose down

# 7. Confirm .env is not tracked
git status

# 8. Confirm .env.example is tracked safely with no real secrets
git diff --check

# 9. Confirm .local/ is ignored
# Create the directory and verify: git status should show nothing for .local/

# 10. Confirm no forbidden packages added
# Inspect root package.json (does not exist) and frontend/package.json
```

Note: `dotnet run` for API and Worker is NOT required as part of Issue #5 validation because the connection string placeholder in `.env.example` is commented out and no EF Core context is implemented yet. The local startup commands are documented but their functional validation is deferred to the sprint that adds the database schema.

---

## 15. Risks And Blockers

| Risk | Severity | Mitigation | Recommendation |
| --- | --- | --- | --- |
| Port 1433 conflict: a SQL Server Developer Edition instance may already be running on the developer's machine, preventing the container from binding port 1433. | Low | Expose `KNOWLEDGEOPS_SQL_PORT` in `.env.example` so developers can override the host port (e.g., `14330:1433`). Document this in troubleshooting section of `docs/local-development.md`. | Document; no open-risks.md update needed. |
| SQL Server SA password complexity: Docker will fail to start if the `MSSQL_SA_PASSWORD` placeholder is used as-is without being replaced. | Low | The `.env.example` placeholder meets complexity requirements; documentation must instruct developers to copy and edit before starting. | Document; no open-risks.md update needed. |
| Docker Desktop Linux container mode: On Windows, if a developer has switched to Windows containers, the SQL Server Linux image will fail to start. | Low | Document the requirement in prerequisites. Linux containers is the default mode. | Document; no open-risks.md update needed. |
| `.env` gitignore coverage: root `.gitignore` uses `*.env` (glob) rather than an explicit `.env` entry. While git's glob engine covers this, the intent is less clear than an explicit entry. | Very Low | Add an explicit `.env` line alongside existing `*.env` during implementation. | Document in implementation; no open-risks.md update needed. |
| Future connection string security: when EF Core is added, the connection string in `.env` will contain the SA password. Developers must understand that `.env` must never be committed. | Low | Already addressed by `.gitignore` coverage and `.env.example` warning comments. | No new risk; existing secret-handling guidance covers this. |
| No blockers found that prevent implementation. | — | — | — |

No updates to `docs/agents/progress/open-risks.md` are required at audit time. The risks above are documentation cautions, not implementation blockers, and are addressed by the implementation plan itself.

---

## 16. Out Of Scope

The following are explicitly out of scope for Issue #5:

| Out-of-scope item | Deferred to |
| --- | --- |
| Azure deployment | Future sprint |
| Production Docker hardening | Future sprint |
| GitHub Actions CI/CD workflows | Future sprint |
| EF Core schema and migrations | Sprint introducing persistence |
| Database initialization scripts or seed SQL | Sprint introducing persistence |
| Authentication and JWT implementation | Sprint 6 |
| Document upload workflow | Sprints 10–11 |
| RAG orchestration and retrieval workflow | Sprint 20 |
| Live AI provider configuration (Azure OpenAI, OpenAI) | Manual/optional only; never required |
| API/Worker/Frontend Dockerfiles | Future sprint; not recommended for Issue #5 |
| Application containers in `docker-compose.yml` | Not justified; hybrid local mode is sufficient |
| App container Compose profiles | Not justified; deferred |
| Production secrets or real API keys | Never; excluded by harness rules |
| Real customer, employee, or client data | Never |
| Rendered architecture diagram PNGs | Never (Mermaid source is canonical per ADR-009) |
| `docker-compose.override.yml` | Not needed for SQL-only scope |

---

## 17. Readiness Recommendation

**READY FOR IMPLEMENTATION**

All prerequisites are satisfied:
- Docker Engine 29.4.3 confirmed available and daemon running.
- Docker Compose v5.1.3 confirmed available.
- SQL Server 2022 Linux container is supported by Engine 29.4.3.
- Repository is a clean slate (no Docker files, no .env files, no conflicting conventions).
- Backend and frontend scaffolds (Issues #3 and #4) are present and validated — no conflicts.
- Port assignments (5194/API, 4200/Frontend, 1433/SQL Server) are consistent and non-conflicting.
- `.gitignore` gaps are identified and addressable during implementation.
- Implementation scope is tightly bounded to SQL Server container, environment template, gitignore update, and local development documentation.

---

## 18. Recommended Next Step

Generate the implementation prompt for Issue #5 (`chore: add local Docker runtime with SQL Server`) using `docs/agents/10-issue-execution-template.md`. The implementation should follow the exact planned structure from Sections 8–13 above and validate using the commands in Section 14.

The implementation agent should:
1. Create `docker-compose.yml` (SQL Server service only).
2. Create `.env.example` (safe placeholder values only).
3. Update root `.gitignore` (add `.env`, `.env.local`, `.env.*.local`, `.local/`, `docker-data/`).
4. Create `docs/local-development.md` (all sections from Section 13 plan).
5. Run validation commands from Section 14.
6. Update all four progress files.
