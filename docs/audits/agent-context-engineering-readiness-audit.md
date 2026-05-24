# Agent Context Engineering Readiness Audit

Audit date: 2026-05-24  
Classification: Agent context engineering readiness audit  
Scope: Documentation and process audit only  
Implementation level: Level 3 / Cross-document / harness audit  
Subagents: Not used. This audit precedes creation of an approved subagent harness.

## 1. Purpose

This audit determines whether **KnowledgeOps-AI** is ready for a modular AI agent context harness and records the design decisions required before that harness is generated.

The future harness should help AI agents load only relevant context, reduce token usage, prevent scope and architecture drift, preserve AI/RAG safety and organization-scoped security, route work by prompt complexity, define when specialist subagents are useful, and maintain implementation progress across issues and pull requests.

This audit creates no agent context files, implementation prompts, source code, implementation issues, migrations, or diagram artifacts.

## 2. Audit Scope

### Canonical Documentation Reviewed

- `docs/00-executive-summary.md`
- `docs/01-business-context.md`
- `docs/02-business-case.md`
- `docs/03-project-charter.md`
- `docs/04-stakeholders.md`
- `docs/05-scope-and-roadmap.md`
- `docs/06-requirements.md`
- `docs/07-use-cases.md`
- `docs/08-business-process-flows.md`
- `docs/09-business-rules.md`
- `docs/10-domain-model.md`
- `docs/11-architecture-overview.md`
- `docs/12-c4-architecture.md`
- `docs/13-uml-diagrams.md`
- `docs/14-database-design.md`
- `docs/15-api-design.md`
- `docs/16-security-and-permissions.md`
- `docs/17-testing-strategy.md`
- `docs/18-deployment-and-devops.md`
- `docs/19-observability-and-support.md`
- `docs/20-risk-register.md`
- `docs/21-implementation-roadmap.md`
- `docs/22-implementation-guardrails.md`

### Audit And Repair Reports Reviewed

- `docs/audits/pre-implementation-documentation-consistency-audit.md`
- `docs/audits/pre-implementation-documentation-consistency-repair.md`
- `docs/audits/pre-implementation-documentation-consistency-audit-v2.md`

### Architecture Decision Records Reviewed

- `docs/decisions/README.md`
- `docs/decisions/ADR-001-use-clean-architecture.md`
- `docs/decisions/ADR-002-use-sql-server.md`
- `docs/decisions/ADR-003-use-angular.md`
- `docs/decisions/ADR-004-use-role-based-access-control.md`
- `docs/decisions/ADR-005-use-entity-framework-core.md`
- `docs/decisions/ADR-006-use-azure-openai-compatible-provider-abstraction.md`
- `docs/decisions/ADR-007-use-rag-with-source-citations.md`
- `docs/decisions/ADR-008-use-asynchronous-document-processing.md`
- `docs/decisions/ADR-009-use-mermaid-for-architecture-diagrams.md`
- `docs/decisions/ADR-010-use-organization-scoped-access-boundaries.md`

### Prompt Harness Reference Check

- `11-prompt-levels.md` was not found in the repository.
- `12-prompt-classifier.md` was not found in the repository.
- No external reference file contents were supplied in the repository for review. The requested structural concepts in this audit brief were used as requirements only, not as product content.

### Missing Optional Or Pending Harness Files

- `docs/agent/prompt_classifier.txt` - not present; previously identified as optional/pending.
- `docs/agent/codex_prompt_levels_templates.txt` - not present; previously identified as optional/pending.
- `docs/agents/` - not present; this is expected because its creation is the subject of the next activity.

No missing optional harness file blocks generation of the new agent context layer.

## 3. Executive Summary

**Status: READY WITH DECISIONS**

KnowledgeOps-AI has a sufficiently consistent canonical documentation set, approved implementation roadmap, and implementation guardrails to support a focused agent context harness. The second-pass documentation audit confirmed that the earlier security, role, lifecycle, API, and scope contradictions were resolved.

The agent context layer can be generated after accepting the decisions documented in this report:

- Use `docs/agents/`, not `docs/agent/`, as the canonical harness root.
- Use modular context summaries plus canonical-document escalation rather than one large all-purpose file.
- Create `docs/agents/12-prompt-levels.md` and `docs/agents/13-prompt-classifier.md`.
- Use prompt Levels 0 through 3; do not introduce Level 4 now.
- Include all proposed specialist subagents and add a dedicated `rag-implementation-agent.md`.
- Create and actively use all four progress files before implementation prompts begin.

No contradiction in existing canonical documentation must be fixed before generating the harness.

## 4. Recommended Agent Context Structure

Use the proposed structure with one adjustment: add a dedicated RAG/AI implementation subagent because grounding, retrieval authorization, prompt construction, citations, insufficient-context behavior, provider isolation, and AI observability form a security-sensitive specialist area.

```text
docs/
  agents/
    00-agent-operating-protocol.md
    01-project-context.md
    02-architecture-context.md
    03-domain-context.md
    04-business-rules-context.md
    05-testing-and-validation-context.md
    06-frontend-context.md
    07-backend-context.md
    08-devops-context.md
    09-observability-context.md
    10-issue-execution-template.md
    11-pr-review-template.md
    12-prompt-levels.md
    13-prompt-classifier.md

    subagents/
      architecture-auditor.md
      backend-implementation-agent.md
      frontend-implementation-agent.md
      database-agent.md
      rag-implementation-agent.md
      testing-agent.md
      documentation-agent.md
      verification-agent.md

    progress/
      current-state.md
      decisions-log.md
      open-risks.md
      completed-issues.md
```

Design principles for the structure:

- Context files summarize canonical documents; they do not replace canonical contracts.
- Agents start with small routed context sets and open canonical documents only when the task requires exact behavior, identifiers, tables, endpoints, permissions, or ADR decisions.
- Subagent use is sequential and justified by task complexity; it is not a default fan-out mechanism.
- Progress files are operational inputs to issue prompts and validation, not decorative logs.

## 5. Folder Naming Decision

**Decision: Use `docs/agents/`. Avoid `docs/agent/` for the new harness.**

Rationale:

- The harness contains multiple shared agent context files, multiple specialist subagent definitions, and multiple progress records.
- The plural name communicates that the directory is an agent operating system rather than one agent-specific prompt.
- It groups prompt levels and the classifier with the contexts they route.
- It prevents future ambiguity between older optional/pending references to singular `docs/agent/` files and the canonical harness.
- It supports a clean layout for `subagents/` and `progress/`.

Treatment of earlier optional paths:

- `docs/agent/prompt_classifier.txt` and `docs/agent/codex_prompt_levels_templates.txt` were never created and are not canonical.
- Their intended function should be fulfilled by `docs/agents/12-prompt-levels.md` and `docs/agents/13-prompt-classifier.md`.
- Existing roadmap and guardrail references to the absent optional files do not block this decision; future documentation updates may identify the plural harness as the adopted replacement once it exists.

## 6. Agent Context File Plan

Context files should remain concise routing aids. They must point agents back to exact canonical documents when an implementation task needs a contract-level answer.

### `00-agent-operating-protocol.md`

| Field | Plan |
| --- | --- |
| Purpose | Define universal behavior for all AI agents and subagents working on KnowledgeOps-AI. |
| Source documents | `docs/05-scope-and-roadmap.md`, `docs/21-implementation-roadmap.md`, `docs/22-implementation-guardrails.md`, ADR index, audit v2. |
| Must include | Repository files as source of truth; issue-scoped changes; state uncertainty; do not invent missing requirements; validation reporting; no secrets or real data; scope/ADR escalation rules; when to update progress files. |
| Must avoid | Feature-specific implementation detail duplicated from specialist contexts; unsupported assumptions; any adjacent product model as active scope. |
| Used by task types | Every task and every subagent invocation. |
| Token-saving role | Provides a small universal policy layer so each task does not reload full governance documents. |

### `01-project-context.md`

| Field | Plan |
| --- | --- |
| Purpose | Give a compact description of what KnowledgeOps-AI is, why it exists, and what MVP does and does not contain. |
| Source documents | `docs/00` through `docs/05`, `docs/20-risk-register.md`, `docs/21-implementation-roadmap.md`, `docs/22-implementation-guardrails.md`. |
| Must include | Internal document-based RAG assistant; contact center/support operations context; value proposition; MVP capabilities; five technical roles; stakeholder-persona distinction; Phase 2/3 boundary; excluded adjacent capabilities. |
| Must avoid | Full requirement catalog; endpoint/schema detail; treating stakeholder labels as RBAC roles; importing unrelated product workflows. |
| Used by task types | All Level 1 through Level 3 tasks; Level 0 only when terminology or scope is touched. |
| Token-saving role | Replaces repeated reading of several business and scope documents for routine orientation. |

### `02-architecture-context.md`

| Field | Plan |
| --- | --- |
| Purpose | Summarize accepted architecture boundaries and ADR-backed constraints. |
| Source documents | `docs/11-architecture-overview.md`, `docs/12-c4-architecture.md`, `docs/13-uml-diagrams.md`, ADR-001 through ADR-010, `docs/22-implementation-guardrails.md`. |
| Must include | Clean Architecture; .NET/API/Worker/Angular/container view; Domain/Application/Infrastructure/API/Worker responsibilities; dependency rules; SQL Server/EF Core placement; provider isolation; organization access boundary; Mermaid source-of-truth note. |
| Must avoid | New architecture decisions; provider-specific implementation commitments not accepted for MVP; production infrastructure promises. |
| Used by task types | Backend, database, AI/RAG, security, DevOps, architecture review, cross-layer and release tasks. |
| Token-saving role | Allows architecture-aware work without routinely loading the full architecture diagrams and all ADR bodies. |

### `03-domain-context.md`

| Field | Plan |
| --- | --- |
| Purpose | Provide canonical domain vocabulary and core invariants. |
| Source documents | `docs/10-domain-model.md` primarily; `docs/14-database-design.md` and ADR-010 for corroborating scope relationships. |
| Must include | `Organization`, `User`, `Role`, `Document`, `DocumentChunk`, `ChunkEmbedding`, `ChatSession`, `ChatInteraction`, `RetrievalResult`, `Citation`, `AnswerFeedback`, `DashboardMetric`, `AuditLogEntry`; document status values; retrieval eligibility predicate; organization ownership; Phase 2 status of `KnowledgeGapSignal`. |
| Must avoid | Recasting `Disabled` as document processing status; making future conceptual entities mandatory MVP storage; inventing fields. |
| Used by task types | Backend, database, document processing, retrieval/RAG, citations, feedback/dashboard, security, testing. |
| Token-saving role | Supplies domain terminology and invariants before exact schema/API pages are needed. |

### `04-business-rules-context.md`

| Field | Plan |
| --- | --- |
| Purpose | Route agents to business rules while retaining `docs/09-business-rules.md` as the only canonical `BR-###` catalog. |
| Source documents | `docs/09-business-rules.md` primarily; `docs/06-requirements.md`, `docs/07-use-cases.md`, and `docs/22-implementation-guardrails.md` for traceability/use. |
| Must include | Statement of canonical BR ownership; concise themes for access, document processing, retrieval eligibility, RAG, citations, insufficient context, feedback, dashboard, observability, AI governance, and scope. |
| Must avoid | Redefining or renumbering BR rules; copying all rule text; inventing new rules; collapsing Phase 2 workflow into MVP. |
| Used by task types | Any implementation or review task affecting behavior, security, data integrity, RAG, tests, acceptance criteria, or documentation traceability. |
| Token-saving role | Identifies relevant rule clusters and tells agents when to open exact BR entries instead of loading the entire rule catalog by default. |

