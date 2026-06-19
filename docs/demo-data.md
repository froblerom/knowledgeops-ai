# Demo Data

## Purpose

This document describes the deterministic fictional seed data used for local
development, automated integration tests, and early sprint validation in
KnowledgeOps-AI.

The seed data provides stable, predictable identities across two fictional
organizations and seven fictional users. It supports cross-organization
access-boundary tests, role-coverage tests, and future authentication and
authorization sprint work.

---

## Fictional Data Warning

**All seed data in this repository is entirely fictional.**

- No real customers, employees, clients, or partners are represented.
- No real internal documents are included.
- No real email addresses are used.
- These are not production accounts.
- Fictional email domains (`asteria.example.com`, `boreal.example.com`) use
  the IANA-reserved `example.com` parent domain and will never deliver mail.
- Seed data must not be used to process or store real business data.

---

## Organizations

Two fictional contact center organizations are seeded:

| Organization | Email Domain | Organization Id |
|---|---|---|
| Asteria Support Group | asteria.example.com | `11111111-1111-4111-8111-111111111111` |
| Boreal Contact Services | boreal.example.com | `22222222-2222-4222-8222-222222222222` |

Both organizations are seeded with `Active` status.

---

## Seeded Personas

### Asteria Support Group

Five users cover all five MVP roles within a single organization:

| Display Name | Email | Role | User Id |
|---|---|---|---|
| Agent A | agent.a@asteria.example.com | Agent | `aaaa0001-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| Supervisor A | supervisor.a@asteria.example.com | Supervisor | `aaaa0002-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| KnowledgeAdmin A | knowledgeadmin.a@asteria.example.com | KnowledgeAdmin | `aaaa0003-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| Manager A | manager.a@asteria.example.com | Manager | `aaaa0004-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| Admin A | admin.a@asteria.example.com | Admin | `aaaa0005-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |

### Boreal Contact Services

Two users for cross-organization scope isolation tests:

| Display Name | Email | Role | User Id |
|---|---|---|---|
| Agent B | agent.b@boreal.example.com | Agent | `bbbb0001-bbbb-4bbb-8bbb-bbbbbbbbbbbb` |
| Admin B | admin.b@boreal.example.com | Admin | `bbbb0002-bbbb-4bbb-8bbb-bbbbbbbbbbbb` |

---

## MVP Roles

The five approved MVP technical roles are:

| Role | Description |
|---|---|
| Agent | Contact center agent who asks questions and uses AI answers |
| Supervisor | Team supervisor with access to agent activity |
| KnowledgeAdmin | Manages and uploads the knowledge base documents |
| Manager | Operations manager with broader reporting visibility |
| Admin | Organization administrator who manages users and configuration |

**Non-MVP roles are not seeded and must not be added.** The following roles
are explicitly out of scope for KnowledgeOps-AI MVP:

- QA / Quality Analyst
- Trainer
- Viewer
- Recruiter
- Portfolio Reviewer
- AI Coding Agent
- Compliance Reviewer
- Contact Center Leadership

---

## Credentials And Passwords

**No passwords or authentication credentials are seeded in this release.**

- All seed users have `password_hash = null`.
- Login is not possible for seed users until Sprint 6 introduces the
  authentication layer.
- Sprint 6 (`feat: add authentication and current-user context`) will add safe
  local credential setup for these personas.
- When credentials are introduced, they must use safe fictional values and
  must never be committed to source control.

---

## Intended Testing Use

These personas are designed to support:

- **Cross-organization scope tests**: Agent A (Asteria) and Agent B (Boreal)
  are in separate organizations. Tests can verify that data, documents, chat,
  and retrieval results are isolated per organization.
- **Role coverage tests**: All five MVP roles are represented across the two
  organizations.
- **Authentication sprint (Sprint 6)**: Stable user IDs allow login tests to
  target known personas without dynamic lookup.
- **Authorization sprint (Sprint 7)**: Stable organization and role assignments
  allow RBAC policy tests to reference consistent identities.
- **Integration test baseline**: `SeedDataIds` constants in Infrastructure
  allow test code to reference seed records by deterministic ID without
  hard-coded strings.

---

## Cross-Organization Validation Use

The two organizations and their role overlap support these test scenarios:

| Scenario | Relevant Personas |
|---|---|
| Agent denied access to Boreal data | Agent A (Asteria) vs. any Boreal record |
| Admin in org A cannot administer org B | Admin A (Asteria) vs. Boreal org |
| Same role, different org scope | Agent A (Asteria) and Agent B (Boreal) |
| Admin access within scope | Admin A administers Asteria; Admin B administers Boreal |

---

## Reset Instructions

### Reset to clean seeded state (preserves local database)

The seed data is applied by the `SeedFictionalOrganizationsAndPersonas`
migration. To restore the local database to the seeded state after manual
changes:

```powershell
# Ensure SQL Server is running
docker compose up -d sqlserver

