# Project Context

## Identity

**KnowledgeOps-AI** is an internal, AI-powered document-based RAG knowledge assistant for contact centers and support operations.

Authorized internal users can upload internal documents, process them into text, chunks, and embeddings, retrieve authorized knowledge, ask grounded questions, review source citations, submit `Useful` or `NotUseful` feedback, and view basic operational signals.

## Business Value

- Faster access to approved internal knowledge.
- More traceable AI-assisted answers through source citations.
- Safer handling when available sources do not support an answer.
- Visibility into usage, processing, latency, cost when available, and quality signals.
- Secure separation of organization-scoped knowledge.

## MVP Includes

- Clean Architecture backend and Angular frontend.
- SQL Server with EF Core in Infrastructure.
- Docker/local SQL Server and GitHub Actions.
- Authentication, RBAC, organization-scoped access, safe health and observability.
- Document upload, asynchronous processing, extraction, chunking, embeddings and retrieval abstraction.
- RAG chat, prompt builder, citations, insufficient-context handling and chat history.
- `Useful`/`NotUseful` feedback and basic dashboard metrics.
- Fictional or synthetic data only.

## MVP Boundaries

- The MVP is internal-only and document-based.
- It does not implement unrelated case-management workflows, real-time interaction assistance, autonomous operational actions, external enterprise integrations, enterprise SSO, advanced model operations, full knowledge-gap workflow, or production cloud hardening.
- Full knowledge-gap queue, categorization, assignment, resolution and clustering remain Phase 2.

## Technical Roles

The five MVP RBAC roles are:

- `Agent`
- `Supervisor`
- `KnowledgeAdmin`
- `Manager`
- `Admin`

Business stakeholders or future personas are not additional MVP RBAC roles.

## Fixed Decisions

- Backend architecture: Clean Architecture.
- Frontend: Angular.
- Relational database: SQL Server.
- ORM: Entity Framework Core in Infrastructure.
- AI pattern: RAG with source citations and safe insufficient-context behavior.
- Access boundary: Organization scope.

## Read Exact Sources When Needed

- Scope and phases: `docs/05-scope-and-roadmap.md`.
- Roadmap: `docs/21-implementation-roadmap.md`.
- Implementation constraints: `docs/22-implementation-guardrails.md`.
- Requirements/rules: `docs/06-requirements.md` and `docs/09-business-rules.md`.
- Accepted decisions: `docs/decisions/`.

