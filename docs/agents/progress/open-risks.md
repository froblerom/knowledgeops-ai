# Open Implementation Risks

Last updated: 2026-05-30 (updated for Sprint 21 Issue #41)

| Risk | Severity | Related Area | Mitigation | Status |
| --- | --- | --- | --- | --- |
| Agent prompts may load excessive context and lose focus. | Medium | Harness / all future issues | Use `13-prompt-classifier.md`, level-based routing and minimum context bundles. | Open |
| RAG may be implemented as unsupported general answer behavior rather than grounded document assistance. | High | Retrieval/RAG | Use `rag-implementation-agent.md`, citation and insufficient-context rules, and fake-provider safety tests. Sprint 17 `RagChatOrchestrationService` enforces retrieval-before-generation and insufficient-context handling. Sprint 18 added grounded prompt construction and context sufficiency policy. Sprint 19 added citation mapping and persistence. Sprint 20 exposes only an authorized chat API/UI with safe outcomes and metadata-only citations. | Mitigated for MVP chat surface through Sprint 20 Issue #40 |
| Authorization may be skipped during retrieval or prompt construction. | Critical | Security/RAG | Load security, business-rules and RAG contexts for relevant tasks; require cross-scope tests and verification. Sprint 16 validates active persisted user state, `Chat.AskQuestion`, organization scope, and Application-level candidate revalidation. Sprint 17 `RagChatOrchestrationService` re-validates org scope from `UserAccessState` (not JWT claims) before passing chunks to generator; chunk text reader enforces org scope. Sprint 18 `GroundedPromptBuilder` applies `IPromptAuthorizationFilter` (org-scope check via `DefaultPromptAuthorizationFilter`) as a second enforcement gate before including any chunk in the grounded prompt. `ContextSufficiencyPolicy` gates on zero authorized chunks. Issue #37 closes this risk for the prompt construction path. | Resolved — Sprint 18 Issue #37 applied IPromptAuthorizationFilter in GroundedPromptBuilder |
| Agent context summaries may diverge from canonical documents over time. | High | Documentation governance | Use documentation and verification agents; update harness when canonical docs change; treat canonical docs as authoritative. | Open |
| Diagram artifact filename cleanup remains pending. | Low | Documentation artifacts | Address in Sprint 28 or an explicitly authorized diagram artifact task. | Open |

## Sprint 0 Issue #2 Disposition

The earlier optional prompt-harness question is resolved: `docs/agents/` is the canonical harness and future implementation prompts must classify first. No open risk remains for whether the harness exists; the ongoing risks above remain relevant for implementation.

## Sprint 1 Issue #3 Disposition

The backend scaffold readiness concern is resolved: the .NET 10 solution, approved project reference graph, minimal hosts and architecture boundary tests were implemented and validated. No new open risk was introduced by Issue #3. The feature-sensitive risks above remain applicable to their owning future sprints, and diagram artifact filename cleanup remains deferred.

## Sprint 2 Issue #4 Disposition

The Angular frontend scaffold is established. The auth guard is intentionally pass-through (Sprint 6 adds real auth); this is an accepted architectural decision, not an open risk. No new open risks introduced by Issue #4. The existing security and RAG risks remain applicable to their owning future sprints.

## Sprint 3 Issue #5 Disposition

The local Docker runtime foundation is established. SQL Server 2022 container confirmed starting successfully (`SQL Server is now ready for client connections`). Safe `.env.example` template committed; `.env` is gitignored. Port 1433 conflict risk is mitigated by the `KNOWLEDGEOPS_SQL_PORT` override convention documented in `.env.example` and `docs/local-development.md`. No new open risks introduced by Issue #5. API/Worker connection errors until EF Core is introduced are an expected and accepted limitation, not an open risk.

## Sprint 4 Issue #6 Disposition

The EF Core SQL Server persistence foundation is established in Infrastructure only. `InitialPersistenceFoundation` was reviewed and applied to local SQL Server, and the opt-in relational integration test passed against a uniquely named temporary database with schema-scope, round-trip, role-value, uniqueness, and organization foreign-key checks. No new open risk remains from migration validation; authentication, authorization, document processing, retrieval, and RAG risks remain applicable to their owning future sprints.

## Sprint 5 Issue #7 Disposition

The fictional seed data migration is established. `SeedFictionalOrganizationsAndPersonas` was generated and applied to local SQL Server; 13 SQL-backed `SeedDataTests` passed (2 organizations, 7 users, 5 MVP roles, 7 role assignments, no passwords, no audit log entries, no unexpected tables, cross-org scope coverage). `SqlServerPersistenceTests` fragility from `SingleAsync()` was resolved before adding seed data. Package/reference boundaries confirmed: Domain has no packages; Application has DI abstractions only; EF Core remains in Infrastructure only. No new open risks introduced by Issue #7; authentication, authorization, and all feature-specific risks remain applicable to their owning future sprints.

## Sprint 6 Issue #13 Disposition

JWT Bearer authentication and current-user context are implemented across all layers. Login, logout, and `/auth/me` endpoints are live. All five MVP login failure modes return HTTP 401 with an identical body (user enumeration prevented). GET /auth/me re-queries the database on every request and rejects Disabled/Pending/missing users. Angular `authGuard` redirects to `/login` when the session is expired or absent; `apiInterceptor` attaches Bearer tokens only to API-URL requests. `ValidateOnStart` enforces a minimum 32-character signing key at host startup. No passwords committed in seed data; test passwords provisioned via fixture setup only. Authorization risk for business features remains open (role-scoped document/RAG access is not yet enforced — see Sprint 7+).

## Sprint 7 Issue #14 Disposition

RBAC permission catalog and policy enforcement are fully implemented. `KnowledgeOpsPermissions` defines all 30 MVP permissions; `RolePermissionMatrix` enforces deny-by-default for unknown roles and unknown permissions; `PermissionService` and `OrganizationScopeService` are pure in-memory (no database dependency). `[RequirePermission]` attribute and `PermissionPolicyProvider` wire the ASP.NET Core policy pipeline; `PermissionAuthorizationHandler` logs only safe fields. Organization-scope enforcement (same-org-only) applies identically to all roles including Admin. `AuthorizationApiTests` confirm 401 without token, 403 for permission violations, 404 for cross-org resource lookups, and safe denial bodies (no permission name, org name, or token leakage). Frontend `RoleVisibilityService` provides UX-only visibility helpers annotated as non-authoritative. Two partial risks remain: (1) `Chat.ViewInteraction`/`Chat.ViewCitations` own-only vs. scoped query-level enforcement deferred to Sprint 17+ chat workflows — the matrix carries a code comment documenting the convention; (2) authorization failure logging is defined at the event level but full structured logging framework, correlation middleware, and health endpoints are deferred to Sprint 8. The critical "Authorization may be skipped during retrieval or prompt construction" risk remains open and applicable to all future RAG sprints.

## Sprint 8 Issue #15 Disposition

Safe operational behavior is established for the currently implemented API surface. Correlation middleware accepts only safe identifiers up to the existing 100-character audit-storage limit and replaces invalid input; canonical API error bodies include an Error ID; authentication and permission denial paths emit sanitized best-effort audit events; public basic health and Admin-only detailed health expose only sanitized application/database status. Issue #15 reused `audit_log_entries` with no migration or package additions and added no provider, RAG, or dashboard functionality. Automated API/frontend tests cover correlation, safe errors, login/permission auditing, public health, Admin/non-Admin detailed health, degraded database status, audit-write failure tolerance, and generic frontend Error ID UX. The full solution run completed with 20 SQL-gated tests skipped because `ConnectionStrings__DefaultConnection` was unset and Docker SQL was unavailable; the new EF audit persistence test remains ready for configured SQL execution. No live API smoke checks were performed because no configured host/credentials were available. Future workflow audit events and future dependency health expansion must preserve these controls; existing retrieval/RAG risks remain open for their owning sprints.

## Sprint 9 Issue #16 Disposition

Admin user and role management foundation is fully implemented. `PermissionAuthorizationHandler` now re-queries the database via `IUserAccessStateReader` / `EfUserAccessStateReader` for every permission check — JWT role/status claims are not used for authorization decisions. Disabled or demoted Admins holding unexpired tokens cannot authorize `/api/v1/users` or any other permission-gated operation. Self-lockout protections (self-disable, self-Admin-role-removal, final-active-Admin) are enforced in the Application service layer with serializable-isolation repository reads. No EF migration was created; existing foundation tables are sufficient. No password reset, invitation, SSO, or enterprise-identity scope was introduced. `UsersControllerTests` confirm 401/403/404/409/400 responses, safe DTO content (no `password` fields), cross-org isolation, email normalization, and self-lockout denial. Existing retrieval/RAG/authorization risks remain open for their owning future sprints.

**New residual risk**: The serializable-isolation final-active-Admin check does not span distributed transactions (single SQL Server node assumed). Acceptable at MVP scale; must be revisited before multi-node deployment.

## Sprint 10 Issue #20 Disposition

Document metadata persistence and lifecycle behavior are validated for Sprint 10. `Document` stores canonical metadata and timestamps, limits processing states to `Uploaded`, `Processing`, `Processed`, and `Failed`, records bounded failure reasons, and encodes only intrinsic retrieval eligibility. Retrieval disablement remains independent from processing status and the atomic repository operation exposes its actual state transition so `DocumentRetrievalDisabled` is emitted only when `true` changes to `false`. The public route is `POST /api/v1/documents/{documentId}/disable`; no re-enable or retry API exists.

`DocumentMetadataFoundation` was regenerated with the required fields, `storage_location` non-null constraint, `is_retrieval_enabled` database default `false`, four-state status check constraint, organization/user foreign keys, and six supporting indexes. On 2026-05-26 the migration applied successfully to a disposable local SQL Server instance and all 32 SQL-gated integration tests passed, including canonical document persistence, default/constraint, scope, sorting, soft-delete, and transition tests. No new Issue #20 residual risk remains; the existing future retrieval/RAG authorization risk remains open for its owning sprints.

## Sprint 11 Issue #21 Disposition

Document upload via `POST /api/v1/documents` (multipart/form-data) is implemented across all layers. The `IDocumentStorage` / `LocalDocumentStorage` abstraction writes files to `.local/storage/documents/` using a `local://` URI scheme; no absolute paths or storage internals are exposed in API responses or Angular UI. The upload flow is atomic: validate → store → persist; `DeleteAsync` is called best-effort when `CreateAsync` fails. Extension, content-type, and size validation are enforced in the Application layer; `FormOptions.MultipartBodyLengthLimit` and `[RequestSizeLimit]` are enforced at the framework level. Three distinct audit events (`DocumentUploadAccepted`, `DocumentUploadRejected`, `DocumentUploadFailed`) allow ops teams to distinguish validation failures from infrastructure failures. Angular `DocumentUploadPage` is role-gated by `canUploadDocuments()` (UX-only; backend `[RequirePermission(Documents.Upload)]` is authoritative).

All 212 Application tests and 102 API tests pass. Angular test suite passes 96/96 with exit code 0 (router navigation mocked via `vi.spyOn` rather than token replacement). `git ls-files .local/` confirms no storage files are committed. No migration was created; `storage_location` is updated to a real `local://` reference on upload. No synchronous processing, extraction, chunking, embeddings, retrieval, RAG, or citations were introduced.

**Residual risk**: `LocalDocumentStorage` uses local filesystem only; no production cloud adapter exists. Uploaded files are lost if the host is replaced. Acceptable for MVP local development; cloud storage adapter must be implemented before any production deployment. Re-enable and retry endpoints remain deferred to Phase 2.

## Sprint 12 Issue #22 Disposition

Asynchronous document processing worker foundation is fully implemented. `DocumentProcessingWorker` (`BackgroundService`) polls for `Uploaded` documents using `PeriodicTimer` and delegates to `DocumentProcessingOrchestrator` per-cycle scope. The orchestrator atomically claims a document via `ExecuteUpdateAsync` (WHERE `ProcessingStatus == Uploaded`), invokes `PlaceholderDocumentProcessingStep` (no-op), then calls `MarkProcessedAsync` or `MarkFailedAsync`. Processing state transitions do not change `IsRetrievalEnabled`. Failure reasons use only `ex.Message`, trimmed and capped at 200 characters — never `ex.ToString()`. Processing audit events (`DocumentProcessingStarted`, `DocumentProcessingSucceeded`, `DocumentProcessingFailed`) are emitted as best-effort. Angular `DocumentDetailPage` now polls status every 5 seconds while the status is non-terminal (Uploaded/Processing) and stops automatically on Processed/Failed or ngOnDestroy.

JWT blocker resolved: `AddJwtInfrastructure()` is split from `AddInfrastructure()`; the Worker calls only `AddInfrastructure()` and does not require `Jwt:SigningKey`. `WorkerCorrelationContext` provides per-scope non-HTTP correlation IDs. `WorkerSettings` defaults `PollingIntervalSeconds` to 10 with safe clamping.

Non-integration tests: 27 Domain + 228 Application + 102 API = 357 passed, 0 failed. Angular test suite passes 103/103. No migration created; all required columns exist from Sprint 10. Scope confirmed: no extraction, chunking, embeddings, vector storage, RAG, citations, retry endpoint, re-enable endpoint, external queue, or distributed dashboard.

**New residual risks**:
- `PlaceholderDocumentProcessingStep` does nothing; documents reach `Processed` status without any real extraction/embedding. `IsEligibleForRetrieval()` remains false until a re-enable operation is implemented (Phase 2+). Acceptable and required by Sprint 12 scope.
- Worker runs locally via `dotnet run`; no production deployment, container, or service-manager configuration exists yet. Cloud deployment must configure `ConnectionStrings__DefaultConnection` and `Worker:PollingIntervalSeconds` via environment variables or secrets.

## Sprint 14 Issue #27 Disposition

Embedding abstraction and deterministic fake provider are fully implemented. `IEmbeddingProvider` / `FakeEmbeddingProvider` (Infrastructure) generates deterministic, network-free embeddings using SHA-256 hashing of `{modelName}|{dimensions}|{text}` — no Azure/OpenAI SDK is required, and no live provider configuration is needed for tests or CI. `GenerateChunkEmbeddingsProcessingStep` reads saved chunks via `IDocumentChunkRepository.GetChunksForDocumentAsync`, calls the provider per chunk with a per-chunk try/catch, saves a `ChunkEmbeddingRecord` per chunk (status `Ready` or `Failed`), and emits `EmbeddingGenerationSucceeded` / `EmbeddingGenerationFailed` audit events best-effort. Invalid or empty vectors map to `status=Failed` with `FailureReason="Embedding vector was invalid."`. `DocumentProcessingOrchestrator` now accepts `IEnumerable<IDocumentProcessingStep>` materialized to an ordered list; both steps execute in registration order within a single transaction. `ChunkEmbeddingsFoundation` EF migration adds `chunk_embeddings` (12 columns, 4 indexes, FKs to `document_chunks` and `organizations`, unique constraint `UX_chunk_embeddings_chunk_id` for MVP one-embedding-per-chunk invariant). `EmbeddingStatus` enum has only `Ready` and `Failed` (terminal states). `vector_data` stored as `nvarchar(max)` JSON `float[]` for MVP. `EmbeddingRequest.Text` is never logged, audited, or included in failure reasons.

RISK-014 (embedding failure makes chunks incorrectly retrievable): **Partially mitigated.** Failed embeddings are stored with `status=Failed`; retrieval eligibility predicate (`IsEligibleForRetrieval`) does not yet check embedding status — enforcement deferred to Sprint 15+ retrieval workflow. The risk remains open until chunk-level retrieval eligibility is enforced.

RISK-020 (SDK coupling): **Resolved for Sprint 14.** `IEmbeddingProvider` is owned by Application; Domain and Application have no Azure/OpenAI SDK references. Assembly dependency tests guard the boundary. `FakeEmbeddingProvider` (Infrastructure) has no external SDK dependency.

RISK-025 (CI live-provider risk): **Resolved for Sprint 14.** `FakeEmbeddingProvider` is deterministic and network-free; no API key or Azure config is required for `FakeEmbeddingProviderTests` or `GenerateChunkEmbeddingsProcessingStepTests`. All tests pass without a live provider.

**New residual risks**:
- `IsEligibleForRetrieval()` does not check chunk embedding status; deferred to retrieval workflow Sprint 15+.
- `FakeEmbeddingProvider` is the only registered provider; no Azure OpenAI or real provider adapter exists. Production deployment requires a real provider registration before retrieval goes live.
- `vector_data` stored as `nvarchar(max)` JSON; no SQL Server native vector type or similarity index exists. Retrieval will require full vector scan or an external vector store Sprint 15+.

## Sprint 15 Issue #28 Disposition

Vector storage and semantic retrieval abstractions are implemented with a local SQL-backed MVP adapter over `chunk_embeddings.vector_data` JSON. Search eligibility now requires `embedding.status = Ready`, `index_status = Indexed`, processed documents, `is_retrieval_enabled = true`, non-soft-deleted documents/chunks, and matching `organization_id` across embedding, chunk, and document records. Organization filtering is applied in the EF query before vector data is materialized or scored.

RISK-002 (weak retrieval quality): **Remains open.** Deterministic fake vectors and local cosine search make behavior testable, but they are not semantically meaningful enough for production quality.

RISK-006 (cross-organization retrieval leakage): **Mitigated for Sprint 15 retrieval candidate selection.** SQL-level organization filters run before scoring, and cross-organization tests seed a higher-scoring other-organization candidate that is excluded. Future prompt construction must preserve this boundary.

RISK-014 (embedding failure makes chunks incorrectly retrievable): **Mitigated.** Search requires both `EmbeddingStatus.Ready` and `EmbeddingIndexStatus.Indexed`; failed, malformed, missing, or unindexed embeddings are excluded.

RISK-015 (vector/index state inconsistent with source records): **Mitigated for MVP.** Index metadata tracks `Indexed` / `Failed`, stale and ineligible source records are filtered, and tests cover retrieval-disabled, soft-deleted, unprocessed, failed, unindexed, malformed, dimension-mismatch, and zero-norm candidates.

RISK-020 (SDK coupling): **Guarded.** Domain and Application dependency tests reject Azure AI Search, OpenAI/Azure OpenAI, Semantic Kernel, and common vector database SDK references.

RISK-025 (CI live-provider risk): **Guarded.** The local SQL-backed adapter has no external network or live-provider dependency; SQL-gated tests skip when no SQL Server connection string is configured.

**Residual risks**:
- SQL Server migration application and SQL-gated retrieval tests were not validated in this run because `ConnectionStrings__DefaultConnection` was unset.
- Production vector retrieval provider selection, semantic quality tuning, hybrid search, query-text embedding orchestration, prompt construction, citations, chat API, and full RAG orchestration remain future work.

## Sprint 16 Issue #29 Disposition

Eligible organization-scoped semantic retrieval is implemented as an Application-level service only. The service validates authentication, re-reads active persisted user access state, requires `Chat.AskQuestion`, uses `UserAccessState.OrganizationId` as the authoritative scope, hashes trimmed query text, generates a query embedding with the fake-compatible `IEmbeddingProvider`, calls semantic search with QueryVector only, bulk revalidates returned candidates with `IRetrievalEligibilityRepository`, excludes cross-organization and stale candidates, assigns rank only after filtering, and emits an insufficient-result signal when no authorized candidates remain.

RISK-006 (cross-organization retrieval leakage): **Mitigated for Sprint 16 retrieval orchestration.** Organization scope comes from active persisted user state, cross-organization semantic candidates are excluded before database revalidation, and the EF repository revalidates document, chunk, embedding, and candidate identities in the requested organization.

RISK-014 (embedding failure makes chunks incorrectly retrievable): **Mitigated.** Application revalidation requires embedding `Ready` and index `Indexed` in addition to the Sprint 15 provider filter.

RISK-015 (vector/index state inconsistent with source records): **Mitigated for Sprint 16.** Stale semantic candidates are excluded after bulk revalidation, including missing embeddings, unindexed embeddings, disabled/deleted/unprocessed documents, and identity mismatches.

RISK-018 (insufficient results not handled safely): **Partially mitigated.** The eligible retrieval service returns `IsInsufficientResult = true` with no candidates when no authorized chunks remain; future chat/RAG must turn that signal into the canonical user-facing insufficient-context response.

RISK-020 (SDK coupling): **Guarded.** The Application service depends only on abstractions; no provider SDK package or Infrastructure reference is introduced.

RISK-025 (CI live-provider risk): **Guarded.** Query embedding remains fake-provider-compatible and semantic retrieval remains local/testable; no live provider dependency is required for normal tests.

**Residual risks**:
- SQL-gated eligibility repository tests were not validated in this run because `ConnectionStrings__DefaultConnection` was unset.
- Physical `retrieval_results` persistence remains deferred until the chat interaction sprint where `chat_interaction_id` exists.
- Future prompt construction must consume only the final authorized candidate set returned by the Sprint 16 service.
- Fake embeddings remain deterministic but not semantically production-quality; production retrieval provider selection and full RAG answer generation remain future work.

## Sprint 18 Issue #37 Disposition

RAG prompt building and defense-in-depth authorization are fully implemented. `GroundedPromptBuilder` applies `IPromptAuthorizationFilter.IsChunkAuthorizedForPrompt` for every candidate chunk — a second org-scope enforcement gate after the orchestration-level filter applied in Step 11 of `RagChatOrchestrationService`. Chunks that fail the filter are excluded and counted as `ExcludedChunkCount`. `ContextSufficiencyPolicy` returns `IsSufficient=false` when zero authorized chunks are available. `InsufficientContextFallbackText` is returned in `AskQuestionResponse.AnswerText` for InsufficientContext outcomes (`ChatInteraction.AnswerText` remains null per spec). `AuditEventTypes.PromptBuildFailed` is emitted if prompt build fails but not logged with chunk/question/answer text. `PromptVersion="rag-grounded-v1"` is stored only for Grounded outcomes via the extended `RecordGroundedOutcome` parameter.

RISK "Authorization may be skipped during prompt construction": **Resolved.** `DefaultPromptAuthorizationFilter` (org-scope equality) is registered as `IPromptAuthorizationFilter` singleton in Application DI and injected into `GroundedPromptBuilder`. `GroundedPromptBuilderTests.GroundedPromptBuilder_ExcludesChunksThatFailPromptAuthorizationFilter` and `GroundedPromptBuilder_ReturnsFailureResultWhenNoChunksPassFilter` enforce this behavior.

**Residual risks**:
- Citation mapping and retrieval result persistence (linking `chat_interactions` to retrieved chunks) remain deferred to Sprint 19.
- Angular chat UI and HTTP chat endpoint remain deferred to Sprint 20.
- Real Azure OpenAI answer generation adapter remains deferred to Phase 2+.
- SQL-gated chat persistence integration tests require `ConnectionStrings__DefaultConnection`; validate before or during PR review.

## Sprint 17 Issue #36 Disposition

RAG chat domain model and application orchestration are fully implemented. `RagChatOrchestrationService` enforces retrieval-before-generation as an invariant: retrieval must complete before the generator is called; `IsInsufficientResult = true` skips the generator; retrieval failure also skips the generator. Organization scope is sourced exclusively from `UserAccessState` (persisted database state), not JWT claims. Chunk text fetched for generation is organization-scoped via `EfChunkTextReader.GetChunkTextsAsync(chunkIds, organizationId)`. Provider exceptions are caught and stored as `ProviderFailed` with a safe failure code — no exception message or stack trace is persisted or returned to the caller. All cost/token/latency fields are `nullable` types; zero is never stored for unavailable values. `FakeAnswerGenerator` is SHA-256 deterministic, requires no network, and is CI-safe.

RISK-018 (insufficient results not handled safely): **Resolved for Sprint 17.** `RagChatOrchestrationService` checks `IsInsufficientResult` before calling the generator and stores `AnswerState.InsufficientContext` with null answer text. The test `RagChatOrchestrationService_DoesNotGenerateWhenRetrievalInsufficient` enforces this.

RISK-006 (cross-organization retrieval leakage): **Mitigated for Sprint 17 generation path.** After retrieval, the orchestration service re-filters candidates to `OrganizationId == activeState.OrganizationId` before passing chunk IDs to `EfChunkTextReader`. Chunk text lookup also enforces `organization_id` at query level. The test `RagChatOrchestrationService_DoesNotPassUnauthorizedChunksToGenerator` enforces this.

RISK-020 (SDK coupling): **Guarded for Sprint 17.** `IAiAnswerGenerator` is Application-owned. `FakeAnswerGenerator` in Infrastructure has no external SDK. Assembly dependency tests continue to reject Azure AI, OpenAI, and Semantic Kernel references from Application and Domain.

RISK-025 (CI live-provider risk): **Guarded for Sprint 17.** `FakeAnswerGenerator` is registered for tests and default configuration; no live API key is needed for normal CI.

**Residual risks**:
- Prompt construction details (system prompt template, context window management, token budget) are deferred to Sprint 18; current implementation passes chunk text directly to generator without a final prompt builder.
- Physical `retrieval_results` persistence (linking interactions to specific retrieved chunks for citation traceability) remains deferred to Sprint 19 citation sprint.
- Citation mapping and citation persistence remain deferred to Sprint 19.
- Angular chat UI and public HTTP chat endpoint remain deferred to Sprint 20.
- Real Azure OpenAI answer generation adapter remains deferred to Phase 2+.
- SQL-gated chat persistence integration tests were not run in this session (require `ConnectionStrings__DefaultConnection`); validate against running SQL Server before or during PR review.

## Sprint 13 Issue #23 Disposition

Text extraction and chunking are fully implemented. `ExtractAndChunkDocumentProcessingStep` replaces `PlaceholderDocumentProcessingStep`; documents now reach `Processed` status with real text extraction and chunk persistence. The Sprint 12 residual risk "PlaceholderDocumentProcessingStep does nothing" is **resolved**.

Chunk persistence and `MarkProcessedAsync` are atomic via `IDocumentProcessingTransactionFactory` / `EfDocumentProcessingTransactionFactory`; rollback on any step or commit failure ensures chunks are never saved without a corresponding `Processed` status update. `StorageLocation` is carried in `ManagedDocument` only and is never exposed through API DTOs, Angular UI, logs, audit events, or failure reasons. Unsupported content types and extraction/chunking failures produce controlled safe-message exceptions; failure reasons passed to `MarkFailedAsync` use only `ex.Message` trimmed to 200 characters.

**New residual risks**:
- PDF and DOCX extraction are deferred; affected documents reach `Failed` status with the message "Unsupported document format for text extraction." Acceptable and required by Sprint 13 scope; extractor implementations for non-text formats are deferred to Phase 2+.
- `IsRetrievalEnabled` remains `false` after processing completes; no re-enable endpoint or retrieval workflow exists. `Document.IsEligibleForRetrieval()` encodes the predicate; re-enable is deferred to Phase 2+.
- `LocalDocumentStorage` local-filesystem-only limitation carries over from Sprint 11; no production cloud storage adapter exists.

## Sprint 21 Issue #41 Disposition

Secure chat history and interaction detail views are implemented. `ChatHistoryService` enforces organization scope from `UserAccessState`, own-only access for Agent and KnowledgeAdmin, and scoped-reviewer access for Supervisor/Manager/Admin via `Chat.ViewScopedHistory`. Cross-organization access returns null (safe 404). Historical citations remain visible independent of source document retrieval state. No new AI generation, prompt construction, or retrieval behavior was introduced. The deferred `Chat.ViewInteraction`/`Chat.ViewCitations` own-only-vs-scoped enforcement (flagged in Sprint 7 with a code comment "deferred to Sprint 17+") is now **resolved for Sprint 21** through `ChatHistoryService.IsAuthorizedForResource`.

The KnowledgeAdmin ambiguity (flagged in the pre-implementation audit) was resolved by explicit decision: KnowledgeAdmin is own-only for Sprint 21 chat history access. The `Chat.ViewScopedHistory` absence from KnowledgeAdmin's permission matrix is the authoritative signal.

## Sprint 20 Issue #40 Disposition

The RAG generic-chatbot risk is mitigated for the exposed Sprint 20 chat surface. `POST /api/v1/chat/questions` is authenticated and permission-gated with `Chat.AskQuestion`, delegates to `IRagChatOrchestrationService`, returns real citations for grounded answers, returns safe insufficient-context and provider-failure outcomes, and does not expose provider payloads or raw source content. The Angular `/chat` page presents the assistant as an internal approved-document assistant, renders metadata-only citations, keeps session continuity in component state only, and avoids final-authority language. Future feedback, dashboard, and history surfaces must preserve these same controls.

## Update Rule

Read this file for Level 3 work and release review. Update risk status, mitigation or new issue references when implementation evidence changes the risk.
