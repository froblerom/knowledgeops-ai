# Implementation Guardrails

## Purpose

This document is the canonical implementation guardrails document for **KnowledgeOps-AI** MVP delivery.

KnowledgeOps-AI is an internal, AI-powered knowledge assistant for contact centers and support operations. It transforms authorized internal documents into searchable knowledge through asynchronous document processing, chunking, embeddings, organization-scoped retrieval, and Retrieval-Augmented Generation (RAG). The MVP returns grounded answers with citations, handles insufficient context safely, stores feedback and chat history, exposes basic operational signals, and protects access through authentication, RBAC, and organization boundaries.

Use these guardrails when:

- Creating implementation issues.
- Planning branches.
- Opening or reviewing pull requests.
- Evaluating Definition of Done.
- Updating implementation documentation.
- Giving tasks to AI coding agents.

This document applies the approved roadmap in `docs/21-implementation-roadmap.md` and must be interpreted with the canonical requirements, architecture, API, security, testing, DevOps, observability, risk, and ADR documents listed below.

Classification:

- Task type: Documentation generation.
- Scope: Implementation guardrails only.
- Implementation level: Pre-implementation governance.
- Subagents: Not used. No repository rule requiring subagents is available.

## Repository Implementation Workflow

Implementation work is issue-driven. A sprint in `docs/21-implementation-roadmap.md` identifies planned delivery order; future implementation issues define the bounded work that may be changed.

### Required Issue Content

Each implementation issue should define:

- Scope.
- Out of scope.
- Acceptance criteria.
- Validation expectations.
- Related documentation and context.
- Related sprint from `docs/21-implementation-roadmap.md`.

An issue should also identify affected architecture boundaries, data/security risks, and any deferred functionality that must remain excluded.

### Branch Naming

Recommended branch names:

```text
docs/issue-<number>-short-description
feature/issue-<number>-short-description
fix/issue-<number>-short-description
chore/issue-<number>-short-description
test/issue-<number>-short-description
ci/issue-<number>-short-description
```

Branches should remain limited to the related issue scope. Pull requests should reference and close the related issue, identify validation performed, and state any intentionally deferred follow-up.

### Commit Examples

```text
docs: add implementation guardrails
chore: update local setup instructions
feat: add document upload command
feat: add RAG chat endpoint
test: add retrieval authorization tests
ci: add backend validation pipeline
fix: enforce retrieval eligibility rule
```

### Issue And Pull Request Review Gate

Before merging a future implementation change, reviewers should confirm:

- The work belongs to the named roadmap sprint or an approved correction.
- The issue does not silently expand MVP scope.
- Relevant acceptance criteria and validation have been completed.
- Architecture, authorization, organization scope, lifecycle, RAG safety, testing, and DevOps guardrails remain satisfied.
- A documentation or ADR update accompanies any approved contract or architectural change.

## Backend Guardrails

### Intended Backend Structure

```text
src/
  KnowledgeOps.Api/
  KnowledgeOps.Application/
  KnowledgeOps.Domain/
  KnowledgeOps.Infrastructure/
  KnowledgeOps.Worker/

tests/
  KnowledgeOps.Domain.Tests/
  KnowledgeOps.Application.Tests/
  KnowledgeOps.Api.Tests/
  KnowledgeOps.IntegrationTests/
  KnowledgeOps.E2ETests/
```

### Technology And Architecture Baseline

Future backend implementation uses:

- .NET 10.
- ASP.NET Core Web API.
- Clean Architecture.
- SQL Server.
- Entity Framework Core in Infrastructure.
- A background worker for asynchronous document processing.
- Provider abstractions for AI, document storage, retrieval/vector storage, observability, and secrets.

### Dependency Rules

- Domain must not depend on API.
- Domain must not depend on Infrastructure.
- Domain must not depend on EF Core.
- Domain must not depend on SQL Server.
- Domain must not depend on ASP.NET Core.
- Domain must not depend on JWT or other security infrastructure.
- Domain must not depend on logging infrastructure.
- Domain must not depend on Angular.
- Application coordinates use cases through commands, queries, handlers, validators, authorization coordination, and observability or audit coordination.
- Application must expose abstractions required from Infrastructure rather than importing provider or storage SDKs.
- Infrastructure implements persistence, storage, AI providers, vector/retrieval providers, secrets, and technical services behind Application abstractions.
- API exposes transport endpoints and delegates behavior inward.
- Worker executes asynchronous document processing and delegates business behavior to Application services.

