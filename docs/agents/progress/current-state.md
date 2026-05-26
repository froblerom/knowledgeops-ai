# Current Implementation State

Last updated: 2026-05-26

## Current Phase

Sprint 10 Document Metadata And Lifecycle Model / Issue #20 implementation complete

## Delivery Status

| Item | Status |
| --- | --- |
| Current sprint | Sprint 10 completed through Issue #20 |
| Last completed sprint | Sprint 10: Document Metadata And Lifecycle Model (Issue #20 implementation verified; PR pending) |
| Active implementation issue | None; Issue #20 is implemented and ready for pull request review. |
| Current architecture status | Buildable .NET 10 backend + Angular 21 frontend + local SQL Server container + EF Core persistence foundation + `SeedFictionalOrganizationsAndPersonas` migration + JWT Bearer authentication + ICurrentUser abstraction + working login page with authGuard and apiInterceptor + **RBAC permission catalog** + **RolePermissionMatrix** (5 MVP roles, 30 permissions) + **IPermissionService / PermissionService** + **IOrganizationScopeService / OrganizationScopeService** + **[RequirePermission] attribute + PermissionPolicyProvider + PermissionAuthorizationHandler** + **persisted-current-state authorization via IUserAccessStateReader / EfUserAccessStateReader** + **correlation ID and global safe-error middleware** + **application observability contracts with Infrastructure EF audit/database-health adapters** + **public basic and Admin-only sanitized health endpoints** + **frontend generic Error ID UX** + **frontend RoleVisibilityService (UX-only)** + **future RAG/retrieval authorization hook interfaces** + **Admin-only same-organization user management API (GET/POST/PUT /api/v1/users, GET/POST/DELETE /api/v1/users/{id}/roles)** + **UserManagementService with initialPassword hashing, email normalization, self-lockout protection, final-active-Admin protection, persisted-disable permission check** + **safe audit events (UserCreated, UserUpdated, UserStatusChanged, UserRoleAssigned, UserRoleRemoved, UserManagementDenied, DocumentRetrievalDisabled)** + **minimal Angular Admin UI (user list, user detail/edit, user create, role assignment/removal)** + **canonical Document metadata and behavior-protected lifecycle (StartProcessing, MarkProcessed, MarkFailed, DisableRetrieval, IsEligibleForRetrieval)** + **DocumentProcessingStatus enum (Uploaded, Processing, Processed, Failed)** + **transition-aware DocumentService + IDocumentRepository** + **EfDocumentRepository** + **DocumentConfiguration with status check constraint + DocumentMetadataFoundation migration** + **4 document API endpoints (GET /api/v1/documents, GET /{id}, GET /{id}/processing-status, POST /{id}/disable)** + **Angular documents list and detail/status/action shell** + **DocumentService Angular service (list/get/getProcessingStatus/disableRetrieval)** + **canDisableDocumentRetrieval() UX helper**. |

## Current Known Limitations

- No business workflow, document processing, retrieval or RAG implementation exists yet.
- Document upload (POST /api/v1/documents) and file storage are deferred to Sprint 11.
- Document re-enable retrieval API and retry processing API are deferred to Phase 2.
- Retrieval eligibility predicate is encoded in `Document.IsEligibleForRetrieval()` but no retrieval workflow exists.
- JWT logout is stateless (client-side clear only); no server-side token revocation.
- Audit emissions are safe best-effort; workflow-specific telemetry remains deferred.
- Detailed health intentionally exposes only application and database status.
- Chat.ViewInteraction and Chat.ViewCitations carry an own-only/scoped convention; actual query-level enforcement is deferred to Sprint 17+ chat workflows.
- Diagram artifact cleanup remains pending for `docs/diagrams/business-process/monitoring-operational-process.png`.
- Serializable-isolation final-active-Admin check in EfUserManagementRepository does not span distributed transactions; acceptable for MVP single-node SQL Server.
- SQL integration tests for user management (UserManagementIntegrationTests) require `ConnectionStrings__DefaultConnection` env var and a running SQL Server container.

## Next Recommended Action

Open the pull request for Issue #20. After merge, prepare Sprint 11 or the next approved sprint.

## Source Of Truth

- `docs/21-implementation-roadmap.md`
- `docs/22-implementation-guardrails.md`
- `docs/agents/`

## Update Rule

Update this file whenever an implementation issue starts or completes, the active sprint changes, a blocker affects recommended next action, or architecture readiness materially changes.
