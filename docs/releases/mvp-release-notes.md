# KnowledgeOps-AI MVP Release Notes

**Version:** MVP Release Candidate
**Issue:** #49 — chore: stabilize and certify MVP release readiness
**Sprint:** Sprint 29 — MVP Stabilization And Release Checklist
**Date:** 2026-06-02
**Release type:** MVP demonstration / controlled release candidate

---

## 1. Release Summary

KnowledgeOps-AI is an internal, AI-powered document-based RAG knowledge assistant for contact centers and support operations. This release candidate delivers the complete MVP workflow: authorized internal users can upload documents, process them asynchronously, ask grounded questions, receive cited answers, review chat history, submit feedback, and view operational metrics — all within enforced organization boundaries and role-based access control.

This is an MVP demonstration release, not a production-grade enterprise deployment. It is suitable for controlled demonstration, stakeholder review, and developer evaluation in a local or CI environment with fictional data.

---

## 2. Implemented MVP Capabilities

### Authentication and Access Control
- JWT Bearer authentication (`POST /api/v1/auth/login`, `POST /api/v1/auth/logout`, `GET /api/v1/auth/me`)
- BCrypt password hashing (workFactor=12); JWT signing key validated at startup
- Five MVP RBAC roles: `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, `Admin`
- 30 permissions across 6 resource areas; deny-by-default; per-request persisted-state revalidation
- Organization-scoped access enforced on all protected data; no cross-organization data access

### Admin User Management
- Admin-only user creation, update, and role assignment/removal
- Self-lockout protection; final-active-Admin serializable-isolation protection
- Org-scoped: Admin A cannot manage users in Org B

### Document Management
- Document upload via multipart form (`POST /api/v1/documents`)
- Allowed formats: TXT (`.txt`), Markdown (`.md`, `.markdown`); max 10 MB
- Metadata management, processing status, and retrieval disablement (`POST /api/v1/documents/{id}/disable`)
- Angular document list, detail, status polling (5s), and upload pages

### Asynchronous Document Processing
- Background `DocumentProcessingWorker` polls on configurable interval
- Lifecycle: `Uploaded → Processing → Processed / Failed`
- Text extraction (TXT/Markdown); unsupported formats fail with a safe message
- Sliding-window chunking (1200 chars / 150-char overlap / ~4-chars-per-token estimate)
- Embedding generation via `FakeEmbeddingProvider` (SHA-256 deterministic, no network required)
- Processing failure reason stored safely; visible to authorized admin users

### Semantic Retrieval
- Local SQL-backed cosine similarity search (`LocalVectorStore`)
- Organization scope, retrieval eligibility, embedding status, and index status filtered at SQL level before scoring
- Eligible retrieval requires: `processing_status=Processed`, `is_retrieval_enabled=true`, `deleted_at IS NULL`, org scope, `EmbeddingStatus.Ready`, `EmbeddingIndexStatus.Indexed`

### RAG Chat
- `POST /api/v1/chat/questions` — requires `Chat.AskQuestion` permission
- 15-step `RagChatOrchestrationService`: auth → persisted user state → permission → org scope → session → interaction → retrieval → sufficiency check → prompt build → answer generation → outcome → citations → persist → respond
- `GroundedPromptBuilder`: max 5 chunks / 6000 chars; org-scope filter per chunk; `PromptVersion=rag-grounded-v1`
- `FakeAnswerGenerator`: SHA-256 deterministic, no API key required
- Three outcomes: `GroundedAnswer`, `InsufficientContext`, `ProviderFailure`

### Source Citations
- Grounded answers include `CitationResponse` list with `CitationId`, `DocumentId`, `ChunkId`, `Rank`, `DocumentTitle`, `RelevanceScore`
- Citations persisted in `citations` table with FK integrity to chat interactions, chunks, documents, and organizations
- InsufficientContext and ProviderFailure outcomes return empty citations

### Chat History
- Session list (`GET /api/v1/chat/sessions?scoped=true`), session detail, interaction detail, and interaction citations
- Own-session access for all roles; `Chat.ViewScopedHistory` (Supervisor/Manager/Admin) for org-scoped review
- KnowledgeAdmin: own-session only
- Angular chat history, session detail, and interaction detail pages

### Answer Feedback
- Submit and update `Useful`/`NotUseful` feedback per interaction
- One-feedback-per-user-per-interaction uniqueness enforced at database level

### Dashboard Metrics
- Four permission-gated endpoints: `/api/v1/dashboard/{overview,documents,chat,feedback}`
- Real-time org-scoped aggregations; null cost shown as "Not available"; null latency shown as "N/A"
- Angular dashboard with 4 sections; `dashboardVisibilityGuard` (UX-only; backend is authoritative)

### Observability and Support
- `GET /api/v1/health` — public basic status
- `GET /api/v1/health/details` — Admin-only sanitized application/database/retrieval status
- Correlation IDs; canonical `ApiErrorResponse` with Error ID; no sensitive content in error responses
- `GET /api/v1/admin/processing-failures` — KnowledgeAdmin/Admin; org-scoped failed document metadata
- `GET /api/v1/admin/audit-log` — Admin-only; org-scoped audit events with `from`/`to`/`eventType` filters

### Infrastructure
- Clean Architecture: Domain / Application / Infrastructure / API / Worker
- SQL Server + EF Core in Infrastructure; 10 ordered additive migrations
- Multi-stage Dockerfiles for API, Worker, and Frontend
- GitHub Actions: `ci.yml` (backend/frontend/docker on PR/push/dispatch); `integration-tests.yml` (SQL-gated, `workflow_dispatch`)
- Angular 21 frontend with lazy-loaded routing, JWT interceptor, `authGuard`, `RoleVisibilityService` (UX-only)

---

## 3. Security Model

- Authentication: JWT Bearer (HS256, 60-minute expiry); BCrypt-hashed passwords; no plaintext credentials committed
- Authorization: per-request persisted `UserAccessState` revalidation; JWT role claims alone are not trusted for permission checks
- Organization scope: all protected data filtered by `organization_id` from persisted state; never from caller input
- Cross-org isolation: HTTP 404 returned for cross-org resource access; tested by 12 cross-org API tests
- Health details: Admin-only; no secrets, stack traces, connection strings, or provider payloads in responses
- Logging: no full prompt content, no chunk text, no secrets, no provider payloads in logs
- Secrets management: signing key via environment variables or user-secrets; no committed credentials

---

## 4. AI / RAG Behavior

- RAG is retrieval-before-generation only. The generator is never called if retrieval returns insufficient results.
- Prompt construction uses only authorized, eligible retrieved chunks. A second org-scope filter (`IPromptAuthorizationFilter`) is applied per chunk during prompt build.
- `ContextSufficiencyPolicy` returns insufficient context when zero authorized chunks remain after filtering.
- The assistant is presented as an internal knowledge assistant, not a final authority on policy, HR, legal, or compliance matters.
- All grounded answers include source citations. Insufficient-context and provider-failure outcomes include no citations.
- Provider metadata (cost, token count, latency) is nullable; zero is never stored or displayed for unavailable values.

---

## 5. Fake-Provider Behavior

Normal automated tests and CI use deterministic fake providers. No live AI calls are required.

| Provider | Class | Behavior |
|---|---|---|
| `IEmbeddingProvider` | `FakeEmbeddingProvider` | SHA-256 hash of `{model}\|{dimensions}\|{text}` → deterministic float[] vector; no network |
| `IAiAnswerGenerator` | `FakeAnswerGenerator` | SHA-256 hash of question + chunks → deterministic answer string; no network |

Both providers are registered by default in `appsettings.json`. They require no API key, no Azure subscription, and no external service.

The `ci.yml` workflow uses these providers for all automated tests. The Docker images do not bake in any provider credentials.

---

## 6. Demo Data Notes

### Seed Data

Two fictional organizations and seven fictional users are seeded by the `SeedFictionalOrganizationsAndPersonas` migration:

| Organization | Domain |
|---|---|
| Asteria Support Group | `asteria.example.com` |
| Boreal Contact Services | `boreal.example.com` |

Five users in Asteria (one per MVP role) and two in Boreal (Agent and Admin) cover all five roles and both organizations. All email addresses use IANA-reserved `example.com` domains and cannot deliver real mail. See `docs/demo-data.md` for full details.

### Demo Credential Provisioning

Seed users have `password_hash = null`. Credentials must be provisioned before demo login is possible. See `docs/demo-data.md` Section "Demo Credential Provisioning" for the admin-provisioned initial-password procedure.

### Demo Document and Retrieval Enablement

After uploading and processing a TXT or Markdown document, `is_retrieval_enabled` remains `false` by default. A direct SQL update is required for a live demo showing grounded answers. See `docs/demo-data.md` Section "Demo Retrieval Enablement" for the procedure.

For automated E2E smoke tests, the `KnowledgeOps.E2ETests` project sets up all required state directly via `WebApplicationFactory<Program>` — no database manipulation is needed to validate RAG behavior.

---

## 7. Local Run Notes

```bash
# 1. Copy env template and configure local secrets (Jwt:SigningKey and SA password)
cp .env.example .env
# Edit .env: set KNOWLEDGEOPS_SQL_PASSWORD to a strong local password
# Set Jwt__SigningKey in user-secrets or .env (minimum 32 characters)

