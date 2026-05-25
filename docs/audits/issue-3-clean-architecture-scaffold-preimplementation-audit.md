# Issue #3 Clean Architecture Scaffold Pre-Implementation Audit

## 1. Purpose

This audit verifies the repository state and implementation readiness before scaffolding the backend Clean Architecture solution structure for Issue #3 (`chore: scaffold Clean Architecture backend solution`), which corresponds to Sprint 1 of the KnowledgeOps-AI MVP roadmap.

The audit confirms that the repository is clean, the .NET SDK is available at the required version, no conflicting structure exists, the canonical project and test names are settled, dependency-direction rules are understood, architecture boundary test approach is decided, and package restrictions are documented. No source code, solution file, projects, packages, migrations, Docker files, GitHub Actions workflows, or diagram artifacts are created during this audit.

---

## 2. Classification

```text
Classification
- Task type: Pre-implementation audit
- Prompt level: Level 3
- Related sprint/issue: Sprint 1 / Issue #3
- Scope: Audit-only / Backend foundation readiness
- Primary affected area: Backend Clean Architecture solution skeleton readiness
- Security or organization-scope impact: None directly; audit must preserve future security
  and organization-scope boundaries by confirming dependency-direction rules and
  architecture boundary test plans.
- AI/RAG impact: None directly; audit must confirm that provider-isolation direction
  (Application depends on abstractions, Infrastructure holds SDK implementations)
  is planned and no AI/RAG behavior is in scope for Issue #3.
- Data or migration impact: None. No schema, EF Core, or SQL Server work in Issue #3.

Reason
- Issue #3 scaffolds the Clean Architecture skeleton. It crosses multiple project layers
  (Domain, Application, Infrastructure, Api, Worker, and four test projects), establishes
  dependency-direction rules enforced by ADR-001, and sets the structural foundation for
  all subsequent sprints. Cross-layer architectural work of this kind is Level 3 per
  docs/agents/12-prompt-levels.md.

Required Context
- Agent context files: 00-agent-operating-protocol.md, 10-issue-execution-template.md,
  12-prompt-levels.md, 13-prompt-classifier.md
- Canonical documents: docs/21-implementation-roadmap.md,
  docs/22-implementation-guardrails.md, docs/11-architecture-overview.md,
  docs/17-testing-strategy.md, docs/18-deployment-and-devops.md
- ADRs: ADR-001, ADR-002, ADR-005, ADR-006, ADR-008, ADR-010
- Progress files: current-state.md, decisions-log.md, open-risks.md, completed-issues.md

Recommended Subagents
- architecture-auditor (this audit fulfills that role)
- backend-implementation-agent (for the follow-on implementation prompt)
- testing-agent (for architecture boundary test wiring)
- verification-agent (to confirm build and dependency direction after implementation)
```

---

## 3. Files And Context Reviewed

### Agent Harness Files

- `docs/agents/00-agent-operating-protocol.md`
- `docs/agents/10-issue-execution-template.md`
- `docs/agents/12-prompt-levels.md`
- `docs/agents/13-prompt-classifier.md`

### Progress Files

- `docs/agents/progress/current-state.md`
- `docs/agents/progress/decisions-log.md`
- `docs/agents/progress/open-risks.md`
- `docs/agents/progress/completed-issues.md`

### Canonical Implementation Planning Documents

- `docs/21-implementation-roadmap.md`
- `docs/22-implementation-guardrails.md`

### Architecture Documents

- `docs/11-architecture-overview.md`
- `docs/12-c4-architecture.md` (referenced; confirms Clean Architecture intent)

### Testing Documents

- `docs/17-testing-strategy.md`

### DevOps Documents

- `docs/18-deployment-and-devops.md`

### Architecture Decision Records

- `docs/decisions/README.md`
- `docs/decisions/ADR-001-use-clean-architecture.md`
- `docs/decisions/ADR-002-use-sql-server.md`
- `docs/decisions/ADR-005-use-entity-framework-core.md`
- `docs/decisions/ADR-006-use-azure-openai-compatible-provider-abstraction.md`
- `docs/decisions/ADR-008-use-asynchronous-document-processing.md`
- `docs/decisions/ADR-010-use-organization-scoped-access-boundaries.md`

