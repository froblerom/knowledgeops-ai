# Current Implementation State

Last updated: 2026-06-01

## Current Phase

Sprint 26 Integration, API, Frontend, And E2E Test Completion / Issue #46 implementation complete

## Active Issue Execution Entry

### Issue Summary

- Issue ID/title: Issue #46 - `[Sprint 26] Complete MVP automated and E2E smoke test coverage`
- Related roadmap sprint: Sprint 26 - Integration, API, Frontend, And E2E Test Completion
- Related milestone, if applicable: MVP automated coverage and smoke validation completion
- Objective: Complete deterministic automated coverage and in-process E2E smoke validation for implemented MVP workflows.
- Expected outcome: MVP success paths and security-failure paths are covered across domain, integration/SQL-gated, API/E2E smoke, and Angular specs without adding product scope, live AI dependencies, browser E2E frameworks, CI workflows, real data, or production credentials.

### Classification

- Task type: MVP test completion / E2E smoke validation / coverage-gap closure
- Prompt level: Level 3
- Primary affected area: domain tests, integration/SQL-gated tests, WebApplicationFactory E2E smoke tests, frontend specs, fake-provider validation, coverage-gap artifact, progress records
- Security/organization-scope impact: High; final smoke and coverage pass must validate RBAC, organization-scope denial, safe errors, support/admin protection, and cross-scope data isolation
- AI/RAG impact: High; tests must validate deterministic fake-provider RAG flow, retrieval-before-generation, authorized prompt context, citations, insufficient context, and provider-failure behavior without live AI calls
- Data/migration impact: Low-Medium; no schema migration expected, but SQL-gated tests validate relational persistence and query behavior
- Recommended subagent(s), if any: None for this run; implementation and verification are handled directly with focused repository inspection and validation commands

### Required Context

- Related documentation and context: Sprint 26 roadmap entry; testing, security, RAG, database, API, frontend, DevOps, observability, and guardrails contracts
- Agent context files read: `docs/agents/00-agent-operating-protocol.md`, `01-project-context.md`, `02-architecture-context.md`, `03-domain-context.md`, `04-business-rules-context.md`, `05-testing-and-validation-context.md`, `06-frontend-context.md`, `07-backend-context.md`, `08-devops-context.md`, `09-observability-context.md`, `10-issue-execution-template.md`, `12-prompt-levels.md`, `13-prompt-classifier.md`. Prompt-requested filenames `06-backend-implementation-context.md`, `07-frontend-implementation-context.md`, `08-rag-implementation-context.md`, and `09-verification-context.md` are not present; mapped to current backend/frontend/testing/DevOps/observability contexts plus canonical RAG/security docs and ADRs.
- Canonical documents read for exact contracts: `docs/06-requirements.md`, `docs/07-use-cases.md`, `docs/08-business-process-flows.md`, `docs/09-business-rules.md`, `docs/10-domain-model.md`, `docs/14-database-design.md`, `docs/15-api-design.md`, `docs/16-security-and-permissions.md`, `docs/17-testing-strategy.md`, `docs/18-deployment-and-devops.md`, `docs/19-observability-and-support.md`, `docs/20-risk-register.md`, `docs/21-implementation-roadmap.md`, `docs/22-implementation-guardrails.md`
- Progress files read: `current-state.md`, `decisions-log.md`, `open-risks.md`, `completed-issues.md`
- Existing source/config/tests to inspect: backend projects, test projects, frontend app/features/core, Docker/local config, appsettings, `.github`, domain/application/infrastructure/API/Angular tests, E2E absence/patterns

### Scope

- In scope: Issue #42 completed-baseline correction, domain entity tests, SQL-gated integration tests, xUnit WebApplicationFactory E2E smoke project, focused Angular spec gaps, coverage-gap review artifact, progress updates
- Behavioral/contracts affected: Test coverage and smoke validation only; production behavior may change only if MVP-scoped tests expose a real defect
- Architecture boundaries affected: Tests may reference API/Infrastructure as appropriate; production Clean Architecture boundaries remain unchanged
- Security or data risks: Tests must prove RBAC, organization scope, safe errors, support/admin protection, and no live AI or real data dependency
- Files or areas expected to change: test projects, frontend specs, `KnowledgeOpsAI.sln`, progress docs, coverage-gap artifact

### Out Of Scope