# Set your local connection string
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=<your-local-password>;TrustServerCertificate=True;Encrypt=True"

# Apply all migrations (including seed migration)
dotnet tool run dotnet-ef database update `
  --project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj `
  --startup-project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj
```

### Full reset (delete all local data and re-seed)

```powershell
# Stop container and delete the data volume
docker compose down -v

# Start a fresh SQL Server container
docker compose up -d sqlserver

# Wait ~15-30 seconds for SQL Server to initialize, then apply migrations
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=<your-local-password>;TrustServerCertificate=True;Encrypt=True"
dotnet tool run dotnet-ef database update `
  --project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj `
  --startup-project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj
```

See `docs/local-development.md` for the full local environment setup guide.

---

## Out Of Scope

The following are explicitly **not** part of the seed data and must not be
added without an authorized sprint:

- Authentication credentials or password hashes
- JWT or session tokens
- Real user or employee records
- Real customer or client records
- Documents, document files, or synthetic document content
- Document chunks or embeddings
- Chat sessions, chat interactions, or AI answers
- Retrieval results or citations
- Answer feedback records
- Dashboard metric records
- Knowledge gap signals
- Non-MVP technical RBAC roles (QA, Trainer, Viewer, etc.)
- Production organization or user accounts
- Audit log seed entries

---

## Demo Credential Provisioning

Seed users have `password_hash = null`. Login is not possible until credentials are provisioned.

**Do not commit real passwords, generated hashes, or any credential material to source control.**

### Option A — Provision via the Admin API (recommended)

Once the API is running and you have direct database access to set an initial Admin password
(see Option B below for the first-Admin bootstrap), use the Admin user creation endpoint:

```http
POST /api/v1/users
Authorization: Bearer <admin-jwt-token>
Content-Type: application/json

{
  "email": "agent.a@asteria.example.com",
  "displayName": "Agent A",
  "initialPassword": "<choose-a-local-demo-password>",
  "roles": ["Agent"]
}
```

This endpoint is Admin-only (`Users.Create` permission). The `initialPassword` is hashed
immediately in the Application layer and is never logged, returned, or persisted in plaintext.

### Option B — Bootstrap the first Admin password directly (local only)

To log in as `Admin A` for the first time, set a password hash directly in the local database.
This is a local-only setup step — never use a real password or commit this command.

Generate a BCrypt hash outside source control (workFactor=12):

```csharp
// In a local .NET script or REPL — do not commit
var hash = BCrypt.Net.BCrypt.HashPassword("<choose-a-local-demo-password>", workFactor: 12);
Console.WriteLine(hash);
```

Then apply it to the local database (replace the placeholder values):

```sql
-- Local demo only. Replace placeholders. Do not commit.
UPDATE users
SET password_hash = '<bcrypt-hash-from-above>'
WHERE email = 'admin.a@asteria.example.com';
```

### Security rules for demo credentials

- Use a local-only demo password — not a real or shared password.
- Never commit the password, the hash, or any credential file.
- After setting the Admin password, provision other users via `POST /api/v1/users` from the Admin UI.
- Demo credentials are for local development and demonstration only.
- Do not use seed identities to access, process, or store real documents.

---

## Demo AI Provider Mode

The chat assistant runs in **Demo mode** by default — no API key required.

| Mode | Config | API Key | Answer Quality |
|---|---|---|---|
| Demo (default) | `Ai:AnswerProvider = Demo` | Not required | Deterministic extractive answer from retrieved policy text |
| OpenAI (optional) | `Ai:AnswerProvider = OpenAI` | Required (user-secrets) | Generative, fluent, grounded answer using OpenAI |
| LocalOpenAICompatible (optional) | `Ai:AnswerProvider = LocalOpenAICompatible` | Not required (Ollama) | Local generative answer via `qwen3:8b` through Ollama |

**Demo mode** is recommended for stable portfolio screenshots and CI — deterministic, no external dependencies.
**OpenAI mode** requires an active OpenAI account with billing credits.
**LocalOpenAICompatible mode** requires [Ollama](https://ollama.com) running locally with `qwen3:8b` pulled. See `docs/local-setup-guide.md` for full setup instructions.

### Switching to OpenAI mode (optional, local only)

```powershell
dotnet user-secrets set "Ai:AnswerProvider" "OpenAI" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
dotnet user-secrets set "Ai:OpenAI:ApiKey" "<your-key>" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

