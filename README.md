# KnowledgeOps-AI

> An enterprise-grade AI knowledge assistant for contact centers — built with .NET 10, Angular, SQL Server, and RAG.

[![CI](https://github.com/froblerom/knowdledgeops_ai/actions/workflows/ci.yml/badge.svg)](https://github.com/froblerom/knowdledgeops_ai/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-19-DD0031?logo=angular)](https://angular.dev/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/en-us/sql-server)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker)](https://www.docker.com/)

KnowledgeOps-AI helps contact center teams turn fragmented internal documentation into a reliable, conversational, and cited knowledge assistant. Agents ask questions in plain language; the system retrieves relevant document sections, generates grounded AI answers, and shows exactly which source was used — so nothing is invented.

---

<!-- SCREENSHOT PLACEHOLDER — replace with actual screenshot once captured -->
<!-- ![KnowledgeOps-AI chat with grounded answer and citations](docs/screenshots/chat-grounded-answer.png) -->

---

## The Problem

Contact center teams depend on dozens of documents: refund policies, escalation procedures, troubleshooting guides, compliance rules, and training manuals. That knowledge lives in shared drives, PDFs, legacy portals, and email threads. Agents spend time searching instead of helping customers. New hires rely on supervisors for questions already answered in writing. No one knows which document is current. Managers have no visibility into recurring knowledge gaps.

## The Solution

KnowledgeOps-AI gives organizations a single interface to upload internal documents, process them into searchable knowledge, and query them through a RAG-powered assistant. Every answer is grounded in retrieved document chunks and comes with source citations. When the system cannot find a relevant source, it says so — rather than inventing a policy.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend API | .NET 10 · ASP.NET Core Web API |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / API) |
| Background processing | .NET Worker Service |
| Database | SQL Server 2022 (Docker) · Entity Framework Core |
| Frontend | Angular 19 · TypeScript |
| AI / Embeddings | Azure OpenAI · OpenAI API · Provider abstraction (pluggable) |
| Retrieval | Vector similarity search over stored chunk embeddings |
| Auth | JWT · BCrypt · RBAC permission matrix |
| DevOps | Docker Compose · GitHub Actions CI |
| Testing | xUnit · Fake AI providers (no live calls in CI) |

---

## Architecture Overview

┌─────────────────────────────────────────────────────────┐
│  Angular SPA  (port 4200)                               │
│  Login · Chat · Documents · Dashboard · Admin           │
└────────────────────────┬────────────────────────────────┘
│ HTTP / JWT
┌────────────────────────▼────────────────────────────────┐
│  ASP.NET Core Web API  (port 5194)                      │
│  Controllers · RBAC · Organization scope · Audit log    │
└────────────────────────┬────────────────────────────────┘
│ CQRS / Application services
┌────────────────────────▼────────────────────────────────┐
│  Application Layer                                      │
│  RAG orchestration · Prompt builder · Citations        │
│  Feedback · Dashboard · Document processing pipeline   │
└──────────┬────────────────────────────┬─────────────────┘
│ EF Core                    │ Provider interfaces
┌──────────▼───────────┐   ┌────────────▼────────────────┐
│  SQL Server           │   │  AI / Embedding providers   │
│  Documents · Chunks  │   │  Azure OpenAI · OpenAI API  │
│  Chat · Feedback     │   │  Fake (CI / local dev)      │
│  Dashboard · Audit   │   └─────────────────────────────┘
└──────────────────────┘
▲
┌──────────┴───────────┐
│  .NET Worker Service │
│  Async doc pipeline  │
│  Extract · Chunk     │
│  Embed · Index       │
└──────────────────────┘



For C4 diagrams, ERD, RAG flow, and Clean Architecture diagram see [docs/12-c4-architecture.md](docs/12-c4-architecture.md).

---

## Implemented Features (MVP)

### Authentication & Authorization
- JWT login with BCrypt password hashing
- Five roles: `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, `Admin`
- Permission-based authorization via policy provider
- Organization-scoped data isolation — users can only access their own organization's data and documents

### Document Management
- Document upload (TXT, Markdown, PDF)
- Document metadata storage and processing status tracking (`Uploaded → Processing → Processed / Failed`)
- Document list and detail views with processing status

### Async Document Processing Pipeline
- Background Worker service picks up uploaded documents and processes them asynchronously
- Text extraction, document chunking, and embedding generation
- Provider abstraction supports Azure OpenAI, OpenAI API, or Fake (deterministic, no API keys needed)
- Failure reason storage and admin visibility into failed documents

### RAG Chat Assistant
- Natural-language questions answered from internal documents
- Semantic retrieval of relevant chunks with organization scope filtering
- Grounded prompt construction — the AI is given only retrieved context, not external knowledge
- Source citations returned with each answer (document title, chunk reference)
- Insufficient-context response when no relevant source is found — the assistant does not invent answers

### Chat History
- Chat sessions and interactions stored per user and organization
- Chat history page and interaction detail view

### Answer Feedback
- Users can mark any answer as `Useful` or `Not Useful`
- Feedback persisted for dashboard metrics and future quality review

### Operational Dashboard
- Questions asked, active users, average latency, estimated AI cost
- Documents uploaded / processed / failed
- Useful and not-useful feedback counts
- Insufficient-context question count
- Role-restricted visibility (Manager / Admin)

### Admin
- User management: create, update role, deactivate
- Audit log viewer
- Processing failures view

### Observability & DevOps
- Structured audit event logging
- Correlation ID middleware
- Health check endpoint
- GitHub Actions CI with backend restore / build / test / lint, frontend install / build / test
- Fake AI providers in CI — no external API calls, no secrets required
- Docker Compose stack (SQL Server)
- EF Core migrations with fictional seed data (two organizations, seven users, five roles)

---

## Planned Features (Not Yet Implemented)

### Phase 2 — Governance & Quality
- Document version management and replacement
- Document tags, categories, effective and expiration dates
- Admin retry for failed document processing
- Enhanced feedback with optional comments and reason categories
- Answer flagging for supervisor / QA review
- Richer dashboard: trends over time, most-used documents, knowledge gap signals
- Citation preview with highlighted source text
- Knowledge gap review workflow for administrators

### Phase 3 — Enterprise Maturity
- Azure Blob Storage (currently local filesystem)
- Azure Application Insights integration
- Azure Entra ID / SSO authentication
- Production Azure deployment with Infrastructure as Code
- Advanced analytics: topic clustering, cost forecasting, team-level insights
- External integrations: SharePoint, Confluence, ServiceNow, Zendesk
- Real-time agent assist
- Multi-language document support

---

## Getting Started

### Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0 (see `global.json`) |
| Node.js / npm | npm 11+ |
| Docker Desktop | 20.10+ (Linux containers) |
| Docker Compose | v2+ |

### 1. Clone and configure

```bash
git clone https://github.com/froblerom/knowdledgeops_ai.git
cd knowdledgeops_ai
cp .env.example .env
# Edit .env — set KNOWLEDGEOPS_SQL_PASSWORD to a strong local password
2. Start SQL Server

docker compose up -d sqlserver
docker compose ps   # wait for "healthy"
3. Apply migrations

$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=<your-local-password>;TrustServerCertificate=True;Encrypt=True"

dotnet tool run dotnet-ef database update `
  --project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj `
  --startup-project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj
4. Run the API

dotnet run --project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
# http://localhost:5194
5. Run the Worker

dotnet run --project src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj
6. Run the frontend

cd frontend
npm install
npm start
# http://localhost:4200
AI provider: The default is Fake mode — no Azure OpenAI or OpenAI API keys needed. To use a real provider, set KNOWLEDGEOPS_AI_PROVIDER_MODE=AzureOpenAI (or OpenAI) and add your credentials in .env only.

Demo Users
Two fictional organizations are seeded. Passwords are not seeded — provision them locally via the Admin API or a direct SQL update. See docs/demo-data.md for the credential bootstrap procedure.

Asteria Support Group — covers all five roles

Display Name	Email	Role
Admin A	admin.a@asteria.example.com	Admin
KnowledgeAdmin A	knowledgeadmin.a@asteria.example.com	KnowledgeAdmin
Manager A	manager.a@asteria.example.com	Manager
Supervisor A	supervisor.a@asteria.example.com	Supervisor
Agent A	agent.a@asteria.example.com	Agent
Boreal Contact Services — separate organization for isolation testing

Display Name	Email	Role
Admin B	admin.b@boreal.example.com	Admin
Agent B	agent.b@boreal.example.com	Agent
Agents from Asteria cannot access documents or chat sessions from Boreal, and vice versa.

Running Tests
Backend

# Build
dotnet msbuild KnowledgeOps.sln -t:Build -p:Configuration=Release

# Unit and integration tests
dotnet test KnowledgeOps.sln --configuration Release
Frontend

cd frontend
npm run test        # unit tests (Karma)
npm run build       # production build check
CI runs both suites on every push. No live AI calls are made — the Fake embedding and generation providers are used. See .github/workflows/ci.yml.

Project Structure

knowdledgeops_ai/
├── src/
│   ├── KnowledgeOps.Domain/          # Entities, value objects — no dependencies
│   ├── KnowledgeOps.Application/     # Use cases, interfaces, orchestration
│   ├── KnowledgeOps.Infrastructure/  # EF Core, JWT, AI providers, file storage
│   ├── KnowledgeOps.Api/             # Controllers, middleware, DI composition
│   └── KnowledgeOps.Worker/          # Background document processing service
├── frontend/                         # Angular 19 SPA
├── tests/                            # Unit, integration, E2E tests
├── docs/                             # Architecture, business context, ADRs
├── .github/workflows/                # CI pipelines
├── docker-compose.yml
└── .env.example
Documentation
Document	Description
Executive Summary	Business problem, value, and technical vision
Scope & Roadmap	MVP scope, Phase 2 / 3 plans, out-of-scope boundaries
C4 Architecture	System context, container, and component diagrams
Local Development	Full local setup guide
Demo Data	Seed organizations, users, credential bootstrap
Security & Permissions	RBAC model and permission matrix
Architecture Decisions	ADRs for SQL Server, Angular, RBAC, EF Core, RAG, Clean Architecture
Video Demo
<!-- VIDEO PLACEHOLDER --> <!-- [![Watch the demo](docs/screenshots/video-thumbnail.png)](https://youtu.be/PLACEHOLDER) -->
Demo video coming soon — will cover: login, document upload, processing status, grounded RAG answer with citations, insufficient-context response, feedback submission, and the operational dashboard.

Screenshots
<!-- SCREENSHOTS PLACEHOLDER — add to docs/screenshots/ and uncomment --> <!-- | Login | Chat — grounded answer | |---|---| | ![Login](docs/screenshots/login.png) | ![Chat](docs/screenshots/chat-grounded-answer.png) | | Source citations | Insufficient context | |---|---| | ![Citations](docs/screenshots/chat-citations.png) | ![No context](docs/screenshots/chat-insufficient-context.png) | | Document upload | Document list + processing status | |---|---| | ![Upload](docs/screenshots/document-upload.png) | ![Documents](docs/screenshots/document-list.png) | | Dashboard metrics | Admin — user management | |---|---| | ![Dashboard](docs/screenshots/dashboard.png) | ![Admin](docs/screenshots/admin-users.png) | | CI — GitHub Actions green | |---| | ![CI](docs/screenshots/ci-green.png) | -->
Known Limitations
This project is a portfolio-grade MVP, not a production deployment. Honest limitations:

No cloud deployment. The stack runs locally via Docker Compose. Azure hosting, Blob Storage, and Application Insights are planned for Phase 2–3.
No enterprise SSO. Authentication uses local JWT + BCrypt. Azure Entra ID is Phase 3.
is_retrieval_enabled requires a manual SQL update for local demos. The re-enable API endpoint is deferred to Phase 2.
No document replacement or versioning. Uploading a new version requires deleting the old document first.
No real AI calls in CI. The Fake provider is deterministic and tests the orchestration layer; it does not validate retrieval or generation quality.
No real-time agent assist. This is an explicit Phase 3 feature, out of scope for MVP.
Portfolio Note
KnowledgeOps-AI was designed and built as a senior-level portfolio project demonstrating applied AI engineering in a realistic enterprise context — not as a generic CRUD app with an LLM bolted on.

It shows:

Business-driven software design with documented scope boundaries
Clean Architecture across a multi-project .NET 10 solution
Provider abstraction for AI / embedding services
Async background processing with a dedicated Worker service
Multi-tenant data isolation enforced at the application layer
RBAC with a permission matrix, not just role guards
RAG orchestration with grounded prompts, citations, and safe insufficient-context handling
Observability from the start: audit events, correlation IDs, health checks
CI with fake providers so no secrets are required to run the full test suite
Built by Fred Roblero · LinkedIn · GitHub
