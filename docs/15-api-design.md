# API Design

## 1. Purpose

This document defines the initial API design for **KnowledgeOps-AI**.

The API design translates approved use cases, requirements, business rules, domain concepts, and database design into stable HTTP contracts.

Controllers must not become a place where business rules are invented. Controllers should expose application behavior already defined in the domain and application layers.

KnowledgeOps-AI is an enterprise AI-powered internal knowledge assistant for contact centers and support operations. The system allows authorized users to upload internal documents, process them into searchable knowledge, ask questions through a Retrieval-Augmented Generation workflow, receive grounded answers with citations, submit feedback, and monitor operational metrics.

This document should guide:

- ASP.NET Core Web API controller design.
- Frontend API client implementation.
- Integration tests.
- Authorization tests.
- AI coding agent implementation.
- Future OpenAPI / Swagger documentation.

---

## 2. API Design Goals

The API should support the approved MVP workflows:

1. Authenticate users.
2. Manage users and roles.
3. Upload internal documents.
4. Review document processing status.
5. Ask knowledge questions.
6. Generate RAG answers with citations.
7. Handle insufficient context safely.
8. Review chat history.
9. Submit answer feedback.
10. Review operational dashboard metrics.
11. Monitor system health and failures.
12. Validate role and organization access boundaries.

The API should be:

- Business-aligned.
- Stable.
- Secure.
- Organization-scoped.
- Testable.
- Observable.
- Consistent.
- Versionable.
- Easy to consume from the frontend.

---

# 3. API Principles

## 3.1 Use Case Driven

API endpoints should map to documented use cases.

Controllers should expose application use cases such as:

- Upload document.
- Ask question.
- Submit feedback.
- Review dashboard.
- Manage users.

Controllers should not contain domain logic, RAG orchestration, retrieval filtering, prompt construction, provider calls, or metric calculations.

---

## 3.2 Thin Controllers

Controllers should be responsible for:

- Receiving HTTP requests.
- Validating request shape.
- Calling application services or command/query handlers.
- Mapping application results to HTTP responses.
- Returning consistent response models.

Controllers should not be responsible for:

- Applying business rules directly.
- Performing retrieval.
- Constructing prompts.
- Calling AI providers.
- Reading files directly from infrastructure.
- Computing dashboard metrics manually.
- Making authorization decisions outside established policies and application services.

---

## 3.3 Explicit Authorization

Every protected endpoint must define authorization requirements.

Authorization must include:

- Authentication.
- Role permissions.
- Organization-aware data access.

Role checks alone are not enough.

Organization scope checks alone are not enough.

Both must be enforced where applicable.

---

## 3.4 Organization-Scoped Data

Business data must be scoped by organization.

The API should not allow users to pass arbitrary organization IDs to access data unless the user is explicitly authorized to act across organizations.

For MVP, most endpoints should infer organization scope from the authenticated user context.

---

## 3.5 Stable Request and Response Contracts

Request and response models should be explicit.

The API should avoid returning database entities directly.

Use DTOs for:

- Request bodies.
- Response bodies.
- Lists.
- Dashboard metrics.
- Errors.
- Citations.
- Feedback.
- Chat history.

---

## 3.6 Consistent Error Handling

The API should return consistent error responses.

Errors should not expose:

- Sensitive document content.
- Prompt content.
- Provider secrets.
- Internal stack traces.
- Unauthorized entity existence across organization boundaries.

---

## 3.7 Async Processing Awareness

Document upload and document processing are separate.

Uploading a document should not imply the document is immediately searchable.

The upload endpoint should return a document record with an initial status such as `Uploaded`.

Document processing status should be queried separately.

---

## 3.8 RAG Safety

Chat endpoints must preserve RAG safety behavior.

The API must support:

- Retrieval before generation.
- Source citations for grounded answers.
- Insufficient-context response.
- Stored chat history.
- Latency and estimated cost metadata.
- User feedback.

---

## 3.9 Provider Isolation

API contracts must not expose provider-specific SDK structures.

The API may expose stable application-level metadata such as:

- Model name.
- Estimated cost.
- Token usage.
- Latency.
- Retrieval score.

It should not expose raw Azure OpenAI or OpenAI SDK response objects.

---

## 3.10 API Versioning

The API should use an explicit version strategy from the beginning.

Recommended MVP route prefix:

```text
/api/v1
```

Example:

```text
POST /api/v1/documents
POST /api/v1/chat/questions
GET  /api/v1/dashboard/overview
```

---

# 4. Base API Conventions

## 4.1 Base URL

```text
/api/v1
```

## 4.2 Content Types

Default JSON endpoints should use:

```text
Content-Type: application/json
Accept: application/json
```

Document upload endpoints should use:

```text
Content-Type: multipart/form-data
```

## 4.3 Authentication

Protected endpoints should require:

```text
Authorization: Bearer <token>
```

The exact authentication implementation may evolve, but API contracts should assume authenticated internal users.

## 4.4 Date and Time Format

Date and time values should use ISO 8601 format.

Example:

```json
"createdAt": "2026-05-20T18:30:00Z"
```

## 4.5 Identifier Format

Public API identifiers should use GUID strings.

Example:

```json
"documentId": "8c2d5e11-4d8d-4c41-a9c9-9d7e0f6f71a1"
```

## 4.6 Naming Convention

JSON fields should use camelCase.

Example:

```json
{
  "documentId": "8c2d5e11-4d8d-4c41-a9c9-9d7e0f6f71a1",
  "processingStatus": "Uploaded"
}
```

---

# 5. Endpoint Catalog

## 5.1 Authentication Endpoints

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| POST | `/api/v1/auth/login` | Authenticate user and return token/session context. | Public |
| POST | `/api/v1/auth/logout` | End authenticated session where applicable. | Authenticated |
| GET | `/api/v1/auth/me` | Return current user context. | Authenticated |

