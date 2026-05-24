# ADR-002: Use SQL Server

## Status

Accepted

## Context

KnowledgeOps-AI requires persistent storage for users, roles, organizations, documents, document chunks, chat interactions, retrieval results, citations, feedback, dashboard metrics, and audit records.

The system is intended to demonstrate enterprise .NET development skills and align with common Microsoft enterprise technology stacks.

The project also needs relational integrity for important business relationships such as:

- Users belonging to organizations.
- Documents belonging to organizations.
- Chunks belonging to documents.
- Chat interactions belonging to users.
- Feedback belonging to chat interactions.
- Citations referencing documents and chunks.

## Decision

KnowledgeOps-AI will use **SQL Server** as the primary relational database.

SQL Server will store core application and business data.

Vector search may be implemented through SQL Server vector capabilities if suitable, or through a separate vector-capable retrieval service while SQL Server preserves relational traceability.

## Consequences

Positive consequences:

- Strong fit with .NET enterprise development.
- Supports relational integrity.
- Works well with Entity Framework Core.
- Familiar to enterprise recruiters and reviewers.
- Supports transactional workflows.
- Enables structured dashboard queries.
- Aligns with Azure SQL for future cloud deployment.

Negative consequences:

- Pure vector search capabilities may require additional design depending on selected SQL Server version and provider strategy.
- Advanced search may eventually require Azure AI Search or another dedicated retrieval service.
- SQL Server adds operational requirements for local development and CI.

## Alternatives Considered

### PostgreSQL

PostgreSQL is strong and has mature vector extensions, but SQL Server better supports the Microsoft enterprise positioning of this portfolio project.

### SQLite

SQLite would simplify local development but would not represent the intended enterprise target environment.

### NoSQL Database

A document database was considered but rejected because the domain has strong relational structure and traceability requirements.

### Vector Database Only

A vector-only database would not satisfy user, role, document lifecycle, chat history, feedback, citation, and audit requirements.