### Repository Files And Directories Inspected

- Repository root: `.claude/`, `.github/`, `docs/`, `.gitignore`, `LICENSE`, `README.md`
- Presence/absence of: `KnowledgeOpsAI.sln`, `KnowledgeOps.sln`, `src/`, `tests/`,
  `global.json`, `Directory.Build.props`
- All expected project directories under `src/` and `tests/`
- `docs/audits/` (existing; contains pre-implementation and agent-context audits)

---

## 4. Repository State

### Root Files And Folders

| Item | Present |
| --- | --- |
| `.claude/` | Yes |
| `.github/` | Yes |
| `docs/` | Yes |
| `.gitignore` | Yes |
| `LICENSE` | Yes |
| `README.md` | Yes |

### Solution Files

| File | Present |
| --- | --- |
| `KnowledgeOpsAI.sln` | **No** |
| `KnowledgeOps.sln` | No |
| Any other `.sln` file | No |

**Finding**: No solution file exists. Implementation must create `KnowledgeOpsAI.sln` from scratch.

### Source And Test Directories

| Directory | Present |
| --- | --- |
| `src/` | **No** |
| `tests/` | **No** |

**Finding**: Neither `src/` nor `tests/` exists. Implementation must create both.

### Global Configuration Files

| File | Present |
| --- | --- |
| `global.json` | **No** |
| `Directory.Build.props` | **No** |

**Finding**: Neither exists. Implementation should create both during scaffolding (see Sections 5 and 6).

### Existing Project Directories

All expected project directories are absent:

| Project Directory | Present |
| --- | --- |
| `src/KnowledgeOps.Api/` | No |
| `src/KnowledgeOps.Application/` | No |
| `src/KnowledgeOps.Domain/` | No |
| `src/KnowledgeOps.Infrastructure/` | No |
| `src/KnowledgeOps.Worker/` | No |
| `tests/KnowledgeOps.Domain.Tests/` | No |
| `tests/KnowledgeOps.Application.Tests/` | No |
| `tests/KnowledgeOps.Api.Tests/` | No |
| `tests/KnowledgeOps.IntegrationTests/` | No |

**Finding**: No project directories exist. Implementation creates all of them.

### Naming Conventions Discovered

`docs/22-implementation-guardrails.md` (canonical authority) specifies the project prefix as `KnowledgeOps` and the repository branding as `KnowledgeOps-AI`. `docs/11-architecture-overview.md` uses the same `KnowledgeOps.*` prefix in its recommended backend structure. No conflicting convention exists in the repository.

`docs/11-architecture-overview.md` uses `KnowledgeOps.UnitTests` in one illustrative structure; the guardrails use the granular test project names (`KnowledgeOps.Domain.Tests`, etc.), which are canonical for implementation.

---

## 5. .NET SDK Readiness

### dotnet --info Summary

```
SDK Version:     10.0.204
Host Version:    10.0.8
Architecture:    x64
Platform:        Windows (win-x64)
Base Path:       C:\Program Files\dotnet\sdk\10.0.204\
```

### Installed SDKs

```
10.0.204 [C:\Program Files\dotnet\sdk]
```

### Installed Runtimes Relevant To This Project

```
Microsoft.AspNetCore.App 10.0.8
Microsoft.NETCore.App 10.0.8
Microsoft.NETCore.App 8.0.19 (present but not targeted)
```

### .NET 10 Availability

**Yes. .NET 10 SDK 10.0.204 is installed.**

### Implementation Readiness

**READY.** .NET 10 is available. No fallback is required or permitted per the accepted Issue #3 decision. The implementation prompt must target `net10.0`.

### global.json Recommendation

A `global.json` file should be created at the repository root during implementation. This pins the SDK version for the project and prevents unexpected behavior if multiple SDK versions are installed in the future.

Recommended content:

```json
{
  "sdk": {
    "version": "10.0.204",
    "rollForward": "latestMinor"
  }
}
```

`rollForward: latestMinor` allows patch-level SDK updates while preserving the major/minor lock.

---

## 6. Scaffold Decisions Confirmed