### Controller Rules

- Controllers stay thin.
- Controllers map requests, invoke application commands/queries/services, and map responses.
- Business rules do not live in controllers.
- RAG orchestration does not live in controllers.
- Retrieval filtering does not live in controllers.
- Prompt construction does not live in controllers.
- Provider SDK calls do not live in controllers.
- Persistence logic does not live in controllers.
- Backend authorization is the source of truth.

## Frontend Guardrails

### Intended Frontend Structure

```text
frontend/
  src/
    app/
      core/
      shared/
      features/
        auth/
        documents/
        chat/
        dashboard/
        admin/
```

### Technology Baseline

Future frontend implementation uses:

- Angular.
- TypeScript.
- RxJS.
- Angular Router.
- Reactive Forms.
- Guards where appropriate.
- Interceptors where appropriate.

### UI Foundation

- Angular is the selected MVP frontend framework under ADR-003.
- Do not present React as an open implementation choice.
- Angular Material may be used as the primary UI component foundation if selected during implementation.
- Tailwind or other UI framework additions require an explicit future decision if not already approved.
- Foundation work may add application shell, routing, guards, interceptors, shared components, layout, error handling, and placeholder pages.
- Business screens should be added only in explicitly scoped feature issues.

### Frontend Security Boundary

- Frontend role and scope checks are user experience controls only.
- Hiding a route, navigation item, button, or field does not authorize an action.
- Frontend visibility never replaces backend authorization.
- Backend authorization remains the security boundary for all protected data and operations.

### AI Experience Rules

- The UI must display citations clearly for grounded answers.
- The UI must display insufficient-context outcomes clearly.
- The UI must not imply AI answers are final policy, business, HR, legal, compliance, or operational authority.
- The UI must not display content that the backend has not authorized for the current user and organization scope.

## Database Guardrails

### Technology Baseline

- SQL Server is the primary relational database.
- Entity Framework Core is the primary data access and migration technology.
- EF Core belongs in `KnowledgeOps.Infrastructure`.

### Dependency Rules

- Application must not depend directly on EF Core.
- Domain must not depend directly on EF Core.
- `DbContext`, entity configurations, migrations, SQL Server-specific behavior, and query-provider implementation details belong in Infrastructure.
- API contracts must not be persistence entity contracts.

### Canonical Document Lifecycle

`processing_status` may contain only:

```text
Uploaded
Processing
Processed
Failed
```

### Retrieval Eligibility

A document is retrievable only when all of the following are true:

```text
processing_status = Processed
is_retrieval_enabled = true
deleted_at IS NULL, where soft delete applies
organization scope authorizes access
```

Important rules:

- Do not model `Disabled` as a document `processing_status`.
- Disablement from retrieval means setting `is_retrieval_enabled = false`.
- Uploaded, processing, failed, retrieval-disabled, soft-deleted, and unauthorized documents must not be used as retrieval sources.
- Historical citations may remain available according to retention and access rules even when future retrieval from a source is disabled.

### Migration Rules

- Future migrations should be reviewed before merge.
- Future migrations should be clearly named.
- Future migrations should be tested locally against SQL Server when migration work exists.
- Avoid destructive changes unless the issue and pull request explicitly justify them.
- Preserve historical traceability for documents, chat interactions, retrieval results, citations, feedback, and audit entries.
- Do not add Phase 2 tables or workflows to the MVP merely because they appear as future logical concepts.

### Data Integrity Rules

- Organization-scoped tables must include `organization_id` where required by `docs/14-database-design.md`.
- Documents, chunks, embeddings, retrieval results, citations, chat data, feedback, dashboard data, knowledge-gap signals if later introduced, and audit data where applicable must respect organization boundaries.
- Retrieval results must reference source chunks and documents.
- Citations must reference source chunks and documents.
- Feedback must belong to a chat interaction and the submitting user.
- Duplicate feedback must not inflate metrics unless a defined update behavior applies.
- `failure_reason` should be recorded safely when document processing fails.
- Estimated AI cost must be nullable when unavailable and must not be represented misleadingly as zero.
- Sensitive prompt or document content must not be stored casually as diagnostic metadata.

