# Implementation Roadmap

## Purpose

This document is the canonical implementation roadmap for development and release of the **KnowledgeOps-AI** MVP.

KnowledgeOps-AI is an internal, AI-powered knowledge assistant for contact centers and support operations. The MVP converts authorized internal documents into searchable, cited knowledge through asynchronous processing, semantic retrieval, and Retrieval-Augmented Generation (RAG), while enforcing authentication, role-based authorization, organization-scoped access, observability, and safe AI behavior.

This roadmap translates the repaired and audited documentation set into sequential implementation increments. It defines planned work only; it does not create GitHub issues, migrations, source code, or rendered diagram artifacts.

Classification:

- Task type: Documentation generation.
- Scope: Implementation roadmap only.
- Implementation level: Pre-implementation planning.
- Subagents: Not used. No repository rule requiring subagents is available.

## Roadmap Principles

- Build foundations before business workflows.
- Keep controllers thin; controllers validate HTTP concerns and delegate to application use cases.
- Keep business rules in Domain or Application services, not controllers or Angular components.
- Keep EF Core and SQL Server persistence concerns in Infrastructure.
- Keep AI, embedding, storage, and vector/retrieval provider SDKs behind Infrastructure abstractions.
- Treat backend authorization as the source of truth; frontend role visibility is user experience guidance only.
- Apply organization scope to protected records, retrieval candidate selection, retrieved results, citations, metrics, and prompt construction.
- Preserve the five MVP technical roles: `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, and `Admin`.
- Keep document processing status independent from retrieval availability.
- Retrieve only from documents that are `Processed`, retrieval-enabled, not soft-deleted, and authorized for the current organization scope.
- Require retrieval before generation, citations for grounded answers, and safe insufficient-context behavior.
- Treat AI output as assisted knowledge access, not final business authority.
- Use fake AI and embedding providers by default in automated tests.
- Do not require live AI provider calls in normal continuous integration.
- Use fictional or synthetic organizations, users, documents, chat interactions, and metrics only.
- Capture operational metadata safely without logging secrets, raw prompt context, or unnecessary sensitive document content.
- Keep the MVP internal-only and document-based.
- Keep Phase 2 and Phase 3 capabilities parked unless a later approved scope change moves them into delivery.

## Canonical Inputs

### Readiness And Repair Records

- `docs/audits/pre-implementation-documentation-consistency-audit.md`
- `docs/audits/pre-implementation-documentation-consistency-repair.md`
- `docs/audits/pre-implementation-documentation-consistency-audit-v2.md`

The v2 audit status is `PASS WITH FINDINGS` and its roadmap readiness decision is `Ready`. Its only remaining finding is a non-blocking rendered diagram artifact filename cleanup.

### Product, Workflow, And Design Documents

- `docs/00-executive-summary.md`
- `docs/01-business-context.md`
- `docs/02-business-case.md`
- `docs/03-project-charter.md`
- `docs/04-stakeholders.md`
- `docs/05-scope-and-roadmap.md`
- `docs/06-requirements.md`
- `docs/07-use-cases.md`
- `docs/08-business-process-flows.md`
- `docs/09-business-rules.md`
- `docs/10-domain-model.md`
- `docs/11-architecture-overview.md`
- `docs/12-c4-architecture.md`
- `docs/13-uml-diagrams.md`
- `docs/14-database-design.md`
- `docs/15-api-design.md`
- `docs/16-security-and-permissions.md`
- `docs/17-testing-strategy.md`
- `docs/18-deployment-and-devops.md`
- `docs/19-observability-and-support.md`
- `docs/20-risk-register.md`

### Accepted Architecture Decisions

- `docs/decisions/README.md`
- `docs/decisions/ADR-001-use-clean-architecture.md`
- `docs/decisions/ADR-002-use-sql-server.md`
- `docs/decisions/ADR-003-use-angular.md`
- `docs/decisions/ADR-004-use-role-based-access-control.md`
- `docs/decisions/ADR-005-use-entity-framework-core.md`
- `docs/decisions/ADR-006-use-azure-openai-compatible-provider-abstraction.md`
- `docs/decisions/ADR-007-use-rag-with-source-citations.md`
- `docs/decisions/ADR-008-use-asynchronous-document-processing.md`
- `docs/decisions/ADR-009-use-mermaid-for-architecture-diagrams.md`
- `docs/decisions/ADR-010-use-organization-scoped-access-boundaries.md`

### Canonical Sources Of Truth

- `docs/09-business-rules.md` is canonical for `BR-###` rules.
- `docs/10-domain-model.md` is canonical for domain language and conceptual relationships.
- `docs/14-database-design.md` is canonical for logical database design.
- `docs/15-api-design.md` is canonical for initial API contracts.
- `docs/16-security-and-permissions.md` is canonical for MVP permissions and security.
- Accepted ADRs are canonical for architecture decisions.
- Markdown Mermaid diagrams are the diagram source of truth; PNGs are rendered artifacts only.

### Active Agent Harness Guidance

- `docs/agents/00-agent-operating-protocol.md` defines the mandatory entry flow for implementation work.
- `docs/agents/13-prompt-classifier.md` is the required classify-first routing entry point.
- `docs/agents/10-issue-execution-template.md` is required before implementation begins.
- `docs/agents/12-prompt-levels.md` defines level-based context and validation routing.
- `docs/agents/progress/` records implementation status, decisions, risks, and verified completions.

The plural `docs/agents/` directory is the canonical implementation-prompt harness. Older optional singular-directory prompt guidance is superseded and must not be created for future work.

## MVP Implementation Strategy

1. Establish repository, solution, Angular, Docker, CI, health, logging, and automated-test foundations before building business workflows.
2. Implement the canonical domain, SQL Server/EF Core persistence, fictional demo data, authentication, RBAC, organization-scoped access, and basic administration.
3. Implement document metadata, protected uploads, storage, asynchronous processing, extraction, and deterministic chunking.
4. Introduce embedding and vector/retrieval abstractions, with fake providers for tests and strict retrieval eligibility plus organization filtering.
5. Implement RAG orchestration only after authorized retrievable chunks exist, then add prompt construction, insufficient-context behavior, citations, chat APIs, UI, and history.
6. Add feedback, basic dashboard metrics, supportable telemetry, audit-sensitive operations, and safe operational views using stored MVP signals.
7. Harden security and cross-scope testing, strengthen CI and local reproducibility, perform approved documentation/artifact housekeeping, and complete MVP stabilization and release checks.

### Canonical MVP Contracts Carried Through Every Sprint

