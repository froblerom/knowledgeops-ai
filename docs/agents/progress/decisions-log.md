# Implementation Decisions Log

This log tracks implementation-time and harness-routing decisions. Accepted ADRs remain authoritative for architecture decisions.

| Date | Decision | Rationale | Affected Area | ADR Needed |
| --- | --- | --- | --- | --- |
| 2026-05-24 | Use `docs/agents/` as the canonical agent context harness root; do not use `docs/agent/`. | Multiple shared contexts, specialist definitions and progress files require one unambiguous plural modular location. | Agent harness | No |
| 2026-05-24 | Use prompt Levels 0 through 3 only. | Level 3 covers cross-layer, security-sensitive, harness and release work for current MVP. | Prompt routing | No |
| 2026-05-24 | Add dedicated `rag-implementation-agent`. | RAG safety crosses authorization, prompts, citations, provider isolation and sensitive AI telemetry. | Agent harness | No |
| 2026-05-24 | Use progress files before implementation prompts begin. | Prompts require current state, decisions, risks and verified completion history. | Execution workflow | No |
| 2026-05-24 | Mermaid Markdown remains diagram source of truth. | Accepted ADR-009; rendered images remain artifacts. | Documentation/diagrams | No |
| 2026-05-24 | Angular is selected for MVP frontend. | Accepted ADR-003. | Frontend | No |
| 2026-05-24 | `docs/09-business-rules.md` is canonical for `BR-###`. | Prevent conflicting rule identifiers and traceability drift. | Rules/traceability | No |
| 2026-05-24 | Document disablement uses `is_retrieval_enabled = false`, not a `Disabled` processing status. | Keeps processing outcome separate from retrieval availability. | Documents/retrieval | No |
| 2026-05-24 | Future implementation prompts must classify first using `docs/agents/13-prompt-classifier.md`. | Ensures scope, required context, risk and validation expectations are declared before implementation begins. | Execution workflow | No |
| 2026-05-25 | Use `KnowledgeOps` for code projects and namespaces, with `KnowledgeOpsAI.sln` as the solution name while retaining KnowledgeOps-AI branding. | Matches the approved scaffold audit and keeps code identifiers consistent without changing product branding. | Backend solution structure | No |
| 2026-05-25 | Target `net10.0` and pin the installed .NET SDK `10.0.204` in `global.json` with .NET 10 minor roll-forward. | Establishes a reproducible backend foundation without downgrading the approved target framework. | Backend toolchain | No |
| 2026-05-25 | Defer `KnowledgeOps.E2ETests` beyond Issue #3. | Issue #3 requires only the four approved scaffold test projects; E2E workflow coverage belongs to a later authorized sprint. | Testing structure | No |
| 2026-05-25 | Keep Issue #3 dependencies limited to host/DI abstractions and xUnit template support. | Persistence, security, provider SDKs, observability integrations and container testing are outside scaffold scope. | Backend dependency surface | No |
| 2026-05-25 | Use Angular 21.2.12 with Vitest (not Karma) as test runner. | Angular 21 ships `@angular/build:unit-test` backed by Vitest + jsdom; Karma is not available. | Frontend toolchain | No |
| 2026-05-25 | Auth guard returns `true` unconditionally until Sprint 6. | Route protection is UX guidance only; backend authorization is source of truth. Sprint 6 implements real auth. | Frontend security | No |
| 2026-05-25 | API interceptor is pass-through only; no JWT or Authorization header injection. | Real auth headers deferred to Sprint 6; pass-through keeps the interceptor chain wired without premature JWT handling. | Frontend HTTP | No |
| 2026-05-25 | Development `apiBaseUrl` points to `http://localhost:5194/api/v1` (port from `launchSettings.json` HTTP profile). | Sprint 3 may change this when Docker Compose local environment is configured; port stored in environment file for easy update. | Frontend/API integration | No |
| 2026-05-25 | Angular 21 does not generate `src/environments/` by default; files created manually. | Angular 15+ removed auto-generation; `fileReplacements` in `angular.json` development configuration performs the swap. | Frontend environment config | No |
| 2026-05-25 | Use hybrid local runtime mode as primary: SQL Server via Docker Compose; API/Worker via `dotnet run`; frontend via Angular CLI. | Aligns with `docs/18-deployment-and-devops.md` Section 3.5 Option A; avoids app container complexity before persistence is implemented. | Local DevOps | No |
| 2026-05-25 | Use `mcr.microsoft.com/mssql/server:2022-latest` with `MSSQL_PID=Developer` for local SQL Server container. | SQL Server 2022 aligns with ADR-002; Developer edition is free for development and testing. | Local DevOps / Database | No |
| 2026-05-25 | Commit `.env.example` with safe placeholder values; ignore `.env` via `.gitignore`. | Matches `docs/18-deployment-and-devops.md` Section 11.2; safe template is committed, live secrets are never committed. | Security / Environment config | No |
| 2026-05-25 | Local document storage convention: `.local/storage/documents/` at repository root; ignored by `.gitignore`. | Keeps local runtime artifacts out of source control; provides a consistent path for future local storage adapter. | Local DevOps / Document storage | No |
| 2026-05-25 | Do not create API/Worker/Frontend Dockerfiles in Issue #5. | No persistence or application workflow is implemented yet; app containerization deferred until a later sprint when the full local stack can be validated end-to-end. | Local DevOps | No |
| 2026-05-25 | Use `KNOWLEDGEOPS_SQL_PORT` env var with default `1433` to allow port override if host port is already in use. | Prevents Docker Compose bind failure when a local SQL Server instance occupies port 1433. | Local DevOps | No |
| 2026-05-25 | Keep EF Core SQL Server and Design packages, `KnowledgeOpsDbContext`, configurations, migrations, and the design-time factory inside `KnowledgeOps.Infrastructure` only. | Preserves ADR-001/ADR-005 boundaries; Domain and Application remain persistence-framework independent. | Persistence architecture | No |
| 2026-05-25 | Issue #6 creates only `organizations`, `users`, `user_roles`, and `audit_log_entries`; role assignments store the five MVP `role_name` values and no `roles` table exists. | `docs/14-database-design.md` is canonical when issue fallback guidance differs; feature tables remain owned by future sprints. | Database foundation / scope | No |
| 2026-05-25 | Implement canonical nullable foreign keys from `audit_log_entries` to organizations and users, while leaving polymorphic `entity_id` unenforced. | Canonical database design lists optional organization/user relationships and explicitly avoids a polymorphic entity FK. | Audit persistence / organization scope | No |
| 2026-05-25 | Use a local `.config/dotnet-tools.json` manifest for `dotnet-ef` `10.0.8` and execute migration commands through `KnowledgeOps.Infrastructure` design-time startup. | Pins migration tooling while keeping `Microsoft.EntityFrameworkCore.Design` out of API/Application/Domain. | Tooling / persistence boundary | No |
| 2026-05-25 | Apply migrations intentionally during development/deployment validation; do not auto-run migrations from API or Worker startup. | Matches MVP deployment guidance and avoids unexpected schema changes during host startup. | Migration execution | No |

## Update Rule

Add a concise entry when a future issue makes a material implementation choice. If a choice changes an accepted architecture decision, identify the required ADR action rather than treating this log as approval.
