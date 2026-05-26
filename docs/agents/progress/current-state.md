# Current Implementation State

Last updated: 2026-05-26

## Current Phase

Sprint 9 Admin User And Role Management Foundation / Issue #16 implementation complete

## Delivery Status

| Item | Status |
| --- | --- |
| Current sprint | Sprint 9 completed through Issue #16 |
| Last completed sprint | Sprint 9: Admin User And Role Management Foundation (Issue #16 implementation verified; PR pending) |
| Active implementation issue | None; Issue #16 is implemented and ready for pull request review. |
| Current architecture status | Buildable .NET 10 backend + Angular 21 frontend + local SQL Server container + EF Core persistence foundation + `SeedFictionalOrganizationsAndPersonas` migration + JWT Bearer authentication + ICurrentUser abstraction + working login page with authGuard and apiInterceptor + **RBAC permission catalog** + **RolePermissionMatrix** (5 MVP roles, 30 permissions) + **IPermissionService / PermissionService** + **IOrganizationScopeService / OrganizationScopeService** + **[RequirePermission] attribute + PermissionPolicyProvider + PermissionAuthorizationHandler** + **persisted-current-state authorization via IUserAccessStateReader / EfUserAccessStateReader** + **correlation ID and global safe-error middleware** + **application observability contracts with Infrastructure EF audit/database-health adapters** + **public basic and Admin-only sanitized health endpoints** + **frontend generic Error ID UX** + **frontend RoleVisibilityService (UX-only)** + **future RAG/retrieval authorization hook interfaces** + **Admin-only same-organization user management API (GET/POST/PUT /api/v1/users, GET/POST/DELETE /api/v1/users/{id}/roles)** + **UserManagementService with initialPassword hashing, email normalization, self-lockout protection, final-active-Admin protection, persisted-disable permission check** + **safe audit events (UserCreated, UserUpdated, UserStatusChanged, UserRoleAssigned, UserRoleRemoved, UserManagementDenied)** + **minimal Angular Admin UI (user list, user detail/edit, user create, role assignment/removal)**. |

## Current Known Limitations

- No business workflow, document processing, retrieval or RAG implementation exists yet.
- Non-admin feature pages are placeholder stubs; content deferred to their respective sprints.
- Persistence is limited to the four approved foundation tables; feature-specific schema remains deferred to its owning sprints.
- JWT logout is stateless (client-side clear only); no server-side token revocation.
- Audit emissions for user management are safe best-effort; workflow-specific telemetry remains deferred to its owning future sprint.
- Detailed health intentionally exposes only application and database status; storage, worker, retrieval, and provider health checks remain deferred until those dependencies exist in scope.
- Chat.ViewInteraction and Chat.ViewCitations carry an own-only/scoped convention for Agent vs. other roles; the actual query-level enforcement is deferred to Sprint 17+ chat workflows.
- Frontend navigation shell placeholder items exist; role-aware visibility uses RoleVisibilityService and adminVisibilityGuard but full navigation shell content deferred to feature sprints.
- Diagram artifact cleanup remains pending for `docs/diagrams/business-process/monitoring-operational-process.png`.
- Serializable-isolation final-active-Admin check in EfUserManagementRepository does not span distributed transactions; acceptable for MVP single-node SQL Server.
- SQL integration tests for user management (UserManagementIntegrationTests) require `ConnectionStrings__DefaultConnection` env var and a running SQL Server container.

## Next Recommended Action

Open the pull request for Issue #16. After merge, prepare Sprint 10 or the next approved sprint.

## Source Of Truth

- `docs/21-implementation-roadmap.md`
- `docs/22-implementation-guardrails.md`
- `docs/agents/`

## Update Rule

Update this file whenever an implementation issue starts or completes, the active sprint changes, a blocker affects recommended next action, or architecture readiness materially changes.