| Contract | Roadmap Constraint |
| --- | --- |
| Product boundary | Internal document-based knowledge assistant only. |
| Frontend | Angular is selected for MVP. |
| RBAC roles | `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, `Admin` only. |
| Stakeholder personas | QA, Trainer, Compliance Reviewer, Viewer, Recruiter, Portfolio Reviewer, and AI Coding Agent are not MVP RBAC roles. |
| Document processing status | `Uploaded`, `Processing`, `Processed`, `Failed`. |
| Retrieval eligibility | `processing_status = Processed` and `is_retrieval_enabled = true` and `deleted_at IS NULL` where applicable and organization scope authorizes access. |
| Retrieval disablement | Set `is_retrieval_enabled = false`; do not invent a `Disabled` processing status. |
| Knowledge gap scope | MVP stores insufficient-context events and `NotUseful` feedback and exposes basic scoped counts; full workflow is Phase 2. |
| Health routes | `GET /api/v1/health` for safe basic status; `GET /api/v1/health/details` for Admin-only sanitized dependency detail. |
| AI testing | Fake providers are normal for tests and CI; live provider calls are optional controlled validation only. |
| Deployment posture | Docker/GitHub Actions and Azure-ready abstractions are MVP concerns; production-grade Azure hardening is deferred. |

## MVP Scope Boundaries

### Included In MVP

- Repository and Clean Architecture backend foundation.
- Angular frontend foundation and approved internal-user screens.
- SQL Server, EF Core, Docker/local development, GitHub Actions CI, and Azure-ready abstractions.
- Fictional seed organizations and users using only the approved five-role RBAC model.
- Authentication, authorization, organization scope, safe errors, correlation IDs, structured logging, health, and audit-sensitive action support.
- Internal document upload, metadata, asynchronous processing, extraction, chunking, embeddings, semantic retrieval, and retrieval disablement.
- RAG chat using authorized retrieved context, source citations, safe insufficient-context outcomes, and chat history.
- `Useful`/`NotUseful` feedback and basic dashboard metrics including insufficient-context and `NotUseful` counts.
- Supportable telemetry for processing, retrieval, provider behavior, latency, cost/tokens when available, access failures, and system health.

### Excluded From MVP

- Any OpsSphere-style or other ticket lifecycle, ticket queue, ticket assignment, ticket closure, or ticket SLA functionality.
- Customer-facing chatbot behavior.
- Real-time call transcription.
- Live agent assist.
- Autonomous ticket actions, autonomous workflow actions, or automatic policy enforcement.
- Custom model training or advanced MLOps.
- Enterprise SSO.
- External enterprise integrations.
- Full contact center platform replacement.
- Advanced document approval or replacement workflow.
- Full knowledge-gap queue, categorization, assignment, decision, resolution, clustering, or dedicated QA/Trainer workflow.
- Dedicated QA, Trainer, or Viewer technical RBAC roles.
- Production-grade Azure hardening unless separately approved as future delivery.

## Sprint / Issue Sequence

The sprints below are sequential. Each sprint is intentionally bounded so it can later be converted into one GitHub issue or a small related issue group without changing MVP scope.

### Sprint 0: Repository Implementation Guardrails

- Goal: Establish implementation-facing scope and architecture guardrails before source-code delivery begins.
- Why this comes now: Every subsequent increment must use the repaired canonical contracts and avoid reintroducing previously resolved contradictions.
- Deliverables: Implementation contribution guidance; canonical-document reference list; MVP/non-MVP boundary checklist; canonical agent prompt-harness guidance for implementation prompts; documented rule that ADR changes require explicit review.
- Backend scope: Define planned project boundaries and backend conventions only; no application implementation in this sprint description.
- Frontend scope: Define Angular as the only MVP frontend direction and identify role-aware navigation as UX-only guidance.
- Database scope: Record the canonical logical schema authority and lifecycle/retrieval predicate for later schema work.
- AI / RAG scope: Record grounding, citation, insufficient-context, provider-abstraction, and fake-provider rules.
- Testing scope: Establish that each later issue includes appropriate unit, integration, API, frontend, or E2E validation.
- DevOps / Observability scope: Establish secret-handling, no-live-AI CI, structured logging, and Azure-ready-not-production-overbuilt rules.
- Out of scope: Application code, implementation issues, rendered PNG generation, Phase 2/3 feature delivery.
- Exit criteria: Future implementation work can cite one unambiguous MVP scope, role model, lifecycle model, retrieval predicate, and architecture decision set.
- Suggested GitHub issue title: `[Sprint 0] Establish KnowledgeOps-AI implementation guardrails and contribution gates`

### Sprint 1: Backend Clean Architecture Solution Structure

- Goal: Create the .NET backend project skeleton that preserves Clean Architecture boundaries.
- Why this comes now: Persistence, security, document processing, and RAG features require stable ownership boundaries first.
- Deliverables: Planned `Domain`, `Application`, `Infrastructure`, `Api`, `Worker`, and test project structure; dependency-direction rules; baseline buildable API and worker hosts.
- Backend scope: ASP.NET Core/.NET solution structure; thin endpoint hosting baseline; dependency injection composition roots; application/domain interface locations.
- Frontend scope: None.
- Database scope: Infrastructure placeholder for later EF Core configuration; no business schema beyond what is required to build.
- AI / RAG scope: Provider interface namespaces and dependency-direction rules only; no AI behavior.
- Testing scope: Baseline build/test project wiring and architecture boundary tests where practical.
- DevOps / Observability scope: Basic configuration conventions for API and worker hosts.
- Out of scope: Authentication, persisted domain workflows, document processing, retrieval, RAG answers.
- Exit criteria: Backend hosts and test projects build with dependency direction consistent with ADR-001; Domain/Application do not depend on Infrastructure or provider SDKs.
- Suggested GitHub issue title: `[Sprint 1] Create Clean Architecture backend solution and host skeletons`

### Sprint 2: Angular Frontend Foundation

- Goal: Create the Angular application foundation for MVP screens and API integration.
- Why this comes now: Frontend architecture and typed API conventions should be established before workflow screens are added.
- Deliverables: Angular workspace/application; layout and navigation shell; routing; configuration model; HTTP service/interceptor foundation; placeholder guarded routes.
- Backend scope: None beyond documenting expected API base configuration.
- Frontend scope: Angular only; application shell, typed service pattern, route guard pattern, auth token/interceptor placeholder, accessible loading/error presentation conventions.
- Database scope: None.
- AI / RAG scope: None.
- Testing scope: Angular unit-test baseline and component/service test conventions.
- DevOps / Observability scope: Frontend environment configuration without secrets; local build command documented for future CI.
- Out of scope: Fully wired login, chat, document upload, dashboard, or admin workflows.
- Exit criteria: Angular app builds and tests; screen areas are routed without assuming unimplemented backend behavior; no React implementation choice remains open.
- Suggested GitHub issue title: `[Sprint 2] Create Angular MVP application shell and frontend conventions`

### Sprint 3: Docker Compose And Local SQL Server Setup

- Goal: Provide a reproducible local runtime foundation for API, worker, Angular app, file storage, and SQL Server.
- Why this comes now: Persistence and integration work need a predictable local environment before schema and workflows are introduced.
- Deliverables: Planned Dockerfiles and Docker Compose services; local SQL Server service; local document-storage volume/directory convention; environment variable templates without real credentials; run instructions.
- Backend scope: API and worker container configuration and startup requirements.
- Frontend scope: Angular development/container option and API base URL configuration.
- Database scope: Local SQL Server container and durable local volume convention.
- AI / RAG scope: Fake-provider configuration path for local and test use; no live provider requirement.
- Testing scope: Local stack smoke procedure suitable for later automation.
- DevOps / Observability scope: Environment configuration separation; secret exclusion rules; health-check wiring assumptions for later implementation.
- Out of scope: Azure resource deployment, production credentials, data migrations executed as part of this roadmap document.
- Exit criteria: Planned local environment supports starting SQL Server and application hosts with safe configuration and fictional data only.
- Suggested GitHub issue title: `[Sprint 3] Establish Docker Compose local environment with SQL Server`

### Sprint 4: EF Core Persistence Foundation And Initial Migration

- Goal: Establish relational persistence implementation for the core MVP model using SQL Server and EF Core.
- Why this comes now: Identity, organization scope, documents, chats, and audit behavior all depend on reliable relational boundaries.
- Deliverables: Planned EF Core context and configurations; initial migration work item; repository/query abstractions; core audit fields and indexing conventions; migration review/rollback notes.
- Backend scope: Infrastructure EF Core setup behind application interfaces; transaction and time/identifier conventions.
- Frontend scope: None.
- Database scope: Initial SQL Server schema foundation for organizations, users, roles, user-role assignments, and audit foundations, with feature tables introduced in their owning sprints as needed; migration history strategy.
- AI / RAG scope: None.
- Testing scope: Persistence integration-test harness against relational behavior; migration application verification plan.
- DevOps / Observability scope: Safe connection configuration and controlled migration execution guidance.
- Out of scope: Embedding/vector implementation, production database deployment, Phase 2 `knowledge_gap_signals`.
- Exit criteria: Persistence foundation can be migrated and integration-tested without EF Core leaking into Domain/Application contracts.
- Suggested GitHub issue title: `[Sprint 4] Implement EF Core and SQL Server persistence foundation`

### Sprint 5: Fictional Seed Data And Demo Personas

- Goal: Provide deterministic fictional organizations and MVP-role users for local validation and automated tests.
- Why this comes now: Authentication, authorization, and cross-organization tests need stable identities and data boundaries.
- Deliverables: Fictional seed organizations; users for each approved MVP role; cross-organization personas; synthetic document/test-data strategy; safe local credentials setup guidance.
- Backend scope: Seed orchestration and environment-safe initialization behavior.
- Frontend scope: None.
- Database scope: Seed records for organizations, roles, user-role assignments, and users only as needed for early flows.
- AI / RAG scope: Synthetic document and question examples may be planned; no provider use.
- Testing scope: Reusable personas such as Agent A, KnowledgeAdmin A, Manager A, Admin A, and Agent B for scope-isolation tests.
- DevOps / Observability scope: Prohibit real user data and committed secrets; make seed reset behavior predictable.
- Out of scope: Real internal documents, production seed accounts, QA/Trainer/Viewer technical roles.
- Exit criteria: Developers and tests can use deterministic fictional users across at least two organizations with only the five MVP roles.
- Suggested GitHub issue title: `[Sprint 5] Add fictional organizations and MVP-role demo personas`

### Sprint 6: Authentication Foundation

- Goal: Authenticate internal MVP users and expose reliable current-user context.
- Why this comes now: No protected document, administration, retrieval, or chat workflow may be implemented safely before identity exists.
- Deliverables: Login/logout behavior as applicable; `GET /api/v1/auth/me`; token or session context; current-user service; disabled-user denial; Angular login and session foundation.
- Backend scope: Authentication endpoints, credential/token handling approach, authenticated request context, safe authentication errors.
- Frontend scope: Login page; authenticated session service; interceptor integration; logout behavior; basic protected-route handling.
- Database scope: User credential/status persistence required by selected MVP authentication approach; no enterprise SSO.
- AI / RAG scope: None.
- Testing scope: Valid/invalid login; disabled user; unauthenticated API denial; Angular auth service and route behavior.
- DevOps / Observability scope: Safe authentication failure logging without passwords or tokens.
- Out of scope: Enterprise SSO or Microsoft Entra ID integration, authorization policies for business capabilities.
- Exit criteria: A seeded MVP user can authenticate and obtain current identity context; invalid or disabled access is rejected safely.
- Suggested GitHub issue title: `[Sprint 6] Implement authentication and current-user context`

### Sprint 7: RBAC And Organization-Scope Authorization Foundation

- Goal: Enforce the MVP permission model and organization access boundary on the backend.
- Why this comes now: All protected records and future retrieval must be scoped before workflow data is implemented.
- Deliverables: Role/permission policies; organization-scope service or specifications; denied-access behavior; test helpers for cross-scope records.
- Backend scope: Policies for `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, and `Admin`; deny-by-default enforcement; scoped query/application-service conventions.
- Frontend scope: Role-aware navigation visibility based on authenticated context, explicitly non-authoritative.
- Database scope: Organization ownership and user-role relationships required for enforcement.
- AI / RAG scope: Define mandatory pre-retrieval and pre-prompt organization filtering hooks; no retrieval yet.
- Testing scope: Role permission tests; cross-organization denial tests; direct-API denial despite hidden/visible frontend actions.
- DevOps / Observability scope: Safe authorization and organization-scope failure event convention.
- Out of scope: Dedicated QA, Trainer, or Viewer RBAC roles; multi-organization super-admin model; business workflows not yet built.
- Exit criteria: Protected use cases have a reusable backend policy and organization-scope pattern that fails closed and is covered by tests.
- Suggested GitHub issue title: `[Sprint 7] Implement MVP RBAC and organization-scope authorization`

