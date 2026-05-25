# Issue #6 EF Core Persistence Pre-Implementation Audit

## 1. Purpose

This audit verifies repository, tooling, local SQL Server, and architecture readiness before implementing the EF Core SQL Server persistence foundation for KnowledgeOps-AI Sprint 4. It confirms the Infrastructure project state, package cleanliness, foundational schema plan aligned with canonical documentation, domain entity field recommendations, integration test strategy, and migration validation plan.

No source code, packages, project files, DbContext, entities, configurations, migrations, or database schema are created or modified by this audit.

---

## 2. Classification

```text
Classification
- Task type: Pre-implementation audit
- Prompt level: Level 3
- Related sprint/issue: Sprint 4 / Issue #6
- Scope: Audit-only / EF Core SQL Server persistence readiness
- Primary affected area: Infrastructure persistence, SQL Server schema
  foundation, migration strategy, relational integration testing
- Security or organization-scope impact: Foundational organization/user/role
  persistence only; organization_id must be present on all protected tables;
  no-secret rules must be preserved (no real connection strings committed)
- AI/RAG impact: None; must not create AI/RAG, document, chunk, embedding,
  retrieval, citation, feedback, dashboard, or knowledge_gap tables
- Data or migration impact: Audit only; no migration creation or schema changes

Reason
- Issue #6 introduces EF Core into Infrastructure, creates domain entities,
  configures Fluent API, creates the initial migration, and adds integration
  test harness — crossing Infrastructure, Domain, Application, and test layers.
  Secret-handling rules (no committed connection strings) and organization-scope
  boundary rules (ADR-010) must be respected. Level 3 is required.

Required Context
- Agent harness: 00-agent-operating-protocol.md, 10-issue-execution-template.md,
  12-prompt-levels.md, 13-prompt-classifier.md, 08-devops-context.md
- Canonical docs: docs/10-domain-model.md, docs/14-database-design.md,
  docs/16-security-and-permissions.md, docs/17-testing-strategy.md,
  docs/18-deployment-and-devops.md, docs/21-implementation-roadmap.md,
  docs/22-implementation-guardrails.md
- ADRs: ADR-001, ADR-002, ADR-005, ADR-010
- Progress files: current-state.md, decisions-log.md, open-risks.md,
  completed-issues.md
- Repository files: all .csproj files, Infrastructure folder contents,
  Domain folder contents, DependencyInjection.cs, appsettings files,
  docker-compose.yml, .env.example

Recommended Subagents
- architecture-auditor: Verify Infrastructure receives EF Core correctly;
  Domain/Application boundaries are not violated.
- database-agent: Confirm table, column, FK, and index conventions against
  docs/14-database-design.md.
- backend-implementation-agent: Implement entities, DbContext, configurations,
  DI registration, migration.
- testing-agent: Add integration test harness to KnowledgeOps.IntegrationTests.
- verification-agent: Final build/test/migration validation pass.
- No frontend or RAG subagent required.

Validation
- docker compose up -d sqlserver (SQL Server runtime)
- dotnet tool restore (local tool manifest, if added)
- dotnet ef migrations add InitialPersistenceFoundation ...
- dotnet ef database update ...
- dotnet build KnowledgeOpsAI.sln
- dotnet test KnowledgeOpsAI.sln

Escalation Or Blockers
- No blockers. One canonical-document discrepancy found (no separate `roles`
  table in docs/14-database-design.md) — documented in Section 17. Must be
  resolved during implementation by following the canonical database design.
```

---

## 3. Files And Context Reviewed

### Agent Harness Files

- `docs/agents/00-agent-operating-protocol.md`
- `docs/agents/10-issue-execution-template.md`
- `docs/agents/12-prompt-levels.md`
- `docs/agents/13-prompt-classifier.md`
- `docs/agents/07-backend-context.md`
- `docs/agents/08-devops-context.md`

### Progress Files