### `05-testing-and-validation-context.md`

| Field | Plan |
| --- | --- |
| Purpose | Define validation depth and required safety checks by work type. |
| Source documents | `docs/17-testing-strategy.md`, `docs/18-deployment-and-devops.md`, `docs/20-risk-register.md`, `docs/21-implementation-roadmap.md`, `docs/22-implementation-guardrails.md`. |
| Must include | Unit/integration/API/frontend/E2E expectations; fake AI providers; no live AI in normal CI; authorization and cross-scope tests; lifecycle/retrieval/RAG/citation/feedback/dashboard/health tests; Definition of Done and reporting rule. |
| Must avoid | Claiming commands exist before implementation; requiring live provider validation in CI; weakening negative security tests. |
| Used by task types | All code/config implementation work, verification/review, CI, release stabilization; documentation tasks when validating no unintended changes. |
| Token-saving role | Avoids rereading the full test strategy for every issue while retaining pointers for exact test cases. |

### `06-frontend-context.md`

| Field | Plan |
| --- | --- |
| Purpose | Guide Angular feature work without turning UI controls into authorization. |
| Source documents | ADR-003, `docs/11-architecture-overview.md`, `docs/12-c4-architecture.md`, `docs/15-api-design.md`, `docs/16-security-and-permissions.md`, `docs/17-testing-strategy.md`, `docs/22-implementation-guardrails.md`. |
| Must include | Angular/TypeScript/RxJS/Router/Reactive Forms direction; intended feature folders; guards/interceptors; login, documents, chat, citation, feedback, dashboard and admin screens; citations and insufficient-context UX; backend authorization boundary. |
| Must avoid | React as an open choice; frontend-only security; screens for deferred workflows; customer-facing UI behavior. |
| Used by task types | Angular foundation, UI feature, frontend test, frontend review, cross-layer UI/API tasks. |
| Token-saving role | Gives UI implementers the approved front-end boundary without reading all architecture and security prose first. |

### `07-backend-context.md`

| Field | Plan |
| --- | --- |
| Purpose | Guide .NET implementation across API, Application, Domain, Infrastructure, and Worker boundaries. |
| Source documents | ADR-001, ADR-002, ADR-004 through ADR-008, ADR-010; `docs/11`, `docs/12`, `docs/14`, `docs/15`, `docs/16`, `docs/22`. |
| Must include | .NET 10/ASP.NET Core; Clean Architecture projects; thin controllers; commands/queries/services; EF Core in Infrastructure; DTO contract boundary; authentication/RBAC/scope; background processing; provider abstractions; RAG orchestration placement. |
| Must avoid | Direct provider or EF Core leakage into Domain/Application; controller business rules; bypassed scope checks; new endpoints outside API contract without review. |
| Used by task types | Backend feature, API, worker, database coordination, auth/security, retrieval/RAG, observability implementation. |
| Token-saving role | Supplies recurring backend conventions; exact endpoint/security/schema documents are loaded only for affected contracts. |

### `08-devops-context.md`

| Field | Plan |
| --- | --- |
| Purpose | Provide safe environment, container, CI, and Azure-ready delivery constraints. |
| Source documents | `docs/18-deployment-and-devops.md`, `docs/17-testing-strategy.md`, `docs/19-observability-and-support.md`, ADR-006, `docs/21`, `docs/22`. |
| Must include | Docker/local SQL Server; GitHub Actions; fictional data; configuration/secrets strategy; fake-provider CI; Azure-ready posture; migrations and rollback caution; environment separation. |
| Must avoid | Hardcoded secrets; production-grade cloud delivery as MVP; live provider requirement for normal CI; unsupported pipeline commands. |
| Used by task types | Docker, configuration, CI/CD, local setup, deployment/readme, release tasks. |
| Token-saving role | Routes DevOps work without requiring the entire deployment document except for exact environment/pipeline decisions. |

### `09-observability-context.md`

| Field | Plan |
| --- | --- |
| Purpose | Define operational telemetry and safety expectations for supportable AI knowledge workflows. |
| Source documents | `docs/19-observability-and-support.md`, `docs/16-security-and-permissions.md`, `docs/18-deployment-and-devops.md`, `docs/20-risk-register.md`, `docs/22`. |
| Must include | Correlation IDs; structured events; canonical health routes; processing/extraction/embedding/retrieval/provider failures; latency; cost/token data when available; insufficient-context and feedback signals; authorization/scope failures; audit-sensitive actions; safe logging. |
| Must avoid | Sensitive content logging; raw prompt/chunk exposure; operational features outside internal document/RAG support; details-health exposure to non-admins. |
| Used by task types | Observability, health, dashboard-support, RAG/provider, processing, DevOps and security review tasks. |
| Token-saving role | Supplies telemetry requirements without opening observability, security, and risk documents for every diagnostic change. |

### `10-issue-execution-template.md`

| Field | Plan |
| --- | --- |
| Purpose | Provide a reusable prompt template for executing one approved implementation issue. |
| Source documents | `docs/21-implementation-roadmap.md`, `docs/22-implementation-guardrails.md`, prompt-level and classifier plans in this audit. |
| Must include | Task classification; related issue/sprint; context to load; scope; out of scope; files to inspect; implementation sequence; acceptance criteria; validation commands/expectations; progress update duties; final response format. |
| Must avoid | Preselected feature scope without an issue; instructions to skip validation; broad document loading by default; uncontrolled subagent fan-out. |
| Used by task types | Future issue execution after an implementation issue is approved. |
| Token-saving role | Standardizes prompts and makes the classifier select only necessary context and specialist assistance. |