### Sprint 8: Global Error Handling, Correlation IDs, Logging, And Health Checks

- Goal: Create safe operational behavior shared by all future workflows.
- Why this comes now: Document processing and AI calls will need correlation, sanitized failures, and health visibility from their first implementation.
- Deliverables: Global error contract; correlation ID middleware; structured logging baseline; audit-event interface baseline; canonical health routes.
- Backend scope: Error handling middleware; correlation propagation across API and worker boundary; `GET /api/v1/health`; `GET /api/v1/health/details` with Admin-only sanitized details.
- Frontend scope: Correlation-aware error display pattern; basic unavailable/forbidden states.
- Database scope: Audit record foundation where persistence is included; no raw sensitive payload logging.
- AI / RAG scope: Provider/retrieval failure logging contract only.
- Testing scope: Error response/correlation tests; safe basic health route tests; Admin-only detailed health authorization and sanitization tests.
- DevOps / Observability scope: Structured event fields, safe dependency checks, health configuration, sensitive-field exclusion.
- Out of scope: Rich operational dashboards, Application Insights production integration.
- Exit criteria: Errors are consistently sanitized and correlated; basic health is safe; detailed health is restricted to `Admin`; logs do not expose secrets or content.
- Suggested GitHub issue title: `[Sprint 8] Implement safe errors, correlation, structured logging, and health endpoints`

### Sprint 9: Admin User And Role Management Foundation

