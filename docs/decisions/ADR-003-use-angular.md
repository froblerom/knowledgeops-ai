# ADR-003: Use Angular

## Status

Accepted

## Context

KnowledgeOps-AI requires a frontend for login, chat, document upload, citation review, feedback, dashboard metrics, and administration workflows.

The project is intended to demonstrate enterprise-ready full stack development, especially in a .NET-oriented portfolio.

Angular is commonly used in enterprise environments and pairs well with structured backend APIs, role-based screens, typed services, route guards, interceptors, forms, and dashboard interfaces.

## Decision

KnowledgeOps-AI will use **Angular** for the frontend application.

The frontend will provide:

- Login flow.
- Chat interface.
- Source citation display.
- Feedback controls.
- Document upload.
- Document processing status.
- Dashboard views.
- Admin screens.
- Role-aware navigation.

## Consequences

Positive consequences:

- Strong enterprise positioning.
- Good fit for structured applications.
- TypeScript support improves maintainability.
- Angular services work well with typed API clients.
- Route guards and interceptors support security workflows.
- Angular is valuable for .NET full stack portfolio positioning.

Negative consequences:

- More initial setup than a lightweight frontend.
- Angular has a steeper learning curve than some alternatives.
- UI development may be slower at the beginning.
- Requires discipline to avoid overbuilding frontend abstractions.

## Alternatives Considered

### React

React is flexible and widely adopted. It was considered a strong alternative. Angular was selected because the project benefits from an opinionated enterprise framework and because it aligns well with .NET enterprise portfolio positioning.

### Blazor

Blazor would keep the project in the .NET ecosystem, but Angular provides stronger evidence of modern full stack capability across backend and frontend.

### Minimal Razor Pages

Razor Pages would reduce frontend complexity but would not demonstrate modern enterprise frontend architecture as strongly.