| Decision | Confirmed Value |
| --- | --- |
| Solution file name | `KnowledgeOpsAI.sln` |
| Project prefix and namespace | `KnowledgeOps` |
| Repository branding in docs and README | `KnowledgeOps-AI` (unchanged) |
| Target framework | `net10.0` |
| Source projects | `KnowledgeOps.Api`, `KnowledgeOps.Application`, `KnowledgeOps.Domain`, `KnowledgeOps.Infrastructure`, `KnowledgeOps.Worker` |
| Test projects | `KnowledgeOps.Domain.Tests`, `KnowledgeOps.Application.Tests`, `KnowledgeOps.Api.Tests`, `KnowledgeOps.IntegrationTests` |
| `KnowledgeOps.E2ETests` | **Deferred.** The guardrails list it in the intended final structure (`docs/22-implementation-guardrails.md`), but the accepted Issue #3 decision explicitly excludes it. It is deferred to a later sprint (likely Sprint 26). |
| EF Core packages | **Forbidden in Issue #3** |
| JWT/Auth packages | **Forbidden in Issue #3** |
| MediatR | **Forbidden in Issue #3** (no established repo convention) |
| AI/provider SDKs | **Forbidden in Issue #3** |
| Business endpoints | **Forbidden in Issue #3** |
| Health endpoints | **Forbidden in Issue #3** |
| Authentication/authorization | **Forbidden in Issue #3** |
| EF DbContext | **Forbidden in Issue #3** |
| Docker, CI, migrations, diagrams | **Forbidden in Issue #3** |

---

## 7. Planned Project Reference Graph

### Source Project References

```text
KnowledgeOps.Domain
  └── (no project references — pure domain concepts and rules only)

KnowledgeOps.Application
  └── references KnowledgeOps.Domain

KnowledgeOps.Infrastructure
  ├── references KnowledgeOps.Application
  └── references KnowledgeOps.Domain

KnowledgeOps.Api
  ├── references KnowledgeOps.Application
  └── references KnowledgeOps.Infrastructure

KnowledgeOps.Worker
  ├── references KnowledgeOps.Application
  └── references KnowledgeOps.Infrastructure
```

### Test Project References

```text
KnowledgeOps.Domain.Tests
  └── references KnowledgeOps.Domain

KnowledgeOps.Application.Tests
  ├── references KnowledgeOps.Application
  └── references KnowledgeOps.Domain

KnowledgeOps.Api.Tests
  ├── references KnowledgeOps.Api
  ├── references KnowledgeOps.Application
  └── references KnowledgeOps.Domain

KnowledgeOps.IntegrationTests
  ├── references KnowledgeOps.Api
  ├── references KnowledgeOps.Infrastructure
  ├── references KnowledgeOps.Application
  └── references KnowledgeOps.Domain
  (KnowledgeOps.Worker reference: add only if host build or integration surface validation requires it)
```

### Dependency Direction Rule Summary

```
Domain → (nothing)
Application → Domain
Infrastructure → Application, Domain
Api → Application, Infrastructure
Worker → Application, Infrastructure
```

**Forbidden directions** (ADR-001 and guardrails):
- Domain must not reference Application, Infrastructure, Api, Worker, ASP.NET Core, EF Core, JWT, AI provider SDKs, or logging infrastructure.
- Application must not reference Infrastructure or AI provider SDKs.
- Infrastructure, Api, and Worker may reference inward layers but must not bypass Application for business behavior.

---

## 8. Architecture Boundary Plan

### Approach

Use assembly marker classes and .NET reflection. No third-party architecture testing package is required for Issue #3.

Each source project should include one marker class in its root namespace:

| Project | Marker Class |
| --- | --- |
| `KnowledgeOps.Domain` | `internal sealed class DomainMarker { }` |
| `KnowledgeOps.Application` | `internal sealed class ApplicationMarker { }` |
| `KnowledgeOps.Infrastructure` | `internal sealed class InfrastructureMarker { }` |
| `KnowledgeOps.Api` | `internal sealed class ApiMarker { }` |
| `KnowledgeOps.Worker` | `internal sealed class WorkerMarker { }` |

Tests use `typeof(DomainMarker).Assembly.GetReferencedAssemblies()` to assert forbidden or expected assembly references.