# 2. Start SQL Server
docker compose up sqlserver -d

# 3. Apply all database migrations (applies all 10 migrations including seed)
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project src/KnowledgeOps.Infrastructure \
  --startup-project src/KnowledgeOps.Infrastructure

# 4. Start the API (in a terminal)
dotnet run --project src/KnowledgeOps.Api

# 5. Start the background Worker (in a separate terminal)
dotnet run --project src/KnowledgeOps.Worker

# 6. Start the Angular frontend (in a separate terminal)
cd frontend && npm install && ng serve
```

Demo user credentials and document demo setup are in `docs/demo-data.md`.
Full local setup guide is in `docs/local-development.md`.

---

## 8. Docker / CI Notes

### Docker
Multi-stage Dockerfiles exist for API, Worker, and Frontend. No credentials are baked into the images. Runtime configuration must be supplied via environment variables or secrets managers.

```bash
# Build API image (from repository root)
docker build -f src/KnowledgeOps.Api/Dockerfile -t knowledgeops-api:local .

# Build Worker image (from repository root)
docker build -f src/KnowledgeOps.Worker/Dockerfile -t knowledgeops-worker:local .

# Build Frontend image (from frontend/ directory)
docker build -f frontend/Dockerfile -t knowledgeops-frontend:local ./frontend
```

### GitHub Actions CI
- `ci.yml` — runs on PR, push to `main`, or `workflow_dispatch`; validates backend build/tests, Angular build/tests, and Docker image builds.
- `integration-tests.yml` — `workflow_dispatch`-only; runs SQL-gated integration tests against a SQL Server 2022 service container. Requires `SQL_SA_PASSWORD` configured in repository settings → Secrets and variables → Actions.

### Local Tests (without SQL Server)
```bash
# Backend (excludes SQL-gated integration tests)
dotnet test KnowledgeOpsAI.sln -c Release --filter "FullyQualifiedName!~IntegrationTests"

