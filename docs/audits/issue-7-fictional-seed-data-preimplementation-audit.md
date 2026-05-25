# Issue #7 Fictional Seed Data Pre-Implementation Audit

## 1. Purpose

This audit verifies repository, persistence model, EF Core configuration, seed strategy, test
compatibility, and safety readiness before adding deterministic fictional seed data for
organizations and MVP-role user personas in KnowledgeOps-AI Sprint 5.

No source code, seed data, migrations, EF configurations, Docker files, or database schema are
created or modified by this audit.

---

## 2. Classification

```text
Classification
- Task type: Pre-implementation audit
- Prompt level: Level 3
- Related sprint/issue: Sprint 5 / Issue #7
- Scope: Audit-only / Fictional deterministic seed data readiness
- Primary affected area: EF Core seed data via HasData, Infrastructure SeedData
  folder, organization and user persona foundation, integration tests, demo-data
  documentation
- Security or organization-scope impact: Fictional users and organization
  boundaries only; must preserve the five-role MVP model; no passwords, no
  auth credentials, no real data; no-secret rules enforced
- AI/RAG impact: None; must not create document, chunk, embedding, retrieval,
  citation, RAG, feedback, dashboard, or knowledge-gap seed records
- Data or migration impact: Audit only; no seed migration creation or database
  changes in this audit

Reason
- Issue #7 adds seed data spanning Infrastructure persistence (HasData /
  migration), domain entity creation, integration test updates, and
  documentation. The seed migration will affect existing integration test
  assumptions. Organization-scope boundary, role-only MVP model, and
  no-password rules must be verified before implementation. Level 3 is
  required.

Required Context
- Agent harness: 00-agent-operating-protocol.md, 10-issue-execution-template.md,
  12-prompt-levels.md, 13-prompt-classifier.md
- Progress files: current-state.md, decisions-log.md, open-risks.md,
  completed-issues.md
- Prior audit: docs/audits/issue-6-ef-core-persistence-preimplementation-audit.md
- Canonical docs: docs/14-database-design.md, docs/16-security-and-permissions.md,
  docs/21-implementation-roadmap.md
- Repository files: all Domain entities, Infrastructure Persistence folder,
  Configurations, Migrations, IntegrationTests, docker-compose.yml, .env.example

Recommended Subagents
- architecture-auditor: Confirm seed data belongs in Infrastructure only;
  Domain/Application layers must not reference seed constants.
- database-agent: Verify seed field values match entity configurations and
  canonical constraints (check constraints, FK rules, unique indexes).
- backend-implementation-agent: Create SeedDataIds.cs, KnowledgeOpsSeedData.cs,
  HasData calls, new seed migration.
- testing-agent: Add SeedDataTests.cs; update SqlServerPersistenceTests.cs for
  seed-data compatibility.
- verification-agent: Final build/test/migration validation pass.

Validation
- dotnet tool restore
- dotnet build KnowledgeOpsAI.sln
- docker compose up -d sqlserver
- dotnet ef database update ... (new seed migration)
- dotnet test KnowledgeOpsAI.sln
- ConnectionStrings__DefaultConnection set: SQL Server-backed seed validation

Escalation Or Blockers
- No blockers. One required implementation detail flagged: existing
  SqlServerPersistenceTests.cs SingleAsync() assertions will fail after seed data
  is applied. Must be updated during implementation (see Section 14).
```

---

## 3. Files And Context Reviewed

### Agent Harness Files

- `docs/agents/00-agent-operating-protocol.md`
- `docs/agents/10-issue-execution-template.md`
- `docs/agents/12-prompt-levels.md`
- `docs/agents/13-prompt-classifier.md`

### Progress Files

- `docs/agents/progress/current-state.md` — Sprint 4 / Issue #6 complete; PR
  pending.
- `docs/agents/progress/decisions-log.md` — EF Core in Infrastructure only;
  no roles table; `InitialPersistenceFoundation` migration applied.
- `docs/agents/progress/open-risks.md` — No Sprint 4 / Issue #6 open risks;
  existing open risks remain applicable to future sprints.