### Planned Boundary Tests And Where They Live

| Test Name | Location | Practical In Issue #3 |
| --- | --- | --- |
| `Domain_Should_Not_Reference_Api` | `KnowledgeOps.Domain.Tests` | Yes |
| `Domain_Should_Not_Reference_Infrastructure` | `KnowledgeOps.Domain.Tests` | Yes |
| `Domain_Should_Not_Reference_EntityFrameworkCore` | `KnowledgeOps.Domain.Tests` | Yes (trivially passes now; guard for future) |
| `Domain_Should_Not_Reference_AspNetCore` | `KnowledgeOps.Domain.Tests` | Yes |
| `Domain_Should_Not_Reference_AiProviderSdks` | `KnowledgeOps.Domain.Tests` | Yes (trivially passes now; guard for future) |
| `Application_Should_Not_Reference_Infrastructure` | `KnowledgeOps.Application.Tests` | Yes |
| `Application_Should_Not_Reference_AiProviderSdks` | `KnowledgeOps.Application.Tests` | Yes (trivially passes now; guard for future) |
| `Infrastructure_Should_Reference_Application_And_Domain` | `KnowledgeOps.IntegrationTests` | Yes |
| `Api_Should_Reference_Application_And_Infrastructure` | `KnowledgeOps.Api.Tests` | Yes |
| `Worker_Should_Reference_Application_And_Infrastructure` | `KnowledgeOps.IntegrationTests` | Yes |

Tests that "trivially pass now" are still valuable because they become regression guards the moment a forbidden package or reference is added in a future sprint.

### Example Reflection-Based Test Pattern

```csharp
[Fact]
public void Domain_Should_Not_Reference_Infrastructure()
{
    var referencedNames = typeof(DomainMarker).Assembly
        .GetReferencedAssemblies()
        .Select(a => a.Name ?? string.Empty)
        .ToList();

    Assert.DoesNotContain("KnowledgeOps.Infrastructure", referencedNames);
}
```

For framework-level checks (ASP.NET Core, EF Core):

```csharp
[Fact]
public void Domain_Should_Not_Reference_AspNetCore()
{
    var referencedNames = typeof(DomainMarker).Assembly
        .GetReferencedAssemblies()
        .Select(a => a.Name ?? string.Empty)
        .ToList();

    Assert.DoesNotContain(name => name.StartsWith("Microsoft.AspNetCore",
        StringComparison.OrdinalIgnoreCase), referencedNames);
}
```

---

## 9. Minimal Host And DI Placeholder Plan

### API Host (`KnowledgeOps.Api`)

The implementation may create a minimal `Program.cs` that:
- Builds and starts the ASP.NET Core host.
- Calls `AddApplication()` and `AddInfrastructure(configuration)` placeholders.
- Contains no business endpoints.
- Contains no health endpoints (deferred to Sprint 8).
- Contains no authentication middleware.
- Contains no authorization policies.
- Contains no EF Core DbContext registration.
- Contains no provider SDK registrations.
- Contains no document processing services.

### Worker Host (`KnowledgeOps.Worker`)

The implementation may create a minimal `Program.cs` that:
- Builds and starts the .NET Worker host.
- Calls `AddApplication()` and `AddInfrastructure(configuration)` placeholders.
- Contains no business processing pipelines.

### DI Placeholder Files

| File | Location | Method Signature |
| --- | --- | --- |
| `DependencyInjection.cs` | `src/KnowledgeOps.Application/` | `public static IServiceCollection AddApplication(this IServiceCollection services)` |
| `DependencyInjection.cs` | `src/KnowledgeOps.Infrastructure/` | `public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)` |

Placeholders must not register business workflows, provider SDKs, EF Core DbContext, authentication, or authorization services in Issue #3.

### Forbidden In Issue #3 Host Configuration

- No `app.UseAuthentication()` / `app.UseAuthorization()`
- No `builder.Services.AddDbContext<>()`
- No Azure OpenAI / OpenAI SDK service registrations
- No MediatR registrations
- No Serilog or Application Insights sink wiring
- No `MapGet()`, `MapPost()`, or controller route definitions for business endpoints
- No health check middleware

