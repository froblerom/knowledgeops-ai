# Backend Context

## Backend Baseline

- .NET 10 and ASP.NET Core Web API.
- Clean Architecture projects: API, Application, Domain, Infrastructure and Worker.
- SQL Server with EF Core implementations in Infrastructure.
- Provider abstractions for storage, extraction, embeddings, retrieval/vector operations, AI generation, cost estimation, observability and secrets.

## Layer Guidance

| Area | Rule |
| --- | --- |
| API | Expose `/api/v1` DTO contracts, middleware and thin controllers only. |
| Application | Coordinate commands, queries, services, validation, authorization and orchestration. |
| Domain | Hold core language and rules without framework/provider dependencies. |
| Infrastructure | Implement EF Core, SQL Server and provider adapters. |
| Worker | Run asynchronous document processing through Application behavior. |

## Required Backend Rules

- Do not put business rules, retrieval filtering, prompt construction or RAG orchestration in controllers.
- Do not return EF Core entities as API DTOs.
- Do not use Infrastructure or provider SDK types in Domain/Application contracts.
- Enforce authentication, role permission and organization scope in protected workflows.
- Enforce eligibility and scope before retrieved context can be used in a prompt.
- Preserve safe errors, correlation IDs and audit-sensitive event handling.

## API Anchors

- Base path: `/api/v1`.
- Basic health: `GET /api/v1/health`.
- Admin-only sanitized health details: `GET /api/v1/health/details`.
- For exact endpoints and DTO/error behavior, read `docs/15-api-design.md`.
- For exact permission behavior, read `docs/16-security-and-permissions.md`.

## Processing And RAG Placement

- Worker owns execution of asynchronous ingestion; Application owns use-case coordination.
- AI and retrieval orchestration belongs in Application using abstractions.
- Infrastructure adapters may use SDKs and technical configuration.
- Automated backend tests should use fake AI providers by default.

## Known EF Core Pitfalls

### Shared DbContext audit-write ordering rule

`EfAuditEventWriter.WriteAsync` calls `dbContext.SaveChangesAsync()` on the shared scoped `KnowledgeOpsDbContext`. If any other tracked entity has pending changes in the same context (e.g. citations tracked as `Added` from a prior step), EF Core flushes all tracked entities in one batch. Without an explicit transaction, SQL Server auto-commits each INSERT independently — the audit entry is committed first, then the child rows fail with an FK violation if their parent row does not yet exist.

**Rule: audit the completion of an action only after the action's persistence has been committed.**

Calling `AuditAsync(ActionCompleted, ...)` before `PersistAsync(...)` in the grounded chat path caused `FK_citations_chat_interactions_chat_interaction_id` failures on every grounded answer (fixed 2026-06-18 in `RagChatOrchestrationService`). Any future orchestration step that emits an audit event before persisting a parent row will reproduce this pattern.

## Sources

- ADR-001, ADR-002, ADR-004 through ADR-008, ADR-010.
- `docs/11-architecture-overview.md`, `docs/14-database-design.md`, `docs/15-api-design.md`, `docs/16-security-and-permissions.md`, `docs/22-implementation-guardrails.md`.

