# ADR-004: Use Role-Based Access Control

## Status

Accepted

## Context

KnowledgeOps-AI handles internal documents, chat history, citations, feedback, dashboard metrics, and administrative functionality.

Different users have different responsibilities:

- Agents ask questions and submit feedback.
- Supervisors review scoped team activity and knowledge gaps.
- Knowledge Administrators upload and manage documents.
- Managers review dashboard metrics.
- Admins manage users, roles, access, health, and audit records.

The system must prevent users from performing actions outside their responsibilities.

## Decision

KnowledgeOps-AI will use **Role-Based Access Control** as the primary permission model for MVP.

The initial roles are:

- Agent
- Supervisor
- KnowledgeAdmin
- Manager
- Admin

Role-based authorization will be combined with organization-scoped access boundaries.

## Consequences

Positive consequences:

- Clear permission model.
- Easy to explain in documentation and portfolio review.
- Fits contact center operations.
- Supports API authorization policies.
- Works well with frontend role-aware navigation.
- Provides a strong security foundation for MVP.

Negative consequences:

- RBAC can become rigid if future permissions become more complex.
- Some users may need multiple roles.
- Fine-grained permission requirements may require a later permission-claim model.
- Admin scope must be carefully documented to avoid accidental cross-organization access.

## Alternatives Considered

### Attribute-Based Access Control

ABAC was considered for more flexible policies but rejected for MVP because it would add unnecessary complexity.

### Permission-Only Model

A direct permission model was considered but would be heavier to manage early.

### No Role Model in MVP

Rejected because internal documents and dashboard metrics require protected access from the beginning.