---

## 10. Package Policy

### Allowed Packages In Issue #3

| Package | Source | Justification |
| --- | --- | --- |
| `Microsoft.NET.Test.Sdk` | xUnit template default | Required for test runner |
| `xunit` | xUnit template default | Selected test framework |
| `xunit.runner.visualstudio` | xUnit template default | IDE integration |
| `coverlet.collector` | xUnit template default | Coverage support |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | SDK default or explicit | Needed for DI placeholder extension methods |
| `Microsoft.Extensions.Configuration.Abstractions` | SDK default or explicit | Needed for `IConfiguration` in Infrastructure DI placeholder |
| `FluentAssertions` | Optional | Permitted only if the boundary test assertions justify it and no prohibition exists; not required |

Templates for `webapi` and `worker` will include additional framework references (`Microsoft.AspNetCore.App`, `Microsoft.Extensions.Hosting`) as implicit framework references — these are acceptable template defaults.

### Forbidden Packages In Issue #3

| Package | Reason |
| --- | --- |
| `Microsoft.EntityFrameworkCore` | EF Core deferred to Sprint 4 |
| `Microsoft.EntityFrameworkCore.SqlServer` | EF Core deferred to Sprint 4 |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Auth deferred to Sprint 6 |
| `MediatR` | Not established as repo convention; deferred until justified |
| `Azure.AI.OpenAI` | Provider SDKs deferred to Sprint 14+ |
| `OpenAI` | Provider SDKs deferred to Sprint 14+ |
| `Azure.Storage.Blobs` | Storage deferred to Sprint 11+ |
| `Azure.Identity` | Secrets/cloud identity deferred |
| `Serilog` | Logging infrastructure deferred to Sprint 8 |
| `Serilog.AspNetCore` | Logging infrastructure deferred to Sprint 8 |
| `Microsoft.ApplicationInsights.AspNetCore` | Observability deferred to Sprint 24 |
| `Testcontainers` | Integration test containers deferred to Sprint 4+ |
| Any vector database or retrieval provider SDK | Deferred to Sprint 14+ |

---

## 11. Out Of Scope

The following must not be implemented in Issue #3:

- Authentication (Sprint 6)
- Authorization policies or RBAC (Sprint 7)
- EF Core entities, DbContext, or entity configurations (Sprint 4)
- Database migrations (Sprint 4)
- Document upload or document metadata (Sprint 10–11)
- Document processing workflow (Sprint 12–13)
- Text extraction (Sprint 13)
- Embedding generation (Sprint 14)
- Vector storage or retrieval (Sprint 15–16)
- RAG orchestration (Sprint 17)
- Prompt builder (Sprint 18)
- Citation mapping (Sprint 19)
- Chat endpoints or chat UI (Sprint 20)
- Answer feedback (Sprint 22)
- Dashboard metrics (Sprint 23)
- Business endpoints of any kind
- Health check endpoints (Sprint 8)
- Docker Compose files or Dockerfiles (Sprint 3)
- GitHub Actions CI workflows (Sprint 27)
- Live AI provider integrations
- Business workflows
- Frontend Angular code (Sprint 2)
- Rendered diagram artifacts
- `KnowledgeOps.E2ETests` project (Sprint 26)

---

## 12. Implementation Plan For Next Prompt

The following is an exact, executable scaffold plan. Commands are not executed during this audit. All commands should be run from the repository root (`c:\Dev\knowdledgeops_ai`).

### Step 1: Create global.json

Create `global.json` at the repository root:

```json
{
  "sdk": {
    "version": "10.0.204",
    "rollForward": "latestMinor"
  }
}
```

### Step 2: Create Directory.Build.props

Create `Directory.Build.props` at the repository root:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

Note: `TreatWarningsAsErrors` is not set at this stage to avoid blocking build on template-generated warnings. It can be added in a later hardening sprint.

### Step 3: Create Solution File

```bash
dotnet new sln --name KnowledgeOpsAI --output .
```

### Step 4: Create Source Projects

