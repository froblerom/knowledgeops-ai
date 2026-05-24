# ADR-005: Use Entity Framework Core

## Status

Accepted

## Context

KnowledgeOps-AI needs relational persistence for users, roles, organizations, documents, chunks, embeddings metadata, chat interactions, retrieval results, citations, feedback, metrics, and audit logs.

The backend is built with .NET 10 and SQL Server.

The project needs a maintainable way to define entities, relationships, migrations, indexes, and data access patterns.

## Decision

KnowledgeOps-AI will use **Entity Framework Core** as the primary ORM for relational persistence.

EF Core will be used for:

- Entity configuration.
- Relationships.
- Migrations.
- Querying.
- Persistence.
- Integration testing with relational database behavior.

Infrastructure-specific EF Core details will remain in the Infrastructure layer.

## Consequences

Positive consequences:

- Strong fit with .NET and SQL Server.
- Supports migrations.
- Supports strongly typed queries.
- Reduces boilerplate data access code.
- Works well with integration testing.
- Familiar to enterprise .NET reviewers.

Negative consequences:

- Developers must avoid leaking EF Core entities into API contracts.
- Complex queries may need careful optimization.
- EF Core tracking behavior must be understood.
- Large document or vector payloads require careful storage decisions.

## Alternatives Considered

### Dapper

Dapper offers more direct SQL control and performance, but EF Core provides better migration and model management for this project’s scope.

### Raw ADO.NET

Rejected because it would add unnecessary boilerplate for a portfolio MVP.

### No ORM

Rejected because the project benefits from strongly modeled relationships and migrations.