---

## 5.2 User and Role Administration Endpoints

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| GET | `/api/v1/users` | List users within authorized scope. | Admin |
| POST | `/api/v1/users` | Create user. | Admin |
| GET | `/api/v1/users/{userId}` | Get user details. | Admin |
| PUT | `/api/v1/users/{userId}` | Update user details or status. | Admin |
| POST | `/api/v1/users/{userId}/roles` | Assign role to user. | Admin |
| DELETE | `/api/v1/users/{userId}/roles/{roleName}` | Remove role from user. | Admin |

---

## 5.3 Document Endpoints

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| POST | `/api/v1/documents` | Upload internal document. | KnowledgeAdmin, Admin |
| GET | `/api/v1/documents` | List documents within organization scope. | KnowledgeAdmin, Manager, Admin |
| GET | `/api/v1/documents/{documentId}` | Get document details and processing status. | KnowledgeAdmin, Manager, Admin |
| POST | `/api/v1/documents/{documentId}/disable` | Set `isRetrievalEnabled = false` without changing processing status. | KnowledgeAdmin, Admin |
| POST | `/api/v1/documents/{documentId}/enable` | Re-enable a processed document for retrieval. Document must have `ProcessingStatus = Processed`. | KnowledgeAdmin, Admin |
| POST | `/api/v1/documents/{documentId}/retry-processing` | Retry failed document processing (Phase 2). | KnowledgeAdmin, Admin |

### MVP Note

`retry-processing` is deferred to Phase 2. `enable` was promoted from Phase 2 to MVP as a demo-readiness correction. MVP supports upload, status review, retrieval disablement, and retrieval enablement for Processed documents.

---

## 5.4 Document Processing Endpoints

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| GET | `/api/v1/documents/{documentId}/processing-status` | Get document processing state and failure reason. | KnowledgeAdmin, Manager, Admin |
| GET | `/api/v1/documents/{documentId}/chunks` | List chunks for a processed document. | KnowledgeAdmin, Admin |
| GET | `/api/v1/documents/{documentId}/usage` | View document usage through retrieval/citations. | Manager, KnowledgeAdmin, Admin |

### MVP Note

Chunk listing may be admin-only or deferred if exposing chunk text is considered too sensitive for MVP.

---

## 5.5 Chat Endpoints

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| POST | `/api/v1/chat/questions` | Ask a natural-language question. | Agent, Supervisor, KnowledgeAdmin, Manager, Admin |
| GET | `/api/v1/chat/sessions` | List current user chat sessions. | Authenticated |
| POST | `/api/v1/chat/sessions` | Create chat session. | Authenticated |
| GET | `/api/v1/chat/sessions/{chatSessionId}` | Get chat session with interactions. | Session owner; Supervisor, Manager, Admin for scoped review |
| GET | `/api/v1/chat/interactions/{chatInteractionId}` | Get specific chat interaction with citations and feedback. | Owner; Supervisor, Manager, Admin for scoped review |

---

## 5.6 Citation Endpoints

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| GET | `/api/v1/chat/interactions/{chatInteractionId}/citations` | Get citations for an answer. | Owner; Supervisor, Manager, Admin for scoped review |
| GET | `/api/v1/citations/{citationId}` | Get citation details. | Owner; Supervisor, Manager, Admin for scoped review |

### MVP Note

Citations returned directly in chat responses satisfy MVP. Separate citation-detail endpoints are deferred unless later scope explicitly adds them.

---

## 5.7 Feedback Endpoints

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| POST | `/api/v1/chat/interactions/{chatInteractionId}/feedback` | Submit useful / not useful feedback. | Authenticated user with access to interaction |
| PUT | `/api/v1/chat/interactions/{chatInteractionId}/feedback` | Update existing feedback. | Feedback owner |
| GET | `/api/v1/feedback` | List feedback for review. | Supervisor, Manager, Admin |

---

## 5.8 Dashboard Endpoints

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| GET | `/api/v1/dashboard/overview` | Get dashboard overview metrics. | Manager, KnowledgeAdmin, Admin |
| GET | `/api/v1/dashboard/documents` | Get document processing metrics. | Manager, KnowledgeAdmin, Admin |
| GET | `/api/v1/dashboard/chat` | Get chat usage, latency, cost, and insufficient-context metrics. | Manager, Admin |
| GET | `/api/v1/dashboard/feedback` | Get useful / not useful feedback metrics. | Supervisor, Manager, Admin |

---

## 5.9 Knowledge Gap Review Endpoints (Phase 2, Not MVP)

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| GET | `/api/v1/knowledge-gaps` | List insufficient-context and negative-feedback signals. | Supervisor, Manager, KnowledgeAdmin, Admin |
| GET | `/api/v1/knowledge-gaps/{knowledgeGapId}` | Get knowledge gap details. | Supervisor, Manager, KnowledgeAdmin, Admin |
| POST | `/api/v1/knowledge-gaps/{knowledgeGapId}/review` | Record review decision. | Supervisor, Manager, Admin |

### MVP Note

These endpoints and their review workflow are deferred to Phase 2. MVP derives basic visibility from scoped insufficient-context and `NotUseful` counts in dashboard metrics and does not define QA, Trainer, or Viewer RBAC roles.

---

## 5.10 Health and Observability Endpoints

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| GET | `/api/v1/health` | Basic health check. | Public or authenticated according to deployment policy |
| GET | `/api/v1/health/details` | Detailed health information. | Admin |
| GET | `/api/v1/admin/processing-failures` | List recent document processing failures. | KnowledgeAdmin, Admin |
| GET | `/api/v1/admin/audit-log` | Query audit log entries. | Admin |

