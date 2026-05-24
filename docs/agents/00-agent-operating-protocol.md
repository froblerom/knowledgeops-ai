# Agent Operating Protocol

## Purpose

This is the universal operating protocol for AI agents and subagents working on **KnowledgeOps-AI**.

## Start Every Task Here

Every task must be classified before editing. For every implementation prompt, the following entry gate is mandatory:

1. Start with `docs/agents/13-prompt-classifier.md` and record the classification.
2. Use `docs/agents/10-issue-execution-template.md` before implementation begins.
3. List the required context files, canonical documents, progress records and source/config/test files before editing.
4. Declare validation expectations before implementation.
5. Load the smallest sufficient context bundle for the classified level and area.
6. Read exact canonical documents when a task affects a contract, rule, role, endpoint, data model, architecture decision, or validation gate.
7. Inspect existing repository conventions and relevant files before editing.
8. Keep implementation work issue-scoped.

## Source Of Truth

- Repository files are the source of truth.
- `docs/22-implementation-guardrails.md` defines implementation boundaries and Definition of Done.
- `docs/21-implementation-roadmap.md` defines planned sequence and MVP completion criteria.
- `docs/09-business-rules.md` is canonical for `BR-###` rules.
- ADRs under `docs/decisions/` are canonical for accepted architecture decisions.
- Context files summarize and route; they do not override canonical documents.

## Required Behavior

- Do not invent missing requirements, files, decisions, endpoints, fields, roles, tests, or results.
- State uncertainty and read the applicable source document before making a consequential assumption.
- Do not expand MVP scope or silently pull deferred work into implementation.
- Do not modify application code unless the task explicitly requires implementation.
- Do not use real data, credentials, secrets, tokens, or production connection strings.
- Do not make unsupported factual claims about the repository or validation results.
- Run relevant validation after changes, or clearly state why validation was not available or not run.
- List changed files and validation in the final response.
- For implementation work, consult `docs/agents/progress/current-state.md`, `decisions-log.md`, `open-risks.md`, and `completed-issues.md` before editing.
- Update `current-state.md` when work starts or completion changes status; update `decisions-log.md` for material implementation decisions; update `open-risks.md` when risks change; update `completed-issues.md` only after verified issue completion.

## Safety Gates

- Backend authorization is authoritative for protected behavior.
- Organization scope must be preserved wherever protected data or retrieval is involved.
- Retrieval and AI tasks must enforce grounding, citations, and insufficient-context behavior.
- Normal CI must not require live AI providers.
- Do not log or expose secrets, full prompts, or protected document text unnecessarily.

## Context Economy

- Do not load all documentation for routine tasks.
- Begin with routed context files and read only directly affected canonical sources.
- Prefer concise summaries of findings and changes over copying large source passages.
- Use subagents only when task complexity or independent verification justifies them.