- Goal: Allow `Admin` users to maintain basic MVP access configuration.
- Why this comes now: Controlled access administration is required before broader internal workflows are exercised.
- Deliverables: Admin user listing/creation/update/status behavior; role assignment/removal; organization assignment validation; minimal Angular administration screens.
- Backend scope: `/api/v1/users` administrative endpoints and role-management operations using backend authorization.
- Frontend scope: Admin-only user list/detail/edit and role assignment UI; hidden navigation for non-admins without relying on it for security.
- Database scope: User, role, organization relationship persistence and audit records for privileged changes.
- AI / RAG scope: None.
- Testing scope: Admin success tests; non-admin denial; cross-scope and invalid-role tests; role/status audit event tests.
- DevOps / Observability scope: Audit user creation, status changes, role changes, and organization-scope changes safely.
- Out of scope: Enterprise provisioning, SSO, custom roles, cross-organization administration not explicitly authorized.
- Exit criteria: An `Admin` can manage approved MVP user access and every privileged operation is backend-protected and audit-sensitive.
- Suggested GitHub issue title: `[Sprint 9] Implement admin user and MVP role management`

### Sprint 10: Document Domain And Metadata Foundation

- Goal: Implement the protected document record and its canonical lifecycle/eligibility model.
- Why this comes now: Upload, worker processing, retrieval, citations, and metrics all depend on one reliable document identity.
- Deliverables: Document domain behavior; metadata persistence; list/detail/status contracts; retrieval-disablement operation; organization scope enforcement.
- Backend scope: Document application service and API models for metadata/status/list/detail/disable actions; authorization for `KnowledgeAdmin`, `Manager`, and `Admin` per contract.
- Frontend scope: Document list/status/detail page shell and role-visible actions; no file submission until the upload sprint.
- Database scope: `documents` table mapping, fields, indexes, timestamps, `failure_reason`, `is_retrieval_enabled`, and nullable `deleted_at`.
- AI / RAG scope: Encode eligibility predicate for later retrieval: only processed, enabled, not soft-deleted, organization-authorized documents.
- Testing scope: Status transition unit tests; disable-from-retrieval tests proving processing status is unchanged; scoped document query tests.
- DevOps / Observability scope: Document metadata and disablement audit-event definitions.
- Out of scope: `Disabled` as a processing status, re-enable/retry APIs, file extraction, embeddings, retrieval.
- Exit criteria: Document records use only `Uploaded`, `Processing`, `Processed`, and `Failed`; retrieval disablement uses `is_retrieval_enabled = false`.
- Suggested GitHub issue title: `[Sprint 10] Implement document metadata, lifecycle, and retrieval-disablement model`

### Sprint 11: Document Upload API And Angular Upload Flow

- Goal: Allow authorized users to submit internal documents safely and see initial status.
- Why this comes now: Documents must enter the system before asynchronous ingestion can be exercised.
- Deliverables: Protected upload endpoint; validation; storage abstraction implementation for local MVP; Angular upload form; status confirmation.
- Backend scope: `POST /api/v1/documents`; file type/size/metadata validation; local/cloud-compatible storage interface; initial `Uploaded` status and organization assignment.
- Frontend scope: Upload form for `KnowledgeAdmin` and `Admin`; validation messages; document list refresh and status view.
- Database scope: Persist metadata and storage reference atomically with upload acceptance where applicable.
- AI / RAG scope: None; upload must not imply immediate retrievability.
- Testing scope: Valid/invalid upload API tests; unauthorized/cross-scope tests; Angular form/role-visibility tests; storage failure behavior.
- DevOps / Observability scope: Upload accepted/rejected/failure logs with correlation ID and no document content logging.
- Out of scope: Synchronous document processing, advanced approval workflow, broad format support beyond approved MVP formats.
- Exit criteria: Authorized fictional users can upload a supported document and see `Uploaded`; unauthorized uploads fail safely.
- Suggested GitHub issue title: `[Sprint 11] Implement protected document upload and Angular upload experience`

### Sprint 12: Asynchronous Document Processing Worker Foundation

- Goal: Process uploaded documents outside the request path with tracked lifecycle transitions.
- Why this comes now: ADR-008 requires asynchronous ingestion, and extraction/embeddings must not block upload.
- Deliverables: Worker execution model; pending-work selection or scheduling; status transition handling; safe processing failure handling; processing-status retrieval.
- Backend scope: Worker/application processing orchestration; processing status API behavior; failure reason handling.
- Frontend scope: Document status refresh/polling presentation and failure reason display for authorized roles.
- Database scope: Worker-safe state transitions and timestamps; processing-status query support.
- AI / RAG scope: None until extraction/chunking/embedding steps are attached; worker pipeline slots defined.
- Testing scope: `Uploaded` to `Processing` to `Processed`/`Failed` lifecycle tests; concurrency/idempotency considerations; failure reason tests.
- DevOps / Observability scope: Worker health and processing duration/error events correlated to document operations.
- Out of scope: Phase 2 retry endpoint, external queue-first platform, retrieval of unprocessed content.
- Exit criteria: Upload returns independently of processing; worker transitions and safe failures are observable and testable.
- Suggested GitHub issue title: `[Sprint 12] Implement asynchronous document processing worker lifecycle`

### Sprint 13: Text Extraction And Chunking

- Goal: Convert processed source files into deterministic, source-traceable document chunks.
- Why this comes now: Embeddings and retrieval require valid chunk records linked to approved document sources.
- Deliverables: Text extraction abstraction/initial provider; deterministic chunking service; chunk metadata; invalid/empty extraction failure behavior.
- Backend scope: Processing steps for supported file extraction, chunk construction, source/page/section metadata when available, and failure propagation.
- Frontend scope: Authorized document details may show safe processing/chunk availability status; sensitive raw chunk exposure follows the API/security contract.
- Database scope: `document_chunks` persistence and indexes with document and organization relationships.
- AI / RAG scope: Chunk preparation only; no generation and no retrieval user flow.
- Testing scope: Extraction success/failure tests; empty-text rejection; deterministic chunk boundaries; source and organization inheritance tests.
- DevOps / Observability scope: Extraction latency/failure and chunk-count telemetry without logging full content.
- Out of scope: Advanced OCR, document versioning, approval, Phase 2 replacement workflow.
- Exit criteria: Supported fictional documents yield non-empty, traceable, organization-scoped chunks, or fail safely and remain non-retrievable.
- Suggested GitHub issue title: `[Sprint 13] Implement document text extraction and deterministic chunking`

### Sprint 14: Embedding Provider Abstraction And Fake Provider

- Goal: Generate searchable chunk representations without coupling the application to a specific AI SDK.
- Why this comes now: Chunks exist, and semantic retrieval cannot be implemented until embeddings have a stable abstraction.
- Deliverables: `IEmbeddingProvider` contract; fake deterministic provider; configuration model; optional infrastructure adapter shape; embedding status/failure metadata.
- Backend scope: Application-owned embedding use case using Infrastructure provider implementations; provider error mapping.
- Frontend scope: None.
- Database scope: `chunk_embeddings` metadata/vector-or-reference persistence decision consistent with the logical design.
- AI / RAG scope: Embedding generation only; fake embeddings used for tests and normal CI.
- Testing scope: Fake-provider deterministic tests; embedding failure moves document processing to `Failed`; incomplete embedding prevents eligibility.
- DevOps / Observability scope: Provider configuration without secrets; latency/token/cost metadata where meaningful; embedding failures logged safely.
- Out of scope: Live-provider dependency in CI, generation prompts, user-facing retrieval.
- Exit criteria: Processed chunk embeddings can be produced through an abstraction and tested deterministically without live provider calls.
- Suggested GitHub issue title: `[Sprint 14] Implement embedding abstraction with deterministic fake provider`

