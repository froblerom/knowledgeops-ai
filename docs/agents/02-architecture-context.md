# Architecture Context

## Purpose

Use this context for architecture-sensitive work. Read exact ADRs and architecture documents before changing a boundary or accepted decision.

## Architecture Baseline

- Backend: .NET 10 and ASP.NET Core Web API using Clean Architecture.
- Frontend: Angular, separated from backend business and security enforcement.
- Persistence: SQL Server through EF Core implementations in Infrastructure.
- Processing: background Worker for asynchronous document ingestion.
- AI and retrieval: application-facing abstractions with provider implementations in Infrastructure.
- Delivery: Docker/local environment and GitHub Actions, structured to be Azure-ready.

## Intended Backend Structure

```text
src/
  KnowledgeOps.Api/
  KnowledgeOps.Application/
  KnowledgeOps.Domain/
  KnowledgeOps.Infrastructure/
  KnowledgeOps.Worker/
```

## Layer Responsibilities

| Layer | Responsibility |
| --- | --- |
| Domain | Core concepts, lifecycle invariants and business behavior independent of technology. |
| Application | Commands, queries, handlers/services, validation, orchestration, authorization coordination and interfaces. |
| Infrastructure | EF Core/SQL Server, storage, extraction, embedding, vector/retrieval, AI, telemetry and secret adapters. |
| API | Thin HTTP transport endpoints, DTO mapping and middleware integration. |
| Worker | Executes asynchronous ingestion work through Application behavior. |
| Angular | User interface and API client behavior; not business-rule or authorization authority. |

## Dependency Rules

- Domain must not depend on API, Infrastructure, EF Core, SQL Server, ASP.NET Core, provider SDKs or Angular.
- Application must not depend on provider SDKs or EF Core implementations.
- Infrastructure implements abstractions owned inward.
- Controllers must remain thin; business rules, retrieval filtering, prompts and RAG orchestration do not belong in controllers.
- Angular must not be treated as a security boundary.

## Provider Isolation

Keep SDK-specific details in Infrastructure for:

- AI answer generation and embeddings.
- Document storage and text extraction.
- Vector/retrieval storage.
- Observability and secret providers.

## Canonical Sources

- Architecture: `docs/11-architecture-overview.md`, `docs/12-c4-architecture.md`.
- Implementation boundaries: `docs/22-implementation-guardrails.md`.
- ADRs: `docs/decisions/ADR-001-use-clean-architecture.md` through `ADR-010-use-organization-scoped-access-boundaries.md`.
- Diagrams: Mermaid Markdown is source of truth; rendered images are artifacts.