```bash
dotnet new classlib --name KnowledgeOps.Domain \
  --output src/KnowledgeOps.Domain --framework net10.0

dotnet new classlib --name KnowledgeOps.Application \
  --output src/KnowledgeOps.Application --framework net10.0

dotnet new classlib --name KnowledgeOps.Infrastructure \
  --output src/KnowledgeOps.Infrastructure --framework net10.0

dotnet new webapi --name KnowledgeOps.Api \
  --output src/KnowledgeOps.Api --framework net10.0

dotnet new worker --name KnowledgeOps.Worker \
  --output src/KnowledgeOps.Worker --framework net10.0
```

### Step 5: Add Source Projects To Solution

```bash
dotnet sln KnowledgeOpsAI.sln add src/KnowledgeOps.Domain/KnowledgeOps.Domain.csproj
dotnet sln KnowledgeOpsAI.sln add src/KnowledgeOps.Application/KnowledgeOps.Application.csproj
dotnet sln KnowledgeOpsAI.sln add src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj
dotnet sln KnowledgeOpsAI.sln add src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
dotnet sln KnowledgeOpsAI.sln add src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj
```

### Step 6: Create Test Projects

```bash
dotnet new xunit --name KnowledgeOps.Domain.Tests \
  --output tests/KnowledgeOps.Domain.Tests --framework net10.0

dotnet new xunit --name KnowledgeOps.Application.Tests \
  --output tests/KnowledgeOps.Application.Tests --framework net10.0

dotnet new xunit --name KnowledgeOps.Api.Tests \
  --output tests/KnowledgeOps.Api.Tests --framework net10.0

dotnet new xunit --name KnowledgeOps.IntegrationTests \
  --output tests/KnowledgeOps.IntegrationTests --framework net10.0
```

### Step 7: Add Test Projects To Solution

```bash
dotnet sln KnowledgeOpsAI.sln add tests/KnowledgeOps.Domain.Tests/KnowledgeOps.Domain.Tests.csproj
dotnet sln KnowledgeOpsAI.sln add tests/KnowledgeOps.Application.Tests/KnowledgeOps.Application.Tests.csproj
dotnet sln KnowledgeOpsAI.sln add tests/KnowledgeOps.Api.Tests/KnowledgeOps.Api.Tests.csproj
dotnet sln KnowledgeOpsAI.sln add tests/KnowledgeOps.IntegrationTests/KnowledgeOps.IntegrationTests.csproj
```

### Step 8: Add Source Project References

```bash
# Application → Domain
dotnet add src/KnowledgeOps.Application/KnowledgeOps.Application.csproj \
  reference src/KnowledgeOps.Domain/KnowledgeOps.Domain.csproj

# Infrastructure → Application, Domain
dotnet add src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj \
  reference src/KnowledgeOps.Application/KnowledgeOps.Application.csproj
dotnet add src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj \
  reference src/KnowledgeOps.Domain/KnowledgeOps.Domain.csproj

# Api → Application, Infrastructure
dotnet add src/KnowledgeOps.Api/KnowledgeOps.Api.csproj \
  reference src/KnowledgeOps.Application/KnowledgeOps.Application.csproj
dotnet add src/KnowledgeOps.Api/KnowledgeOps.Api.csproj \
  reference src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj

# Worker → Application, Infrastructure
dotnet add src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj \
  reference src/KnowledgeOps.Application/KnowledgeOps.Application.csproj
dotnet add src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj \
  reference src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj
```

### Step 9: Add Test Project References