## API Guardrails

### Route Baseline

Business API routes use:

```text
/api/v1
```

Canonical API areas include:

```text
/api/v1/auth
/api/v1/users
/api/v1/documents
/api/v1/chat
/api/v1/dashboard
/api/v1/feedback
/api/v1/health
```

Administrative operational endpoints defined by `docs/15-api-design.md` may use `/api/v1/admin` only within the documented permission model.

Canonical health routes are:

```text
GET /api/v1/health
GET /api/v1/health/details
```

### API Rules

- Controllers stay thin.
- Request and response DTOs must be explicit.
- Do not return EF Core entities directly.
- Success response shapes should remain consistent with the API contract.
- Error response shapes should follow `docs/15-api-design.md`.
- Protected endpoints require authentication.
- Authorization combines role permissions and organization scope.
- Backend authorization is the source of truth.
- Frontend visibility never replaces backend authorization.
- API contracts must not expose provider SDK response objects.
- API errors must not expose secrets, raw provider payloads, full prompt content, sensitive document text, raw exception traces, or connection strings.
- Endpoint additions or changes require review against `docs/15-api-design.md` and `docs/16-security-and-permissions.md`.

## Security and Permissions Guardrails

### Canonical MVP Technical Roles

The only MVP technical RBAC roles are:

- `Agent`
- `Supervisor`
- `KnowledgeAdmin`
- `Manager`
- `Admin`

QA, Trainer, Viewer, Compliance Reviewer, leadership, recruiter, portfolio reviewer, and AI coding agent labels may describe stakeholders, readers, or future personas. Do not add them as MVP RBAC roles unless future approved documentation and any necessary ADR change explicitly authorize that scope.

### Authorization Principles

- Deny by default.
- Apply least privilege.
- Backend authorization is the source of truth.
- Role permission alone is insufficient.
- Organization scope alone is insufficient.
- Protected workflows require authentication, role permission, and organization scope.
- Apply authorization before retrieval and before prompt construction.
- Citations must not expose unauthorized sources.
- Chat history and feedback must not expose unauthorized interactions.
- Dashboard metrics must not leak cross-organization data.
- Health details are `Admin`-only.
- Audit logs are `Admin`-only unless future scope explicitly defines otherwise.
- `Admin` remains organization-scoped for MVP unless a future cross-organization model is documented and approved.

### Protected Data Surface

Organization scope must be enforced for protected records where applicable, including:

- Documents.
- Document chunks.
- Embeddings.
- Retrieval results.
- Citations.
- Chat sessions.
- Chat interactions.
- Feedback.
- Dashboard metrics.
- Audit records.
- Future knowledge-gap signals, if implemented in a later approved phase.

## AI / RAG Guardrails

This section is mandatory for every future AI, retrieval, chat, prompt, citation, feedback-evaluation, or AI-observability issue.

### Product And Safety Rules

- KnowledgeOps-AI is not a generic chatbot.
- RAG chat must retrieve authorized context before answer generation.
- Prompt construction may use only authorized, eligible retrieved chunks.
- Grounded answers must include source citations.
- If context is insufficient, return an insufficient-context response.
- The assistant must not invent official policy or unsupported source claims.
- The assistant must not act as final business, HR, legal, compliance, or operational authority.
- Do not send unauthorized document content to an AI provider.
- Provider metadata may support observability and cost visibility but must not define business rules.

### Abstraction Rules

AI, storage, extraction, and retrieval provider SDKs must remain in Infrastructure. Application should depend on suitable abstractions, including as appropriate:

```text
IEmbeddingProvider
IAiAnswerGenerator
IRetrievalService
IVectorIndex
ICostEstimator
IDocumentStorage
IDocumentTextExtractor
```

### Test And Logging Rules

- Automated tests should use fake providers.
- Normal CI must not require live Azure OpenAI or OpenAI calls.
- Optional manual or controlled validation may use a configured provider only when secrets and cost limits are handled safely.
- Do not log full prompt content.
- Do not log full document chunks.
- Do not log provider secrets or raw sensitive payloads.

