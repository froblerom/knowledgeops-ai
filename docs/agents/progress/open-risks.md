# Open Implementation Risks

Last updated: 2026-05-25

| Risk | Severity | Related Area | Mitigation | Status |
| --- | --- | --- | --- | --- |
| Agent prompts may load excessive context and lose focus. | Medium | Harness / all future issues | Use `13-prompt-classifier.md`, level-based routing and minimum context bundles. | Open |
| RAG may be implemented as unsupported general answer behavior rather than grounded document assistance. | High | Retrieval/RAG | Use `rag-implementation-agent.md`, citation and insufficient-context rules, and fake-provider safety tests. | Open |
| Authorization may be skipped during retrieval or prompt construction. | Critical | Security/RAG | Load security, business-rules and RAG contexts for relevant tasks; require cross-scope tests and verification. | Open |
| Agent context summaries may diverge from canonical documents over time. | High | Documentation governance | Use documentation and verification agents; update harness when canonical docs change; treat canonical docs as authoritative. | Open |
| Diagram artifact filename cleanup remains pending. | Low | Documentation artifacts | Address in Sprint 28 or an explicitly authorized diagram artifact task. | Open |

## Sprint 0 Issue #2 Disposition

The earlier optional prompt-harness question is resolved: `docs/agents/` is the canonical harness and future implementation prompts must classify first. No open risk remains for whether the harness exists; the ongoing risks above remain relevant for implementation.

## Sprint 1 Issue #3 Disposition

The backend scaffold readiness concern is resolved: the .NET 10 solution, approved project reference graph, minimal hosts and architecture boundary tests were implemented and validated. No new open risk was introduced by Issue #3. The feature-sensitive risks above remain applicable to their owning future sprints, and diagram artifact filename cleanup remains deferred.

## Sprint 2 Issue #4 Disposition

The Angular frontend scaffold is established. The auth guard is intentionally pass-through (Sprint 6 adds real auth); this is an accepted architectural decision, not an open risk. No new open risks introduced by Issue #4. The existing security and RAG risks remain applicable to their owning future sprints.

## Sprint 3 Issue #5 Disposition

The local Docker runtime foundation is established. SQL Server 2022 container confirmed starting successfully (`SQL Server is now ready for client connections`). Safe `.env.example` template committed; `.env` is gitignored. Port 1433 conflict risk is mitigated by the `KNOWLEDGEOPS_SQL_PORT` override convention documented in `.env.example` and `docs/local-development.md`. No new open risks introduced by Issue #5. API/Worker connection errors until EF Core is introduced are an expected and accepted limitation, not an open risk.

## Sprint 4 Issue #6 Disposition

The EF Core SQL Server persistence foundation is established in Infrastructure only. `InitialPersistenceFoundation` was reviewed and applied to local SQL Server, and the opt-in relational integration test passed against a uniquely named temporary database with schema-scope, round-trip, role-value, uniqueness, and organization foreign-key checks. No new open risk remains from migration validation; authentication, authorization, document processing, retrieval, and RAG risks remain applicable to their owning future sprints.

## Sprint 5 Issue #7 Disposition

The fictional seed data migration is established. `SeedFictionalOrganizationsAndPersonas` was generated and applied to local SQL Server; 13 SQL-backed `SeedDataTests` passed (2 organizations, 7 users, 5 MVP roles, 7 role assignments, no passwords, no audit log entries, no unexpected tables, cross-org scope coverage). `SqlServerPersistenceTests` fragility from `SingleAsync()` was resolved before adding seed data. Package/reference boundaries confirmed: Domain has no packages; Application has DI abstractions only; EF Core remains in Infrastructure only. No new open risks introduced by Issue #7; authentication, authorization, and all feature-specific risks remain applicable to their owning future sprints.

## Sprint 6 Issue #13 Disposition

JWT Bearer authentication and current-user context are implemented across all layers. Login, logout, and `/auth/me` endpoints are live. All five MVP login failure modes return HTTP 401 with an identical body (user enumeration prevented). GET /auth/me re-queries the database on every request and rejects Disabled/Pending/missing users. Angular `authGuard` redirects to `/login` when the session is expired or absent; `apiInterceptor` attaches Bearer tokens only to API-URL requests. `ValidateOnStart` enforces a minimum 32-character signing key at host startup. No passwords committed in seed data; test passwords provisioned via fixture setup only. Authorization risk for business features remains open (role-scoped document/RAG access is not yet enforced — see Sprint 7+).

## Update Rule

Read this file for Level 3 work and release review. Update risk status, mitigation or new issue references when implementation evidence changes the risk.
