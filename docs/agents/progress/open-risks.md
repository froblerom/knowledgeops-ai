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

## Update Rule

Read this file for Level 3 work and release review. Update risk status, mitigation or new issue references when implementation evidence changes the risk.