`/api/v1/health` exposes only a safe basic status and may be public or authenticated according to deployment policy. `/api/v1/health/details` is Admin-only and must not expose secrets, provider keys, raw exception traces, or sensitive content.

---

# 6. Request Models

## 6.1 LoginRequest

```json
{
  "email": "agent@example.com",
  "password": "example-password"
}
```

### Fields

| Field | Type | Required | Notes |
|---|---|---:|---|
| email | string | Yes | User login identifier. |
| password | string | Yes | User password for local authentication. |

---

## 6.2 CreateUserRequest

```json
{
  "displayName": "Jane Agent",
  "email": "jane.agent@example.com",
  "organizationId": "2c75ec98-80d9-44e2-8e65-7461df9e1001",
  "roles": ["Agent"],
  "status": "Active"
}
```

### Fields

| Field | Type | Required | Notes |
|---|---|---:|---|
| displayName | string | Yes | User-facing name. |
| email | string | Yes | Login identifier. |
| organizationId | guid | Conditional | May be inferred for scoped admins. |
| roles | string[] | No | Initial roles. |
| status | string | Yes | Pending, Active, Disabled. |

---

## 6.3 UpdateUserRequest

```json
{
  "displayName": "Jane Agent",
  "status": "Active"
}
```

---

## 6.4 AssignRoleRequest

```json
{
  "roleName": "KnowledgeAdmin"
}
```

### Supported Roles

```text
Agent
Supervisor
KnowledgeAdmin
Manager
Admin
```

---

## 6.5 UploadDocumentRequest

This endpoint uses `multipart/form-data`.

### Fields

| Field | Type | Required | Notes |
|---|---|---:|---|
| file | file | Yes | Uploaded document file. |
| title | string | Yes | Business-readable title. |
| organizationId | guid | Conditional | Usually inferred from current user for MVP. |
| tags | string[] | No | Optional, may be deferred. |

### Example Multipart Fields

```text
file=<uploaded file>
title=Refund Escalation Policy
```

---

## 6.6 AskQuestionRequest

```json
{
  "chatSessionId": "5e9f7014-6672-4a63-a70b-cc118b402910",
  "questionText": "What is the escalation process for a critical customer complaint?"
}
```

### Fields

| Field | Type | Required | Notes |
|---|---|---:|---|
| chatSessionId | guid | No | If omitted, system may create or use default session. |
| questionText | string | Yes | Natural-language question. |
| retrievalOptions | object | No | Optional retrieval overrides within allowed limits. Deferred to Phase 2. |
| retrievalOptions.maxResults | int | No | Must not exceed configured system maximum. Deferred to Phase 2. |

---

## 6.7 CreateChatSessionRequest

```json
{
  "title": "Escalation Questions"
}
```

---

## 6.8 SubmitFeedbackRequest

```json
{
  "rating": "Useful"
}
```

### Supported Ratings

```text
Useful
NotUseful
```

### Future Optional Field

```json
{
  "rating": "NotUseful",
  "comment": "The answer referenced an outdated procedure."
}
```

The `comment` field may be deferred to Phase 2.

---

## 6.9 ReviewKnowledgeGapRequest (Phase 2)

```json
{
  "status": "Reviewed",
  "decision": "DocumentationUpdateRecommended",
  "notes": "The policy is missing a clear escalation window."
}
```

### MVP Note

This request is deferred to Phase 2; a full knowledge-gap review workflow is not part of MVP.

---

# 7. Response Models

## 7.1 ApiResponse Envelope

The API may use a consistent response envelope.

```json
{
  "data": {},
  "meta": {
    "correlationId": "b6c8c79f4a9a4a3e9f221187f64a2d41"
  }
}
```

For errors, use the standard error model defined in this document.

---

## 7.2 LoginResponse

```json
{
  "accessToken": "jwt-or-session-token",
  "expiresAt": "2026-05-20T20:00:00Z",
  "user": {
    "userId": "150ec956-7257-48b9-b9d4-0dcf560db080",
    "displayName": "Jane Agent",
    "email": "jane.agent@example.com",
    "organizationId": "2c75ec98-80d9-44e2-8e65-7461df9e1001",
    "roles": ["Agent"]
  }
}
```

---

## 7.3 CurrentUserResponse

```json
{
  "userId": "150ec956-7257-48b9-b9d4-0dcf560db080",
  "displayName": "Jane Agent",
  "email": "jane.agent@example.com",
  "organizationId": "2c75ec98-80d9-44e2-8e65-7461df9e1001",
  "roles": ["Agent"]
}
```

---

## 7.4 UserResponse

```json
{
  "userId": "150ec956-7257-48b9-b9d4-0dcf560db080",
  "displayName": "Jane Agent",
  "email": "jane.agent@example.com",
  "organizationId": "2c75ec98-80d9-44e2-8e65-7461df9e1001",
  "roles": ["Agent"],
  "status": "Active",
  "createdAt": "2026-05-20T18:30:00Z",
  "updatedAt": "2026-05-20T18:30:00Z"
}
```

---

## 7.5 DocumentResponse

```json
{
  "documentId": "8c2d5e11-4d8d-4c41-a9c9-9d7e0f6f71a1",
  "title": "Refund Escalation Policy",
  "fileName": "refund-escalation-policy.pdf",
  "contentType": "application/pdf",
  "fileSizeBytes": 245812,
  "processingStatus": "Uploaded",
  "failureReason": null,
  "isRetrievalEnabled": false,
  "uploadedByUserId": "150ec956-7257-48b9-b9d4-0dcf560db080",
  "uploadedAt": "2026-05-20T18:30:00Z",
  "processedAt": null
}
```

---

## 7.6 DocumentProcessingStatusResponse