- Deferred phase behavior excluded: performance/load testing, live-provider acceptance gate, Enterprise SSO, Phase 2 knowledge-gap workflow, production Azure deployment tests, CI workflow, Playwright/Cypress/browser E2E
- Explicit deferred functionality, if applicable: Phase 2/3 product behavior, new dashboard/support features, new RAG strategy, new AI provider behavior, new auth model, new RBAC roles
- Architecture/contracts not being changed: production authentication, authorization, RAG, retrieval, citation, feedback, dashboard, supportability, processing, and CI contracts unless a test exposes a real MVP defect
- Prohibited expansion: no new product feature, no new migration unless an existing persistence defect requires it, no live AI dependency, no real customer/employee/internal data

### Files To Inspect

- Existing implementation files: Domain chat/document/feedback/citation entities; Application RAG/chat/feedback/dashboard/admin services; Infrastructure EF repositories; API controllers; Angular auth/documents/chat/dashboard/admin/core
- Existing tests: Domain/Application/API/Integration/Angular tests and factories; SQL-gated skip conventions; API WebApplicationFactory patterns
- Relevant configuration/documentation: `KnowledgeOpsAI.sln`, project files, `docker-compose.yml`, appsettings, `.env.example`, `.github`, progress records

### Implementation Plan

1. Correct the Issue #42 completed baseline row.
2. Add domain entity tests for `ChatInteraction`, `AnswerFeedback`, and `Citation`.
3. Add SQL-gated integration tests for feedback, chat/citations, dashboard, audit/admin support, and document processing lifecycle.
4. Create xUnit WebApplicationFactory-based E2E smoke project with seven scenarios.
5. Add focused Angular specs for failure reason, feedback update/error, and admin user detail.
6. Create `coverage-gap-review-issue-46.md`, update progress files, then validate.

### Acceptance Criteria

- [x] Issue #42 completed baseline row is added.
- [x] Domain tests cover `ChatInteraction`, `AnswerFeedback`, and `Citation` invariants.
- [x] SQL-gated tests are added for feedback, chat/citations, dashboard, audit/admin support, and deterministic document processing.
- [x] xUnit WebApplicationFactory E2E smoke project includes seven MVP scenarios.
- [x] Angular spec gaps are covered without adding UI behavior.
- [x] Coverage-gap review artifact is created.
- [x] Scope boundaries remain intact.
- [x] Security, organization scope and AI/RAG safeguards are preserved where applicable.

### Validation Plan

- Commands/checks to run: `dotnet msbuild KnowledgeOpsAI.sln -t:Build -p:Configuration=Release`; `dotnet test KnowledgeOpsAI.sln --no-build -c Release --filter "FullyQualifiedName!~IntegrationTests"`; `dotnet test tests/KnowledgeOps.E2ETests/KnowledgeOps.E2ETests.csproj -c Release`; from `frontend/`, `npm run build`; `npm test -- --watch=false`; `git diff --check`; SQL-gated tests when SQL Server is available
- Testing expectations by affected area: domain invariants, SQL-gated persistence/query contracts, API/E2E smoke success and denial paths, Angular UI states, fake-provider deterministic RAG behavior
- Negative/security/cross-scope cases: RBAC denial, cross-org document/chat/citation/feedback/dashboard/support filtering or denial, safe insufficient context, provider failure, safe errors, no unsafe metadata
- Expected limitations or commands unavailable: SQL-gated tests may be environment-blocked without `ConnectionStrings__DefaultConnection` and running SQL Server; if so, document in `open-risks.md` and coverage artifact

### Documentation Updates

- Canonical docs affected, if any: none expected; add coverage artifact under progress docs
- Progress files to update on completion: `current-state.md`, `decisions-log.md`, `open-risks.md`, `completed-issues.md`, `coverage-gap-review-issue-46.md`
- ADR review required: no; implementation follows accepted Angular, Clean Architecture, RBAC, EF Core, provider isolation, async processing, RAG safety, and organization-scope decisions

## Delivery Status

