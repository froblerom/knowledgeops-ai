# ADR-010: Use Organization-Scoped Access Boundaries

## Status

Accepted

## Context

KnowledgeOps-AI handles internal documents, chat history, citations, feedback, dashboard metrics, and audit records.

In contact center and support environments, knowledge may differ by client account, business unit, campaign, department, or organization.

Users must not access documents or AI answers based on documents outside their authorized scope.

The system therefore needs a stable access boundary that applies across documents, retrieval, chat, citations, feedback, metrics, and administration.

## Decision

KnowledgeOps-AI will use **Organization** as the primary access boundary for MVP.

Most protected business records will include `organizationId`.

The backend must enforce:

```text
CurrentUser.OrganizationId == Resource.OrganizationId
```

unless a future cross-organization access model is explicitly documented.

Organization scope must apply to:

- Documents.
- Document chunks.
- Embeddings.
- Retrieval results.
- Chat sessions.
- Chat interactions.
- Citations.
- Feedback.
- Dashboard metrics.
- Knowledge gap signals.
- Audit records where applicable.

## Consequences

Positive consequences:

- Clear data isolation model.
- Supports contact center account or business unit boundaries.
- Makes authorization testable.
- Protects retrieval from using unauthorized documents.
- Prevents cross-scope dashboard leakage.
- Supports future tenant-like expansion.

Negative consequences:

- Most tables need organization scope.
- Queries must consistently apply organization filters.
- Tests must cover cross-organization access.
- Future multi-organization users or super-admin roles will require additional design.

## Alternatives Considered

### Global Document Access

Rejected because internal documents may be sensitive and account-specific.

### Role-Only Access Control

Rejected because role permissions do not define which organization’s data a user may access.

### Full Multi-Tenant Model from Day One

Rejected for MVP because it would introduce unnecessary complexity. Organization scope provides a practical starting boundary.