```json
{
  "documentId": "8c2d5e11-4d8d-4c41-a9c9-9d7e0f6f71a1",
  "processingStatus": "Processed",
  "failureReason": null,
  "isRetrievalEnabled": true,
  "uploadedAt": "2026-05-20T18:30:00Z",
  "processingStartedAt": "2026-05-20T18:31:00Z",
  "processedAt": "2026-05-20T18:32:15Z",
  "chunkCount": 42,
  "embeddingStatus": "Ready"
}
```

---

## 7.7 DocumentChunkResponse

```json
{
  "chunkId": "ea37399e-8c89-4d9b-8b4c-f5e3054be3e1",
  "documentId": "8c2d5e11-4d8d-4c41-a9c9-9d7e0f6f71a1",
  "chunkIndex": 1,
  "textPreview": "Critical customer complaints must be escalated...",
  "pageNumber": 2,
  "sectionLabel": "Escalation Procedure",
  "characterLength": 1200,
  "tokenEstimate": 280,
  "createdAt": "2026-05-20T18:32:00Z"
}
```

### Security Note

Full chunk text should only be exposed to authorized roles. The MVP may return previews instead of full text.

---

## 7.8 AskQuestionResponse

```json
{
  "chatInteractionId": "426d593a-2c37-40df-bcb4-32e04b465831",
  "chatSessionId": "5e9f7014-6672-4a63-a70b-cc118b402910",
  "questionText": "What is the escalation process for a critical customer complaint?",
  "answerText": "According to the available escalation policy, critical customer complaints should be escalated to a supervisor immediately and documented according to the escalation procedure.",
  "answerStatus": "GroundedAnswer",
  "insufficientContext": false,
  "citations": [
    {
      "citationId": "832c0796-6111-4323-bd72-f124173661a8",
      "documentId": "8c2d5e11-4d8d-4c41-a9c9-9d7e0f6f71a1",
      "documentTitle": "Refund Escalation Policy",
      "chunkId": "ea37399e-8c89-4d9b-8b4c-f5e3054be3e1",
      "pageNumber": 2,
      "sectionLabel": "Escalation Procedure",
      "relevanceScore": 0.8912
    }
  ],
  "metadata": {
    "responseLatencyMs": 1430,
    "retrievalLatencyMs": 210,
    "generationLatencyMs": 970,
    "estimatedCost": 0.0021,
    "promptTokens": 1320,
    "completionTokens": 180,
    "totalTokens": 1500,
    "promptVersion": "rag-v1",
    "retrievalConfigurationVersion": "default-v1"
  },
  "createdAt": "2026-05-20T18:35:00Z"
}
```

### answerStatus Values

The `answerStatus` field uses one of three stable string values:

| Value | Meaning |
|---|---|
| `GroundedAnswer` | Retrieval succeeded and a grounded answer with citations was generated. |
| `InsufficientContext` | Retrieval returned insufficient authorized context; no answer was generated. |
| `ProviderFailure` | The AI generation provider failed; a safe failure response was returned. |

---

## 7.9 InsufficientContextResponse

This response uses the same shape as `AskQuestionResponse`, but with insufficient-context values.

```json
{
  "chatInteractionId": "426d593a-2c37-40df-bcb4-32e04b465831",
  "chatSessionId": "5e9f7014-6672-4a63-a70b-cc118b402910",
  "questionText": "What is the refund policy for VIP customers in Brazil?",
  "answerText": "The available documents do not contain enough information to answer this question safely. Please contact a supervisor or knowledge administrator for confirmation.",
  "answerStatus": "InsufficientContext",
  "insufficientContext": true,
  "citations": [],
  "metadata": {
    "responseLatencyMs": 410,
    "retrievalLatencyMs": 180,
    "generationLatencyMs": null,
    "estimatedCost": null,
    "promptTokens": null,
    "completionTokens": null,
    "totalTokens": null,
    "promptVersion": "rag-v1",
    "retrievalConfigurationVersion": "default-v1"
  },
  "createdAt": "2026-05-20T18:35:00Z"
}
```

---

## 7.10 CitationResponse

```json
{
  "citationId": "832c0796-6111-4323-bd72-f124173661a8",
  "chatInteractionId": "426d593a-2c37-40df-bcb4-32e04b465831",
  "documentId": "8c2d5e11-4d8d-4c41-a9c9-9d7e0f6f71a1",
  "documentTitle": "Refund Escalation Policy",
  "chunkId": "ea37399e-8c89-4d9b-8b4c-f5e3054be3e1",
  "pageNumber": 2,
  "sectionLabel": "Escalation Procedure",
  "textPreview": "Critical customer complaints must be escalated...",
  "relevanceScore": 0.8912
}
```

---

## 7.11 FeedbackResponse

```json
{
  "feedbackId": "d5a73f98-3d3b-4f1a-b358-8be819fb9f55",
  "chatInteractionId": "426d593a-2c37-40df-bcb4-32e04b465831",
  "rating": "Useful",
  "createdAt": "2026-05-20T18:36:00Z",
  "updatedAt": "2026-05-20T18:36:00Z"
}
```

---

## 7.12 ChatSessionResponse

```json
{
  "chatSessionId": "5e9f7014-6672-4a63-a70b-cc118b402910",
  "title": "Escalation Questions",
  "status": "Active",
  "createdAt": "2026-05-20T18:30:00Z",
  "updatedAt": "2026-05-20T18:35:00Z",
  "interactionCount": 3
}
```

---

## 7.13 ChatInteractionResponse