### `11-pr-review-template.md`

| Field | Plan |
| --- | --- |
| Purpose | Standardize review of future implementation pull requests against contracts and Definition of Done. |
| Source documents | `docs/22-implementation-guardrails.md`, `docs/21-implementation-roadmap.md`, `docs/17-testing-strategy.md`, security/risk documents. |
| Must include | Summary; related issue/sprint; scope and excluded-scope confirmation; files/areas changed; validation; architecture boundaries; security/scope checks; data/lifecycle/RAG checks where applicable; tests; docs updates; remaining risk; merge recommendation. |
| Must avoid | Generic approval without evidence; accepting frontend checks as security proof; treating unrun tests as passing. |
| Used by task types | PR review, verification, release stabilization. |
| Token-saving role | Ensures review loads only contract domains affected by the PR. |

### `12-prompt-levels.md`

| Field | Plan |
| --- | --- |
| Purpose | Define work complexity levels, baseline context routing, subagent expectations, and validation depth. |
| Source documents | This audit; `docs/21-implementation-roadmap.md`; `docs/22-implementation-guardrails.md`; `docs/17-testing-strategy.md`. |
| Must include | Levels 0-3; when to use; core and specialized contexts; allowed/expected subagents; validation; token-saving rules; anti-hallucination gates; escalation triggers. |
| Must avoid | Product-specific material from unrelated examples; Level 4 unless later justified; using levels to bypass required security context. |
| Used by task types | Every prompt classification and issue template creation. |
| Token-saving role | Establishes the minimum context bundle for each complexity class. |

### `13-prompt-classifier.md`

| Field | Plan |
| --- | --- |
| Purpose | Classify a future task before work begins and prescribe context, agent routing, progress records, and validation. |
| Source documents | This audit; `docs/12-prompt-levels.md` once created; roadmap; guardrails. |
| Must include | Output format; level questions; KnowledgeOps-AI task mappings; escalation rules; recommended subagents; relevant progress files; validation expectation; scope-contamination screen. |
| Must avoid | Automatically enabling subagents for small work; bypassing issue scope; loading all docs indiscriminately. |
| Used by task types | Every future implementation prompt and significant documentation/review prompt. |
| Token-saving role | Becomes the routing gateway that prevents unnecessary full-document reads. |

## 7. Subagent Plan

All future subagent definition files should contain:

- Responsibility.
- Allowed Scope.
- Forbidden Actions.
- Required Context Files.
- Optional Context Files.
- Expected Output.
- Validation Duties.
- Handoff Format.

Subagents should be invoked sequentially only when the classified work benefits from specialization or independent verification. They should not be used for routine Level 0 or Level 1 work.

| Subagent | Should Exist | Responsibility | Required Context | Forbidden Actions | Validation Duties |
| --- | --- | --- | --- | --- | --- |
| `architecture-auditor` | Yes | Check Clean Architecture boundaries, ADR alignment, cross-layer design, MVP scope, dependency direction, and significant contract changes. | `00`, `01`, `02`, relevant ADRs, `docs/21`, `docs/22`; relevant specialist context. | Implement broad feature changes; silently override ADRs; expand MVP. | Report boundary/ADR/scope findings and required documentation decisions before implementation proceeds. |
| `backend-implementation-agent` | Yes | Implement scoped .NET API/Application/Domain/Infrastructure/Worker work not primarily owned by specialist database or RAG concerns. | `00`, `01`, `02`, `03`, `04`, `05`, `07`, relevant API/security docs and progress state. | Put rules in controllers; leak EF/provider types inward; bypass authorization; add unscoped endpoints. | Run relevant backend tests/builds; confirm scope/security/layer boundaries; hand off affected contracts. |
| `frontend-implementation-agent` | Yes | Implement scoped Angular UI, service, routing, forms, citations/feedback/display and frontend tests. | `00`, `01`, `05`, `06`, affected API/security docs and progress state. | Treat UI hiding as security; use an unapproved framework; create deferred screens as MVP. | Run Angular validation when available; verify API-bound behavior and visible safety states. |
| `database-agent` | Yes | Implement or review SQL Server/EF Core schema, migrations, indexes, query integrity and lifecycle/scope persistence. | `00`, `01`, `02`, `03`, `04`, `07`, `docs/14`, ADR-002/005/010, progress state. | Introduce `Disabled` processing state; make destructive changes without issue justification; leak EF Core inward; create deferred tables casually. | Validate migration/schema behavior locally when applicable; verify relationships, indexes, lifecycle, scope and traceability. |
| `rag-implementation-agent` | Yes | Implement or review embeddings, retrieval eligibility, vector abstractions, prompt builder, RAG orchestration, citations, insufficient-context behavior and relevant AI telemetry. | `00`, `01`, `02`, `03`, `04`, `05`, `07`, `09`, ADR-006/007/010, relevant API/security docs and progress state. | Use unauthorized content; omit citations; invent authority; directly couple core logic to provider SDKs; require live AI in CI. | Validate retrieval-before-generation, authorization-before-prompt, citations, insufficient-context paths, provider failures and fake-provider tests. |
| `testing-agent` | Yes | Design, add, or review deterministic tests and validate test sufficiency against risk and Definition of Done. | `00`, `01`, `05`, affected specialist context, issue/PR and progress state. | Replace implementation ownership unnecessarily; claim unrun tests passed; require live AI in normal CI. | Execute/assess relevant validation, identify missing negative/cross-scope coverage, and report residual risk. |
| `documentation-agent` | Yes | Create/update scoped documentation and traceability while preserving canonical sources and terminology. | `00`, `01`, relevant context and canonical docs, roadmap/guardrails when governance affected. | Alter architecture/scope implicitly; invent requirements; generate unrelated product content. | Search for naming/scope drift, verify links/contracts, state whether code/artifacts were untouched when applicable. |
| `verification-agent` | Yes | Conduct final independent issue/PR/release verification against scope, Definition of Done, tests and security/risk controls. | `00`, `01`, `05`, relevant specialist contexts, `docs/21`, `docs/22`, progress records. | Add feature scope during verification; silently fix material issues without reporting them; certify without evidence. | Check acceptance criteria, validation results, architecture/security/scope rules, progress updates and readiness outcome. |

