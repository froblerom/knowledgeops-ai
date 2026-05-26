# Open Implementation Risks

Last updated: 2026-05-26

| Risk | Severity | Related Area | Mitigation | Status |
| --- | --- | --- | --- | --- |
| Agent prompts may load excessive context and lose focus. | Medium | Harness / all future issues | Use `13-prompt-classifier.md`, level-based routing and minimum context bundles. | Open |
| RAG may be implemented as unsupported general answer behavior rather than grounded document assistance. | High | Retrieval/RAG | Use `rag-implementation-agent.md`, citation and insufficient-context rules, and fake-provider safety tests. | Open |
| Authorization may be skipped during retrieval or prompt construction. | Critical | Security/RAG | Load security, business-rules and RAG contexts for relevant tasks; require cross-scope tests and verification. | Open |
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

## Update Rule

Read this file for Level 3 work and release review. Update risk status, mitigation or new issue references when implementation evidence changes the risk.