- `docs/agents/progress/completed-issues.md` — Issue #6 implementation
  verified and pending PR.

### Prior Audit

- `docs/audits/issue-6-ef-core-persistence-preimplementation-audit.md`

### Canonical Documentation

- `docs/14-database-design.md` — Section 5.1–5.3 (organizations, users,
  user_roles); Section 13 (enumerations); Section 15 (MVP tables).
- `docs/16-security-and-permissions.md` — Sections 3, 5 (authentication
  strategy; role definitions).
- `docs/21-implementation-roadmap.md` — Sprint 5 scope and exit criteria.

### Repository Source Files Inspected

- `src/KnowledgeOps.Domain/Organizations/Organization.cs`
- `src/KnowledgeOps.Domain/Organizations/OrganizationStatus.cs`
- `src/KnowledgeOps.Domain/Users/User.cs`
- `src/KnowledgeOps.Domain/Users/UserRole.cs`
- `src/KnowledgeOps.Domain/Users/UserRoleName.cs`
- `src/KnowledgeOps.Domain/Users/UserStatus.cs`
- `src/KnowledgeOps.Domain/Audit/AuditLogEntry.cs`
- `src/KnowledgeOps.Domain/Audit/AuditSeverity.cs`
- `src/KnowledgeOps.Infrastructure/KnowledgeOps.Infrastructure.csproj`
- `src/KnowledgeOps.Infrastructure/DependencyInjection.cs`
- `src/KnowledgeOps.Infrastructure/Persistence/KnowledgeOpsDbContext.cs`
- `src/KnowledgeOps.Infrastructure/Persistence/KnowledgeOpsDbContextFactory.cs`
- `src/KnowledgeOps.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs`
- `src/KnowledgeOps.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `src/KnowledgeOps.Infrastructure/Persistence/Configurations/UserRoleConfiguration.cs`
- `src/KnowledgeOps.Infrastructure/Persistence/Configurations/UtcDateTimeOffsetConverter.cs`
- `src/KnowledgeOps.Infrastructure/Persistence/Migrations/20260525175722_InitialPersistenceFoundation.cs`
- `src/KnowledgeOps.Infrastructure/Persistence/Migrations/KnowledgeOpsDbContextModelSnapshot.cs`
- `tests/KnowledgeOps.IntegrationTests/PersistenceModelTests.cs`
- `tests/KnowledgeOps.IntegrationTests/SqlServerPersistenceTests.cs`
- `tests/KnowledgeOps.IntegrationTests/SqlServerFactAttribute.cs`
- `.config/dotnet-tools.json`
- `docker-compose.yml`
- `.env.example`

---

## 4. Persistence Model State

### 4.1 Foundational Entities

All four approved foundational entities from Issue #6 are confirmed present.

| Entity | C# Class | Table | Confirmed |
|---|---|---|---|
| Organization | `Organization` | `organizations` | Yes |
| User | `User` | `users` | Yes |
| User Role | `UserRole` | `user_roles` | Yes |
| Audit Log Entry | `AuditLogEntry` | `audit_log_entries` | Yes |

### 4.2 User Role Representation

`UserRole` uses a **surrogate GUID primary key** (`Id` / `user_role_id`). It
is NOT a composite key. Confirmed from `UserRoleConfiguration`:

```csharp
builder.HasKey(userRole => userRole.Id).HasName("PK_user_roles");
builder.Property(userRole => userRole.Id).HasColumnName("user_role_id");
```

**Impact on seed data**: Deterministic GUIDs are required for all seven
`UserRole` seed records.

### 4.3 Role Name Representation

`UserRoleName` is a C# `enum` stored as a `nvarchar(50)` string via
`HasConversion<string>()`. The five MVP values are enforced by a database
check constraint:

```sql
CK_user_roles_role_name:
[role_name] IN (N'Agent', N'Supervisor', N'KnowledgeAdmin', N'Manager', N'Admin')
```

### 4.4 No Separate Roles Table

Confirmed: there is no `Role` entity, no `roles` table, and no separate role
entity class anywhere in the solution. Roles are stored only as `role_name`
string values in `user_roles`. This matches `docs/14-database-design.md` and
the `decisions-log.md` entry from 2026-05-25:

> Issue #6 creates only organizations, users, user_roles, and audit_log_entries;
> role assignments store the five MVP role_name values and no roles table exists.

### 4.5 Organization Scope Derivation

`User` carries `OrganizationId` directly. `UserRole` does NOT have its own
`OrganizationId` — organization scope for a role is derived through the
`user_id → users.organization_id` relationship. This is correct per canonical
design and means seed role records do not need a separate `OrganizationId`
field.

### 4.6 Required Fields For Seed Entities

| Entity | Required fields | Nullable fields safe to omit |
|---|---|---|
| `Organization` | Id, Name, Status, CreatedAt, UpdatedAt | None |
| `User` | Id, OrganizationId, DisplayName, Email, Status, CreatedAt, UpdatedAt | PasswordHash, LastLoginAt, DeletedAt |
| `UserRole` | Id, UserId, RoleName, AssignedAt | AssignedByUserId |
| `AuditLogEntry` | Not seeded — see Section 9.4 | — |

### 4.7 Password Hash Field

`User.PasswordHash` is `string?` (nullable) in the domain entity and mapped
as `nvarchar(max) nullable` in the database. It may be left `null` for all
seed records. No password storage is needed in Sprint 5.

---

## 5. EF Configuration And Migration State

### 5.1 DbContext DbSets

```csharp
public DbSet<Organization> Organizations => Set<Organization>();
public DbSet<User> Users => Set<User>();
public DbSet<UserRole> UserRoles => Set<UserRole>();
public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
```

`OnModelCreating` uses `ApplyConfigurationsFromAssembly(...)` which auto-discovers
all `IEntityTypeConfiguration<T>` implementations in the Infrastructure assembly.

### 5.2 Existing Configurations (Schema Only)

| Configuration class | Entity | HasData used | Notes |
|---|---|---|---|
| `OrganizationConfiguration` | `Organization` | No | Schema + constraints only |
| `UserConfiguration` | `User` | No | Schema + constraints only |
| `UserRoleConfiguration` | `UserRole` | No | Schema + constraints only |
| `AuditLogEntryConfiguration` | `AuditLogEntry` | No | Schema + constraints only |

**`HasData` is not currently used anywhere in the model.** Adding it will
create a clean, isolated seed migration.

### 5.3 Value Converter Behavior With HasData

The `UtcDateTimeOffsetConverter` converts `DateTimeOffset` (C#) → `DateTime`
(database). When `HasData` is used with a value converter:

- Seed values are provided as the **C# entity type** (`DateTimeOffset`).
- EF Core applies the converter when generating the migration SQL.
- INSERT statements in the seed migration will use `DateTime` values.

The fixed seed timestamp `2026-01-01T00:00:00Z` converts cleanly:
- `new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)` →
  stored as `2026-01-01T00:00:00.0000000` in `datetime2`.

No converter workarounds are needed.

### 5.4 Foreign Key Constraints Relevant To Seed Data

| FK | Constraint | Seed Data Impact |
|---|---|---|
| `users.organization_id → organizations.organization_id` | `Restrict` | Seed organizations must be inserted before seed users (EF handles ordering automatically). |
| `user_roles.user_id → users.user_id` | `Restrict` | Seed users must be inserted before seed roles (EF handles ordering). |
| `user_roles.assigned_by_user_id → users.user_id` | `Restrict`, nullable | Set to `null` for all seed roles. No circular dependency. |
| `audit_log_entries.organization_id → organizations.organization_id` | Optional, nullable FK | Not seeded; no impact. |

EF Core's `HasData` migration generator respects FK ordering when inserting
seed data, inserting parent rows before child rows automatically.

### 5.5 Unique Constraint Impact

| Constraint | Column(s) | Seed Data Implication |
|---|---|---|
| `UX_users_email` | `email` | All seven seed email addresses must be unique. They are, per the canonical fictional emails. |
| `UX_user_roles_user_role` | `(user_id, role_name)` | One role per user in seed data. No duplicates. |

### 5.6 Migration State

| Migration | Timestamp | Content | Action |
|---|---|---|---|
| `InitialPersistenceFoundation` | 20260525175722 | Schema only; all four tables, indexes, constraints | Leave unchanged |

One migration exists. No seed data migration exists. A new migration named
`SeedFictionalOrganizationsAndPersonas` should be created during implementation.

---

## 6. Local SQL Server Readiness

### 6.1 Docker Compose

`docker-compose.yml` is confirmed valid. `docker compose config` renders
the configuration successfully with:

- Image: `mcr.microsoft.com/mssql/server:2022-latest`
- `MSSQL_PID: Developer`
- Port: `1433:1433` (or `KNOWLEDGEOPS_SQL_PORT` override)
- Volume: `sqlserver-data` (persistent named volume)

### 6.2 Environment Configuration

- `.env.example` is committed with safe placeholder values.
- `.env` is gitignored (`line 433` in root `.gitignore`).
- `ConnectionStrings__DefaultConnection` follows the `Server=localhost,{port};
  Database=...;User Id=sa;Password=...;TrustServerCertificate=True;Encrypt=True`
  convention documented in `.env.example`.

### 6.3 Local Tool Manifest

`.config/dotnet-tools.json` exists and pins `dotnet-ef 10.0.8`:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "dotnet-ef": {
      "version": "10.0.8",
      "commands": ["dotnet-ef"]
    }
  }
}
```