### Sprint 15: Vector And Retrieval Storage Abstraction

- Goal: Introduce semantic-index storage and query interfaces while retaining relational traceability.
- Why this comes now: Retrieval needs indexed embeddings, but the implementation must remain replaceable and Azure-ready.
- Deliverables: Retrieval/vector store interface; MVP-compatible storage adapter selection; indexed chunk reference behavior; configuration; query-result model.
- Backend scope: Infrastructure retrieval adapter and Application-facing interface; preserve document/chunk identifiers and organization metadata.
- Frontend scope: None.
- Database scope: Vector data or external-index references; indexes and relationships needed to trace retrieval candidates back to SQL Server entities.
- AI / RAG scope: Vector indexing/query capability over fake/test embeddings; no answer generation.
- Testing scope: Adapter contract tests; deterministic semantic query fixtures; failure and stale-reference behavior.
- DevOps / Observability scope: Retrieval provider configuration and safe index/storage health indicators.
- Out of scope: Azure AI Search production provisioning, hybrid search tuning, RAG chat.
- Exit criteria: Eligible embedded chunks can be indexed and queried through an abstraction while retaining source identity and organization metadata.
- Suggested GitHub issue title: `[Sprint 15] Implement vector storage and semantic retrieval abstractions`

### Sprint 16: Retrieval Eligibility And Organization-Scoped Retrieval

- Goal: Return relevant chunks only from sources the requesting user is permitted to use.
- Why this comes now: RAG cannot safely start until retrieval exclusion and scope rules are enforced and tested.
- Deliverables: Query embedding/retrieval application service; top-K result model; retrieval eligibility filter; retrieval result persistence contract; cross-scope protection.
- Backend scope: Retrieval service executing identity/role/scope checks and eligible-source filtering before returning candidates.
- Frontend scope: None.
- Database scope: `retrieval_results` persistence support and query/index validation; scope relationships among document, chunk, and result.
- AI / RAG scope: Query embedding plus semantic retrieval; generation remains out of scope.
- Testing scope: Exclude uploaded, processing, failed, retrieval-disabled, soft-deleted, and unauthorized sources; include only eligible authorized chunks; persist no unauthorized retrieval result.
- DevOps / Observability scope: Retrieval latency/failure and insufficient-result signals; organization-scope failure auditing.
- Out of scope: Prompt construction or answer generation; Phase 2 search enhancements.
- Exit criteria: Retrieval demonstrably enforces the complete canonical predicate before any context can reach an AI prompt.
- Suggested GitHub issue title: `[Sprint 16] Implement eligible organization-scoped semantic retrieval`

### Sprint 17: RAG Chat Domain And Application Orchestration

- Goal: Establish the application workflow that coordinates authorized retrieval and answer generation.
- Why this comes now: Authentication, scope, documents, chunks, embeddings, and retrieval safety now exist as prerequisites.
- Deliverables: Chat interaction/session domain behavior; `IAiAnswerGenerator` contract and fake generator; RAG orchestration application service; answer metadata storage model.
- Backend scope: Chat application service that validates user context, invokes scoped retrieval, handles orchestration outcomes, and stores interaction state/metadata.
- Frontend scope: None beyond typed response model planning.
- Database scope: `chat_sessions` and `chat_interactions` persistence including latency, cost/token fields when available and answer state.
- AI / RAG scope: Retrieval-before-generation pipeline with fake generator by default; no unsupported answer behavior.
- Testing scope: Orchestration order tests; fake-generated grounded outcome; provider failure outcome; scope propagation; nullable cost handling.
- DevOps / Observability scope: Retrieval/generation/total latency, provider failure, and interaction correlation metadata.
- Out of scope: Final prompt template details, user-facing chat UI, full citation display.
- Exit criteria: Application orchestration never calls generation before authorized retrieval and stores safe interaction outcomes using fake providers in automated tests.
- Suggested GitHub issue title: `[Sprint 17] Implement RAG chat orchestration and chat interaction persistence`

### Sprint 18: Prompt Builder And Insufficient-Context Behavior

- Goal: Construct grounded prompts only from authorized retrieved content and fail safely when context is weak or absent.
- Why this comes now: Orchestration exists, but safe generation depends on explicit prompt and insufficient-context rules.
- Deliverables: Prompt builder; prompt/version metadata; context sufficiency decision behavior; safe response and human-authority wording; insufficient-context persistence.
- Backend scope: Application/domain behavior for context threshold outcome and safe response representation.
- Frontend scope: Response-state contract for displaying insufficient-context messages later.
- Database scope: Store `insufficient_context` and relevant prompt/retrieval configuration metadata without unnecessary sensitive prompt retention.
- AI / RAG scope: Grounded context construction, bounded prompt content, and no-generation or safe-generation behavior when context is insufficient.
- Testing scope: Prompt uses authorized retrieved chunks only; no-context/weak-context tests; provider not invoked when policy requires safe fallback; insufficient event stored.
- DevOps / Observability scope: Insufficient-context count signal and safe prompt metadata logging rules.
- Out of scope: Full knowledge-gap queue, classification, assignment, resolution, or clustering.
- Exit criteria: Unsupported questions result in a disclosed insufficient-context response and recorded MVP signal rather than invented guidance.
- Suggested GitHub issue title: `[Sprint 18] Implement grounded prompt building and insufficient-context handling`

### Sprint 19: Source Citation Mapping And Citation Persistence

- Goal: Make every grounded answer traceable to authorized document chunks.
- Why this comes now: RAG answers are not a safe MVP feature without visible and persisted source support.
- Deliverables: Citation mapper; citation persistence; response citation model; historical citation access rule handling.
- Backend scope: Map selected retrieval results to citations with document, chunk, page/section, and relevance metadata where available.
- Frontend scope: Citation display component contract and interaction behavior ready for chat integration.
- Database scope: `citations` table and indexes, preserving organization scope and answer/source relationships.
- AI / RAG scope: Require citations for grounded answers; do not attach unauthorized or unrelated sources.
- Testing scope: Citation mapping/persistence; no unauthorized citation disclosure; grounded-answer citation requirement; historical visibility after retrieval disablement subject to access rules.
- DevOps / Observability scope: Citation traceability telemetry without logging protected source text.
- Out of scope: Separate citation-detail endpoints unless required after MVP review; document replacement/version workflows.
- Exit criteria: Grounded RAG responses include stored, organization-authorized citations linked to retrieved chunks.
- Suggested GitHub issue title: `[Sprint 19] Implement source citation mapping and persistence`

### Sprint 20: Chat API And Angular Chat UI

