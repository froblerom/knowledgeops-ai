# Observability and Support

## 1. Purpose

This document defines observability and operational support for **KnowledgeOps-AI**, the internal document-based RAG knowledge assistant for contact centers and support operations.

The objective is to diagnose document ingestion, retrieval, grounded answer generation, security boundaries, feedback signals, and platform health without exposing sensitive document or prompt content.

## 2. Observability Principles

- Use structured events with correlation identifiers across API, worker, retrieval, and AI provider calls.
- Measure processing and RAG behavior without logging full document chunks, full prompts, secrets, or unnecessary answer content.
- Scope dashboard and reviewable operational data by authorized organization.
- Treat authorization and organization-scope failures as audit-sensitive security signals.
- Keep provider implementation details behind the accepted provider abstractions.
- Use fake AI providers for normal CI and automated testing.

## 3. Structured Logging

### 3.1 Common Fields

| Field | Purpose |
|---|---|
| `timestamp` | UTC event timestamp. |
| `level` | Log severity. |
| `service` | API, background worker, retrieval service, or provider adapter. |
| `operation` | Stable event name. |
| `correlationId` | End-to-end request or processing correlation. |
| `organizationId` | Scope identifier when safe and required. |
| `userId` | Acting user for authenticated operations when appropriate. |
| `role` | Approved MVP role when relevant. |
| `documentId` | Document reference for ingestion events. |
| `chatInteractionId` | Chat reference for RAG events. |
| `durationMs` | Latency for completed operations. |
| `result` | Succeeded, Failed, Denied, or InsufficientContext. |
| `errorCode` | Safe diagnostic category on failure. |

### 3.2 Events To Capture

| Event | Reason |
|---|---|
| `DocumentUploadAccepted` / `DocumentUploadRejected` | Validate upload volume and failures. |
| `DocumentProcessingStarted` | Track background ingestion start. |
| `TextExtractionFailed` | Diagnose unsupported or corrupt content. |
| `EmbeddingGenerationFailed` | Diagnose provider or data preparation failures. |
| `DocumentProcessingCompleted` / `DocumentProcessingFailed` | Track document readiness and failure reason. |
| `DocumentRetrievalDisabled` / `DocumentRetrievalEnabled` (Phase 2 if re-enable is introduced) | Audit knowledge source availability changes. |
| `RetrievalCompleted` / `RetrievalFailed` | Measure retrieval behavior and dependency failures. |
| `RagAnswerGenerated` / `AiGenerationFailed` | Measure answer flow and provider failures. |
| `InsufficientContextReturned` | Measure gaps in available approved knowledge. |
| `AnswerFeedbackSubmitted` | Track useful and not useful outcomes. |
| `AuthorizationFailure` / `OrganizationScopeFailure` | Detect access boundary failures. |
| `UserRoleChanged` | Audit privilege changes. |
| `HealthDetailsViewed` / `AuditLogViewed` | Audit sensitive administrative visibility. |

### 3.3 Safe Event Example

```json
{
  "timestamp": "2026-05-23T18:40:00Z",
  "level": "Information",
  "service": "KnowledgeOps.Api",
  "operation": "RagAnswerGenerated",
  "correlationId": "d0d56c5b6f0d45c8b2d81e5999d98003",
  "organizationId": "2c75ec98-80d9-44e2-8e65-7461df9e1001",
  "chatInteractionId": "9363d246-7b11-4c19-95ea-c99a729b9e31",
  "durationMs": 1240,
  "result": "Succeeded",
  "citationCount": 2,
  "insufficientContext": false
}
```

Do not log raw prompt context, complete source chunks, provider keys, bearer tokens, or raw exception output in user-visible data.

## 4. Correlation And Traceability

- API requests accept or create a correlation identifier and return it in safe error responses.
- Background processing preserves correlation from document upload when practical or creates a processing correlation linked to `documentId`.
- A RAG interaction links its retrieval result, citations, provider metadata, latency, feedback, and insufficient-context marker through `chatInteractionId`.
- Citation traceability must preserve authorized references to source documents and chunks.

## 5. Metrics

### 5.1 Document Pipeline Metrics

| Metric | Operational Use |
|---|---|
| Documents uploaded | Measure ingestion activity. |
| Upload failures | Detect validation or storage issues. |
| Documents processing / processed / failed | Monitor background pipeline results. |
| Processing duration | Detect slow ingestion. |
| Text extraction failures | Identify format or extractor problems. |
| Embedding generation failures | Detect provider or vector preparation problems. |
| Retrieval-disabled document count | Understand unavailable knowledge sources. |

### 5.2 Retrieval And RAG Metrics

