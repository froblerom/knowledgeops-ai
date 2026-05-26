# Current Implementation State

Last updated: 2026-05-26

## Current Phase

Sprint 7 RBAC and Organization-Scope Authorization Foundation / Issue #14 implementation complete

## Delivery Status

| Item | Status |
| --- | --- |
| Current sprint | Sprint 7 completed through Issue #14 |
| Last completed sprint | Sprint 7: RBAC and Organization-Scope Authorization Foundation (Issue #14 implementation verified; PR pending) |
| Active implementation issue | None; Issue #14 is implemented and ready for pull request review. |
| Current architecture status | Buildable .NET 10 backend + Angular 21 frontend + local SQL Server container + EF Core persistence foundation + `SeedFictionalOrganizationsAndPersonas` migration + JWT Bearer authentication + ICurrentUser abstraction + working login page with authGuard and apiInterceptor + **RBAC permission catalog** + **RolePermissionMatrix** (5 MVP roles, 30 permissions) + **IPermissionService / PermissionService** + **IOrganizationScopeService / OrganizationScopeService** + **[RequirePermission] attribute + PermissionPolicyProvider + PermissionAuthorizationHandler** + **frontend RoleVisibilityService (UX-only)** + **future RAG/retrieval authorization hook interfaces**. |

## Current Known Limitations

- No business workflow, document processing, retrieval or RAG implementation exists yet.
- Feature pages are placeholder stubs; content deferred to their respective sprints.
- Persistence is limited to the four approved foundation tables; feature-specific schema remains deferred to its owning sprints.
- Seed users have `password_hash = null`; test passwords must be provisioned via test fixture setup — never committed in seed data.
- JWT logout is stateless (client-side clear only); no server-side token revocation.
- Authorization failure logging is defined at the event level (PermissionDenied, OrganizationScopeDenied, AuthorizationDenied) but full structured logging framework, correlation middleware, and health endpoints are deferred to Sprint 8.
- Chat.ViewInteraction and Chat.ViewCitations carry an own-only/scoped convention for Agent vs. other roles; the actual query-level enforcement is deferred to Sprint 17+ chat workflows.
- Frontend navigation shell placeholder items exist; role-aware visibility uses RoleVisibilityService but full navigation shell content deferred to feature sprints.
- Diagram artifact cleanup remains pending for `docs/diagrams/business-process/monitoring-operational-process.png`.

## Next Recommended Action

Open the pull request for Issue #14. After merge, prepare Sprint 8 (observability and operational safety) or the next approved sprint.

## Source Of Truth

- `docs/21-implementation-roadmap.md`
- `docs/22-implementation-guardrails.md`
- `docs/agents/`

## Update Rule

Update this file whenever an implementation issue starts or completes, the active sprint changes, a blocker affects recommended next action, or architecture readiness materially changes.
