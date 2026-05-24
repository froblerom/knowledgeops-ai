# Prompt Classifier

## Purpose

This file is the routing entry point for future KnowledgeOps-AI prompts. **Classify first, then execute.**

For every implementation prompt, classification must be completed before editing and the classified work must be recorded using `docs/agents/10-issue-execution-template.md`. The required context and validation plan must be declared before implementation begins.

## Classification Output Format

```text
Classification
- Task type:
- Prompt level: Level 0 / Level 1 / Level 2 / Level 3
- Related sprint/issue:
- Scope: MVP / Deferred / Documentation-only / Review-only
- Primary affected area:
- Security or organization-scope impact:
- AI/RAG impact:
- Data or migration impact:

Reason
- ...

Required Context
- Agent context files:
- Canonical documents:
- Progress files:
- Source/config/test files to inspect:

Recommended Agents/Subagents
- ...

Validation
- ...

Escalation Or Blockers
- ...
```

## Level Selection Questions

1. Is the task mechanical, localized documentation, one-area implementation, or cross-layer/sensitive work?
2. Is an approved issue/sprint applicable, or is the task planning, audit or harness work?
3. Does it change scope, an ADR, business rule interpretation, a domain term, API/security contract or database contract?
4. Does it affect authentication, roles, organization scope, citations, audit/health access or sensitive data?
5. Does it affect processing, retrieval eligibility, AI providers, prompts, RAG, citations or insufficient context?
6. Does it require a migration, CI change, multi-layer edits or release verification?
7. What is the smallest context bundle that safely answers these questions?

## Fast Mapping

| Task | Level | Typical Routing |
| --- | ---: | --- |
| Typo or formatting correction | 0 | Protocol, project context and target file; no subagent. |
| Single Markdown document update | 1 | Protocol, project, affected context and target doc; documentation/verification optional. |
| ADR documentation update | 1 | Add architecture context and affected ADR; escalate if decision changes. |
| One backend command/use case | 2 | Backend context plus architecture/domain/rules/testing and current state. |
| One Angular form/page | 2 | Frontend/testing contexts plus affected API/security docs. |
| One API endpoint with tests | 2 | Backend/testing plus API contract; escalate if protected/RAG/cross-scope sensitive. |
| EF Core entity or migration | 2 or 3 | Database/back-end context; Level 3 for scope, history, destructive or cross-layer effect. |
| Document processing worker | 3 | Backend/database/testing/verification with lifecycle and observability sources. |
| Authentication/RBAC/organization scope framework | 3 | Backend/testing/verification; security contract and ADR-004/010. |
| Retrieval authorization | 3 | RAG/backend/testing/verification; eligibility and security sources. |
| RAG orchestration | 3 | RAG/testing/verification; grounding and provider sources. |
| Citation pipeline | 3 | RAG/database/testing/verification; citation and scope contracts. |
| Observability foundation | 3 | Observability/backend/testing/verification; health and safe logging contracts. |
| CI/CD pipeline | 3 | DevOps/testing/verification; fake-provider and secret rules. |
| Cross-document audit | 3 | Documentation/verification; all affected canonical docs. |
| Agent harness change | 3 | Documentation/verification; harness audit, roadmap and guardrails. |
| Release stabilization | 3 | Verification with affected specialists and all progress records. |

## Escalation Rules

Escalate to Level 3 when a task:

- Could expand MVP or alter a deferred boundary.
- Changes or conflicts with an accepted ADR.
- Adds or modifies a technical role, protected endpoint, lifecycle status or organization-scope rule.
- Could send protected document content to a model or expose it in responses, citations, metrics or logs.
- Affects security-sensitive migrations, CI/release gates or multiple layers.
- Cannot be implemented confidently from the routed context and exact canonical sources.

## Subagent Selection Rules

- Use no subagent for routine Level 0 work.
- For Level 1, use a documentation or verification specialist only when beneficial.
- For Level 2, use the specialist for the primary implementation area and optionally testing/verification.
- For Level 3, use specialists sequentially and end with verification when work is implemented.
- Select `rag-implementation-agent` for embeddings, retrieval, prompts, RAG, citations or insufficient-context behavior.
- Never use subagent fan-out merely to read documentation.

## Progress Routing Rules

- Every Level 2 or Level 3 implementation prompt must consult `docs/agents/progress/current-state.md`, `decisions-log.md`, `open-risks.md`, and `completed-issues.md` before editing.
- Update `current-state.md` as implementation status changes.
- Update `decisions-log.md` when a material implementation-time decision is made.
- Update `open-risks.md` when a risk is discovered, changed or mitigated.
- Update `completed-issues.md` after each verified completed implementation issue.

## Validation Routing

- Documentation: re-read changed docs and run documentation lint only if available.
- Backend/API: relevant build/unit/API/integration tests when projects exist.
- Frontend: relevant Angular build/lint/tests when tooling exists.
- Database: SQL Server/migration and integrity checks when relevant.
- AI/RAG: fake-provider tests, retrieval-before-generation, citations, insufficient-context and scope-denial cases.
- DevOps/CI: affected configuration/pipeline/container checks, no committed secrets and no normal-CI live AI dependency.
- Review/release: Definition of Done, progress records, risk disposition and reported validation evidence.

## Final Rule

Classify first. For implementation work, complete the issue execution template, consult required progress files, load the routed minimum context, declare validation, inspect exact canonical contracts when needed, then execute, validate and update progress records.