# E2E smoke (7 in-process scenarios via WebApplicationFactory)
dotnet test tests/KnowledgeOps.E2ETests/KnowledgeOps.E2ETests.csproj -c Release

# Frontend
cd frontend && npm test -- --watch=false
```

---

## 9. Migration / Rollback Notes

### Apply All Migrations (Clean Setup)
```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project src/KnowledgeOps.Infrastructure \
  --startup-project src/KnowledgeOps.Infrastructure
```

### Full Reset (Delete All Local Data and Re-Seed)
```powershell
# Stop container and delete the data volume
docker compose down -v

# Start a fresh SQL Server container
docker compose up -d sqlserver

# Wait ~15–30 seconds for SQL Server to initialize, then apply migrations
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=<your-local-password>;TrustServerCertificate=True;Encrypt=True"
dotnet tool run dotnet-ef database update `
  --project src/KnowledgeOps.Infrastructure `
  --startup-project src/KnowledgeOps.Infrastructure
```

### Migration Sequence (10 migrations, all additive)
1. `InitialPersistenceFoundation` — organizations, users, user_roles, audit_log_entries
2. `SeedFictionalOrganizationsAndPersonas` — fictional seed data (InsertData only)
3. `DocumentMetadataFoundation` — documents table
4. `DocumentChunksFoundation` — document_chunks table
5. `ChunkEmbeddingsFoundation` — chunk_embeddings table
6. `AddChunkEmbeddingIndexMetadata` — index metadata columns on chunk_embeddings
7. `CreateChatSessionsAndInteractions` — chat_sessions, chat_interactions
8. `AddCitationsTable` — citations table
9. `ChatSessionStatusAndCitationDocumentFk` — chat_sessions.status column + citations.document_id FK
10. `AddAnswerFeedbackTable` — answer_feedback table

All migrations are additive. Each `Down()` drops only what the corresponding `Up()` created. No destructive data changes.

---

## 10. Known Limitations

The following capabilities are explicitly not included in this MVP release. They are documented here and must not be represented as implemented behavior.

| Limitation | Phase |
|---|---|
| Enterprise SSO (Azure AD / Entra ID) | Phase 3 |
| Customer-facing chatbot behavior | Out of scope |
| Live agent assist | Out of scope |
| Real-time call transcription | Out of scope |
| Autonomous workflow actions or policy enforcement | Out of scope |
| Full knowledge-gap review workflow | Phase 2 |
| Advanced analytics and exported reports | Phase 2 |
| Production-grade Azure hardening | Phase 3 |
| External enterprise integrations (SharePoint, Teams, CRM) | Phase 3 |
| PDF and DOCX text extraction | Phase 2 — TXT/Markdown only |
| Document retrieval re-enable endpoint | Phase 2 — see demo procedure in `docs/demo-data.md` |
| JWT server-side token revocation | MVP is stateless logout (client-side token clear only) |
| Local filesystem document storage | MVP only — cloud storage (Azure Blob) deferred to Phase 2/3 |
| Distributed worker dashboard | Worker runs locally via `dotnet run` |
| Diagram PNG artifact cleanup | Pending explicit authorization — `monitoring-sla-process.png` is stale |
| SQL integration tests in standard CI | Manual `workflow_dispatch` trigger required; needs `SQL_SA_PASSWORD` secret |

---

## 11. Deferred Features

### Phase 2 Parking Lot (not started)
- Full knowledge-gap review workflow
- Document processing re-enable and retry
- PDF/DOCX text extraction
- Topic clustering
- Richer feedback comments
- Hybrid search improvements
- Advanced analytics and reporting
- Notification center
- Document versioning and approval workflow

### Phase 3 Parking Lot (not started)
- Production Azure deployment
- Azure SQL, Azure Blob Storage, Azure AI Search
- Azure Key Vault
- Application Insights production integration
- Enterprise SSO / Microsoft Entra ID
- SharePoint / Teams / CRM integrations
- Performance and load testing
- Production hardening and disaster recovery

---

## 12. Optional Live-Provider Configuration Note

The MVP is fully functional using deterministic fake providers for all automated tests and CI. No live provider configuration is required.

To optionally configure a live AI provider for manual demonstration with real answer quality:

```bash
# In .env (local only — never commit)
# Azure OpenAI (recommended production path)
KNOWLEDGEOPS_AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
KNOWLEDGEOPS_AZURE_OPENAI_API_KEY=<your-key>

# OR OpenAI
KNOWLEDGEOPS_OPENAI_API_KEY=<your-key>
```

A real `IEmbeddingProvider` and `IAiAnswerGenerator` adapter for Azure OpenAI / OpenAI is deferred to Phase 2. The abstraction interfaces (`IEmbeddingProvider`, `IAiAnswerGenerator`) are fully defined and ready for real provider registration.

**Live provider configuration is optional, manual-only, and must never be committed to source control.**

---

## 13. Release Safety Statement

This MVP release candidate does not require real customer data, real employee data, client confidential documents, committed secrets, provider keys, production connection strings, or live AI provider calls in normal CI.

All seed data is entirely fictional. No real organizations, employees, customers, or documents are referenced. Email addresses use IANA-reserved `*.example.com` domains. Local credentials are managed through environment files that are explicitly excluded from source control.

Normal CI validation (ci.yml) uses deterministic fake providers (SHA-256-based `FakeEmbeddingProvider` and `FakeAnswerGenerator`) that require no external service, no API key, and no Azure subscription.