- Goal: Expose the central MVP question-and-cited-answer experience to authorized internal users.
- Why this comes now: The UI must not expose chat until secure RAG, insufficient context, and citations work behind stable contracts.
- Deliverables: Chat question endpoint; session creation path as needed; Angular chat screen; answer, citation, insufficient-context, and error rendering.
- Backend scope: `POST /api/v1/chat/questions` and necessary session contract using existing RAG service and authorization.
- Frontend scope: Question entry; response rendering; citation presentation; insufficient-context state; loading/errors; role-appropriate access for all five roles.
- Database scope: Persist interactions, retrieval metadata, citations, and AI usage metadata already defined by prior sprints.
- AI / RAG scope: End-to-end authorized RAG request through chat contract.
- Testing scope: API tests for grounded and insufficient-context responses; Angular component/service tests; fake-provider workflow smoke tests.
- DevOps / Observability scope: End-to-end correlation across API, retrieval, generator, and persistence; latency visible in support telemetry.
- Out of scope: Customer-facing chat, live assistance, autonomous actions, streaming optimization.
- Exit criteria: An authenticated authorized internal user can ask a question and receive either a cited grounded answer or a safe insufficient-context response.
- Suggested GitHub issue title: `[Sprint 20] Deliver authorized RAG chat API and Angular chat experience`

### Sprint 21: Chat History And Interaction Detail

- Goal: Allow users and authorized reviewers to retrieve stored conversation evidence securely.
- Why this comes now: Completed chat interactions and citations now exist and can be exposed through scoped history APIs.
- Deliverables: Session list/detail and interaction detail endpoints; Angular chat-history/detail views; citation and metadata continuity.
- Backend scope: Owner and scoped-review access for chat sessions/interactions according to the permission contract.
- Frontend scope: Own-history navigation; interaction detail display; scoped-review visibility only where the user has an approved MVP role.
- Database scope: History query indexes, session ordering, and preserved citation relationships.
- AI / RAG scope: No new generation behavior; display stored RAG evidence safely.
- Testing scope: Owner access; permitted Supervisor/Manager/Admin scoped access; non-owner/cross-organization denial; citation continuity tests.
- DevOps / Observability scope: Audit or safe telemetry for protected history access where required.
- Out of scope: Broad transcript analytics, knowledge-gap review workflow, exported reports.
- Exit criteria: Stored interactions and citations are accessible only to their owner or authorized scoped reviewers.
- Suggested GitHub issue title: `[Sprint 21] Implement secure chat history and interaction detail views`

### Sprint 22: Answer Feedback

- Goal: Capture useful/not useful response signals associated with stored chat interactions.
- Why this comes now: Feedback requires completed chat interactions and is an input to MVP quality metrics.
- Deliverables: Feedback submit/update behavior; simple authorized feedback inspection; Angular feedback controls; duplicate-inflation prevention.
- Backend scope: Feedback endpoints and application rules for `Useful` and `NotUseful`, ownership, scope, and review permissions.
- Frontend scope: Feedback controls on answer/history views and safe success/error states.
- Database scope: `answer_feedback` persistence, organization relationship, rating constraints, uniqueness for user/interaction.
- AI / RAG scope: Store evaluation signal only; do not create automated retraining or response changes.
- Testing scope: Store useful/not useful; duplicate behavior; cross-scope denial; review permission/API tests; frontend submission tests.
- DevOps / Observability scope: Feedback submission event and counts; no sensitive answer/content leakage in event logs.
- Out of scope: Comment-rich review workflow unless explicitly approved later; dedicated knowledge-gap workflow.
- Exit criteria: Authorized users can rate answers and scoped permitted users can inspect simple feedback signals without creating Phase 2 workflow scope.
- Suggested GitHub issue title: `[Sprint 22] Implement answer feedback capture and basic inspection`

### Sprint 23: Basic Dashboard Metrics

- Goal: Expose role- and organization-scoped MVP operational metrics derived from stored data.
- Why this comes now: Document statuses, chat interactions, latency/cost metadata, insufficient-context flags, and feedback must exist before metrics are meaningful.
- Deliverables: Dashboard query services/endpoints; Angular dashboard views; data source definitions; cost-unavailable display state.
- Backend scope: Overview/document/chat/feedback dashboard endpoints with explicit permission checks and scoped aggregation.
- Frontend scope: Dashboard cards/tables for authorized roles; no display for users without permission; clear unavailable-cost treatment.
- Database scope: Efficient aggregate queries and indexes; compute metrics dynamically for MVP unless later performance evidence requires snapshots.
- AI / RAG scope: Report retrieval/generation/total latency, estimated cost/token use when available, and insufficient-context outcomes; no model evaluation workflow.
- Testing scope: Scoped aggregation tests; permission tests; unavailable cost not represented as zero; frontend visibility/rendering tests.
- DevOps / Observability scope: Metrics query timing and safe operational diagnostic behavior.
- Out of scope: `knowledge_gap_signals` persistence, queue/review workflow, advanced analytics, exported reports.
- Exit criteria: Authorized users see correct scoped counts for documents, usage, feedback, latency, cost when available, and insufficient context.
- Suggested GitHub issue title: `[Sprint 23] Implement organization-scoped MVP operational dashboard`

### Sprint 24: Observability And Supportability Completion

- Goal: Make MVP workflows diagnosable and supportable without exposing protected content or secrets.
- Why this comes now: All principal workflows now emit meaningful operational and audit-sensitive events.
- Deliverables: Final event catalog implementation; processing failure administrative view; audit-log view for `Admin`; operational troubleshooting verification; safe health dependency coverage.
- Backend scope: Administrative processing-failure and audit-log endpoints; append-oriented audit behavior for sensitive actions.
- Frontend scope: Minimal authorized operational support/admin views where included by API contract.
- Database scope: Audit-log storage/indexes and safe operational query support as defined in logical model.
- AI / RAG scope: Observe provider failures, retrieval/generation latency, token/cost metadata where available, and citation traceability.
- Testing scope: Audit event creation; sensitive-field exclusion; admin/non-admin access; health-detail sanitization; processing failure visibility.
- DevOps / Observability scope: Structured events for upload, processing, retrieval, generation, feedback, authorization/scope failure, privileged action, and health detail access.
- Out of scope: Full Application Insights production deployment, production alert platform, Phase 2 quality-review queue.
- Exit criteria: Support staff with approved roles can diagnose safe MVP telemetry and failures; logs and health data reveal no secrets or raw protected content.
- Suggested GitHub issue title: `[Sprint 24] Complete safe observability, audit, and supportability features`

### Sprint 25: Security Hardening And Cross-Scope Validation

- Goal: Prove that the implemented product cannot disclose unauthorized knowledge through APIs, retrieval, prompts, citations, history, metrics, or support views.
- Why this comes now: Cross-cutting security can be validated comprehensively only after all MVP data paths exist.
- Deliverables: Security review checklist execution; cross-organization threat tests; permission-matrix validation; sensitive-output review; remedial fixes scoped to MVP behavior.
- Backend scope: Validate and tighten authorization/scoping on every protected endpoint and application query.
- Frontend scope: Verify role-visible UI is consistent with permissions while documenting that backend enforcement is decisive.
- Database scope: Validate organization IDs, foreign-key alignment, soft-delete/retrieval filters, audit fields, and query scoping.
- AI / RAG scope: Verify unauthorized chunks cannot become retrieval results, prompt context, citations, or stored AI evidence.
- Testing scope: Direct API bypass attempts; cross-scope document/chat/feedback/dashboard/audit tests; safe error/not-found behavior.
- DevOps / Observability scope: Secret/configuration review and security-event coverage for denied access.
- Out of scope: New authentication models, enterprise SSO, production penetration-test program.
- Exit criteria: Security tests demonstrate backend authorization and organization boundary enforcement across all MVP pathways.
- Suggested GitHub issue title: `[Sprint 25] Harden authorization and validate organization isolation end to end`