### Dedicated RAG/AI Subagent Recommendation

**Recommendation: Add `docs/agents/subagents/rag-implementation-agent.md`.**

Rationale:

- RAG work is not merely generic backend work; it crosses retrieval eligibility, organization authorization, AI provider isolation, prompt construction, citations, insufficient-context behavior, token/cost metadata and safe observability.
- Errors in this area can expose unauthorized content to a model, create unsupported answers, omit citations, or undermine the product's central safety promise.
- A dedicated subagent gives Level 3 RAG tasks a precise context path without forcing every backend task to load all AI/RAG material.
- The backend implementation agent remains responsible for ordinary backend features and may collaborate sequentially with the RAG agent for cross-layer API or persistence changes.

No further subagent is recommended now. Security and observability requirements are sufficiently integrated into the architecture, backend, RAG, testing and verification roles for MVP; a separate security subagent can be reconsidered if future production-hardening scope is approved.

## 8. Prompt Levels Plan

**Decision: Use Levels 0 through 3. Do not add Level 4 now.**

Level 3 is sufficient for cross-layer, architecture-sensitive, security-sensitive, audit, harness and release-stabilization work in the current MVP. A future Level 4 may be justified only if the project begins multi-sprint production release operations, major platform migration, approved external integration work, or a substantial architecture redesign.

| Level | Meaning For KnowledgeOps-AI | Typical Tasks | Baseline Context | Subagent Guidance | Validation Expectation |
| --- | --- | --- | --- | --- | --- |
| Level 0 | Tiny mechanical or presentation-only change with no product-contract impact. | Typo, whitespace, link correction, minor Markdown formatting. | `00`; target file; `01` only if terminology/scope appears. | None. | Re-read changed file; confirm no unintended scope/content change. |
| Level 1 | Localized documentation or narrowly scoped non-architectural adjustment in one area. | One Markdown update, one ADR wording/update requiring review, one small config/document clarification. | `00`, `01`, affected specialist context or canonical doc, `docs/22` when governance applies. | Documentation agent optional for document-intensive review; no default subagents. | Validate affected document/contract; report touched scope and any not-run tooling. |
| Level 2 | Implementable feature in one main area with bounded contracts and tests. | One backend use case, Angular page/form, one API endpoint with tests, bounded EF entity/migration with no security-sensitive ripple. | `00`, `01`, affected specialist contexts, `05`, relevant canonical docs, `progress/current-state.md`. | Use one specialist subagent only when it materially improves implementation or testing. | Run affected builds/tests; check Definition of Done and update progress. |
| Level 3 | Cross-layer, security-, architecture-, data-integrity-, RAG-safety-, CI-, audit- or release-sensitive work. | Auth/RBAC/scope, worker pipeline, retrieval authorization, RAG orchestration, citations, observability foundation, CI/CD, cross-document audit, harness generation/audit, release stabilization. | Core contexts `00`-`05`, affected specialist contexts, exact canonical docs, relevant ADRs, `docs/21`, `docs/22`, `progress/current-state.md`, `progress/open-risks.md`. | Sequential specialist agents and verification agent as justified; architecture auditor for decision/boundary impact. | Comprehensive affected-layer validation, negative/security tests where applicable, explicit residual risk and progress updates. |

### Level Escalation Rules

- Escalate from Level 0 or 1 when a change affects scope, roles, lifecycle, permissions, API contracts, database design, ADR decisions, testing obligations or release readiness.
- Escalate from Level 2 to Level 3 whenever organization scope, authentication/authorization, retrieval eligibility, prompts, citations, sensitive telemetry, migrations with traceability impact, CI gates or multiple application layers are affected.
- Treat any task that could send document content to an AI provider or expose it in citations/metrics/logs as Level 3.
- Treat generation or modification of the harness itself as Level 3 because it governs future work.

### Level Token And Safety Rules

- Begin with the minimum context bundle for the level and affected area.
- Load exact canonical documents when deciding contracts, permissions, schema fields, BR identifiers or ADR-constrained design.
- Never lower a task level merely to avoid context or validation.
- No level permits inventing requirements, adding deferred scope or bypassing authorization and data-isolation rules.

## 9. Prompt Classifier Plan

Create `docs/agents/13-prompt-classifier.md` as the entry routing document for future prompts.

### Classifier Output Format

The classifier should produce:

```text
Task Classification

- Task type:
- Prompt level:
- Related sprint/issue:
- Implementation scope:
- MVP or deferred scope:
- Primary affected area:
- Security/organization-scope impact:
- AI/RAG impact:
- Data/migration impact:
- Required agent context files:
- Required canonical documents:
- Progress files to read/update:
- Recommended subagent(s):
- Validation expectations:
- Escalation or blocker notes:
```

### Classification Questions

1. Is the requested work documentation-only, implementation, review, verification, DevOps, or release work?
2. Is there an approved issue and related sprint, or is this still planning/governance work?
3. Does the task affect scope, ADRs, domain terms, BRs, API/security/database contracts or release gates?
4. Does it affect authentication, roles, organization scope, sensitive data, citations, health or audit access?
5. Does it affect document processing, retrieval eligibility, RAG prompts, AI providers, insufficient context or AI telemetry?
6. Does it require migrations, CI changes, multi-layer edits or extensive validation?
7. Which concise context files are sufficient initially, and which canonical documents must be opened for exact decisions?
8. Are progress records required to identify state, risk, prior decisions or completed work?