```bash
# Domain.Tests → Domain
dotnet add tests/KnowledgeOps.Domain.Tests/KnowledgeOps.Domain.Tests.csproj \
  reference src/KnowledgeOps.Domain/KnowledgeOps.Domain.csproj

# Application.Tests → Application, Domain
dotnet add tests/KnowledgeOps.Application.Tests/KnowledgeOps.Application.Tests.csproj \
  reference src/KnowledgeOps.Application/KnowledgeOps.Application.csproj
dotnet add tests/KnowledgeOps.Application.Tests/KnowledgeOps.Application.Tests.csproj \
  reference src/KnowledgeOps.Domain/KnowledgeOps.Domain.csproj

# Api.Tests → Api, Application, Domain
dotnet add tests/KnowledgeOps.Api.Tests/KnowledgeOps.Api.Tests.csproj \
  reference src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
dotnet add tests/KnowledgeOps.Api.Tests/KnowledgeOps.Api.Tests.csproj \
  reference src/KnowledgeOps.Application/KnowledgeOps.Application.csproj
dotnet add tests/KnowledgeOps.Api.Tests/KnowledgeOps.Api.Tests.csproj \
  reference src/KnowledgeOps.Domain/KnowledgeOps.Domain.csproj

# IntegrationTests → Api, Infrastructure, Application, Domain
dotnet add tests/KnowledgeOps.IntegrationTests/KnowledgeOps.IntegrationTests.csproj \
  reference src/KnowledgeOps.Api/KnowledgeOps.Api.csproj
dotnet add tests/KnowledgeOps.IntegrationTests/KnowledgeOps.IntegrationTests.csproj \
  reference src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj
dotnet add tests/KnowledgeOps.IntegrationTests/KnowledgeOps.IntegrationTests.csproj \
  reference src/KnowledgeOps.Application/KnowledgeOps.Application.csproj
dotnet add tests/KnowledgeOps.IntegrationTests/KnowledgeOps.IntegrationTests.csproj \
  reference src/KnowledgeOps.Domain/KnowledgeOps.Domain.csproj
```

### Step 10: Add Marker Classes

In each source project, add one internal marker class in the project's root namespace:

| File | Class |
| --- | --- |
| `src/KnowledgeOps.Domain/DomainMarker.cs` | `internal sealed class DomainMarker { }` |
| `src/KnowledgeOps.Application/ApplicationMarker.cs` | `internal sealed class ApplicationMarker { }` |
| `src/KnowledgeOps.Infrastructure/InfrastructureMarker.cs` | `internal sealed class InfrastructureMarker { }` |
| `src/KnowledgeOps.Api/ApiMarker.cs` | `internal sealed class ApiMarker { }` |
| `src/KnowledgeOps.Worker/WorkerMarker.cs` | `internal sealed class WorkerMarker { }` |

### Step 11: Add DI Placeholder Files

```csharp
// src/KnowledgeOps.Application/DependencyInjection.cs
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
```

```csharp
// src/KnowledgeOps.Infrastructure/DependencyInjection.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}
```

### Step 12: Add Minimal Host Files

**`src/KnowledgeOps.Api/Program.cs`** (replace template content):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
```

**`src/KnowledgeOps.Worker/Program.cs`** (replace template content):

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
```

### Step 13: Add Architecture Boundary Tests

Add boundary tests using the reflection pattern described in Section 8. Minimum tests for Issue #3 exit criteria:

- `KnowledgeOps.Domain.Tests/ArchitectureBoundaryTests.cs` — domain isolation tests
- `KnowledgeOps.Application.Tests/ArchitectureBoundaryTests.cs` — application isolation tests
- `KnowledgeOps.Api.Tests/ArchitectureBoundaryTests.cs` — API reference tests
- `KnowledgeOps.IntegrationTests/ArchitectureBoundaryTests.cs` — Infrastructure and Worker reference tests

---

## 13. Validation Plan For Implementation

Run these commands after implementation, from the repository root:

```bash
# 1. Confirm SDK
dotnet --info
dotnet --list-sdks

# 2. Restore
dotnet restore KnowledgeOpsAI.sln

# 3. Build (all projects)
dotnet build KnowledgeOpsAI.sln

# 4. Run tests (all test projects)
dotnet test KnowledgeOpsAI.sln

# 5. Inspect project references for each source project
dotnet list src/KnowledgeOps.Domain/KnowledgeOps.Domain.csproj reference
dotnet list src/KnowledgeOps.Application/KnowledgeOps.Application.csproj reference
dotnet list src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj reference
dotnet list src/KnowledgeOps.Api/KnowledgeOps.Api.csproj reference
dotnet list src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj reference

# 6. Confirm no uncommitted secrets or accidental files
git diff --check
git status
```

### Expected Validation Outcomes

