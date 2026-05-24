# Observability Context

## Purpose

Use operational signals to support the internal document/RAG assistant safely without exposing protected source or prompt content.

## Required Signals

- Document upload acceptance/rejection.
- Processing start, completion and failure.
- Text extraction and embedding failures.
- Retrieval completion/failure and retrieval latency.
- AI generation completion/failure and generation latency.
- Total RAG response latency.
- Correlation IDs across related processing and chat work.
- Estimated AI cost and token usage when available.
- Insufficient-context outcomes.
- `Useful`/`NotUseful` feedback signals.
- Authentication, authorization and organization-scope failures.
- Privileged administrative and audit-sensitive actions.

## Health Endpoints

```text
GET /api/v1/health
GET /api/v1/health/details
```

- Basic health exposes safe status only according to deployment policy.
- Detailed health is `Admin`-only and sanitized.

## Safe Logging Rules

- Use structured logs and correlation IDs where practical.
- Use safe error envelopes with correlation identifiers and sanitized user-visible messages.
- Sanitize provider and error details.
- Do not log secrets, tokens, passwords, connection strings or keys.
- Do not log full prompt context or full protected document/chunk text.
- Do not expose sensitive dependency details in basic health.
- Scope reviewable telemetry and metrics by role and organization where applicable.

## MVP Metrics Boundary

MVP may surface processing health, latency, cost/token metadata when available, feedback counts and insufficient-context counts. Detailed knowledge-gap workflow and richer operational dashboards are deferred.

## Sources

- `docs/19-observability-and-support.md`
- `docs/16-security-and-permissions.md`
- `docs/18-deployment-and-devops.md`
- `docs/20-risk-register.md`
- `docs/22-implementation-guardrails.md`
