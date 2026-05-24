# Prompt Levels

## Purpose

Use Levels 0 through 3 to route KnowledgeOps-AI tasks to the smallest sufficient context, suitable validation and justified specialist assistance. Level 4 is not needed for the current MVP.

## Classification Rules

- Classify before editing by using `docs/agents/13-prompt-classifier.md`.
- Increase the level when work affects scope, accepted architecture, security, organization scope, data integrity, RAG safety, CI/release gates or multiple layers.
- Do not lower a level merely to reduce context or avoid validation.

## Level 0 - Tiny / Mechanical Change

Use for:

- Typo correction.
- Heading correction.
- One link fix.
- Formatting-only adjustment.

Context:

- `docs/agents/00-agent-operating-protocol.md`
- `docs/agents/01-project-context.md`
- Only the file being modified.

Subagents: None.

Validation:

- Re-read the changed file.
- Confirm no product or contract meaning changed.

## Level 1 - Single-Area Documentation Or Localized Change

Use for:

- A single documentation update.
- An ADR documentation task that does not silently change an accepted decision.
- A template or README change.
- A localized configuration explanation.

Context:

- `docs/agents/00-agent-operating-protocol.md`
- `docs/agents/01-project-context.md`
- One relevant specialized context file.
- Target canonical or local document.

Subagents:

- Usually none.
- Optional `verification-agent` when correctness risk merits an independent check.

Validation:

- Re-read changed files.
- Check terminology, scope, links and affected contract consistency.

## Level 2 - Feature Implementation In One Main Area

Use for:

- One backend command or use case.
- One Angular form or page.
- One API endpoint with tests when it is not security- or RAG-sensitive.
- One localized EF entity/configuration or low-impact migration.

Context:

- `docs/agents/00-agent-operating-protocol.md`
- `docs/agents/01-project-context.md`
- `docs/agents/02-architecture-context.md`
- `docs/agents/03-domain-context.md`
- `docs/agents/04-business-rules-context.md`
- `docs/agents/05-testing-and-validation-context.md`
- Relevant specialized context.
- Relevant canonical docs and source files.
- `docs/agents/progress/current-state.md`.
- `docs/agents/progress/decisions-log.md`.
- `docs/agents/progress/open-risks.md`.
- `docs/agents/progress/completed-issues.md`.

Subagents:

- Use the relevant implementation agent only when specialization is justified by complexity or risk.
- Use `testing-agent` or `verification-agent` when risk, review or validation needs justify them.

Validation:

- Run relevant build/test commands where projects exist.
- Check affected Definition of Done items and update applicable progress files.

## Level 3 - Cross-Layer / Architecture-Sensitive Work

Use for:

- Authentication, RBAC or organization scope.
- Document processing worker.
- Retrieval authorization.
- RAG orchestration or prompt construction.
- Citation pipeline.
- Observability foundation or sensitive operational endpoints.
- CI/CD pipeline.
- Database plus API plus test changes.
- Cross-document audit.
- Agent harness change.
- Release stabilization.

Context:

- Context files `00` through `09` as relevant to the affected areas.
- Exact canonical documents and ADRs required by the contracts being changed.
- `docs/agents/progress/current-state.md`.
- `docs/agents/progress/decisions-log.md`.
- `docs/agents/progress/open-risks.md`.
- `docs/agents/progress/completed-issues.md`.

Subagents:

- Sequential subagents only when justified; do not fan out subagent work in parallel.
- Use `architecture-auditor`, the relevant implementation agent, `testing-agent` and `verification-agent` when their roles are justified.
- Use `rag-implementation-agent` for embeddings, retrieval, prompts, RAG, citations or AI safety behavior.

Validation:

- Run broad relevant validation for the affected surfaces, without unrelated commands.
- Include negative authorization or cross-scope testing where relevant.
- State residual risk and update applicable progress records.

## Implementation Entry And Progress Rules

- Every Level 2 or Level 3 implementation prompt must use `docs/agents/10-issue-execution-template.md` after classification and before editing.
- Required context and validation expectations must be recorded before implementation begins.
- Read all four progress files before Level 2 or Level 3 implementation work.
- Update progress records after verified work according to their update rules, including `completed-issues.md` after each completed implementation issue.

## Escalation Rules

- Any scope or ADR decision impact escalates to Level 3.
- Any authorization, organization-scope, retrieval eligibility, prompt context, citation security or sensitive telemetry impact escalates to Level 3.
- A migration is Level 3 when it affects access scope, traceability, lifecycle, destructive data handling or multiple feature areas.
- A feature crossing backend/front-end/data/testing boundaries is Level 3.

## Anti-Hallucination Rules

- Treat repository canonical documents as authoritative.
- Do not invent requirements, roles, statuses, endpoints or validations.
- Do not state tests passed unless run.
- Do not add deferred functionality to MVP.
- Do not use unauthorized context for retrieval, prompts or citations.

## Token-Saving Rules

- Start with level-selected contexts; do not load all docs by default.
- Open exact canonical docs only for affected contracts.
- Use subagents only when justified by complexity or verification risk.
- Summarize evidence and output instead of copying large documents.