## Document Processing Guardrails

Document processing is asynchronous.

### Required Lifecycle Flow

1. Upload stores the file and document metadata.
2. Upload sets `processing_status = Uploaded`.
3. The worker moves the document to `Processing`.
4. The worker extracts text.
5. The worker splits valid text into source-traceable chunks.
6. The worker generates embeddings behind a provider abstraction.
7. The worker stores searchable embedding data or vector references behind the retrieval design.
8. The worker sets `Processed` on success.
9. The worker sets `Failed` with a safe failure reason on failure.

### Processing Rules

- Upload does not imply immediate retrievability.
- `Uploaded`, `Processing`, and `Failed` documents are not retrievable.
- A `Processed` document is retrievable only if `is_retrieval_enabled = true`, it is not soft-deleted where that applies, and organization scope authorizes access.
- Disabling retrieval must not change processing status.
- Empty chunks must not be stored.
- Chunks and embeddings must retain source document and organization relationships.
- Processing failures must be visible to authorized users without leaking sensitive content.
- Re-enable and processing-retry operations remain deferred to Phase 2 unless scope is approved later.

## Observability and Supportability Guardrails

### Operational Signals

Observability should support:

- Document upload accepted and rejected events.
- Processing started, completed, and failed events.
- Text extraction failures.
- Embedding generation failures.
- Retrieval completion and failure.
- AI generation completion and failure.
- Insufficient-context events.
- Feedback submission.
- Authentication and authorization failures.
- Organization-scope failures.
- Dashboard access or usage where useful and safe.
- Health checks and detailed health access.
- Audit-log access and privileged administrative changes.
- Correlation IDs.
- Processing, retrieval, generation, and total RAG latency.
- Estimated AI cost when available.
- Token usage when available.

### Logging Rules

- Use structured logs.
- Include correlation IDs where practical across API, Worker, retrieval, and provider interactions.
- Avoid logging full document text.
- Avoid logging full prompt content.
- Avoid secrets, tokens, connection strings, API keys, and passwords.
- Sanitize provider failures and exception details.
- Keep audit-sensitive records append-oriented and organization-scoped where applicable.

### Health Endpoint Rules

- `GET /api/v1/health` exposes only safe basic status and may follow deployment policy for public or authenticated exposure.
- `GET /api/v1/health/details` is `Admin`-only.
- Detailed health must not expose secrets, provider keys, raw stack traces, raw prompt or document content, connection strings, or sensitive configuration.

## Testing Guardrails By Issue Type

Every future issue should define validation expectations before implementation begins. Test depth should match behavioral risk and the layers affected.

### Documentation Issues

- Review Markdown clarity, headings, links, terminology, and scope alignment.
- Confirm no source code, package, migration, Docker, or GitHub Actions changes were included unless explicitly scoped.
- Confirm accepted ADRs and canonical source documents remain aligned.

### Backend Issues

- Run relevant .NET build and test commands when projects exist.
- Add or update unit tests for Domain and Application rules.
- Add integration or API tests for persistence, authentication, authorization, organization scope, lifecycle, retrieval eligibility, citations, feedback, and dashboard behavior where applicable.
- Include negative authorization and cross-scope tests where applicable.
- Confirm controllers remain thin.
- Confirm Infrastructure details do not leak into Domain or Application.

### Frontend Issues

- Run relevant Angular build, lint, and test commands when frontend projects and scripts exist.
- Test route guards, interceptors, forms, validation, loading states, error states, role-aware visibility, citation display, and insufficient-context display where applicable.
- Confirm UI role and scope checks remain UX-only.
- Confirm protected behavior is still denied by backend tests when the UI is bypassed.

### Database Issues

- Test migrations locally against SQL Server when migrations are part of the issue.
- Verify migration names are clear.
- Confirm Domain and Application do not gain EF Core dependencies.
- Confirm destructive database changes are avoided or explicitly justified.
- Validate organization scope, lifecycle values, retrieval eligibility, foreign-key traceability, and historical relationships.

### AI / RAG Issues