| Item | Status |
| --- | --- |
| Current sprint | Sprint 26 completed through Issue #46 |
| Last completed sprint | Sprint 26: Integration, API, Frontend, And E2E Test Completion / Issue #46 (implementation complete; PR pending) |
| Active implementation issue | None; Issue #46 is implemented. Open PR after validation. |
| Current architecture status | Buildable .NET 10 backend + Angular 21 frontend + local SQL Server container + EF Core persistence foundation + `SeedFictionalOrganizationsAndPersonas` migration + JWT Bearer authentication + ICurrentUser abstraction + working login page with authGuard and apiInterceptor + **RBAC permission catalog** + **RolePermissionMatrix** (5 MVP roles, 30 permissions) + **IPermissionService / PermissionService** + **IOrganizationScopeService / OrganizationScopeService** + **[RequirePermission] attribute + PermissionPolicyProvider + PermissionAuthorizationHandler** + **persisted-current-state authorization via IUserAccessStateReader / EfUserAccessStateReader** + **correlation ID and global safe-error middleware** + **application observability contracts with Infrastructure EF audit/database-health adapters** + **public basic and Admin-only sanitized health endpoints** + **frontend generic Error ID UX** + **frontend RoleVisibilityService (UX-only)** + **future RAG/retrieval authorization hook interfaces** + **Admin-only same-organization user management API (GET/POST/PUT /api/v1/users, GET/POST/DELETE /api/v1/users/{id}/roles)** + **UserManagementService with initialPassword hashing, email normalization, self-lockout protection, final-active-Admin protection, persisted-disable permission check** + **safe audit events (UserCreated, UserUpdated, UserStatusChanged, UserRoleAssigned, UserRoleRemoved, UserManagementDenied, DocumentRetrievalDisabled, DocumentUploadAccepted, DocumentUploadRejected, DocumentUploadFailed, DocumentProcessingStarted, DocumentProcessingSucceeded, DocumentProcessingFailed, EmbeddingGenerationSucceeded, EmbeddingGenerationFailed)** + **minimal Angular Admin UI (user list, user detail/edit, user create, role assignment/removal)** + **canonical Document metadata and behavior-protected lifecycle (StartProcessing, MarkProcessed, MarkFailed, DisableRetrieval, IsEligibleForRetrieval)** + **DocumentProcessingStatus enum (Uploaded, Processing, Processed, Failed)** + **transition-aware DocumentService + IDocumentRepository** + **EfDocumentRepository** + **DocumentConfiguration with status check constraint + DocumentMetadataFoundation migration** + **5 document API endpoints (POST /api/v1/documents, GET /api/v1/documents, GET /{id}, GET /{id}/processing-status, POST /{id}/disable)** + **IDocumentStorage abstraction (Application, now includes OpenReadAsync) + LocalDocumentStorage (Infrastructure, local:// URI scheme, path-containment-safe OpenReadAsync)** + **UploadDocumentCommand with atomic validate→store→persist flow; best-effort cleanup on persistence failure** + **Angular documents list, detail/status/action (with 5 s status polling), and upload pages** + **DocumentService Angular service (list/get/getProcessingStatus/disableRetrieval/upload)** + **canUploadDocuments() / canDisableDocumentRetrieval() UX helpers** + **IDocumentProcessingOrchestrator / DocumentProcessingOrchestrator (atomic claim, ordered multi-step pipeline, transaction-wrapped steps + MarkProcessed, safe failure reason, processing audit events)** + **IDocumentProcessingStep / ExtractAndChunkDocumentProcessingStep + GenerateChunkEmbeddingsProcessingStep** + **IDocumentProcessingTransactionFactory / EfDocumentProcessingTransactionFactory (transaction wraps all steps + MarkProcessed atomically)** + **IDocumentTextExtractor / TxtMarkdownTextExtractor (text/plain, text/markdown, parameterized forms; normalizes line endings)** + **IDocumentChunker / DocumentChunker (sliding window, MaxChunkCharacters=1200, OverlapCharacters=150, token_estimate=ceil(len/4.0))** + **IDocumentChunkRepository / EfDocumentChunkRepository (includes GetChunksForDocumentAsync)** + **DocumentChunk domain entity + DocumentChunkConfiguration + DocumentChunksFoundation migration (11 columns, 4 indexes including UX on document_id+chunk_index)** + **DocumentExtractionException + DocumentChunkingException + DocumentEmbeddingException (controlled safe-message exceptions)** + **IEmbeddingProvider / FakeEmbeddingProvider (Infrastructure, SHA-256 deterministic, network-free, no SDK required)** + **FakeEmbeddingProviderSettings bound from Embeddings:Fake config** + **EmbeddingStatus enum (Ready, Failed)** + **ChunkEmbedding domain entity + ChunkEmbeddingConfiguration + ChunkEmbeddingsFoundation migration (12 columns, 4 indexes, UX_chunk_embeddings_chunk_id unique)** + **IChunkEmbeddingRepository / EfChunkEmbeddingRepository** + **ManagedDocument includes StorageLocation (never serialized to API/Angular)** + **IDocumentProcessingOrchestrator DI registration in Application** + **4 processing lifecycle repository methods (FindPendingForProcessingAsync, ClaimForProcessingAsync, MarkProcessedAsync, MarkFailedAsync) on IDocumentRepository + EfDocumentRepository** + **DocumentProcessingWorker BackgroundService (PeriodicTimer, scoped orchestrator per cycle, safe error logging)** + **WorkerCorrelationContext (per-scope, non-HTTP ICorrelationContext)** + **WorkerSettings (PollingIntervalSeconds with safe default 10)** + **AddJwtInfrastructure() split from AddInfrastructure() (Worker does not need JWT ValidateOnStart)**. |

Issue #28 adds Application-owned retrieval contracts, a local SQL-backed `LocalVectorStore`, QueryVector-only cosine search, Ready + Indexed retrieval eligibility, `AddChunkEmbeddingIndexMetadata`, safe retrieval settings, sanitized retrieval health, and provider-SDK boundary tests. No retrieval API endpoint, RAG answer generation, prompt construction, chat API, citation mapping, frontend retrieval work, external vector service, Azure AI Search, OpenAI/Azure OpenAI SDK, Semantic Kernel, or vector database SDK was added.

Issue #29 adds an Application-level eligible semantic retrieval service that validates the authenticated active user from persisted access state, requires `Chat.AskQuestion`, derives organization scope from `UserAccessState`, hashes query text, generates a query embedding through `IEmbeddingProvider`, calls `ISemanticSearchProvider` with QueryVector only, revalidates returned candidates through `IRetrievalEligibilityRepository`, excludes stale/cross-organization candidates, assigns final ranks after filtering, and returns metadata-only authorized candidates with insufficient-result signaling. Physical `retrieval_results` / `retrieval_queries` persistence remains deferred until the chat interaction sprint where `chat_interaction_id` exists.

Issue #36 adds the full RAG chat domain model and application orchestration: `ChatSession` and `ChatInteraction` domain entities with `AnswerState` enum (Grounded, InsufficientContext, ProviderFailed); `IAiAnswerGenerator` abstraction in Application; `RagChatOrchestrationService` implementing the 15-step retrieval-before-generation workflow (auth → UserAccessState → Chat.AskQuestion → session load/create → interaction create → retrieval → insufficient-result guard → retrieval-failure guard → chunk text resolution → authorized generation → outcome recording → persist → return); `FakeAnswerGenerator` (SHA-256 deterministic, no network, CI-safe); `EfChatSessionRepository` and `EfChatInteractionRepository`; `EfChunkTextReader`; `ChatSessionConfiguration` and `ChatInteractionConfiguration` EF configurations; `CreateChatSessionsAndInteractions` additive migration (chat_sessions + chat_interactions with all FKs, indexes, and nullable cost/token/latency fields); 5 new audit event types (ChatInteractionStarted, ChatAnswerGenerationCompleted, ChatAnswerGenerationFailed, ChatInteractionStored, InsufficientContextReturned); 10 new orchestration tests. All cost/token/latency fields are nullable — zero never represents unavailable. No HTTP endpoint, Angular chat UI, citation mapper, prompt builder, feedback, dashboard, or live provider was added.

Issue #37 adds RAG prompt building and defense-in-depth authorization: **IGroundedPromptBuilder / GroundedPromptBuilder** (Application, Chat/Prompting, applies IPromptAuthorizationFilter per chunk, max 5 chunks / 6000 chars, PromptVersion rag-grounded-v1) + **IContextSufficiencyPolicy / ContextSufficiencyPolicy** (Application, Chat/Prompting, returns insufficient when zero authorized chunks) + **DefaultPromptAuthorizationFilter** (Application, org-scope check, implements IPromptAuthorizationFilter) + **InsufficientContextFallbackText constant** in RagChatOrchestrationService + **AuditEventTypes.PromptBuildFailed**. `ChatInteraction.PromptVersion` is now set via the extended `RecordGroundedOutcome(... promptVersion: ...)` parameter. `AskQuestionResponse.AnswerText` for InsufficientContext returns the canonical fallback string. `RagChatOrchestrationService` orchestration extended with steps 11a (sufficiency check) and 11b (prompt build) before generation. No migration, HTTP endpoint, Angular file, citation persistence, or live provider was added.

## Current Known Limitations

- Text extraction (TXT/Markdown only) and deterministic character-based chunking are implemented. PDF, DOCX extraction remain deferred (fail-safe with controlled error message).
- Chunks and embeddings are persisted, local SQL-backed semantic retrieval exists, and Application-level eligible retrieval orchestration exists, but `IsRetrievalEnabled` remains `false` by default; no re-enable endpoint, user-facing retrieval endpoint, chat API, prompt construction, answer generation, citations, or RAG chat exists yet.
- `MarkProcessed` does not enable retrieval; re-enable and retry endpoints are deferred to Phase 2.
- Retrieval search eligibility is enforced in `LocalVectorStore` with SQL-level organization, document, chunk, embedding, and index-status filters before vector scoring.
- JWT logout is stateless (client-side clear only); no server-side token revocation.
- Audit emissions are safe best-effort; workflow-specific telemetry remains deferred.
- Detailed health exposes sanitized application, database, and retrieval storage status only.
- Chat.ViewInteraction and Chat.ViewCitations carry an own-only/scoped convention; actual query-level enforcement is deferred to Sprint 17+ chat workflows.
- Diagram artifact cleanup remains pending for `docs/diagrams/business-process/monitoring-operational-process.png`.
- Serializable-isolation final-active-Admin check in EfUserManagementRepository does not span distributed transactions; acceptable for MVP single-node SQL Server.
- SQL integration tests for user management, document processing, and local vector retrieval require `ConnectionStrings__DefaultConnection` env var and a running SQL Server container.
- SQL-gated validation for Issue #44 EF support queries requires `ConnectionStrings__DefaultConnection` and a running SQL Server container.
- Local document storage writes files to `.local/storage/documents/` at repository root; no production cloud storage adapter exists yet.
- Worker runs locally via `dotnet run`; no distributed worker dashboard or external queue platform.

Issue #39 adds source citation mapping and persistence: `Citation` domain entity; `ICitationMapper` / `CitationMapper` (Application, `Chat/Citations`) maps from `GroundedPrompt.SourceHandles` only; `IDocumentTitleReader` / `EfDocumentTitleReader` for title lookup; `ICitationRepository` / `EfCitationRepository` for persistence; `DefaultCitationAuthorizationFilter` enforcing org-scope equality; `CitationConfiguration` EF configuration (`citations` table, FK Restrict to `chat_interactions`, `document_chunks`, `organizations`); `AddCitationsTable` additive migration; `RagChatOrchestrationService` extended with Step 13.5 (citation mapping and persistence in Grounded branch only); `AskQuestionResponse` returns `IReadOnlyList<CitationResponse>?`; `CitationResponse` DTO; `PromptSourceHandle` extended with `RelevanceScore`; `AuthorizedChunkContext` extended with `RelevanceScore = 0.0`; two new audit event types (`CitationsPersisted`, `CitationMappingFailed`). InsufficientContext and ProviderFailed paths create no citations. No HTTP endpoint, Angular UI, retrieval_results table, or live provider introduced.

Issue #40 exposes the RAG question workflow to authorized internal users through `POST /api/v1/chat/questions` and the Angular `/chat` experience. The API uses `[RequirePermission(Chat.AskQuestion)]`, accepts only `questionText` and optional `chatSessionId`, trims and validates empty questions, delegates to `IRagChatOrchestrationService`, maps `AnswerState` explicitly to `GroundedAnswer`, `InsufficientContext`, or `ProviderFailure`, returns real Application citations for grounded answers, and returns empty citations for insufficient-context and provider-failure outcomes. `CitationResponse` was aligned to include `CitationId` from persisted `Citation.Id`. Angular adds typed chat models, a `ChatService`, in-memory session continuity, loading/error/empty states, grounded answer rendering, metadata-only citations, insufficient-context and provider-failure safe rendering, a safe uncited-grounded fallback, and an internal-assistant/not-final-authority disclaimer. No chat history page, session list/detail endpoint, citation-detail endpoint, feedback UI/API, dashboard analytics, streaming, document viewer, new migration, live provider integration, or retrieval/prompt/citation pipeline rewrite was introduced.

Issue #43 adds dashboard metrics: four permission-gated API endpoints (`GET /api/v1/dashboard/overview`, `/documents`, `/chat`, `/feedback`), `DashboardService` (Application), `EfDashboardRepository` (Infrastructure) with org-scoped real-time aggregations, `DashboardController` (API, thin), `DashboardDateRange` value object (default 30-day period with optional ?from=/?to= params), `dashboardVisibilityGuard` (Angular UX-only), `DashboardService` (Angular), `DashboardPage` expanded with four sections (overview, documents, chat, feedback), cost shown as "Not available" when unavailable (never $0.00), latencies shown as "N/A" when null. No new EF migration, no Phase 2 tables, no new RBAC roles.

Issue #45 adds defense-in-depth security hardening: `EfChatInteractionRepository.FindByIdAsync` now accepts `organizationId` and enforces org scope at the DB level (consistent with other org-scoped repository methods). `IChatInteractionRepository` contract updated; `ChatHistoryService` passes `activeState.OrganizationId`; all test fakes updated. Cross-org API-layer security tests added: G-2 (4 dashboard cross-org tests), G-3 (feedback review cross-org), G-4 (feedback submit/update cross-org), G-5 (chat sessions cross-org), G-6 (chat interaction detail and citations cross-org), G-7 (document disable cross-org) — 12 new API tests total. No migration, no authentication/RBAC/RAG/feature behavior change.

Issue #44 adds safe observability and supportability completion: `GET /api/v1/admin/processing-failures` for KnowledgeAdmin/Admin with `System.ViewProcessingFailures`; `GET /api/v1/admin/audit-log` for Admin with `Audit.View`, `from`/`to`/`eventType` filters, newest-first safe limiting, and best-effort `AuditLogViewed`; Application `AdminSupportService`; Infrastructure EF support queries over existing `documents` and `audit_log_entries`; Angular `/admin/processing-failures` and `/admin/audit-log` pages with loading/error/empty states; role visibility helpers `canViewProcessingFailures()` and `canViewAuditLog()`. Responses expose only safe metadata and remain organization-scoped from persisted current user state. No EF migration, no retry/re-enable workflow, no raw diagnostics, no prompt/chunk/provider payload exposure, and no RAG behavior change.

Issue #41 adds secure chat history and interaction detail views: `ChatSession.Status` domain property (default `Active`); `IChatHistoryService` / `ChatHistoryService` (Application) enforcing owner-only vs. scoped-reviewer access via `Chat.ViewScopedHistory` (Supervisor/Manager/Admin only); `KnowledgeAdmin` is own-only; cross-org access returns null (safe 404); read-query methods added to `IChatSessionRepository`, `IChatInteractionRepository`, `ICitationRepository` and their EF implementations; three new audit event types (`ChatHistoryViewed`, `ChatInteractionViewed`, `ChatHistoryDenied`); five new API endpoints (`GET /api/v1/chat/sessions`, `POST /api/v1/chat/sessions`, `GET /api/v1/chat/sessions/{chatSessionId}`, `GET /api/v1/chat/interactions/{chatInteractionId}`, `GET /api/v1/chat/interactions/{chatInteractionId}/citations`); `ChatHistoryModels.cs` API response models; `ChatSessionStatusAndCitationDocumentFk` additive migration (`chat_sessions.status nvarchar(50) NOT NULL DEFAULT 'Active'`; `FK_citations_documents_document_id` FK constraint on existing `citations.document_id`); `CitationConfiguration` extended with `document_id` FK; Angular `chat.models.ts` extended with history types; `ChatService` extended with `getSessions`, `createSession`, `getSession`, `getInteraction`, `getInteractionCitations`; three new Angular pages (`ChatHistoryPage`, `ChatSessionDetailPage`, `ChatInteractionDetailPage`); routes `/chat/history`, `/chat/history/:chatSessionId`, `/chat/interactions/:chatInteractionId`; `RoleVisibilityService` extended with `canViewChatHistory()` (all authenticated MVP roles) and `canViewScopedChatHistory()` (Supervisor/Manager/Admin only); History nav link in `app.html`; 35 new Application tests, 39 new API tests, 7 new integration model tests, 29 new Angular tests. No analytics, exports, knowledge-gap workflow, feedback tables, retrieval_results table, streaming, new AI generation, new prompt behavior, new retrieval behavior, or KnowledgeAdmin scoped review was introduced.

## Next Recommended Action

Open the pull request for Issue #44. Before merge, run SQL-gated validation for the EF processing-failure and audit-log support queries when `ConnectionStrings__DefaultConnection` and SQL Server are available.

## Source Of Truth

- `docs/21-implementation-roadmap.md`
- `docs/22-implementation-guardrails.md`
- `docs/agents/`

## Update Rule

Update this file whenever an implementation issue starts or completes, the active sprint changes, a blocker affects recommended next action, or architecture readiness materially changes.
