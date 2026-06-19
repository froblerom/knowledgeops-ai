# KnowledgeOps-AI

Enterprise AI-Powered Internal Knowledge Assistant for Contact Centers and Support Operations.

KnowledgeOps-AI is a portfolio-grade enterprise software project that demonstrates how modern .NET architecture and applied AI can be combined to transform internal business documents into reliable, searchable, cited, and measurable knowledge.

The system is designed for contact centers and support operations where agents, supervisors, managers, trainers, quality analysts, and administrators depend on accurate internal knowledge to perform their work.

## Core Capabilities

- Internal document upload and metadata management
- Background document processing
- Text extraction, chunking, and embedding generation
- Semantic retrieval
- Retrieval-Augmented Generation chat assistant
- Source citations
- Insufficient-context handling
- Chat history
- Useful / not useful feedback
- Role-based access control
- Organization-aware document access
- Operational dashboard metrics
- Latency and estimated AI cost tracking

## Technology Direction

- .NET 10
- ASP.NET Core Web API
- Clean Architecture
- SQL Server
- Entity Framework Core
- Azure OpenAI or OpenAI API
- Docker
- GitHub Actions
- Azure-ready deployment design

## Project Status

MVP Implementation — Post-Sprint 29 Complete.

The core MVP workflow is implemented and verified through Post-Sprint 29:

- Authenticated internal users with JWT Bearer, five RBAC roles (Agent, Supervisor, KnowledgeAdmin, Manager, Admin), and organization-scoped access.
- Document upload (TXT/Markdown), asynchronous background processing, text extraction, sliding-window chunking, and deterministic embedding generation.
- Organization-scoped semantic retrieval using a local SQL-backed vector store.
- Retrieval-Augmented Generation (RAG) chat pipeline with grounded prompt construction, context sufficiency policy, and safe insufficient-context handling.
- Source citations for grounded answers; safe `ProviderFailure` outcome for AI provider errors.
- Chat history, session management, and scoped review (Supervisor/Manager/Admin).
- Useful/NotUseful answer feedback.
- Permission-gated dashboard metrics (overview, documents, chat, feedback).
- Admin user and role management; safe audit log and processing failure visibility.
- Public basic health (`GET /api/v1/health`) and Admin-only sanitized health details (`GET /api/v1/health/details`).
- Multi-stage Dockerfiles for API, Worker, and Frontend; GitHub Actions CI workflow.

**Fake providers are normal for automated tests and CI.** `FakeEmbeddingProvider` (SHA-256 deterministic) and `DemoGroundedAnswerGenerator` (extractive, CI-safe) require no external credentials and are the default for all automated tests. Azure OpenAI, OpenAI API, or a local Ollama-compatible model are optional production providers, manually configured only.

**Known limitations:**
- Text extraction supports TXT and Markdown only; PDF and DOCX extraction are deferred to Phase 2.
- Document retry-processing endpoint is deferred to Phase 2.
- JWT logout is stateless (client-side token clear only); no server-side token revocation.
- Local filesystem document storage only; no production cloud storage adapter.
- SQL Server integration tests require `ConnectionStrings__DefaultConnection` and a running SQL Server container.
- Azure deployment, enterprise SSO, and production-grade cloud hardening are deferred to Phase 2/3.

## Local Setup

```bash
# 1. Copy the env template and configure local secrets
cp .env.example .env
# Edit .env with your Jwt:SigningKey and other local values

# 2. Start SQL Server
docker compose up sqlserver -d

# 3. Apply database migrations
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

Demo user credentials are defined in [`docs/demo-data.md`](docs/demo-data.md).