- `docs/agents/progress/current-state.md` — Sprint 3 / Issue #5 complete; next is Issue #6.
- `docs/agents/progress/decisions-log.md` — 21 entries; hybrid local mode, SQL Server image, env template, storage convention confirmed.
- `docs/agents/progress/open-risks.md` — 5 open risks; Sprint 3 disposition added; no new open risks.
- `docs/agents/progress/completed-issues.md` — Issues #2, #3, #4, #5 complete; Issue #6 not started.

### Canonical Docs Reviewed

- `docs/10-domain-model.md` — entity definitions, lifecycle states, constraints, relationships.
- `docs/14-database-design.md` — canonical table definitions, columns, indexes, FKs, enum values, MVP vs deferred tables.
- `docs/16-security-and-permissions.md` — roles, org scope enforcement rules.
- `docs/17-testing-strategy.md` — integration test approach.
- `docs/18-deployment-and-devops.md` — secrets strategy, Docker strategy.
- `docs/21-implementation-roadmap.md` — Sprint 4 scope (lines 227–240).
- `docs/22-implementation-guardrails.md` — canonical sources of truth, implementation rules.

### ADRs Reviewed

- `ADR-001-use-clean-architecture.md` — Domain/Application must not depend on Infrastructure.
- `ADR-002-use-sql-server.md` — SQL Server is the required database.
- `ADR-005-use-entity-framework-core.md` — EF Core in Infrastructure only; Fluent API.
- `ADR-010-use-organization-scoped-access-boundaries.md` — `organization_id` required on all protected tables.

### Repository Files / Folders Inspected