| Command | Expected Result |
| --- | --- |
| `dotnet restore` | All packages restored; no forbidden packages installed |
| `dotnet build` | All 9 projects build successfully |
| `dotnet test` | All architecture boundary tests pass |
| `dotnet list ... reference` | Each project references only expected inward dependencies |
| `git diff --check` | No whitespace or encoding issues |
| `git status` | All new files staged cleanly; no accidental secrets or binaries |

### Known Limitation

`dotnet test` on newly created architecture boundary tests using reflection will require that the projects compile first. The tests are expected to pass at project creation because no forbidden packages or cross-layer references exist in the initial scaffold.

---

## 14. Risks And Blockers

### Blockers

| Blocker | Status |
| --- | --- |
| .NET 10 SDK unavailable | **Not a blocker.** SDK 10.0.204 is confirmed installed. |
| Conflicting solution or project structure | **Not a blocker.** Repository is a clean slate. |

**No blockers found.** Implementation can proceed.

### Risks

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Template-generated packages may include unexpected references | Low | Review `.csproj` files after generation; remove any non-allowed packages before committing. |
| `webapi` template may include weather forecast sample controller | Low | Delete or empty template-generated sample controllers before commit. |
| `Directory.Build.props` targeting `net10.0` may conflict with template-specified TFM in individual `.csproj` files | Low | Remove `<TargetFramework>` from individual `.csproj` files if `Directory.Build.props` is authoritative, or let both coexist (individual `.csproj` overrides). |
| `KnowledgeOps.E2ETests` omission | Very Low | Accepted decision for Issue #3. Guardrails list it as future structure. Must be created in Sprint 26 or a formally authorized issue. Note the deferred status in decisions log during implementation. |
| Architecture overview doc lists `KnowledgeOps.UnitTests` (different name) | Very Low | The canonical authority is `docs/22-implementation-guardrails.md`. Use granular test project names. Note the docs/11 name is illustrative, not prescriptive. |
| Marker classes are `internal` but test assemblies reference them as `InternalsVisibleTo` may be needed | Low | For reflection-based boundary tests, `typeof(DomainMarker)` works within the same assembly. Test projects reference the source assembly, so `typeof(DomainMarker)` is accessible if the class is in a namespace the test references. Use `[assembly: InternalsVisibleTo("KnowledgeOps.Domain.Tests")]` in `Domain` if needed. |

### Progress File Update Recommendation

The following updates are recommended for the **implementation prompt** (not the audit):

- `current-state.md`: Update to reflect Sprint 1 / Issue #3 as active implementation.
- `decisions-log.md`: Add entry for the `KnowledgeOps.E2ETests` deferral decision and the `KnowledgeOpsAI.sln` naming decision.
- `open-risks.md`: No new risks discovered that require a new entry. Existing risks remain unchanged.
- `completed-issues.md`: Update only after Issue #3 implementation is verified complete.

**open-risks.md is not updated during this audit** because no new blocker or materially new risk was discovered. The existing tracked risks continue to apply.

---

## 15. Readiness Recommendation

**READY FOR IMPLEMENTATION**

- .NET 10.0.204 SDK is confirmed installed.
- Repository state is a clean slate with no conflicting solution, project structure, or naming conventions.
- All canonical decisions are settled and documented.
- No blockers exist.
- No deferred scope risks affect Issue #3 readiness.

---

## 16. Recommended Next Step

Generate the Issue #3 implementation prompt using the following canonical sources:

1. This audit report as the pre-implementation checklist.
2. `docs/agents/10-issue-execution-template.md` as the implementation template.
3. `docs/agents/13-prompt-classifier.md` as the classification entry.
4. `docs/22-implementation-guardrails.md` and `docs/21-implementation-roadmap.md` as scope authorities.
5. The scaffold plan in Section 12 and validation plan in Section 13 as the implementation blueprint.

The implementation prompt should:
- Classify the task as Level 3 per the harness.
- Execute the scaffold commands in Section 12 in order.
- Remove template-generated sample controllers and content.
- Add marker classes and DI placeholders.
- Add minimal host `Program.cs` files.
- Add architecture boundary tests.
- Run validation commands in Section 13.
- Update `current-state.md`, `decisions-log.md`, and `completed-issues.md` after verified implementation.
- Not mark Issue #3 complete until `dotnet build` and `dotnet test` pass.
