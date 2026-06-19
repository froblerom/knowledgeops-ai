# Portfolio Review Guide

A structured path through KnowledgeOps-AI for technical evaluators. The project has many moving parts; this guide surfaces the most signal-dense areas first.

---

## 5-Minute Review

1. **Read the [Executive Summary](00-executive-summary.md).** Understand the business problem and the scope boundary (what is MVP, what is Phase 2, what is out of scope entirely).
2. **View the grounded answer screenshot** ([docs/screenshots/chat-grounded-answer.png](screenshots/chat-grounded-answer.png)). The answer cites an exact document chunk — nothing is invented.
3. **View the insufficient-context screenshot** ([docs/screenshots/chat-insufficient-context.png](screenshots/chat-insufficient-context.png)). The system refuses to answer rather than hallucinating when no relevant chunk is found.
4. **Check the C4 container diagram** ([docs/diagrams/architecture/c4-container-diagram.png](diagrams/architecture/c4-container-diagram.png)) and [C4 architecture doc](12-c4-architecture.md). Five containers: API, Worker, Angular SPA, SQL Server, local file storage.
5. **Check GitHub Actions CI** ([.github/workflows/ci.yml](../.github/workflows/ci.yml)). Green without any external API calls or secrets.

---

## 15-Minute Technical Review

### 1. Clean Architecture structure
Open `src/`. Each project has a single responsibility and the dependency rule is enforced:
- `KnowledgeOps.Domain` — entities, value objects, no dependencies
- `KnowledgeOps.Application` — use cases, interfaces, orchestration; depends only on Domain
- `KnowledgeOps.Infrastructure` — EF Core, AI providers, file storage; implements Application interfaces
- `KnowledgeOps.Api` — controllers, middleware, DI composition
- `KnowledgeOps.Worker` — background document processing; registers only `AddApplicationCore()`, explicitly excluding `ICurrentUser`-dependent services

The DI split is guarded by `tests/KnowledgeOps.Application.Tests/DI/WorkerDiCompositionTests.cs` — a test that fails at build time if a core service accidentally injects `ICurrentUser`.

### 2. RAG orchestration
Start at `src/KnowledgeOps.Application/Chat/` — specifically the `AskQuestionCommandHandler` and the retrieval pipeline. The grounded prompt is constructed from retrieved chunks only; the AI is not given access to external knowledge. Source citations are returned as structured data alongside the answer, not embedded in text.

### 3. Document processing Worker
`src/KnowledgeOps.Worker/` and `src/KnowledgeOps.Application/Documents/` show the three-step pipeline:
1. `ExtractAndChunkDocumentProcessingStep` — text extraction and chunking
2. `GenerateChunkEmbeddingsProcessingStep` — embedding generation per chunk
3. `IndexDocumentChunkEmbeddingsProcessingStep` — marks embeddings ready for vector search (runs before retrieval is enabled; `SearchAsync` enforces the retrieval guard separately)

### 4. Authorization and organization scope
`src/KnowledgeOps.Application/Authorization/KnowledgeOpsPermissions.cs` — granular permission constants.  
`src/KnowledgeOps.Application/Authorization/RolePermissionMatrix.cs` — maps five roles to permission sets.  
`src/KnowledgeOps.Application/Authorization/OrganizationScopeService.cs` — every query is scoped to the authenticated user's `OrganizationId`; cross-organization data access is architecturally impossible, not just a missing check.

### 5. Tests and deterministic AI providers
`tests/` contains unit, integration, and E2E suites. The key design decision: CI uses `DemoGroundedAnswerGenerator` (deterministic, extractive, no API key) for answer generation and `FakeEmbeddingProvider` (fixed-dimension vectors, no API key) for embeddings. This means the full test suite runs on GitHub Actions without any secrets. See `tests/KnowledgeOps.Application.Tests/Documents/` for processing pipeline tests and `tests/KnowledgeOps.IntegrationTests/` for SQL-backed integration tests.

---

## What This Project Proves

| Claim | Evidence |
|---|---|
| Business-driven design, not tutorial code | [Executive Summary](00-executive-summary.md) + [Scope & Roadmap](05-scope-and-roadmap.md) with explicit Phase 2 / Phase 3 boundaries |
| Clean Architecture enforced structurally | `AddApplicationCore()` / `AddApplicationApiFeatures()` split, `WorkerDiCompositionTests` compile-time guard |
| Pluggable AI without vendor lock-in | `IAiAnswerGenerator` abstraction with Demo, OpenAI, and local Ollama/Qwen implementations, switched via config |
| Safe RAG — no hallucination path | Insufficient-context response path is a first-class feature, not an afterthought |
| Multi-tenant isolation | `OrganizationScopeService` enforces org boundaries at the application layer on every query |
| RBAC beyond role guards | Permission matrix with 31 granular permissions mapped to five roles; `[RequirePermission]` on every endpoint |
| Observability from day one | Structured audit events, correlation ID middleware, sanitized health diagnostics (no secrets in output) |
| CI without secrets | Full test suite runs green on GitHub Actions with deterministic AI stubs |

---

*For the full architecture decision record, see [docs/decisions/](decisions/).*