**Never commit an API key to source control.** Remove user-secrets when done:

```powershell
dotnet user-secrets remove "Ai:AnswerProvider" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
dotnet user-secrets remove "Ai:OpenAI:ApiKey" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

### Switching to Local Qwen mode (optional, requires Ollama)

```powershell
dotnet user-secrets set "Ai:AnswerProvider" "LocalOpenAICompatible" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

Revert to Demo when done:

```powershell
dotnet user-secrets set "Ai:AnswerProvider" "Demo" --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
```

---

## Demo Retrieval Enablement

Documents are retrievable for RAG chat only when all of the following conditions are true:

- `processing_status = 'Processed'`
- `is_retrieval_enabled = 1` (true)
- `deleted_at IS NULL`
- Organization scope authorizes access

By default, `is_retrieval_enabled` is `false` for all newly uploaded documents. KnowledgeAdmin
and Admin users can enable retrieval from the Documents UI once a document has been processed.

### E2E smoke tests

The `KnowledgeOps.E2ETests` project validates the full grounded-answer and
insufficient-context workflows using `WebApplicationFactory<Program>` and sets up the
required database state directly. No manual intervention is needed for automated test coverage.

### Live demo setup procedure (local only)

1. Log in as KnowledgeAdmin A or Admin A.
2. Upload the synthetic sample policy from `docs/demo-samples/Customer Support Refund & Escalation Policy.md` via `/documents/new`.
3. Wait for the Worker to process the document. The document detail page polls every 5 seconds and shows `Processing Status: Processed` when done.
4. In the document detail page (`/documents/<id>`), click **Enable retrieval**.
5. Confirm the Retrieval field shows `Enabled` and the Sources section becomes active.
6. Ask a question in the `/chat` page (see recommended demo questions below).
7. Confirm the answer is grounded with policy text and a Sources citation.
8. Click **Useful** to submit feedback.
9. Navigate to `/dashboard` to see metrics update.
10. Navigate to Chat History → interaction detail for technical metadata.

You can also enable retrieval from the **Documents list page** (`/documents`) using the
inline Enable button in the Actions column without navigating to the detail page.

### Recommended demo questions

```text
A customer says their item arrived damaged 20 days after purchase. What should I do before offering a refund?

What evidence do I need before processing a damaged item claim?

A customer wants a refund but it has been 18 days since delivery. What are my options?

What is the escalation process for refund requests outside the standard window?
```

### Important notes

- Never use real customer, employee, or internal operational documents in a demo.
- Use only fictional seed data or the synthetic sample from `docs/demo-samples/`.
- Never commit document files, document IDs, or retrieval state to source control.
- `retry-processing` (retry failed documents) remains deferred to Phase 2.
- No raw SQL update is required for retrieval enablement — use the UI Enable button.

---

## Safety Rules

- Never commit real passwords, API keys, or secrets to source control.
- Never store real customer, employee, or client data in the local database.
- Never use fictional seed identities to process or access real documents.
- The `.env` file containing local SQL Server credentials is gitignored and
  must never be committed. Only `.env.example` (with safe placeholder values)
  is committed.
- See `docs/local-development.md` for the full security and data rules.