```json
{
  "chatInteractionId": "426d593a-2c37-40df-bcb4-32e04b465831",
  "chatSessionId": "5e9f7014-6672-4a63-a70b-cc118b402910",
  "questionText": "What is the escalation process for a critical customer complaint?",
  "answerText": "According to the available escalation policy...",
  "answerStatus": "GroundedAnswer",
  "insufficientContext": false,
  "citations": [],
  "feedback": {
    "rating": "Useful",
    "submittedAt": "2026-05-20T18:36:00Z"
  },
  "createdAt": "2026-05-20T18:35:00Z"
}
```

---

## 7.14 Dashboard Responses

Dashboard responses use a flat MVP shape. Cost and token fields are nullable and must never
be shown as zero when unavailable. Latency averages are null when no data exists for the period.

> **Implementation note (Sprint 23):** The implemented flat field layout was chosen over the
> earlier nested draft (questions.total, users.active, documents.*, etc.) for MVP frontend
> simplicity. The cost object remains a nested availability-aware shape to distinguish
> "unavailable" from "actual zero cost."

### GET /api/v1/dashboard/overview

```json
{
  "period": {
    "from": "2026-05-01T00:00:00Z",
    "to": "2026-05-31T00:00:00Z"
  },
  "questionsAsked": 128,
  "activeUsers": 12,
  "documentsUploaded": 24,
  "documentsProcessed": 20,
  "documentsFailed": 3,
  "averageResponseLatencyMs": 1450,
  "insufficientContextCount": 14,
  "providerFailureCount": 2,
  "usefulFeedbackCount": 88,
  "notUsefulFeedbackCount": 11,
  "cost": {
    "available": true,
    "estimatedTotal": 0.3825
  }
}
```

### GET /api/v1/dashboard/documents

```json
{
  "period": {
    "from": "2026-05-01T00:00:00Z",
    "to": "2026-05-31T00:00:00Z"
  },
  "uploaded": 24,
  "processing": 1,
  "processed": 20,
  "failed": 3,
  "retrievalDisabled": 2
}
```

### GET /api/v1/dashboard/chat

```json
{
  "period": {
    "from": "2026-05-01T00:00:00Z",
    "to": "2026-05-31T00:00:00Z"
  },
  "questionsAsked": 128,
  "activeUsers": 12,
  "averageResponseLatencyMs": 1450,
  "retrievalLatencyMs": 240,
  "generationLatencyMs": 980,
  "totalRagLatencyMs": 1450,
  "insufficientContextCount": 14,
  "providerFailureCount": 2,
  "tokens": {
    "input": 45000,
    "output": 12000,
    "total": 57000
  },
  "cost": {
    "available": true,
    "estimatedTotal": 0.3825
  }
}
```

### GET /api/v1/dashboard/feedback

```json
{
  "period": {
    "from": "2026-05-01T00:00:00Z",
    "to": "2026-05-31T00:00:00Z"
  },
  "useful": 88,
  "notUseful": 11,
  "total": 99
}
```

---

## 7.15 ProcessingFailureResponse

```json
{
  "documentId": "8c2d5e11-4d8d-4c41-a9c9-9d7e0f6f71a1",
  "title": "Refund Escalation Policy",
  "processingStatus": "Failed",
  "failureReason": "TextExtractionFailed",
  "failedAt": "2026-05-20T18:32:00Z"
}
```

---

# 8. Error Responses

## 8.1 Standard Error Response

The API should return errors in a consistent format.

```json
{
  "error": {
    "code": "validation_error",
    "message": "One or more validation errors occurred.",
    "details": [
      {
        "field": "questionText",
        "message": "Question text is required."
      }
    ],
    "correlationId": "b6c8c79f4a9a4a3e9f221187f64a2d41"
  }
}
```

---

## 8.2 Common Error Codes

| HTTP Status | Code | Meaning |
|---:|---|---|
| 400 | validation_error | Request is invalid. |
| 401 | unauthenticated | Authentication is required or invalid. |
| 403 | forbidden | User is authenticated but not authorized. |
| 404 | not_found | Resource was not found or is not visible to the user. |
| 409 | conflict | Request conflicts with current state. |
| 413 | file_too_large | Uploaded file exceeds allowed size. |
| 415 | unsupported_file_type | Uploaded file type is not supported. |
| 422 | business_rule_violation | Request violates a business rule. |
| 429 | rate_limited | Request rate limit exceeded, if implemented. |
| 500 | internal_error | Unexpected server error. |
| 502 | provider_error | External provider failure. |
| 503 | service_unavailable | Service temporarily unavailable. |

---

## 8.3 Validation Error Example

```json
{
  "error": {
    "code": "validation_error",
    "message": "One or more validation errors occurred.",
    "details": [
      {
        "field": "title",
        "message": "Document title is required."
      }
    ],
    "correlationId": "e7642901ef5e4e92aa2f4f5e665f19aa"
  }
}
```

---

## 8.4 Authorization Error Example

```json
{
  "error": {
    "code": "forbidden",
    "message": "You are not authorized to perform this action.",
    "details": [],
    "correlationId": "3a59cddbd5ea4e6f871fb37df57e7d97"
  }
}
```

---

## 8.5 Not Found Error Example

```json
{
  "error": {
    "code": "not_found",
    "message": "The requested resource was not found.",
    "details": [],
    "correlationId": "c9c70813c6e14b37bb21e21a9645d8dc"
  }
}
```

### Security Note

For organization-scoped resources, the API may return `404 Not Found` instead of `403 Forbidden` to avoid revealing whether a resource exists outside the user’s scope.

---

## 8.6 Business Rule Violation Example

```json
{
  "error": {
    "code": "business_rule_violation",
    "message": "The document is not eligible for retrieval.",
    "details": [
      {
        "field": "processingStatus",
        "message": "Only processed and retrieval-enabled documents can be used for retrieval."
      }
    ],
    "correlationId": "86d6628f1cdf47809591a0ab06f8aeb8"
  }
}
```

---

## 8.7 Provider Error Example

