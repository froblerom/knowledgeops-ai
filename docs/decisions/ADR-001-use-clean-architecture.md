# ADR-001: Use Clean Architecture

## Status

Accepted

## Context

KnowledgeOps-AI is an enterprise AI-powered internal knowledge assistant for contact centers and support operations.

The system must support document ingestion, document processing, chunking, embeddings, semantic retrieval, RAG answer generation, source citations, feedback, dashboard metrics, authentication, authorization, and observability.

The project also needs to demonstrate senior-level software engineering practices in a portfolio context.

Because the system integrates business rules, persistence, AI providers, storage providers, retrieval infrastructure, and frontend APIs, the architecture needs clear boundaries.

Without clear boundaries, business rules could become scattered across controllers, UI components, provider adapters, or database scripts.

## Decision

KnowledgeOps-AI will use **Clean Architecture** for the backend.

The backend will be organized around the following conceptual layers:

- Domain
- Application
- Infrastructure
- API
- Worker / background processing
- Tests

The Domain layer will contain core business concepts and rules.

The Application layer will orchestrate use cases.

The Infrastructure layer will implement provider-specific concerns such as SQL Server, file storage, AI providers, retrieval providers, observability, and secrets.

The API layer will expose HTTP endpoints and remain thin.

Background processing will be separated from request-facing API workflows where appropriate.

## Consequences

Positive consequences:

- Business rules remain easier to locate and test.
- Controllers remain thin.
- Provider-specific logic is isolated.
- AI, storage, and retrieval implementations can evolve with less impact on business logic.
- The architecture supports unit testing without live external providers.
- The project demonstrates enterprise-grade structure for portfolio review.

Negative consequences:

- More project structure is required.
- More interfaces and mapping code may be needed.
- Developers must understand layer responsibilities.
- Small features may require touching multiple layers.

## Alternatives Considered

### Simple Layered Architecture

A simpler controller-service-repository structure was considered.

It would be faster to start but would likely become harder to maintain as AI, retrieval, document processing, and authorization rules grow.

### Minimal API with Direct Infrastructure Access

This would reduce initial complexity but would risk placing business rules directly in endpoints and make testing more difficult.

### Microservices Architecture

Microservices were rejected for the MVP because they would introduce unnecessary deployment, networking, and operational complexity before the core product value is validated.