### Sprint 26: Integration, API, Frontend, And E2E Test Completion

- Goal: Complete deterministic automated coverage for critical MVP workflows.
- Why this comes now: The feature surface is complete enough for end-to-end scenarios without moving the target during test design.
- Deliverables: Final unit/integration/API/frontend suites; E2E smoke scenarios; UAT support fixtures; coverage-gap review.
- Backend scope: Fix only defects exposed by tests within the canonical MVP contract.
- Frontend scope: Validate login, upload/status, chat/citations, feedback, dashboard, admin/health visibility, and error handling.
- Database scope: Test relational persistence, lifecycle constraints, query scope, citation relationships, and metric aggregation.
- AI / RAG scope: Fake-provider E2E for cited answer and insufficient-context outcome; retrieval-before-generation verification.
- Testing scope: Critical workflows include auth, roles, scope, document upload/processing, retrieval eligibility, RAG, citations, history, feedback, dashboard, health/audit, and cross-scope denial.
- DevOps / Observability scope: Tests assert correlation/safe logging where practical; no live AI dependency.
- Out of scope: Performance/load testing, live-provider acceptance gate, Phase 2 functionality.
- Exit criteria: Deterministic local/CI-suitable tests cover all critical MVP success and security-failure paths.
- Suggested GitHub issue title: `[Sprint 26] Complete MVP automated and E2E smoke test coverage`

### Sprint 27: CI Pipeline And Container Hardening

- Goal: Automate reliable validation for backend, Angular, tests, and container readiness.
- Why this comes now: CI should enforce a stable implemented MVP and its test gates before release.
- Deliverables: GitHub Actions workflow(s); backend restore/build/test; Angular install/build/test; E2E smoke execution strategy where practical; Docker build validation; secret-safe configuration.
- Backend scope: Resolve build/test configuration issues required for repeatable CI.
- Frontend scope: Angular CI build and tests.
- Database scope: Integration-test database/migration setup appropriate for CI; controlled migration checks.
- AI / RAG scope: Configure fake embedding/generation/retrieval providers for all normal CI workflows.
- Testing scope: CI executes selected deterministic test suites and reports failures; optional live-provider check is not a required gate.
- DevOps / Observability scope: Container build checks, configuration validation, artifact/log retention without secrets, rollback/migration caution documented.
- Out of scope: Production Azure release pipeline, committed provider credentials, CI dependence on live Azure OpenAI/OpenAI.
- Exit criteria: A clean CI run builds the application, runs critical tests with fakes, and validates containers/configuration without real secrets.
- Suggested GitHub issue title: `[Sprint 27] Harden GitHub Actions CI and container validation`

### Sprint 28: Diagram Artifact Cleanup And Documentation Polish

- Goal: Bring implementation-facing documentation and permitted rendered artifacts into final alignment after the implemented design is stable.
- Why this comes now: Diagram artifacts should reflect verified implementation documentation rather than being regenerated during moving design work.
- Deliverables: Review Markdown Mermaid sources; review API/security/deployment/readme/runbook text for implemented behavior; explicitly approved artifact cleanup plan.
- Backend scope: None.
- Frontend scope: None.
- Database scope: None.
- AI / RAG scope: None.
- Testing scope: Documentation and link/path checks if a documentation lint/check mechanism is introduced.
- DevOps / Observability scope: Confirm operational support/run instructions match implemented health and telemetry behavior.
- Out of scope: Architecture changes, new product capability, generating PNGs unless separately and explicitly authorized at implementation time.
- Exit criteria: Documentation remains consistent with the implemented MVP; when diagram artifact generation is explicitly authorized, replace or remove `docs/diagrams/business-process/monitoring-sla-process.png` in favor of `docs/diagrams/business-process/monitoring-operational-process.png`.
- Suggested GitHub issue title: `[Sprint 28] Align final documentation and approved diagram artifacts`

### Sprint 29: MVP Stabilization And Release Checklist

- Goal: Confirm the document-based internal assistant is ready for MVP demonstration or release.
- Why this comes now: Release must follow completed features, security validation, test completion, CI validation, and documentation alignment.
- Deliverables: MVP acceptance checklist; release notes scope review; demo data verification; known limitations; rollback/migration notes; readiness sign-off.
- Backend scope: Defect fixes required to satisfy documented MVP acceptance criteria only.
- Frontend scope: Final Angular workflow validation and accessibility/usability checks for core screens.
- Database scope: Migration review, clean-seed verification, relational integrity verification, and release data-safety review.
- AI / RAG scope: Validate cited grounded answers and insufficient-context outcomes with deterministic fakes, plus optional controlled provider demonstration if separately configured.
- Testing scope: Run release test set and E2E smoke scenarios; confirm CI is green.
- DevOps / Observability scope: Validate local/Docker instructions, health endpoints, logging safety, secrets posture, and Azure-ready configuration notes.
- Out of scope: Phase 2/3 features, production-grade Azure hardening, any expansion into adjacent operational platforms.
- Exit criteria: All MVP completion criteria below are satisfied, non-MVP items remain parked, and the release does not depend on real data, committed secrets, or normal-CI live AI calls.
- Suggested GitHub issue title: `[Sprint 29] Stabilize and certify the KnowledgeOps-AI MVP release`

## MVP Completion Criteria