```json
{
  "error": {
    "code": "provider_error",
    "message": "The AI provider could not complete the request. Please try again later.",
    "details": [],
    "correlationId": "ac2a4e508f26434eb20868111e944f47"
  }
}
```

### Security Note

Provider errors must not expose:

- API keys.
- Raw provider stack traces.
- Sensitive prompt content.
- Sensitive document content.
- Internal infrastructure details.

---

# 9. Authorization Requirements

## 9.1 Role Matrix

| API Area | Agent | Supervisor | KnowledgeAdmin | Manager | Admin |
|---|---:|---:|---:|---:|---:|
| Login / Me | Yes | Yes | Yes | Yes | Yes |
| Ask Question | Yes | Yes | Yes | Yes | Yes |
| Review Own Chat History | Yes | Yes | Yes | Yes | Yes |
| Review Scoped Team Chat | No | Yes | No | Yes | Yes |
| Submit Feedback | Yes | Yes | Yes | Yes | Yes |
| Upload Documents | No | No | Yes | No | Yes |
| Review Document Status | No | No | Yes | Yes | Yes |
| Disable Documents | No | No | Yes | No | Yes |
| Dashboard Overview | No | No | Yes | Yes | Yes |
| Feedback Review | No | Yes | No | Yes | Yes |
| User Management | No | No | No | No | Yes |
| Health Details | No | No | No | No | Yes |
| Audit Log | No | No | No | No | Yes |

Dedicated knowledge-gap review endpoints and permissions are Phase 2 and are not part of this MVP authorization matrix. Quality Analyst and Trainer are stakeholder personas represented by one of the approved MVP roles where permitted.

---

## 9.2 Organization Scope Requirements

Most endpoints must apply organization filtering.

Examples:

| Endpoint | Organization Scope Behavior |
|---|---|
| `GET /documents` | Return documents from current user’s organization only. |
| `GET /documents/{documentId}` | Return only if document belongs to current user’s organization. |
| `POST /chat/questions` | Retrieve chunks only from authorized organization scope. |
| `GET /chat/sessions` | Return sessions visible to current user. |
| `GET /dashboard/overview` | Return metrics scoped to current user’s organization. |
| `GET /feedback` | Return feedback scoped to authorized organization. |
| `GET /admin/audit-log` | Return audit logs within admin’s authorized scope. |

---

## 9.3 Authorization Failure Behavior

The API should:

- Return `401` when authentication is missing or invalid.
- Return `403` when the user is authenticated but lacks required role.
- Return `404` when returning `403` would leak cross-organization resource existence.
- Log authorization failures safely.
- Never expose sensitive content in authorization error responses.

---

# 10. Pagination and Filtering

## 10.1 Standard Pagination Parameters

List endpoints should support pagination where applicable.

```text
?page=1&pageSize=25
```

### Parameters

| Parameter | Type | Default | Max | Notes |
|---|---:|---:|---:|---|
| page | int | 1 | n/a | Must be greater than 0. |
| pageSize | int | 25 | 100 | Must be between 1 and configured max. |

---