- Use fake providers in automated tests.
- Test retrieval before generation.
- Test the no-answer or safe-response path for insufficient context.
- Test citations for grounded answers.
- Test that unauthorized or ineligible chunks are excluded before prompt construction.
- Test provider failure handling safely.
- Confirm no live AI calls are required in normal CI.

### DevOps Issues

- Validate local setup and CI commands affected by the issue.
- Confirm no secrets are committed.
- Confirm pipeline changes are minimal and documented.
- Confirm CI does not require live AI providers unless a separate optional/manual job is explicitly approved.
- Confirm Docker and configuration changes remain Azure-ready without adding unapproved production hardening scope.

### Observability Issues

- Validate health, structured logging, correlation ID, and error behavior.
- Confirm logs avoid sensitive content.
- Confirm detailed health is restricted to `Admin`.
- Confirm operational signals support troubleshooting without leaking protected data.
- Confirm scoped metrics or audit data do not leak across organizations.

## Definition of Done

A future implementation issue is done when:

- The work matches the related issue scope and roadmap sprint.
- Out-of-scope items were not added.
- Acceptance criteria are satisfied.
- Validation requested by the issue was completed, or an explicit reason for any omitted validation is documented.
- The pull request references and closes the related issue.
- The branch name includes the issue number.
- Code, documentation, configuration, and templates follow established conventions.
- Backend authorization remains the source of truth for protected behavior.
- Organization scope is enforced where applicable.
- Business rules are implemented in Domain or Application, not controllers or Angular.
- Controllers remain thin when endpoints are involved.
- Domain and Application dependency boundaries are preserved.
- EF Core remains isolated to Infrastructure.
- AI, storage, and retrieval provider SDKs remain isolated to Infrastructure.
- Retrieval excludes unauthorized, uploaded, processing, failed, soft-deleted, or retrieval-disabled documents.
- Prompt construction uses only authorized retrieved context.
- Grounded answers include citations.
- Insufficient context is handled safely.
- Tests are added or updated according to issue type and risk.
- Local validation commands are run when relevant projects exist.
- Health, logging, correlation ID, audit, and error behavior remain safe where affected.
- Documentation is updated when behavior, architecture, setup, or validation expectations change.
- No real data, secrets, tokens, passwords, or production connection strings are committed.
- MVP boundaries are respected.

## Release Readiness Guardrails

An MVP release candidate is ready for review only when:

- The completion criteria in `docs/21-implementation-roadmap.md` have been evaluated and satisfied or explicitly documented as unmet.
- Clean Architecture boundaries, Angular frontend build behavior, SQL Server/EF Core persistence, and Docker/local setup have been validated.
- Authentication, the five-role RBAC model, and organization-scoped access have been validated through positive and negative cases.
- Document upload and asynchronous processing use the canonical lifecycle and retain safe failure behavior.
- Retrieval uses only eligible authorized chunks and cannot cross organization boundaries.
- Grounded RAG answers include citations and insufficient context is handled safely.
- Chat history, feedback, dashboard metrics, health endpoints, structured logs, correlation IDs, and audit-sensitive behavior work according to their approved contracts.
- Critical backend, API, integration, frontend, and E2E smoke validation has completed where implemented.
- Normal CI is green using fake AI providers without requiring live provider calls.
- Release configuration contains no real data or committed secrets.
- Known limitations and deferred Phase 2 or Phase 3 capabilities remain documented rather than silently implemented.

## MVP Boundaries

### MVP Includes

- Internal user authentication.
- RBAC with the five approved roles.
- Organization-scoped access.
- Admin user and role-management basics.
- Document upload.
- Document metadata and processing status.
- Asynchronous document processing.
- Text extraction.
- Chunking.
- Embedding generation.
- Retrieval/vector abstraction.
- Retrieval eligibility rules.
- RAG chat.
- Prompt builder.
- Source citations.
- Insufficient-context handling.
- Chat history.
- `Useful` and `NotUseful` feedback.
- Basic dashboard metrics.
- Safe health endpoints.
- Structured logging and correlation IDs.
- Audit-sensitive operational events.
- Docker/local SQL Server setup.
- GitHub Actions CI.
- Angular frontend MVP workflows.
- Fictional or synthetic demo data only.

### MVP Excludes

