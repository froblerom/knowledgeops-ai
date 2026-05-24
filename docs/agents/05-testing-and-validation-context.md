# Testing And Validation Context

## Purpose

Use this context to choose validation proportionate to a task. Never claim validation passed unless the command or review was actually performed.

## Validation Strategy

| Test Type | Primary Coverage |
| --- | --- |
| Unit | Domain/Application rules, lifecycle, eligibility, prompt building, citation mapping, feedback and metric calculations. |
| Integration | SQL Server/EF Core relationships, scoped queries, processing persistence, retrieval/RAG persistence and aggregations. |
| API | HTTP contracts, authentication, authorization, organization scope, safe errors and health routes. |
| Frontend | Angular services/components, forms, guards, UI states, citations, feedback and dashboard presentation. |
| E2E smoke | High-value internal user paths using deterministic seeded data and fake providers. |
| UAT | Business-facing MVP acceptance over approved workflows. |

## Required Safety Coverage

- Authentication and disabled/invalid user paths.
- Five-role permissions and backend denial for unauthorized roles.
- Organization-scope isolation and cross-scope rejection.
- Document upload and asynchronous processing transitions.
- Retrieval exclusion for uploaded, processing, failed, soft-deleted, retrieval-disabled and unauthorized documents.
- Retrieval before generation and authorized prompt context only.
- Citations for grounded answers and no unauthorized citation exposure.
- Safe insufficient-context behavior.
- Feedback association, duplication handling and scope.
- Dashboard permission/scope, latency/cost behavior and signal counts.
- Safe health, error, logging and audit-sensitive behavior.

## AI Test Rules

- Use fake embeddings, fake answer generation and other deterministic provider adapters by default.
- Normal CI must not call live AI providers.
- Optional live validation requires explicit configuration, protected secrets and non-blocking status unless later approved.

## Validation Commands

- Run relevant .NET build/test commands when backend projects exist.
- Run relevant Angular build/lint/test commands when frontend tooling exists.
- Run database/integration validation when migrations or relational behavior are changed.
- Run E2E smoke tests when their required environment and scenario are implemented.
- For documentation-only changes, re-read changed files and run documentation lint only when available.

## Definition Of Done Summary

- Scope and acceptance criteria are met.
- Architecture, security, organization scope and provider boundaries remain intact.
- Relevant positive and negative validation is complete or omissions are stated.
- Documentation and progress records are updated when required.
- No real data or secrets are committed.

## Sources

- `docs/17-testing-strategy.md`
- `docs/18-deployment-and-devops.md`
- `docs/21-implementation-roadmap.md`
- `docs/22-implementation-guardrails.md`