## 10.2 Paginated Response Model

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalItems": 120,
  "totalPages": 5
}
```

---

## 10.3 Document Filters

`GET /api/v1/documents`

Supported filters:

```text
?status=Processed
?uploadedByUserId=<guid>
?search=refund
?from=2026-05-01
?to=2026-05-20
```

### Filter Behavior

- `status` filters by document processing status.
- `uploadedByUserId` filters by uploader within authorized scope.
- `search` searches title or file name.
- `from` and `to` filter upload date range.

---

## 10.4 Chat History Filters

`GET /api/v1/chat/sessions/{chatSessionId}` or future chat history endpoint.

Possible filters:

```text
?from=2026-05-01
?to=2026-05-20
?answerStatus=InsufficientContext
?hasFeedback=true
```

---

## 10.5 Feedback Filters

`GET /api/v1/feedback`

Supported filters:

```text
?rating=NotUseful
?from=2026-05-01
?to=2026-05-20
```

---

## 10.6 Dashboard Filters

Dashboard endpoints may support:

```text
?from=2026-05-01
?to=2026-05-20
```

The system should apply sensible defaults when no period is provided.

Example default:

```text
Last 30 days
```

---

# 11. Versioning Strategy

## 11.1 URL-Based Versioning

The MVP should use URL-based versioning.

Example:

```text
/api/v1/documents
/api/v1/chat/questions
/api/v1/dashboard/overview
```

## 11.2 Versioning Rationale

URL-based versioning is simple, visible, and easy for portfolio reviewers, frontend clients, and AI coding agents to understand.

## 11.3 Breaking Changes

Breaking changes should require a new API version.

Examples:

- Removing response fields.
- Changing response field meanings.
- Changing authorization semantics.
- Changing request model shape.
- Changing pagination behavior.
- Replacing endpoint purpose.

## 11.4 Non-Breaking Changes

Non-breaking changes may remain in the same version.

Examples:

- Adding optional response fields.
- Adding optional filters.
- Adding new endpoints.
- Adding new enum values only if clients can tolerate them.
- Adding metadata fields.

---

# 12. Endpoint Details

## 12.1 POST /api/v1/auth/login

### Use Case

UC-001: Authenticate User.

### Authorization

Public.

### Request

`LoginRequest`

### Response

`LoginResponse`

### Success Status

```text
200 OK
```

### Error Statuses

| Status | Meaning |
|---:|---|
| 400 | Invalid request shape. |
| 401 | Invalid credentials. |
| 403 | User disabled or not allowed. |

---

## 12.2 GET /api/v1/auth/me

### Use Case

UC-001: Authenticate User.

### Authorization

Authenticated.

### Response

`CurrentUserResponse`

### Success Status

```text
200 OK
```

---

## 12.3 POST /api/v1/documents

### Use Case

UC-003: Upload Internal Document.

### Authorization

KnowledgeAdmin, Admin.

### Request

`multipart/form-data`

### Response

`DocumentResponse`

### Success Status

```text
201 Created
```

### Business Rules

- Only authorized roles may upload documents.
- Uploaded documents must have metadata.
- Unsupported file types must be rejected.
- Documents must enter a processing lifecycle after upload.

### Error Statuses

| Status | Meaning |
|---:|---|
| 400 | Missing metadata. |
| 401 | Unauthenticated. |
| 403 | Not authorized to upload. |
| 413 | File too large. |
| 415 | Unsupported file type. |
| 500 | Storage failure. |

---

## 12.4 GET /api/v1/documents

### Use Case

UC-005: Review Document Processing Status.

### Authorization

KnowledgeAdmin, Manager, Admin.

### Response

Paginated list of `DocumentResponse`.

### Supported Filters

- `status`
- `uploadedByUserId`
- `search`
- `from`
- `to`

### Success Status

```text
200 OK
```

### Business Rules

- Organization boundaries apply.
- Role permissions apply.
- Sensitive content must be protected.

---

## 12.5 GET /api/v1/documents/{documentId}

### Use Case

UC-005: Review Document Processing Status.

### Authorization

KnowledgeAdmin, Manager, Admin.

### Response

`DocumentResponse`

### Success Status

```text
200 OK
```

### Error Statuses

| Status | Meaning |
|---:|---|
| 401 | Unauthenticated. |
| 403 | Not authorized. |
| 404 | Document not found or not visible. |

---

## 12.6 POST /api/v1/documents/{documentId}/disable

### Use Case

UC-014: Disable Document from Retrieval.

### Authorization

KnowledgeAdmin, Admin.

### Response

`DocumentResponse`

### Success Status

```text
200 OK
```

### Business Rules

- Only authorized roles may disable documents.
- Documents where `isRetrievalEnabled = false` are not searchable, and the processing status is unchanged.
- Organization boundaries apply.

---

## 12.7 GET /api/v1/documents/{documentId}/processing-status

### Use Case

UC-005: Review Document Processing Status.

### Authorization

KnowledgeAdmin, Manager, Admin.

### Response

`DocumentProcessingStatusResponse`

### Success Status

```text
200 OK
```

---

## 12.8 POST /api/v1/chat/questions

### Use Case

UC-006: Ask Knowledge Question.  
UC-007: Generate RAG Answer with Citations.  
UC-008: Handle Insufficient Context.

### Authorization

Agent, Supervisor, KnowledgeAdmin, Manager, Admin.

### Request

`AskQuestionRequest`

### Response

`AskQuestionResponse`

### Success Status

```text
200 OK
```

### Business Rules

- Authenticated access required.
- Role permissions apply.
- Organization boundaries apply.
- Retrieval must respect authorization.
- Retrieved context must be passed to the RAG prompt.
- Grounded answers require citations.
- Insufficient context must be disclosed.
- AI is not final business authority.

### Error Statuses

| Status | Meaning |
|---:|---|
| 400 | Empty or invalid question. |
| 401 | Unauthenticated. |
| 403 | Not authorized to use chat. |
| 502 | AI provider failure. |
| 503 | Retrieval or service unavailable. |

---

## 12.9 GET /api/v1/chat/sessions

### Use Case

UC-011: Review Chat History.

### Authorization

Authenticated.

### Response

Paginated list of `ChatSessionResponse`.

### Success Status

```text
200 OK
```

### Business Rules

- Users can view their own chat history.
- `Supervisor`, `Manager`, and `Admin` may view scoped history within their organization boundary.
- Organization boundaries apply.

---

## 12.10 GET /api/v1/chat/interactions/{chatInteractionId}

### Use Case

UC-011: Review Chat History.  
UC-009: Review Source Citations.

### Authorization

Owner; `Supervisor`, `Manager`, or `Admin` for scoped review within organization boundary.

### Response

`ChatInteractionResponse`

### Success Status

```text
200 OK
```

---

## 12.11 POST /api/v1/chat/interactions/{chatInteractionId}/feedback

### Use Case

UC-010: Submit Answer Feedback.

### Authorization

Authenticated user with access to interaction.

### Request

`SubmitFeedbackRequest`

### Response

`FeedbackResponse`

### Success Status

```text
200 OK` or `201 Created`
```

### Business Rules

- Feedback must belong to a chat interaction.
- Duplicate feedback must not inflate metrics.
- Organization boundaries apply.
- Metrics must respect access boundaries.

---

## 12.12 GET /api/v1/dashboard/overview

### Use Case

UC-012: Review Operational Dashboard.

### Authorization

Manager, KnowledgeAdmin, Admin.

### Response

`DashboardOverviewResponse`

### Success Status

```text
200 OK
```

### Business Rules

- Metrics must respect access boundaries.
- Dashboard metrics must not expose sensitive content.
- Estimated AI cost should be shown only when available.

---

## 12.13 GET /api/v1/feedback

### Use Case

UC-010: Submit Answer Feedback.

For MVP, this endpoint supports simple `NotUseful` feedback inspection for dashboard and quality signals. It is not the Phase 2 UC-013 knowledge-gap review workflow.

### Authorization

Supervisor, Manager, Admin.

### Response

Paginated feedback review list.

### Success Status

```text
200 OK
```

---

## 12.14 GET /api/v1/health

### Use Case

UC-015: Monitor System Health and Failures.

### Authorization

Public or Authenticated, depending on deployment policy.

### Response

```json
{
  "status": "Healthy",
  "timestamp": "2026-05-20T18:40:00Z"
}
```

---

## 12.15 GET /api/v1/health/details