### KnowledgeOps-AI Fast Mapping

| Task Example | Level | Primary Agent Or Subagent | Mandatory Routing Note |
| --- | ---: | --- | --- |
| Typo or formatting correction | 0 | Direct or documentation agent | Read target file; do not broaden scope. |
| Single Markdown document update | 1 | Documentation agent optional | Load relevant context and source canonical doc. |
| ADR update | 1, escalate to 3 if decision changes | Architecture auditor plus documentation agent if needed | Accepted architectural decision changes require explicit review. |
| One backend command/use case | 2 | Backend implementation agent | Add validation and load exact affected contracts. |
| One Angular form/page | 2 | Frontend implementation agent | UI permissions are not security; load API/security when protected. |
| One API endpoint with tests | 2, or 3 if protected/RAG/cross-scope | Backend plus testing as needed | Use DTO/error/security contracts. |
| EF Core entity or migration | 2, or 3 for scoped/traceability/destructive changes | Database agent | Preserve lifecycle, organization scope and history. |
| Document processing worker | 3 | Backend and testing agents; architecture auditor if design changes | Preserve asynchronous lifecycle and non-retrievability before eligibility. |
| Authentication, RBAC or organization-scope framework | 3 | Backend, testing and verification agents | Security document is canonical; deny by default. |
| Retrieval authorization | 3 | RAG, backend, testing and verification agents | Apply authorization before prompts and prevent cross-scope leakage. |
| RAG orchestration or prompt builder | 3 | RAG and testing agents; verification agent | Require grounding, citations and safe insufficient-context behavior. |
| Citation pipeline | 3 | RAG, database/testing as applicable | Preserve source traceability and authorized visibility. |
| Feedback or bounded dashboard UI/API | 2, escalate to 3 for scope/security aggregation | Backend/frontend/testing as applicable | Do not implement full knowledge-gap workflow. |
| Observability foundation or sensitive health/audit behavior | 3 | Backend or DevOps, testing and verification agents | Sanitize data and restrict details access. |
| CI/CD pipeline | 3 | DevOps plus testing/verification agents | No normal-CI live AI dependency or committed secrets. |
| Cross-document documentation audit | 3 | Documentation agent and verification agent | Check canonical alignment and report findings. |
| Agent harness audit or generation | 3 | Documentation agent with verification | Do not use not-yet-approved harness to justify itself. |
| Release stabilization | 3 | Verification plus affected specialists | Level 4 is not necessary for MVP now. |

### Classifier Escalation Triggers

- Any uncertainty about whether a feature is MVP or deferred.
- A request that conflicts with `docs/21` or `docs/22`.
- A potential ADR change.
- A new role, endpoint, status, table, provider coupling or access model.
- A request that could expose cross-organization, source-document, prompt, citation, audit or secret data.
- A claimed validation path that cannot be run or is not yet defined.

## 10. Progress Files Plan

**Decision: Create all four progress files with the harness and require their use before implementation prompts begin.**

Without progress records, prompts can know the canonical design but still miss current sprint state, recently made implementation decisions, unresolved delivery risk or completed validation. Progress records should remain short, current and referenced by prompts rather than becoming duplicated design documents.

### `progress/current-state.md`

| Field | Plan |
| --- | --- |
| Purpose | Provide the immediate implementation state an agent needs before starting an issue. |
| Must track | Current implementation phase; current sprint; last completed sprint; active issue/PR if any; architectural status; current known limitations; next recommended action; last updated date. |
| Initial content when harness is generated | Documentation and governance are prepared; implementation has not begun unless separately confirmed; no active implementation issue unless one exists at generation time. |
| Read when | Every Level 2 or 3 implementation task, issue execution, verification, and release task; Level 1 where roadmap/governance progress changes. |
| Update when | Starting/completing an implementation issue, changing current sprint status, or recording a material blocker/readiness change. |
| Must avoid | Detailed code history, speculative completion claims, or canonical rule duplication. |

### `progress/decisions-log.md`

| Field | Plan |
| --- | --- |
| Purpose | Record implementation-time choices that are below or approaching ADR significance. |
| Must track | Date; decision; rationale; affected issue/sprint; affected docs/code; alternatives considered where useful; whether an ADR/update is required. |
| Read when | Level 2/3 work that touches an existing decision area or might make a new technical choice. |
| Update when | A nontrivial implementation choice is made or an issue identifies a decision needing ADR review. |
| Must avoid | Silently replacing ADRs; repeating all accepted ADR content; logging transient coding details. |

### `progress/open-risks.md`

| Field | Plan |
| --- | --- |
| Purpose | Keep active implementation risks visible in prompts and reviews. |
| Must track | Risk; severity; affected sprint/issue; owner or next action; mitigation status; link to canonical risk category if applicable. |
| Read when | Level 3 tasks; security/RAG/data/DevOps work; reviews; release stabilization. |
| Update when | A new risk is discovered, mitigation changes, or a risk is closed/transferred. |
| Must avoid | Duplicating the whole risk register; hiding blocked risks in prose; adding unsupported product concerns. |

### `progress/completed-issues.md`

| Field | Plan |
| --- | --- |
| Purpose | Provide concise delivery evidence and avoid redoing or misrepresenting completed work. |
| Must track | Completed issue; related sprint; short summary; validation performed; merged PR/reference if available; follow-up items or known limitations. |
| Read when | Beginning dependent work, verification, audit, or release stabilization. |
| Update when | An implementation issue is complete and its outcome is verified/merged according to workflow. |
| Must avoid | Marking incomplete work complete; storing large PR transcripts; treating follow-ups as already delivered. |