`dotnet tool restore` activates this manifest for migration generation.

### 6.4 Migration Application Readiness

The existing `InitialPersistenceFoundation` has been applied to the local SQL
Server container in Issue #6 validation. The seed migration will be an
incremental application: `dotnet ef database update` will apply only the new
migration.

If the local database has been reset (`docker compose down -v`), both
migrations must be applied in sequence. EF Core handles this automatically.

---

## 7. Seed Strategy Recommendation

### 7.1 Recommended Strategy: EF HasData / Migration-Based Seed

**Use EF Core `HasData` in a new dedicated migration**. This is the
recommended strategy because:

- Seed records are deterministic and version-controlled.
- EF Core ensures FK insertion order automatically.
- `dotnet ef database update` applies seed data atomically with the schema.
- Developers get seed data after any fresh `dotnet ef database update` run
  with no extra steps.
- Reset behavior is predictable: `docker compose down -v` + `dotnet ef database
  update` restores to clean seeded state.

### 7.2 Rejected Alternatives

| Alternative | Reason rejected |
|---|---|
| Runtime seed service (auto-seed on startup) | Explicitly prohibited. Migrations apply intentionally; auto-seed on startup adds unexpected database side effects. |
| Test-only fixtures (no migration) | Seed data is for local development validation and cross-sprint test reuse, not only tests. A migration-based approach is necessary. |
| SQL script initialization | Out of pattern for this project; EF migration tooling is already established. |
| Modifying `InitialPersistenceFoundation` | Explicitly prohibited. The existing migration is applied and committed. |