- All source `.csproj` files in `src/`
- All test `.csproj` files in `tests/`
- `src/KnowledgeOps.Infrastructure/DependencyInjection.cs`
- `src/KnowledgeOps.Infrastructure/` (root listing)
- `src/KnowledgeOps.Domain/` (root listing)
- `src/KnowledgeOps.Api/appsettings.json`, `appsettings.Development.json`
- `src/KnowledgeOps.Worker/appsettings.json`, `appsettings.Development.json`
- `docker-compose.yml` (Issue #5 artifact)
- `.env.example` (Issue #5 artifact)
- `.config/` directory (does not exist)
- Global and local dotnet tool list

---

## 4. Repository State

| Item | Finding |
| --- | --- |
| `KnowledgeOpsAI.sln` | Present. |
| `src/KnowledgeOps.Domain/` | Present. Contains only `AssemblyMarker.cs` and project file. No entities, no value objects. Clean slate. |
| `src/KnowledgeOps.Application/` | Present. Contains `AssemblyMarker.cs`, `DependencyInjection.cs`. No commands, queries, or use cases yet. |
| `src/KnowledgeOps.Infrastructure/` | Present. Contains `AssemblyMarker.cs`, `DependencyInjection.cs` (stub returning empty services). No `Persistence/` folder. No entities. Clean slate. |
| `src/KnowledgeOps.Api/` | Present. Minimal host. `appsettings.json` has no `ConnectionStrings` section. |
| `src/KnowledgeOps.Worker/` | Present. Minimal host. `appsettings.json` has no `ConnectionStrings` section. |
| `tests/KnowledgeOps.Domain.Tests/` | Present. xUnit. References Domain only. |
| `tests/KnowledgeOps.Application.Tests/` | Present. xUnit. References Application + Domain. |
| `tests/KnowledgeOps.Api.Tests/` | Present. xUnit. References Api + Application + Domain. |
| `tests/KnowledgeOps.IntegrationTests/` | Present. xUnit. References Api + Infrastructure + Application + Domain. Ready for persistence tests. |
| `Infrastructure/Persistence/` | Does not exist. |
| Existing migrations | None. |
| Existing EF Core config | None in appsettings. |
| `docker-compose.yml` | Present (Issue #5). SQL Server service configured. |
| `.env.example` | Present (Issue #5). Includes `ConnectionStrings__DefaultConnection` as a commented forward reference. |
| `.config/dotnet-tools.json` | Does not exist. No local tool manifest. |

---

## 5. Package And Dependency State

| Project | EF Core | SQL Server Provider | EF Design | EF Tools | Other Packages |
| --- | --- | --- | --- | --- | --- |
| `KnowledgeOps.Domain` | Absent ✓ | Absent ✓ | Absent ✓ | Absent ✓ | None |
| `KnowledgeOps.Application` | Absent ✓ | Absent ✓ | Absent ✓ | Absent ✓ | `Microsoft.Extensions.DependencyInjection.Abstractions` only |
| `KnowledgeOps.Infrastructure` | Absent (to be added) | Absent (to be added) | Absent (to be added) | — | `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `KnowledgeOps.Api` | Absent ✓ | Absent ✓ | Absent ✓ | — | None direct |
| `KnowledgeOps.Worker` | Absent ✓ | Absent ✓ | Absent ✓ | — | `Microsoft.Extensions.Hosting` only |
| `KnowledgeOps.IntegrationTests` | Absent (may add EF Core ref) | Absent (may add) | — | — | xUnit, coverlet |

**Package state is clean.** No accidental EF Core or SQL Server provider references exist in Domain or Application. Domain is dependency-free. Application depends only on abstractions. Infrastructure is ready to receive EF Core packages.

**Recommended EF Core packages for Issue #6** (all in `KnowledgeOps.Infrastructure` only):

| Package | Version | Purpose |
| --- | --- | --- |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.8` | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.8` | Design-time tools (migrations) |

`Microsoft.EntityFrameworkCore.Design` must be added with `<PrivateAssets>all</PrivateAssets>` to prevent it leaking as a transitive dependency.

`KnowledgeOps.IntegrationTests` may add `Microsoft.EntityFrameworkCore.SqlServer` as a test-only reference for schema verification.

**Do not add** EF Core packages to Domain, Application, Api.Tests, or Worker unless the architecture boundary tests actively prevent it.

---

## 6. SQL Server Local Runtime Readiness

| Item | Finding |
| --- | --- |
| `docker-compose.yml` | Present. `sqlserver` service configured with `mcr.microsoft.com/mssql/server:2022-latest`. |
| SQL Server port | `${KNOWLEDGEOPS_SQL_PORT:-1433}:1433` — default 1433. |
| `MSSQL_SA_PASSWORD` | Reads from `${KNOWLEDGEOPS_SQL_PASSWORD}` — requires `.env`. |
| `.env.example` | Present. `ConnectionStrings__DefaultConnection` appears as a commented line: `Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=Change_this_local_password_123!;TrustServerCertificate=True;Encrypt=True`. |
| `.env` coverage | `.env` is gitignored (confirmed in Issue #5). |
| `ConnectionStrings__DefaultConnection` convention | Confirmed: this is the expected environment-variable name for .NET configuration binding. `configuration.GetConnectionString("DefaultConnection")` maps to this env var. |
| `appsettings.json` (API and Worker) | No `ConnectionStrings` section exists yet. Will need to be added during implementation with a blank/placeholder value or development comment. No real connection string may be committed. |
| Docker availability | Docker 29.4.3 confirmed available (Issue #5 validation). SQL Server 2022 container confirmed starting successfully. |
| Readiness status | **READY** — local SQL Server runtime from Issue #5 is fully functional. |

**Implementation note:** During implementation, `appsettings.Development.json` for API and Worker may include a `ConnectionStrings` section with a comment or empty placeholder, but the actual connection string must come from the developer's local `.env` file via environment variable injection.

---

## 7. EF Tooling Readiness

| Item | Finding |
| --- | --- |
| Global `dotnet-ef` | `dotnet-ef 10.0.8` installed globally (`dotnet tool list -g`). |
| Local tool manifest | Does not exist. No `.config/dotnet-tools.json`. |
| Local tool manifest recommendation | **Create during implementation.** A local manifest ensures reproducibility for other contributors who may not have the global tool. |
| Recommended local tool entry | `dotnet-ef` version `10.0.8` — matches the globally available version. |
| Version alignment | `dotnet-ef 10.0.8` aligns with `Microsoft.EntityFrameworkCore.SqlServer 10.0.8` and .NET SDK `10.0.204`. No version mismatch. |
| `dotnet ef --version` | Would report `10.0.8` when invoked via the global install. |

**Recommended commands during implementation:**

```bash
# Create local tool manifest
dotnet new tool-manifest

# Install dotnet-ef as a local tool
dotnet tool install dotnet-ef --version 10.0.8

# Restore local tools before first use by a new contributor
dotnet tool restore
```

The global tool is sufficient for the initial implementation, but the local manifest should be committed so that future CI and contributors have a pinned, reproducible toolchain.

---

## 8. Foundational Data Model Decision

### Approved Tables For Issue #6

| Table | Rationale |
| --- | --- |
| `organizations` | Foundation for all organization-scoped access boundaries (ADR-010). All other tables depend on it. |
| `users` | Foundation for authentication, role assignment, document ownership, and audit traceability. |
| `user_roles` | Maps roles to users. Role names stored as string enum per canonical docs (see Section 17 discrepancy note). |
| `audit_log_entries` | Foundation for traceability; does not depend on feature tables. Required by multiple business rules. |
| `__EFMigrationsHistory` | Automatically created by EF Core during migration. |

### Deferred Tables — Must NOT Be Created In Issue #6

| Table | Deferred To |
| --- | --- |
| `documents` | Sprints 10–11 |
| `document_chunks` | Sprints 10–11 |
| `chunk_embeddings` | Sprint introducing embedding workflow |
| `chat_sessions` | Sprint introducing chat workflow |
| `chat_interactions` | Sprint introducing chat workflow |
| `retrieval_results` | Sprint introducing retrieval workflow |
| `citations` | Sprint introducing citation workflow |
| `answer_feedback` | Sprint introducing feedback workflow |
| `dashboard_metric_snapshots` | Deferred; may use dynamic computation |
| `knowledge_gap_signals` | Phase 2 |

### Key Discrepancy With Issue #6 Task Specification

The Issue #6 accepted decisions list a `roles` table as a foundational entity. **The canonical `docs/14-database-design.md` does not contain a separate `roles` table.** Per the canonical design, roles are stored as a `role_name` string column in `user_roles` (Section 5.3). There is no `roles` table in the canonical Section 15.1 MVP table list.

**Recommendation:** Do not create a separate `roles` table in Issue #6. Follow the canonical database design: store `role_name` as a string (nvarchar enum) in `user_roles`. If a separate roles table is needed in the future, it requires an explicit architecture decision.

---

## 9. Entity Field Plan

Fields are derived from `docs/14-database-design.md` Section 5 (canonical). C# property names use PascalCase; database column names use snake_case configured via Fluent API.

### Organization

```csharp
Guid Id                        // → organization_id (PK)
string Name                    // → name, nvarchar(200), required
string Status                  // → status, nvarchar(50): Active | Disabled
DateTimeOffset CreatedAt       // → created_at, datetime2, required
DateTimeOffset UpdatedAt       // → updated_at, datetime2, required
```

### User

```csharp
Guid Id                        // → user_id (PK)
Guid OrganizationId            // → organization_id (FK → organizations)
string DisplayName             // → display_name, nvarchar(200), required
string Email                   // → email, nvarchar(320), required
string? PasswordHash           // → password_hash, nvarchar(max), nullable (deferred to Sprint 6 auth)
string Status                  // → status, nvarchar(50): Pending | Active | Disabled
DateTimeOffset? LastLoginAt    // → last_login_at, datetime2, nullable
DateTimeOffset CreatedAt       // → created_at, datetime2, required
DateTimeOffset UpdatedAt       // → updated_at, datetime2, required
DateTimeOffset? DeletedAt      // → deleted_at, datetime2, nullable (soft delete)
```

### UserRole

```csharp
Guid Id                        // → user_role_id (PK — surrogate per canonical design)
Guid UserId                    // → user_id (FK → users)
string RoleName                // → role_name, nvarchar(50): Agent | Supervisor | KnowledgeAdmin | Manager | Admin
DateTimeOffset AssignedAt      // → assigned_at, datetime2, required
Guid? AssignedByUserId         // → assigned_by_user_id (FK → users, nullable)
```

**Note:** No `OrganizationId` column in `user_roles` per the canonical design. Organization scope for a user's roles is derived through the `users.organization_id` join. No separate `RoleId` FK because there is no `roles` table.

### AuditLogEntry

```csharp
Guid Id                        // → audit_log_entry_id (PK)
Guid? OrganizationId           // → organization_id, nullable (FK → organizations)
Guid? UserId                   // → user_id, nullable (FK → users)
string EventType               // → event_type, nvarchar(150), required
string? EntityType             // → entity_type, nvarchar(150), nullable
Guid? EntityId                 // → entity_id, uniqueidentifier, nullable
string Message                 // → message, nvarchar(1000), required
string Severity                // → severity, nvarchar(50): Info | Warning | Error | Critical
string? CorrelationId          // → correlation_id, nvarchar(100), nullable
DateTimeOffset CreatedAt       // → created_at, datetime2, required
```

**Note on audit FK design:** Per `docs/14-database-design.md`, `organization_id` and `user_id` are nullable foreign keys for audit entries and should be mapped without delete cascade. Section 8.2 applies the "avoid complex polymorphic FK design" guidance to `audit_log_entries.entity_id`, which remains a nullable reference without an enforced FK.

---

## 10. Database Naming, Constraints, And Index Plan

All names from `docs/14-database-design.md`. C# properties map via Fluent API — no snake_case convention package.

### organizations

| Column | Type | Constraint |
| --- | --- | --- |
| `organization_id` | `uniqueidentifier` | PK |
| `name` | `nvarchar(200)` | NOT NULL |
| `status` | `nvarchar(50)` | NOT NULL |
| `created_at` | `datetime2` | NOT NULL |
| `updated_at` | `datetime2` | NOT NULL |

Indexes: `IX_organizations_name (name)`, `IX_organizations_status (status)`

### users

| Column | Type | Constraint |
| --- | --- | --- |
| `user_id` | `uniqueidentifier` | PK |
| `organization_id` | `uniqueidentifier` | FK → organizations.organization_id NOT NULL |
| `display_name` | `nvarchar(200)` | NOT NULL |
| `email` | `nvarchar(320)` | NOT NULL |
| `password_hash` | `nvarchar(max)` | NULL |
| `status` | `nvarchar(50)` | NOT NULL |
| `last_login_at` | `datetime2` | NULL |
| `created_at` | `datetime2` | NOT NULL |
| `updated_at` | `datetime2` | NOT NULL |
| `deleted_at` | `datetime2` | NULL |

Constraints: `UX_users_email (email)` unique  
Indexes: `IX_users_organization_id (organization_id)`, `IX_users_status (status)`, `IX_users_deleted_at (deleted_at)`

### user_roles

| Column | Type | Constraint |
| --- | --- | --- |
| `user_role_id` | `uniqueidentifier` | PK |
| `user_id` | `uniqueidentifier` | FK → users.user_id NOT NULL |
| `role_name` | `nvarchar(50)` | NOT NULL |
| `assigned_at` | `datetime2` | NOT NULL |
| `assigned_by_user_id` | `uniqueidentifier` | FK → users.user_id NULL |

Constraints: `UX_user_roles_user_role (user_id, role_name)` unique — prevents duplicate role assignment  
Indexes: `IX_user_roles_user_id (user_id)`, `IX_user_roles_role_name (role_name)`

### audit_log_entries

| Column | Type | Constraint |
| --- | --- | --- |
| `audit_log_entry_id` | `uniqueidentifier` | PK |
| `organization_id` | `uniqueidentifier` | FK → organizations.organization_id NULL |
| `user_id` | `uniqueidentifier` | FK → users.user_id NULL |
| `event_type` | `nvarchar(150)` | NOT NULL |
| `entity_type` | `nvarchar(150)` | NULL |
| `entity_id` | `uniqueidentifier` | NULL |
| `message` | `nvarchar(1000)` | NOT NULL |
| `severity` | `nvarchar(50)` | NOT NULL |
| `correlation_id` | `nvarchar(100)` | NULL |
| `created_at` | `datetime2` | NOT NULL |

Indexes: `IX_audit_log_entries_organization_id (organization_id)`, `IX_audit_log_entries_user_id (user_id)`, `IX_audit_log_entries_event_type (event_type)`, `IX_audit_log_entries_created_at (created_at)`, `IX_audit_log_entries_correlation_id (correlation_id)`

### Primary Key Generation Strategy

Use `Guid.NewGuid()` in the entity constructor (client-side generation). This avoids database round-trips and supports future distributed contexts. Configure via `HasDefaultValueSql("NEWID()")` as fallback in Fluent API only if explicit client-side assignment is not used consistently.

---

## 11. Application Abstraction Decision

**Recommendation: Do not add repository abstractions or `IApplicationDbContext` in Issue #6.**

Rationale:
- No Application use cases exist yet (no commands, no queries).
- Adding an abstraction before a concrete consumer exists violates the YAGNI principle.
- A premature `IKnowledgeOpsDbContext` would expose `DbSet` and risk coupling Application to EF Core types.
- The canonical `docs/22-implementation-guardrails.md` prohibits expanding scope beyond what the task requires.
- ADR-005 states EF Core details remain in Infrastructure; this is fully satisfied without an Application abstraction in this sprint.

**Future pattern (when first use case is implemented):**
When Application needs to persist data, introduce a minimal abstraction (e.g., `IOrganizationRepository`) that exposes only domain-level return types, not `DbSet` or `IQueryable`. This will be the correct time to add the abstraction.

---

## 12. DbContext, Configuration, Migration, And Factory Plan

### Planned Structure

```
src/KnowledgeOps.Infrastructure/
  Persistence/
    KnowledgeOpsDbContext.cs
    KnowledgeOpsDbContextFactory.cs
    Configurations/
      OrganizationConfiguration.cs
      UserConfiguration.cs
      UserRoleConfiguration.cs
      AuditLogEntryConfiguration.cs
    Migrations/
      [generated by dotnet ef]
```

### KnowledgeOpsDbContext

- Inherits from `DbContext`.
- Exposes `DbSet<Organization>`, `DbSet<User>`, `DbSet<UserRole>`, `DbSet<AuditLogEntry>`.
- Applies all entity configurations via `modelBuilder.ApplyConfigurationsFromAssembly(...)` in `OnModelCreating`.
- Does **not** call `Database.Migrate()` on startup.

### Entity Configurations (Fluent API)

Each entity has a dedicated `IEntityTypeConfiguration<T>` class in `Configurations/`. Rules:
- All column names explicitly mapped to snake_case.
- All string lengths explicitly set.
- All FKs explicitly configured.
- All indexes explicitly configured.
- No EF Core attributes on domain entity classes.

### KnowledgeOpsDbContextFactory

- Implements `IDesignTimeDbContextFactory<KnowledgeOpsDbContext>`.
- Reads `ConnectionStrings__DefaultConnection` from environment variables using `IConfigurationRoot` built with `AddEnvironmentVariables()`.
- Throws a clear `InvalidOperationException` if the connection string is missing or empty.
- No hardcoded connection strings.
- Used by `dotnet ef` CLI for migration operations.

### Migration Location

`--output-dir Persistence/Migrations` places generated migrations inside the Infrastructure project's Persistence folder.

### Startup Behavior

- No `Database.Migrate()` call in `Program.cs` or `DependencyInjection.cs`.
- Migrations are applied manually via `dotnet ef database update` during local development.
- Rationale: Auto-migration on startup is unsafe in production-adjacent environments and defeats the purpose of controlled migration review.

---

## 13. Infrastructure DI Plan

Update `src/KnowledgeOps.Infrastructure/DependencyInjection.cs`:

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<KnowledgeOpsDbContext>(options =>
        options.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.MigrationsAssembly(
                typeof(KnowledgeOpsDbContext).Assembly.FullName)));

    return services;
}
```

Rules:
- Only `KnowledgeOpsDbContext` is registered here for Issue #6.
- No repository registrations until use cases require them.
- No migration-on-startup logic.
- No business workflow registrations.
- Connection string is read from `IConfiguration` (environment variable or user secrets, never from a committed file).
- `KnowledgeOps.Api/Program.cs` and `KnowledgeOps.Worker/Program.cs` already call `AddInfrastructure(configuration)` — confirm during implementation, add call if absent.

---

## 14. Integration Test Strategy

### Existing Test Projects

`KnowledgeOps.IntegrationTests` already references Infrastructure, Application, Api, and Domain. It is the correct home for persistence integration tests.

### Approach

1. **Keep existing unit/boundary tests unchanged.** `Domain.Tests`, `Application.Tests`, and `Api.Tests` must not depend on SQL Server and must remain fast and deterministic.

2. **Add persistence integration test harness to `KnowledgeOps.IntegrationTests`.** This project may add `Microsoft.EntityFrameworkCore.SqlServer` if not already inherited transitively.

3. **Connection string handling in tests:**
   - Read `ConnectionStrings__DefaultConnection` from environment variable.
   - If connection string is absent or empty, skip with `Skip` attribute and a clear reason: `"Skipped: local SQL Server not configured. Set ConnectionStrings__DefaultConnection to run persistence tests."`
   - Do not fail the build if SQL Server is unavailable — this allows CI to pass without a running SQL Server until a SQL Server-enabled CI pipeline is established.

4. **Test approach for Issue #6:**
   - Verify that migrations can be applied against a real SQL Server schema.
   - Verify that all four entity types can be round-tripped (insert → read back).
   - Verify that unique constraints are enforced (duplicate email in users, duplicate role assignment in user_roles).
   - Verify that FK constraints are enforced (user with invalid organization_id is rejected).
   - No Testcontainers — use local SQL Server from Docker Compose.

5. **Do not seed demo data** in tests. Use test-specific unique values (e.g., `Guid.NewGuid()` for org names, randomized emails).

6. **Cleanup strategy:** Either use a dedicated test database (`KnowledgeOpsTest`) or truncate tables in `Dispose`. Prefer a separate test database to avoid interfering with the local development database.

---

## 15. Migration Validation Plan

Run these commands during implementation to validate the migration and schema:

```bash
# Step 1: Ensure SQL Server is running
docker compose up -d sqlserver

# Step 2: Set connection string (from local .env)
# On Windows PowerShell:
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeOpsLocal;User Id=sa;Password=<your_local_password>;TrustServerCertificate=True;Encrypt=True"

# Step 3: Create the local tool manifest (if not already present)
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.8
dotnet tool restore

# Step 4: Add Infrastructure EF Core packages
dotnet add src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.8
dotnet add src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.8

# Step 5: Create migration
dotnet ef migrations add InitialPersistenceFoundation \
  --project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj \
  --startup-project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj \
  --output-dir Persistence/Migrations

# Step 6: Apply migration
dotnet ef database update \
  --project src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj \
  --startup-project src/KnowledgeOps.Api/KnowledgeOps.Api.csproj

# Step 7: Build solution
dotnet build KnowledgeOpsAI.sln

# Step 8: Run all tests
dotnet test KnowledgeOpsAI.sln

# Step 9: Confirm repo safety
git status
git diff --check
```

**Validation expectations:**
- `dotnet ef migrations add` succeeds and generates `Persistence/Migrations/[timestamp]_InitialPersistenceFoundation.cs`.
- `dotnet ef database update` creates tables: `organizations`, `users`, `user_roles`, `audit_log_entries`, `__EFMigrationsHistory`.
- `dotnet build` passes (0 errors, 0 warnings if possible).
- `dotnet test` passes — all existing boundary/unit tests pass; integration tests run if SQL Server is reachable or skip gracefully.
- `git status` shows no `.env`, no committed connection strings with real passwords.
- `git diff --check` passes with no whitespace errors.

---

## 16. Out Of Scope

| Item | Status |
| --- | --- |
| `documents`, `document_chunks`, `chunk_embeddings` tables | Deferred to Sprints 10–11 |
| `chat_sessions`, `chat_interactions` tables | Deferred to chat sprint |
| `retrieval_results`, `citations` tables | Deferred to retrieval sprint |
| `answer_feedback` table | Deferred to feedback sprint |
| `dashboard_metric_snapshots` table | May compute dynamically; deferred |
| `knowledge_gap_signals` table | Phase 2 |
| Separate `roles` table | Not in canonical design; do not create |
| Seed data (demo users, orgs, roles) | Deferred; not in Issue #6 |
| Authentication / JWT | Sprint 6 |
| Authorization policies | Sprint 6+ |
| Admin user management UI/API | Deferred |
| Document upload workflow | Sprints 10–11 |
| Document processing / Worker implementation | Deferred |
| Retrieval / embeddings / RAG chat | Deferred |
| Feedback | Deferred |
| Dashboard metrics | Deferred |
| Production database deployment | Out of scope |
| GitHub Actions CI with SQL Server | Deferred |
| Azure deployment | Out of scope |
| Live AI provider integration | Never required for local |
| Frontend changes | None |
| Docker Compose changes | None |
| Rendered diagram PNGs | Never |

---

## 17. Risks And Blockers

| Risk | Severity | Mitigation | open-risks.md update? |
| --- | --- | --- | --- |
| **Canonical doc discrepancy — no `roles` table**: The Issue #6 accepted decisions list a separate `roles` table, but `docs/14-database-design.md` has no such table. Implementing a `roles` table would diverge from the canonical schema. | Medium | **Follow canonical docs.** Do not create a `roles` table. Roles are string enum values in `user_roles.role_name`. Note this decision in `decisions-log.md` during implementation. | No — this is a discrepancy resolved by following canonical docs, not an open risk. |
| **No local tool manifest**: `dotnet-ef` is only globally installed. CI or new contributors without the global tool cannot run migrations. | Low | Create `.config/dotnet-tools.json` during implementation with `dotnet-ef 10.0.8`. | No — blocked only if someone lacks the global tool; implementation resolves it. |
| **No `ConnectionStrings` in appsettings**: API and Worker appsettings have no connection string section. | Low | The design-time factory reads from env vars only. Runtime DI reads from `IConfiguration` which includes env vars. No committed connection string needed. Add a commented-out placeholder in `appsettings.Development.json` for developer guidance. | No. |
| **Integration tests skipped without SQL Server in CI**: Tests that require SQL Server will skip when not available. | Low | Acceptable for Issue #6. A future sprint can add SQL Server to CI pipeline. | No — this is an accepted limitation. |
| **EF Core version lock**: Using `10.0.8` aligns with currently installed tooling. If EF Core releases a patch (e.g., `10.0.9`), a manual version bump may be needed. | Very Low | Pin to `10.0.8` initially; update intentionally. | No. |
| **No blockers for implementation.** | — | — | — |

**No updates to `docs/agents/progress/open-risks.md` are required at audit time.** The discrepancy above is resolved by following canonical docs. All other items are documentation cautions addressed by the implementation plan.

---

## 18. Readiness Recommendation

**READY FOR IMPLEMENTATION**

All prerequisites are satisfied:
- Repository is a clean slate — no EF Core packages, no entities, no migrations, no accidental boundary violations.
- Domain, Application, Infrastructure package state is clean and boundary-correct.
- Local SQL Server runtime (Issue #5) is functional and validated.
- `dotnet-ef 10.0.8` global tool is available and version-aligned with EF Core 10.0.x.
- `docker-compose.yml` and `.env.example` are in place and support `ConnectionStrings__DefaultConnection`.
- `KnowledgeOpsIntegrationTests` project already references Infrastructure — ready for persistence tests.
- Canonical database design in `docs/14-database-design.md` provides exact table, column, constraint, and index definitions.
- One canonical discrepancy identified (no separate `roles` table) — resolved by following canonical docs; no blocker.

---

## 19. Recommended Next Step

Generate the implementation prompt for Issue #6 (`feat: add EF Core SQL Server persistence foundation`) using `docs/agents/10-issue-execution-template.md`.

The implementation should follow:
- Section 8 (approved tables)
- Section 9 (entity field plan, including the adjusted `user_roles` design without a separate roles table)
- Section 10 (naming, constraints, indexes)
- Section 12 (DbContext, factory, configuration, migration structure)
- Section 13 (DI registration)
- Section 14 (integration test strategy)
- Section 15 (validation commands)

The implementation agent must **not** create a separate `roles` table and must record this decision in `decisions-log.md`.