### Progress Integration With Prompts

- `10-issue-execution-template.md` must require reading `current-state.md` and require updates to affected progress files at completion.
- `11-pr-review-template.md` must check whether progress updates match delivered behavior and validation.
- `13-prompt-classifier.md` must state which progress files are required for the classified work.
- Level 0 tasks normally do not read or update progress.
- Level 1 tasks update progress only when they change governance, a roadmap state or a recorded decision.
- Level 2 tasks read current state and update completion/decisions/risk records where applicable.
- Level 3 tasks read current state and open risks by default, and update all applicable progress files after validation.

## 11. Context Routing Matrix

Context file numbers below refer to future files under `docs/agents/`. Exact canonical documents remain authoritative where a task changes a contract.

| Task Type | Prompt Level | Required Agent Context Files | Relevant Canonical Docs | Recommended Subagents | Validation |
| --- | ---: | --- | --- | --- | --- |
| Tiny doc edit | 0 | `00`; target file only, `01` if terminology/scope affected | Target document | None | Re-read edited Markdown; confirm no scope/contract drift. |
| Single doc update | 1 | `00`, `01`, affected specialist context | Target doc; `docs/22` if governance; related canonical source | Documentation optional | Re-read, terminology/scope scan, links/contract consistency. |
| ADR update | 1 or 3 if decision changes | `00`, `01`, `02`, `10`/`11` where applicable | ADR index; affected ADR; related architecture docs | Architecture auditor; documentation/verification as needed | Confirm decision status, downstream docs and explicit approval. |
| Backend feature | 2 | `00`-`05`, `07`, `progress/current-state` | `docs/09`, `docs/10`, `docs/15`, `docs/16`, affected ADRs | Backend; testing if needed | Unit/API/integration tests, layer and security review. |
| Frontend feature | 2 | `00`, `01`, `05`, `06`, `progress/current-state` | `docs/15`, `docs/16`, `docs/17`, ADR-003 | Frontend; testing if needed | Angular tests/build where present; UI states; backend authority check. |
| Database/migration | 2 or 3 | `00`-`05`, `07`, `progress/current-state`; `open-risks` for Level 3 | `docs/10`, `docs/14`, `docs/16`, ADR-002/005/010 | Database; testing; architecture for contract changes | SQL Server migration/schema validation, scope/traceability/integrity checks. |
| Document processing | 3 | `00`-`05`, `07`, `08`, `09`, `progress/current-state`, `progress/open-risks` | `docs/06`-`10`, `docs/14`-`19`, ADR-008 | Backend, testing, verification; database if schema affected | Async lifecycle, failure reason, eligibility exclusion, telemetry and security tests. |
| Retrieval/RAG feature | 3 | `00`-`05`, `07`, `09`, `progress/current-state`, `progress/open-risks` | `docs/09`, `docs/10`, `docs/14`-`20`, ADR-006/007/010 | RAG, testing, verification; backend/database as affected | Authorized retrieval before prompt, fake providers, citations, insufficient-context and cross-scope tests. |
| Citation feature | 3 | `00`-`05`, `07`, `09`, progress state/risks | `docs/09`, `docs/10`, `docs/14`-`17`, ADR-007/010 | RAG, database/testing, verification | Traceability, persisted references, unauthorized-source denial. |
| Feedback/dashboard feature | 2 or 3 for scoped aggregation/security impact | `00`, `01`, `03`-`05`, `06` or `07`, `09`, `progress/current-state` | `docs/06`-`10`, `docs/14`-`17`, `docs/19` | Backend/frontend/testing; verification for Level 3 | Feedback duplication/scope; dashboard scope, cost-null and insufficient-context metrics. |
| Auth/RBAC/scope | 3 | `00`-`05`, `07`, `progress/current-state`, `progress/open-risks` | `docs/04`, `docs/06`-`10`, `docs/15`-`17`, ADR-004/010 | Backend, testing, verification, architecture if model changes | Deny-by-default, five roles, direct API denial, cross-organization tests. |
| Observability | 3 | `00`, `01`, `02`, `05`, `07`-`09`, state/risks | `docs/15`-`20`, `docs/22` | Backend or DevOps, testing, verification | Safe logs/health/audit access, correlation, sensitive-content exclusion. |
| DevOps/CI | 3 | `00`, `01`, `05`, `08`, `09`, state/risks | `docs/17`-`19`, `docs/21`, `docs/22`, ADR-006 | DevOps role through direct agent; testing and verification | Build/test pipeline validation, fake-provider CI, no secrets, Docker/setup checks. |
| Cross-document audit | 3 | `00`-`05`, relevant specialist contexts, state/risks when implementation status matters | All affected docs/ADRs/audit history | Documentation, verification; architecture if decisions affected | Evidence-based findings; confirm no unrequested implementation change. |
| Release stabilization | 3 | `00`-`09`, `10`, `11`, all progress records | `docs/17`-`22`, ADRs, affected contracts | Verification plus affected specialists sequentially | Definition of Done, release gates, CI/test evidence, scope and risk disposition. |

## 12. Token Optimization Rules