### 7.3 Seed Structure Recommendation

Create the following new files in `src/KnowledgeOps.Infrastructure/Persistence/SeedData/`:

```text
src/KnowledgeOps.Infrastructure/Persistence/SeedData/
  SeedDataIds.cs          — static class with deterministic Guid constants
  KnowledgeOpsSeedData.cs — static class with ApplySeedData(ModelBuilder)
```

`KnowledgeOpsDbContext.OnModelCreating` should be updated to call:

```csharp
KnowledgeOpsSeedData.ApplySeedData(modelBuilder);
```

after `ApplyConfigurationsFromAssembly(...)`. This keeps the seed data out of
the schema configuration classes and provides a single named entry point.

`SeedDataIds.cs` and `KnowledgeOpsSeedData.cs` belong in Infrastructure only.
Domain and Application must not reference seed constants.

---

## 8. Deterministic Seed Identity Plan

### 8.1 Organizations

| Organization | Deterministic Id |
|---|---|
| Asteria Support Group | `11111111-1111-4111-8111-111111111111` |
| Boreal Contact Services | `22222222-2222-4222-8222-222222222222` |

### 8.2 Users

| User | Organization | Email | Deterministic Id |
|---|---|---|---|
| Agent A | Asteria | agent.a@asteria.example.com | `aaaa0001-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| Supervisor A | Asteria | supervisor.a@asteria.example.com | `aaaa0002-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| KnowledgeAdmin A | Asteria | knowledgeadmin.a@asteria.example.com | `aaaa0003-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| Manager A | Asteria | manager.a@asteria.example.com | `aaaa0004-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| Admin A | Asteria | admin.a@asteria.example.com | `aaaa0005-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| Agent B | Boreal | agent.b@boreal.example.com | `bbbb0001-bbbb-4bbb-8bbb-bbbbbbbbbbbb` |
| Admin B | Boreal | admin.b@boreal.example.com | `bbbb0002-bbbb-4bbb-8bbb-bbbbbbbbbbbb` |

### 8.3 User Roles

Since `UserRole` has a surrogate primary key (`user_role_id`), deterministic
GUIDs are required for role assignment seed records.

| Role Assignment | User | Role | Deterministic Id |
|---|---|---|---|
| Agent A → Agent | Agent A | Agent | `cccc0001-cccc-4ccc-8ccc-cccccccccccc` |
| Supervisor A → Supervisor | Supervisor A | Supervisor | `cccc0002-cccc-4ccc-8ccc-cccccccccccc` |
| KnowledgeAdmin A → KnowledgeAdmin | KnowledgeAdmin A | KnowledgeAdmin | `cccc0003-cccc-4ccc-8ccc-cccccccccccc` |
| Manager A → Manager | Manager A | Manager | `cccc0004-cccc-4ccc-8ccc-cccccccccccc` |
| Admin A → Admin | Admin A | Admin | `cccc0005-cccc-4ccc-8ccc-cccccccccccc` |
| Agent B → Agent | Agent B | Agent | `cccc0006-cccc-4ccc-8ccc-cccccccccccc` |
| Admin B → Admin | Admin B | Admin | `cccc0007-cccc-4ccc-8ccc-cccccccccccc` |

### 8.4 Seed Timestamp

Fixed UTC timestamp for all seed records:

```text
2026-01-01T00:00:00Z
C# value: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
Stored as: 2026-01-01T00:00:00.0000000 (datetime2, UTC)
```

Do not use `DateTimeOffset.UtcNow` for seed data.

---

## 9. Seed Field Plan

### 9.1 Organization Seed Fields

```
Organization {
    Id:         <deterministic Guid from Section 8.1>
    Name:       "Asteria Support Group" | "Boreal Contact Services"
    Status:     OrganizationStatus.Active
    CreatedAt:  new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    UpdatedAt:  new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
}
```

Both organizations seed as `Active`. The `CK_organizations_name_not_empty`
check constraint is satisfied by both non-empty names. The
`CK_organizations_status` check constraint is satisfied by `Active`.

### 9.2 User Seed Fields

```
User {
    Id:             <deterministic Guid from Section 8.2>
    OrganizationId: <deterministic Guid of owning organization>
    DisplayName:    "Agent A" | "Supervisor A" | "KnowledgeAdmin A" | etc.
    Email:          <canonical fictional email from Section 8.2>
    PasswordHash:   null                    ← no password in Sprint 5
    Status:         UserStatus.Active
    LastLoginAt:    null                    ← no login history
    CreatedAt:      new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    UpdatedAt:      new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    DeletedAt:      null                    ← not soft-deleted
}
```

`CK_users_status` check constraint satisfied by `Active`. `UX_users_email`
unique constraint satisfied — all seven emails are distinct.

### 9.3 User Role Seed Fields

```
UserRole {
    Id:               <deterministic Guid from Section 8.3>
    UserId:           <deterministic Guid of the user receiving the role>
    RoleName:         UserRoleName.<EnumValue>
    AssignedAt:       new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    AssignedByUserId: null   ← no admin assigner; nullable FK satisfied
}
```

`CK_user_roles_role_name` check constraint satisfied by all five MVP role
enum values. `UX_user_roles_user_role` unique constraint satisfied — one role
per user per role name (no duplicates). `FK_user_roles_users_assigned_by_user_id`
satisfied by `null`.

### 9.4 Audit Log Entry Seed Decision

**Do not seed `audit_log_entries` records.**

`docs/14-database-design.md` does not require initial audit records. The
audit log is operational event storage, not seed data. Seed rows would not
represent real events. Omitting them keeps seed data meaningful and avoids
spurious audit records in tests.

---

## 10. Password And Credential Decision

**No passwords or credentials of any kind are seeded in Sprint 5.**

Specific confirmations:

| Item | Decision |
|---|---|
| `User.PasswordHash` | `null` for all seed records |
| JWT tokens | Not generated, not stored |
| Authentication secrets | Not introduced |
| Password complexity validation | Not enforced in seed — deferred to Sprint 6 |
| Auth endpoint credentials | Not introduced |

Sprint 5 seed data creates recognizable fictional identities for local
development and cross-sprint test reuse only. Authentication and credential
management belong to Sprint 6 (`feat: add authentication and current-user
context`).

Documentation in `docs/demo-data.md` (Section 12) must include a clear
explanation that these personas have no login credentials until Sprint 6
introduces the authentication layer.

---

## 11. Test Strategy

### 11.1 New Test Class: SeedDataTests.cs

Create `tests/KnowledgeOps.IntegrationTests/SeedDataTests.cs`.

Test cases:

| # | Test name | Assertion |
|---|---|---|
| 1 | `Seed_Contains_Exactly_Two_Organizations` | `Organizations.CountAsync() == 2` |
| 2 | `Seed_Contains_Exactly_Seven_Users` | `Users.CountAsync() == 7` |
| 3 | `Seed_Asteria_Has_Five_Users` | Five users with `OrganizationId == AsteriaId` |
| 4 | `Seed_Boreal_Has_Two_Users` | Two users with `OrganizationId == BorealId` |
| 5 | `Seed_Contains_Exactly_Seven_Role_Assignments` | `UserRoles.CountAsync() == 7` |
| 6 | `Seed_All_Five_MVP_Roles_Represented` | Distinct role names == `{Agent, Supervisor, KnowledgeAdmin, Manager, Admin}` |
| 7 | `Seed_No_Passwords_Stored` | All users have `PasswordHash == null` |
| 8 | `Seed_Agent_A_And_Agent_B_In_Different_Organizations` | `AgentA.OrganizationId != AgentB.OrganizationId` |
| 9 | `Seed_No_Unexpected_Tables` | Table names match `{__EFMigrationsHistory, audit_log_entries, organizations, user_roles, users}` |
| 10 | `Seed_No_Audit_Log_Entries` | `AuditLogEntries.CountAsync() == 0` |

### 11.2 SQL Server Gating

All `SeedDataTests` must use `[SqlServerFact]` attribute (already established
in `SqlServerFactAttribute.cs`). Tests skip gracefully when
`ConnectionStrings__DefaultConnection` is not set. Normal `dotnet test
KnowledgeOpsAI.sln` remains green on machines without a local SQL Server.

### 11.3 Required Update: SqlServerPersistenceTests.cs

**Critical implementation detail**: The existing
`SqlServerPersistenceTests.Migration_Creates_Only_Foundation_Tables_And_Enforces_Core_Integrity`
test calls `context.Database.MigrateAsync()`, which will apply ALL migrations,
including the new `SeedFictionalOrganizationsAndPersonas` migration.

After the seed migration, the database will contain:
- 2 organizations (seeded)
- 7 users (seeded)
- 7 user roles (seeded)

The following private helper methods in `SqlServerPersistenceTests.cs`
currently use `SingleAsync()` which assumes exactly one record and will throw
`InvalidOperationException` with multiple seeded records:

| Method | Current call | Problem after seed |
|---|---|---|
| `AssertDuplicateRoleRejectedAsync` | `context.UserRoles.SingleAsync()` | 8 roles (7 seeded + 1 test), throws |
| `AssertDuplicateEmailRejectedAsync` | `context.Users.SingleAsync()` | 8 users (7 seeded + 1 test), throws |
| `AssertInvalidRoleRejectedAsync` | `context.Users.Select(...).SingleAsync()` | Same problem |

**Required fix during implementation**: Update these methods to filter by the
specific user or role ID created within the test (the test already has
`user.Id` and the role's `user.Id` available). Replace `SingleAsync()` with
filtered `SingleAsync(predicate)` or `FirstAsync(predicate)` using the known
test IDs.

The `PersistenceModelTests.cs` tests are not affected — they inspect the
model metadata only and do not query live database rows.

### 11.4 Test Database Lifecycle

`SeedDataTests` should follow the same pattern as `SqlServerPersistenceTests`:
create a unique database name per test run, call `MigrateAsync()`, run
assertions, drop the database in `finally`. This ensures clean state without
depending on the local developer database's migration history.

---

## 12. Documentation Plan

### 12.1 Create: docs/demo-data.md

Create a new documentation file at `docs/demo-data.md`.

Required content:
- **Purpose**: Explains that all seed data is fictional and deterministic,
  intended only for local development and automated test use.
- **Fictional-only warning**: No real user, employee, client, or partner data.
  No production accounts. No real internal documents.
- **Organizations**: Asteria Support Group and Boreal Contact Services with
  their fictional email domains.
- **Personas**: All seven seed users with display name, email, role, and
  organization mapping.
- **MVP roles**: The five allowed roles (Agent, Supervisor, KnowledgeAdmin,
  Manager, Admin) and explicit statement that QA, Trainer, Viewer, and any
  other non-MVP roles are not seeded.
- **No credentials**: Explicit note that seed users have no login credentials
  in Sprint 5. Authentication will be added in Sprint 6.
- **Reset instructions**: `docker compose down -v` + `docker compose up -d
  sqlserver` + `dotnet ef database update` to restore seed state.
- **Out-of-scope data**: No documents, chat sessions, retrieval results,
  embeddings, feedback, dashboard metrics, knowledge gaps, or synthetic
  document corpus are seeded.

### 12.2 Update: docs/local-development.md

Add a short section or link in `docs/local-development.md` pointing to
`docs/demo-data.md`. The new section should appear near the "Smoke
Validation" area and explain that after running `dotnet ef database update`,
fictional seed organizations and personas are present. Reference
`docs/demo-data.md` for the complete persona list.

---

## 13. Out Of Scope

The following must not be introduced by Issue #7 implementation:

| Category | Excluded |
|---|---|
| Authentication | Login flow, JWT generation, token handling, session service |
| Credentials | Passwords, password hashes, password complexity, API keys |
| Authorization | RBAC policies, organization-scope middleware, permission evaluation |
| Non-MVP roles | QA, Trainer, Viewer, Recruiter, Portfolio Reviewer, AI Coding Agent |
| Production accounts | No real users, employees, clients, or internal accounts |
| Admin UI/API | Admin endpoints, user management APIs, role management APIs |
| Documents | Document records, document files, synthetic document corpus |
| Document processing | Processing queues, chunk records, embedding records |
| Retrieval | Vector search, retrieval results, embeddings |
| RAG | Chat sessions, chat interactions, AI answer records |
| Citations | Citation records |
| Feedback | Answer feedback records |
| Dashboard metrics | Metric snapshot records or computed metric data |
| Knowledge gaps | Knowledge gap signal records |
| Audit log entries | Seeded audit records (audit log is operational, not seed data) |
| Separate roles table | No `roles` table; roles remain as `role_name` in `user_roles` |
| Runtime seed service | No auto-seed on API/Worker startup |
| Frontend changes | No Angular changes |
| Docker changes | No Dockerfile or docker-compose.yml changes |

---

## 14. Risks And Blockers

### 14.1 Risks

| Risk | Severity | Area | Mitigation |
|---|---|---|---|
| Existing `SingleAsync()` assertions in `SqlServerPersistenceTests.cs` fail after seed migration applies | High | Integration tests | Required: update `AssertDuplicateRoleRejectedAsync`, `AssertDuplicateEmailRejectedAsync`, and `AssertInvalidRoleRejectedAsync` to filter by specific IDs created within the test, not `SingleAsync()` on all rows. |
| Value converter behavior with `HasData` is unexpected | Low | EF migration generation | `UtcDateTimeOffsetConverter` is `ValueConverter<DateTimeOffset, DateTime>`. EF Core 10 applies converter at migration generation time. Fixed UTC timestamp is clean. Verify generated migration SQL during implementation. |
| `AssignedByUserId` FK creates ordering issue in HasData | Low | Migration seed ordering | `AssignedByUserId` is null for all seed roles. Nullable FK is satisfied without referencing any seeded user as assigner. EF Core handles organization → user → role ordering automatically. |
| Seed GUIDs conflict with future manually inserted test records | Very low | Integration tests | Seed test class creates a fresh unique database per run and drops it after. No conflict with persistent databases. |

### 14.2 Blockers

**None.** The current persistence model fully supports deterministic fictional
seed data via `HasData`. No schema changes, entity changes, or configuration
changes are needed before seed data can be added.

### 14.3 Open Risks Update

No new open risks need to be added to `open-risks.md` during this audit.
The `SingleAsync()` compatibility issue is a required implementation detail
(documented above), not a new architectural risk. The existing security, RAG,
and authorization risks already in `open-risks.md` remain applicable to their
owning future sprints.

---

## 15. Readiness Recommendation

**READY FOR IMPLEMENTATION**

All preconditions are satisfied:

- ✅ Foundational entities (Organization, User, UserRole, AuditLogEntry) are present and correct.
- ✅ No separate `roles` table exists; five-role MVP model is enforced by check constraint.
- ✅ `UserRole` uses surrogate GUID PK — deterministic IDs are required and planned.
- ✅ `HasData` is not yet used — seed migration will be a clean addition.
- ✅ `UtcDateTimeOffsetConverter` works correctly with `HasData` for fixed UTC timestamps.
- ✅ FK ordering for organizations → users → roles is handled automatically by EF Core.
- ✅ `AssignedByUserId` is nullable — no circular FK dependency in seed data.
- ✅ `UX_users_email` uniqueness satisfied by all seven distinct fictional emails.
- ✅ `UX_user_roles_user_role` uniqueness satisfied by one role per user.
- ✅ Docker Compose config is valid; local SQL Server is ready.
- ✅ `dotnet-tools.json` exists with `dotnet-ef 10.0.8`.
- ✅ `InitialPersistenceFoundation` is applied and must not be modified.
- ✅ Test gating via `SqlServerFactAttribute` is established.
- ⚠️ Implementation must update `SqlServerPersistenceTests.cs` `SingleAsync()` calls
  to avoid test failures after seed migration applies (see Section 11.3).

---

## 16. Recommended Next Step

Issue #7 audit is complete. Status: **READY FOR IMPLEMENTATION**.

Generate the implementation prompt for Issue #7
(`chore: seed fictional organizations and MVP role personas`) using
`docs/agents/10-issue-execution-template.md`.

The implementation must follow:
- Section 7 (HasData migration strategy; `SeedData/` folder structure)
- Section 8 (deterministic IDs for all organizations, users, and role records)
- Section 9 (seed field plan including null password hash for all users)
- Section 10 (no passwords, no credentials)
- Section 11 (new `SeedDataTests.cs`; updated `SqlServerPersistenceTests.cs`)
- Section 12 (create `docs/demo-data.md`; update `docs/local-development.md`)
- Section 13 (out-of-scope exclusions)

The implementation agent must:
- NOT modify `InitialPersistenceFoundation`.
- NOT create a `roles` table.
- NOT seed `AuditLogEntry` records.
- NOT add passwords or password hashes.
- NOT auto-seed on application startup.
- Update `SqlServerPersistenceTests.cs` to filter by specific IDs before
  calling `SingleAsync()`.
- Record the seed strategy decision in `decisions-log.md` during
  implementation.
- Update all four progress files after verified completion.
