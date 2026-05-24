# Domain Context

## Purpose

Use canonical KnowledgeOps-AI language and invariants. Open `docs/10-domain-model.md` or `docs/14-database-design.md` for exact attributes, relationships, constraints or persistence work.

## Core Concepts

| Concept | Meaning |
| --- | --- |
| `Organization` | Primary MVP data-access boundary. |
| `User` | Authenticated internal user with status and role assignments. |
| `Role` | One of the approved MVP RBAC roles. |
| `Document` | Uploaded internal knowledge source with processing and retrieval state. |
| `DocumentChunk` | Source-traceable extracted text segment used for retrieval. |
| `ChunkEmbedding` | Searchable vector data or reference associated with a chunk. |
| `ChatSession` | Grouping for a user's chat interactions. |
| `ChatInteraction` | Stored question/answer or insufficient-context event and metadata. |
| `RetrievalResult` | Retrieved authorized source-chunk result for an interaction. |
| `Citation` | User-visible reference from an answer to an authorized source. |
| `AnswerFeedback` | `Useful` or `NotUseful` evaluation tied to an interaction. |
| `DashboardMetric` | Scoped derived operational measurement or optional snapshot representation. |
| `AuditLogEntry` | Safe record of audit-sensitive behavior. |

## Roles

MVP technical roles are `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, and `Admin` only.

## Document Lifecycle

`processing_status` values:

```text
Uploaded
Processing
Processed
Failed
```

- Upload creates metadata and initial `Uploaded` status.
- Worker changes status to `Processing`, then `Processed` or `Failed`.
- Failed processing stores a safe failure reason where applicable.
- Do not use `Disabled` as a document processing status.

## Retrieval Eligibility

A document is retrievable only when:

```text
processing_status = Processed
is_retrieval_enabled = true
deleted_at IS NULL, where soft delete applies
organization scope authorizes access
```

Disablement from retrieval means setting `is_retrieval_enabled = false` without changing processing status.

## Knowledge Gap Boundary

`KnowledgeGapSignal` is a Phase 2 conceptual or deferred entity. MVP records insufficient-context outcomes and `NotUseful` feedback and exposes basic scoped dashboard counts; it does not require a full review workflow.

## Canonical Sources

- Domain language: `docs/10-domain-model.md`.
- Logical persistence: `docs/14-database-design.md`.
- Security scope: `docs/16-security-and-permissions.md` and ADR-010.