- Do not load all canonical documentation for every task.
- Start with `00-agent-operating-protocol.md`, the project context when needed, and only the specialist context files selected by classification.
- Load the affected canonical document when the task requires an exact rule, endpoint, permission, schema field, lifecycle transition, testing expectation or ADR decision.
- For Level 0 work, read only the operating protocol when needed and the target file unless product terminology is touched.
- For Level 1 work, read only the local context and directly relevant canonical source.
- For Level 2 and Level 3 work, read `progress/current-state.md`; add `progress/open-risks.md` when risk/security/cross-layer behavior matters.
- Load source-code files only in areas related to an approved implementation issue.
- Use context summaries for orientation, not as a substitute for verifying exact canonical contracts.
- Use `docs/09-business-rules.md` selectively for applicable `BR-###` rules rather than copying all rules into prompts.
- Use subagents only where complexity or independent verification justifies their context cost.
- Do not use subagents for Level 0 or routine Level 1 work unless explicitly requested or verification has unusual risk.
- Prefer one specialist agent followed by verification for Level 3 work over broad parallel fan-out.
- Keep prompt outputs concise: summarize findings, validations and changed files rather than pasting full documents.
- Keep progress records concise and factual so they reduce rediscovery rather than becoming another large documentation corpus.

## 13. Anti-Hallucination Rules

- Repository files are the source of truth; context summaries route to them but do not override them.
- Do not invent missing files, requirements, endpoints, entities, roles, statuses, tests, migrations or architecture decisions.
- Do not expand MVP scope without explicit approved documentation changes.
- State uncertainty and inspect the applicable canonical document before making a contract-sensitive change.
- Inspect existing repository conventions and current implementation state before editing.
- Do not claim tests, builds, lint checks or validations passed unless they were run successfully.
- Do not assume a live AI provider, Azure environment, secrets or external integration is available.
- Do not require live AI calls in normal CI.
- Do not treat frontend visibility as security; backend authorization is authoritative.
- Do not bypass role permission or organization-scope checks.
- Do not use unauthorized, ineligible or cross-organization document content for retrieval, prompt construction, citations or AI-provider requests.
- Do not treat retrieval disablement as a `Disabled` document processing status.
- Do not convert the Phase 2 knowledge-gap review workflow into an MVP requirement.
- Do not add a technical RBAC role beyond `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, and `Admin` without approved documentation and any needed ADR change.
- Do not log or expose secrets, tokens, passwords, connection strings, raw provider data, full prompt content or protected document text.
- When a task conflicts with canonical documentation or ADRs, report the conflict and request/record the required decision rather than silently choosing a new behavior.

## 14. Forbidden Scope and Terminology

Future context files must describe only KnowledgeOps-AI and its approved phases. The following content is forbidden as MVP product or implementation scope:

| Forbidden As MVP Scope | Acceptable Usage |
| --- | --- |
| `OpsSphere` as the product identity | Only in an explicit anti-contamination warning stating that KnowledgeOps-AI is not that product. |
| Ticket lifecycle, ticket queues, ticket assignment, ticket comments, ticket escalation or ticket closure | Only as explicit exclusions or future-scope rejection examples. |
| Ticket SLA tracking or SLA-driven workflows | Only as an explicit excluded capability; operational monitoring and latency metrics are the accepted terms for MVP. |
| Customer ticket management | Only as an out-of-scope warning. |
| Customer-facing chatbot | Only as out of scope or as a future decision requiring approved scope change. |
| Live agent assist | Only as out of scope or explicitly future/deferred strategy consideration. |
| Real-time call transcription | Only as out of scope or explicitly future/deferred strategy consideration. |
| Autonomous workflow actions or automatic policy enforcement | Only as out of scope. |
| Full contact center platform replacement | Only as out of scope. |
| Full knowledge-gap review workflow in MVP | Only as Phase 2 deferred scope. |
| Dedicated QA, Trainer or Viewer RBAC roles in MVP | Only as explicitly deferred future role considerations or stakeholder/persona descriptions. |

Preferred KnowledgeOps-AI terms:

- Internal document-based RAG knowledge assistant.
- Document processing, retrieval eligibility and grounded answers.
- Source citations and insufficient-context handling.
- Operational monitoring, supportability, response latency, processing health, retrieval quality and AI cost visibility.
- Approved five-role RBAC and organization-scoped access.

## 15. Decisions Required Before Generation

No blocking decisions remain.

The following decisions are recommended as accepted defaults for generation of `docs/agents/`:

| Decision | Recommendation | Blocking If Not Accepted |
| --- | --- | --- |
| Harness root folder | Use `docs/agents/`; do not create a new singular `docs/agent/` harness. | The next generation task should confirm or override before writing files. |
| Context file set | Generate `00` through `13` exactly as routed in this report. | The harness should not be partially generated without deciding its routing surface. |
| Prompt levels | Use Levels 0-3 only. | No current need for Level 4. |
| Prompt classifier location | Create `docs/agents/13-prompt-classifier.md`. | Required for the proposed harness workflow. |
| Subagent set | Use the seven proposed subagents plus `rag-implementation-agent.md`. | A harness without RAG specialization remains usable, but is not recommended for this security-sensitive product. |
| Progress records | Create all four progress files before implementation prompts begin. | Required for usable issue-state and risk routing once implementation starts. |
| Prior optional singular files | Do not create `docs/agent/prompt_classifier.txt` or `docs/agent/codex_prompt_levels_templates.txt`; supersede their intent through the plural harness. | Not blocking to existing documentation; establishes the future canonical location. |

## 16. Readiness Recommendation

**Generate after accepting decisions in this audit.**

The repository is documentation-ready for the agent context layer. Generation should create the modular `docs/agents/` hierarchy, including the classifier, prompt levels, specialist subagents, and initialized progress records described in this report.

No application-code, architecture-contract or scope repair is required before generation.

## 17. Suggested Next Step

Create the KnowledgeOps-AI agent context harness under `docs/agents/` according to this audit, including:

- Context files `00` through `13`.
- The recommended specialist subagents, including `rag-implementation-agent.md`.
- The four initialized progress files.

That generation task should remain documentation-only and should re-read this audit, `docs/21-implementation-roadmap.md`, and `docs/22-implementation-guardrails.md` before writing the harness.