- OpsSphere-style or other ticket lifecycle behavior.
- Ticket queues, ticket assignment, ticket comments, ticket escalation, ticket closure, or ticket SLA tracking.
- Customer-facing chatbot behavior.
- Real-time call transcription.
- Live agent assist.
- Autonomous workflow or ticket actions.
- Automatic policy enforcement.
- Custom model training.
- Advanced MLOps.
- Enterprise SSO.
- External enterprise integrations.
- Full contact center platform replacement.
- Advanced document approval workflow.
- Full knowledge-gap review workflow.
- Dedicated QA, Trainer, or Viewer technical RBAC roles.
- Production-grade Azure hardening.

### Practical Boundary Rule

Build KnowledgeOps-AI as an internal document-based RAG knowledge assistant, not a ticketing system, not a customer chatbot, and not a full contact center operations platform.

## Phase 2 / Phase 3 Parking Rule

Implementation issues must not pull Phase 2 or Phase 3 features into MVP unless:

- Scope is explicitly changed.
- Affected canonical documentation is updated.
- An ADR is added, superseded, or updated if an architecture decision changes.
- Tests, acceptance criteria, and release assumptions are updated.

### Phase 2 Examples

- Full knowledge-gap queue and review workflow.
- Knowledge-gap classification, assignment, decision, and resolution.
- Topic clustering.
- Dedicated QA or Trainer technical roles, if later justified and approved.
- Document versioning, approval, or replacement workflow.
- Richer feedback comments and review.
- Hybrid search improvements.
- Notifications.
- Advanced analytics.
- Export/report features.

### Phase 3 Examples

- Production Azure deployment.
- Azure AI Search production setup.
- Azure Key Vault production setup.
- Application Insights production dashboards.
- Enterprise SSO.
- SharePoint, Teams, or CRM integrations.
- Advanced MLOps.
- Live agent assist only if a future strategy explicitly approves it.
- Real-time transcription only if a future strategy explicitly approves it.

## Diagram Artifact Guardrails

- Markdown Mermaid diagrams are the source of truth.
- PNG files are rendered artifacts only.
- Do not generate diagram PNGs unless the issue explicitly authorizes diagram artifact generation.

Known non-blocking cleanup from the second-pass audit:

- Existing artifact: `docs/diagrams/business-process/monitoring-sla-process.png`.
- Canonical future artifact name: `docs/diagrams/business-process/monitoring-operational-process.png`.
- The existing artifact should be replaced or removed only when diagram artifacts are explicitly regenerated.

## Agent Harness Guardrails

The modular agent context harness is established under `docs/agents/`.

For every future implementation prompt:

- Start by classifying the task with `docs/agents/13-prompt-classifier.md`.
- Use `docs/agents/10-issue-execution-template.md` before implementation begins.
- Load the smallest sufficient routed context and exact canonical contracts needed by the task.
- Consult and update `docs/agents/progress/` records according to the implementation workflow.
- Use specialist subagents only when justified and, for Level 3 work, sequentially.

The older optional `docs/agent/` prompt-preparation paths are not required; their function is superseded by `docs/agents/12-prompt-levels.md` and `docs/agents/13-prompt-classifier.md`.

AI coding agents must:

- Review the relevant issue, sprint, canonical documentation, security contract, and ADRs before changing behavior.
- Respect the MVP boundary and deferred phase rules.
- Avoid silently changing architecture decisions or security contracts.
- Apply the Definition of Done and validation expectations appropriate to the issue type.

## Related Documents

- `docs/05-scope-and-roadmap.md`
- `docs/06-requirements.md`
- `docs/07-use-cases.md`
- `docs/09-business-rules.md`
- `docs/10-domain-model.md`
- `docs/11-architecture-overview.md`
- `docs/14-database-design.md`
- `docs/15-api-design.md`
- `docs/16-security-and-permissions.md`
- `docs/17-testing-strategy.md`
- `docs/18-deployment-and-devops.md`
- `docs/19-observability-and-support.md`
- `docs/20-risk-register.md`
- `docs/21-implementation-roadmap.md`
- `docs/audits/pre-implementation-documentation-consistency-audit-v2.md`
- `docs/decisions/`
