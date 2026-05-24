# Issue Execution Template

## Usage Rule

Every implementation task must use this template. Before implementation begins:

1. Classify the task using `docs/agents/13-prompt-classifier.md`.
2. Complete the `Classification`, `Required Context`, `Scope`, `Out Of Scope`, `Acceptance Criteria`, and `Validation Plan` sections below.
3. Read the routed context and exact canonical documents needed for affected behavior.
4. Consult all four progress records: `current-state.md`, `decisions-log.md`, `open-risks.md`, and `completed-issues.md`.

After verified implementation progress, update each applicable progress record according to its update rule. The final response must report files changed, validation performed, and progress or documentation updates.

## Issue Summary

- Issue ID/title:
- Related roadmap sprint:
- Objective:
- Expected outcome:

## Classification

- Task type:
- Prompt level:
- Primary affected area:
- Security/organization-scope impact:
- AI/RAG impact:
- Data/migration impact:
- Recommended subagent(s), if any:

## Required Context

- Agent context files to read:
- Canonical documents to read for exact contracts:
- Progress files to read:
- Existing source/config/tests to inspect:

## Scope

- In scope:
- Behavioral/contracts affected:
- Files or areas expected to change:

## Out Of Scope

- Deferred phase behavior excluded:
- Architecture/contracts not being changed:
- Prohibited expansion:

## Files To Inspect

- Existing implementation files:
- Existing tests:
- Relevant configuration/documentation:

## Implementation Plan

1. Inspect current conventions and implementation state.
2. Confirm contract and boundary assumptions.
3. Make the smallest issue-scoped change.
4. Add or update validation coverage appropriate to risk.
5. Update documentation/progress records when required.

## Acceptance Criteria

- [ ] Issue-specific acceptance criterion:
- [ ] Scope boundaries remain intact.
- [ ] Security, organization scope and AI/RAG safeguards are preserved where applicable.

## Validation Plan

- Commands/checks to run:
- Negative/security/cross-scope cases:
- Expected limitations or commands unavailable:

## Documentation Updates

- Canonical docs affected, if any:
- Progress files to update on completion:
- ADR review required: yes/no and why.

## Final Response Format

```text
Implementation Result

Status
- COMPLETED / PARTIAL / BLOCKED

Files Changed
- ...

Scope Completed
- ...

Validation
- ...

Progress/Documentation Updated
- ...

Remaining Notes
- ...
```
