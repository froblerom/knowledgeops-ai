# Architecture Decision Records

This folder contains Architecture Decision Records for **KnowledgeOps-AI**.

ADRs document significant architectural decisions, the context behind them, the tradeoffs they introduce, and the alternatives considered.

ADRs are not requirements. Requirements define what the system must do. ADRs explain why a technical direction was selected.

## ADR Format

Each ADR should use the following structure:

- Title
- Status
- Context
- Decision
- Consequences
- Alternatives Considered

## Status Values

Recommended status values:

- Proposed
- Accepted
- Superseded
- Deprecated

## Decision Index

| ADR | Title | Status |
|---|---|---|
| ADR-001 | Use Clean Architecture | Accepted |
| ADR-002 | Use SQL Server | Accepted |
| ADR-003 | Use Angular | Accepted |
| ADR-004 | Use Role-Based Access Control | Accepted |
| ADR-005 | Use Entity Framework Core | Accepted |
| ADR-006 | Use Azure OpenAI-Compatible Provider Abstraction | Accepted |
| ADR-007 | Use RAG with Source Citations | Accepted |
| ADR-008 | Use Asynchronous Document Processing | Accepted |
| ADR-009 | Use Mermaid for Architecture Diagrams | Accepted |
| ADR-010 | Use Organization-Scoped Access Boundaries | Accepted |

## Guidance for Developers and AI Agents

Developers and AI coding agents must review relevant ADRs before making architecture-sensitive changes.

ADRs should be updated or superseded when a major technical decision changes.

AI agents must not silently override accepted ADRs during implementation.