| Metric | Operational Use |
|---|---|
| Retrieval latency | Identify semantic/vector query degradation. |
| Retrieval failures | Detect index, vector, or query provider problems. |
| AI generation latency | Identify provider response degradation. |
| Total RAG response latency | Measure user-facing experience. |
| Provider failures | Detect AI dependency instability. |
| Token usage, when available | Support responsible AI usage monitoring. |
| Estimated AI cost, when available | Monitor cost without showing misleading zero values. |
| Citation count/coverage | Support traceability checks. |
| Insufficient-context count | Detect unanswered knowledge demand. |
| `NotUseful` feedback count | Detect answer quality concerns. |

### 5.3 Security And Audit Metrics

| Metric | Operational Use |
|---|---|
| Authentication failures | Detect access problems or misuse. |
| Authorization failures | Detect denied protected actions. |
| Organization-scope failures | Detect attempted cross-scope access. |
| Document retrieval availability changes | Track knowledge source governance. |
| Role changes and audit-log access | Track privileged administrative behavior. |

All metric queries and dashboard displays must respect role permission and organization scope.

## 6. Health Endpoints

The canonical health endpoints are:

```text
GET /api/v1/health
GET /api/v1/health/details
```

| Endpoint | Visibility | Content |
|---|---|---|
| `/api/v1/health` | Public or authenticated according to deployment policy | Basic safe application status only. |
| `/api/v1/health/details` | `Admin` only | Sanitized dependency status for database, storage, background processing, retrieval/vector dependency, and AI provider connectivity. |

Health responses must not expose secrets, provider keys, raw exception traces, connection strings, document content, prompt content, or internal configuration values.

## 7. Dashboard Support

The MVP operational dashboard supports authorized views of:

- question count and active usage;
- response, retrieval, and AI generation latency where available;
- estimated AI cost and token usage where available;
- document upload, processed, failed, and retrieval-disabled counts;
- useful and `NotUseful` feedback counts;
- insufficient-context count;
- safe operational failure indicators.

The MVP does not require a dedicated knowledge-gap queue or review workflow. Detailed categorization, assignment, decisions, resolution, and clustering are Phase 2.

## 8. Audit-Sensitive Actions

Audit logs should be append-oriented and organization-scoped where applicable. Capture:

- user creation, disablement, and role assignment changes;
- document upload and retrieval-availability changes;
- document processing failures where administrative action is required;
- authorization or organization-scope failures;
- detailed health access;
- audit-log access;
- configuration changes affecting providers or retrieval, if exposed administratively.

Audit events should identify the actor, approved MVP role, scope, target reference, action, timestamp, outcome, and correlation identifier without storing unnecessary sensitive content.

## 9. Operational Troubleshooting

| Scenario | Investigate | Safe Corrective Direction |
|---|---|---|
| Upload rejected or storage failure | Correlation ID, validation error, storage health, document metadata. | Correct validation/input or storage configuration; retry only after cause is understood. |
| Processing remains incomplete | Worker health, processing status, correlation ID, duration. | Restore worker/dependency availability and follow documented retry policy when introduced. |
| Text extraction failure | Failure reason, file type, extractor logs. | Replace or correct source document; keep it non-retrievable. |
| Embedding generation failure | Provider status, vector dependency, safe failure category. | Resolve provider/dependency issue and reprocess according to supported workflow. |
| Retrieval returns no useful context | Organization filter, retrieval eligibility, embeddings, query metadata. | Confirm authorized processed sources exist; treat outcome as insufficient context when appropriate. |
| Answer lacks traceable sources | Stored retrieval results and citation mapping. | Do not treat the answer as grounded until citation behavior is corrected. |
| High response latency or cost | Retrieval and generation timings, token/cost metadata, provider health. | Tune configuration or investigate provider behavior without bypassing grounding controls. |
| Unexpected access denial | User role, organization scope, authorization event. | Correct approved access configuration only; do not weaken scope filtering. |
| Suspected cross-organization access | Authorization/scope logs, retrieval metadata, citations, audit trail. | Escalate as a security incident and prevent unauthorized disclosure. |

## 10. Alerts And Operational Review

Recommended alert signals include:

- repeated document processing, extraction, embedding, retrieval, or provider failures;
- sustained total RAG latency increase;
- unusual estimated AI cost growth where cost data is available;
- spikes in insufficient-context or `NotUseful` outcomes;
- repeated authorization or organization-scope failures;
- failed detailed health dependencies.

Alert configuration is deployment-specific, but the monitored event and metric meanings must remain consistent with this document.

## 11. Acceptance Criteria

- Structured logging covers document ingestion, retrieval, RAG, feedback, and authorization behavior.
- Latency and estimated AI usage metadata are recorded where available.
- Sensitive document, prompt, answer, and secret content is not unnecessarily logged.
- Citation traceability can be investigated using authorized stored references.
- Dashboard metrics remain organization-scoped.
- Canonical health endpoints and Admin-only details are documented consistently.
- Support procedures address the AI knowledge assistant and its document processing pipeline.