### Use Case

UC-015: Monitor System Health and Failures.

### Authorization

Admin.

### Response

```json
{
  "status": "Healthy",
  "dependencies": {
    "database": "Healthy",
    "storage": "Healthy",
    "aiProvider": "Healthy",
    "vectorStore": "Healthy"
  },
  "timestamp": "2026-05-20T18:40:00Z"
}
```

---

# 13. API to Use Case Traceability

| API Area | Related Use Cases |
|---|---|
| Authentication | UC-001 |
| User and Role Administration | UC-002, UC-016 |
| Documents | UC-003, UC-005, UC-014 |
| Document Processing Status | UC-004, UC-005 |
| Chat | UC-006, UC-007, UC-008, UC-009, UC-011 |
| Feedback | UC-010, UC-013 |
| Dashboard | UC-012, UC-013 |
| Knowledge Gaps | UC-008, UC-010, UC-013 |
| Health and Observability | UC-015 |
| Access Boundary Enforcement | UC-016 |

---

# 14. API to Business Rule Traceability

| API Area | Related Business Rules |
|---|---|
| Authentication | BR-001 to BR-005 |
| User and Role Administration | BR-001 to BR-005, BR-038 |
| Documents | BR-006 to BR-012, BR-039, BR-040 |
| Document Chunks and Processing | BR-013, BR-014, BR-034, BR-036 |
| Retrieval | BR-015 to BR-018 |
| Chat / RAG | BR-017 to BR-022, BR-042 to BR-045 |
| Citations | BR-019, BR-037, BR-044 |
| Feedback | BR-023 to BR-027, BR-030 |
| Dashboard | BR-028 to BR-033 |
| Health and Observability | BR-034 to BR-037, BR-041 |
| Scope Control | BR-046 to BR-049 |

---

# 15. API to Database Traceability

| API Area | Primary Tables |
|---|---|
| Authentication | users, user_roles, organizations |
| User Administration | users, user_roles, audit_log_entries |
| Documents | documents, document_chunks, chunk_embeddings, audit_log_entries |
| Chat | chat_sessions, chat_interactions, retrieval_results, citations |
| Feedback | answer_feedback |
| Dashboard | chat_interactions, answer_feedback, documents, retrieval_results |
| Knowledge Gaps | chat_interactions, answer_feedback, knowledge_gap_signals |
| Health and Audit | audit_log_entries |

---

# 16. Testing Expectations

API tests should validate both HTTP behavior and business rule enforcement.

## 16.1 Authentication Tests

- Valid login succeeds.
- Invalid login fails.
- Disabled user cannot access protected endpoints.
- `/auth/me` returns current user context.

## 16.2 Authorization Tests

- Unauthenticated users cannot access protected endpoints.
- Users without required roles are rejected.
- Cross-organization resource access is blocked or safely hidden.
- Admin-only endpoints reject non-admin users.

## 16.3 Document API Tests

- Authorized user can upload supported document.
- Unsupported file type is rejected.
- Missing metadata is rejected.
- Uploaded document appears in scoped document list.
- Failed, retrieval-disabled, soft-deleted, or unprocessed documents are not retrievable.

## 16.4 Chat API Tests

- Authenticated user can ask valid question.
- Empty question is rejected.
- Retrieval is invoked before generation.
- Answer includes citations when sources are found.
- Insufficient context returns safe response.
- Chat interaction is stored.

## 16.5 Feedback API Tests

- User can submit useful feedback.
- User can submit not useful feedback.
- Duplicate feedback does not inflate metrics.
- Feedback outside user scope is rejected.

## 16.6 Dashboard API Tests

- Authorized manager can view dashboard.
- Unauthorized user cannot view dashboard.
- Dashboard data is organization-scoped.
- Metrics reflect stored chats, feedback, documents, latency, and cost.

## 16.7 Health API Tests

- Basic health endpoint responds.
- Detailed health endpoint requires Admin role.
- Health details do not expose secrets.

---

# 17. API Design Guidance for AI Agents

AI coding agents must use this document as the source of truth for API-related implementation.

## 17.1 AI Agents Must

- Map endpoints to documented use cases.
- Keep controllers thin.
- Call application services or command/query handlers.
- Preserve request and response DTO boundaries.
- Enforce authentication and authorization.
- Preserve organization-scoped access.
- Preserve RAG retrieval-before-generation.
- Preserve citations for grounded answers.
- Preserve insufficient-context behavior.
- Return consistent error responses.
- Avoid exposing sensitive content in errors.
- Add or update API tests when implementing endpoints.

## 17.2 AI Agents Must Not

- Invent new endpoints outside approved scope.
- Put business rules inside controllers.
- Return database entities directly.
- Expose provider SDK response objects.
- Bypass organization scope filters.
- Generate answers from unauthorized documents.
- Treat AI responses as final authority.
- Add customer-facing chatbot endpoints during MVP.
- Add real-time transcription endpoints during MVP.
- Add autonomous workflow or ticket-action endpoints during MVP.

---

# 18. Summary

This API design translates the approved KnowledgeOps-AI use cases into stable HTTP contracts.

The MVP API is organized around authentication, user administration, document management, document processing, chat, citations, feedback, dashboard metrics, and health monitoring. Dedicated knowledge-gap review endpoints are Phase 2.

The design preserves the project’s core architectural principles:

- Controllers remain thin.
- Application services own use case orchestration.
- Business rules stay outside controllers.
- Provider details stay behind abstractions.
- Protected data is organization-scoped.
- AI answers are retrieval-grounded.
- Citations are required for grounded answers.
- Insufficient context is handled safely.
- Operational metrics remain measurable and secure.

This document should guide backend implementation, frontend integration, OpenAPI generation, integration testing, and AI coding agent behavior.