- [ ] Clean Architecture boundaries are preserved across Domain, Application, Infrastructure, API, Worker, and tests.
- [ ] Controllers remain thin and delegate use-case orchestration to Application services.
- [ ] Angular frontend builds and supports the approved MVP screens.
- [ ] SQL Server and EF Core persistence work with reviewed migration behavior.
- [ ] Docker/local environment is reproducible using safe configuration.
- [ ] Only fictional or synthetic demo/test data is included.
- [ ] Authentication works for approved internal users and rejects invalid or disabled access safely.
- [ ] RBAC works using only `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, and `Admin`.
- [ ] Organization scope is enforced on protected reads, writes, retrieval, prompts, citations, metrics, and applicable audit records.
- [ ] Admin user and basic role-management functions are protected and auditable.
- [ ] Document upload validates and stores approved internal source metadata and files safely.
- [ ] Document processing lifecycle works using only `Uploaded`, `Processing`, `Processed`, and `Failed`.
- [ ] Processing failures retain safe `failure_reason` information for authorized support.
- [ ] Chunks and embeddings are generated for successfully processed supported documents.
- [ ] Retrieval uses only documents where `processing_status = Processed`, `is_retrieval_enabled = true`, `deleted_at IS NULL` where applicable, and organization scope authorizes access.
- [ ] Retrieval-disabled documents remain excluded without using a `Disabled` processing status.
- [ ] RAG answers are based on authorized retrieved context.
- [ ] Grounded answers return and persist source citations.
- [ ] Insufficient context is disclosed safely and stored as an MVP signal.
- [ ] Chat sessions/interactions and their source relationships are stored and securely viewable.
- [ ] `Useful` and `NotUseful` feedback is stored without duplicate metric inflation.
- [ ] Basic dashboard metrics work for authorized scoped users, including document status, question count, latency, cost when available, feedback counts, and insufficient-context count.
- [ ] `GET /api/v1/health` exposes safe basic status and `GET /api/v1/health/details` is sanitized and Admin-only.
- [ ] Safe structured logging, correlation IDs, and audit-sensitive event records exist.
- [ ] Tests cover critical workflow, authorization, scope, retrieval, RAG, citation, feedback, dashboard, health, and failure behavior.
- [ ] Normal CI uses fake AI providers and is green without live AI calls.
- [ ] No real data, API keys, secrets, tokens, passwords, or committed sensitive connection strings are present.
- [ ] Non-MVP items remain explicitly parked and are not represented as completed MVP behavior.

## Phase 2 Parking Lot

The following capabilities may be considered only after the MVP is stable and scope is approved:

- Full knowledge-gap review workflow.
- Knowledge-gap queue, classification, assignment, review decisions, and resolution tracking.
- Topic clustering or similar-question grouping.
- Dedicated QA or Trainer roles if a later approved permission model requires them.
- Advanced analytics and quality review views.
- Document versioning and advanced approval workflow.
- Document replacement workflow.
- Richer feedback comments and review handling.
- Hybrid search improvements.
- CSV/report exports.
- Notification center.
- More complete observability dashboards.
- Expanded E2E coverage beyond MVP release confidence.
- Application Insights integration if it is not included as an MVP supportability implementation.
- Document processing re-enable and retry operations where formally approved.

## Phase 3 Parking Lot

The following capabilities remain future enterprise/deployment considerations and are not required for MVP release:

- Production Azure deployment.
- Azure Static Web Apps or Azure App Service frontend hosting.
- Azure App Service or Azure Container Apps API hosting.
- Azure-hosted worker deployment.
- Azure SQL.
- Azure Blob Storage.
- Azure AI Search.
- Azure Key Vault.
- Application Insights production integration.
- Enterprise SSO or Microsoft Entra ID.
- External integrations such as SharePoint, Teams, or CRM systems if approved later.
- Performance and load testing program.
- Production hardening, resilience, disaster recovery, and operational readiness work.
- Advanced MLOps.
- Live agent assist only if a future strategy explicitly approves it.
- Real-time transcription only if a future strategy explicitly approves it.

## Risks And Controls

| Risk | Control |
| --- | --- |
| AI hallucination or unsupported guidance | Require retrieval-before-generation, safe insufficient-context handling, citations for grounded answers, human-authority messaging, and deterministic safe-path tests. |
| Weak retrieval quality | Persist retrieval metadata, evaluate representative synthetic questions, track insufficient-context outcomes, and tune only behind the retrieval abstraction. |
| Weak chunking strategy | Use deterministic testable chunking, preserve source/page/section metadata where available, and evaluate retrieval fixtures over representative fictional documents. |
| Missing or incorrect citations | Persist retrieval-to-citation relationships, require citations for grounded answers, and test mapping plus authorized visibility. |
| Unauthorized document exposure | Enforce backend authentication, permissions, organization filters, safe errors, and direct API security tests. |
| Cross-organization retrieval leakage | Apply organization scope before candidate retrieval and before prompt construction; test that foreign chunks never reach results, prompts, or citations. |
| Prompt context leakage | Avoid logging raw prompts/chunks, restrict stored metadata, sanitize errors and diagnostics, and review telemetry fields. |
| Provider downtime | Isolate providers behind interfaces, handle failures safely, record operational signals, and use deterministic fakes for normal tests. |
| High AI cost | Store token and estimated-cost metadata when available, bound retrieved context, expose scoped metrics, and avoid misleading zero values. |
| High response latency | Capture retrieval, generation, and total RAG latency; preserve asynchronous ingestion; diagnose slow dependencies safely. |
| Document processing failure | Track processing lifecycle and safe failure reason, expose authorized failure visibility, and keep failed documents non-retrievable. |
| Secrets exposure | Use environment/secret storage, prohibit committed credentials, sanitize health/log output, and review configuration in CI/release checks. |
| Dashboard metric leakage or inaccuracy | Scope all aggregations by role and organization, derive metrics from approved records, and test unavailable-cost and cross-scope behavior. |
| Business rules leak into controllers | Maintain thin controllers, place use-case rules in Domain/Application, and use architecture review/tests in each feature sprint. |
| EF Core leaks into Domain or Application | Keep EF Core mappings/repositories in Infrastructure and expose stable application/domain contracts. |
| AI/provider SDKs leak into Application or Domain | Preserve provider interfaces and Infrastructure adapters; use fake implementations in automated tests. |
| Frontend-only authorization is mistaken for security | Enforce every protected capability in backend policies/application queries and test direct API calls from denied roles. |
| Scope creep into a customer chatbot, live agent assist, ticketing, or adjacent platform functionality | Apply sprint out-of-scope checks and canonical scope review before accepting feature work. |
| CI depends on live AI calls | Make fake providers the default CI configuration; reserve live-provider checks for optional, explicitly controlled validation. |
| Lifecycle and retrieval availability are conflated | Use only four processing statuses and filter retrieval through `is_retrieval_enabled`, soft-delete, and organization scope. |
| Health/support diagnostics expose sensitive information | Keep basic health safe, restrict details to `Admin`, and sanitize dependencies, errors, and operational displays. |

## Validation Checklist

Use this checklist when closing each future implementation issue or issue group:

- [ ] Scope matches MVP or is explicitly parked for Phase 2/3.
- [ ] No ticketing/SLA/OpsSphere behavior introduced.
- [ ] No customer-facing chatbot introduced.
- [ ] No real-time transcription/live agent assist introduced.
- [ ] Controllers remain thin.
- [ ] Application services coordinate use cases.
- [ ] Domain rules do not depend on infrastructure.
- [ ] EF Core remains in Infrastructure.
- [ ] AI provider SDKs remain in Infrastructure.
- [ ] Backend authorization is enforced independently of frontend visibility.
- [ ] Organization scope applies to all scoped reads/writes.
- [ ] Retrieval excludes unauthorized, unprocessed, failed, soft-deleted, or retrieval-disabled documents.
- [ ] Prompt construction only uses authorized retrieved context.
- [ ] Grounded answers include citations.
- [ ] Insufficient context is handled safely.
- [ ] Tests cover success, validation, authorization, and cross-scope cases where applicable.
- [ ] No live AI calls are required in normal CI.
- [ ] No real data, secrets, tokens, passwords, or connection strings are committed.
- [ ] Health, logging, correlation ID, and error behavior remain safe.
- [ ] Documentation is updated when behavior or architecture decisions change.
