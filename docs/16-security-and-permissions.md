# Security and Permissions

## 1. Purpose

This document defines the security and permission model for **KnowledgeOps-AI**.

This document is the canonical MVP security and permission contract.

Security must be designed before protected business APIs are implemented.

KnowledgeOps-AI handles internal documents, chat history, AI-generated answers, citations, feedback, user roles, dashboard metrics, and operational logs. These records may contain sensitive business, operational, client, employee, or compliance-related information.

The purpose of this document is to define:

- Authentication strategy.
- Supported roles.
- Permissions.
- Permission matrix.
- Scope-based access rules.
- Authorization principles.
- Audit-sensitive actions.
- Security assumptions.
- Security constraints.

Frontend visibility is only a user experience concern. Backend authorization remains the source of truth.

---

## 2. Security Goals

The security model must support the following goals:

1. Ensure only authenticated users can access protected functionality.
2. Ensure users can only perform actions allowed by their roles.
3. Ensure users can only access data within their authorized organization scope.
4. Prevent unauthorized access to internal documents, chunks, citations, chats, feedback, dashboard metrics, and audit records.
5. Prevent unauthorized documents from influencing retrieval or AI-generated answers.
6. Protect sensitive document content, prompt content, provider credentials, and operational metadata.
7. Log security-relevant actions without leaking sensitive content.
8. Keep backend authorization as the source of truth.
9. Support secure local development and Azure-ready deployment.
10. Keep AI provider details and secrets out of business logic and source control.

---

# 3. Authentication Strategy

## 3.1 MVP Authentication Strategy

The MVP may use local application authentication with:

- User records stored in the application database.
- Password hashes stored securely.
- Login endpoint returning a JWT or equivalent authenticated session token.
- Role claims or role lookup based on stored user roles.
- Organization scope included in the authenticated user context.

Recommended MVP endpoints:

```text
POST /api/v1/auth/login
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

## 3.2 Authentication Context

After successful authentication, the backend must be able to identify:

- User ID.
- Display name.
- Email.
- User status.
- Assigned roles.
- Organization ID or authorized organization scope.
- Token/session expiration.
- Authentication state.

Example authenticated user context:

```json
{
  "userId": "150ec956-7257-48b9-b9d4-0dcf560db080",
  "email": "jane.agent@example.com",
  "displayName": "Jane Agent",
  "organizationId": "2c75ec98-80d9-44e2-8e65-7461df9e1001",
  "roles": ["Agent"],
  "status": "Active"
}
```

## 3.3 Authentication Rules

### SEC-AUTH-001: Protected Endpoints Require Authentication

All protected endpoints must reject unauthenticated requests.

### SEC-AUTH-002: Disabled Users Must Be Blocked

A disabled user must not be allowed to access protected functionality.

### SEC-AUTH-003: Authentication Errors Must Be Generic

Invalid login attempts must not reveal whether the email, username, or password was incorrect.

### SEC-AUTH-004: User Context Must Be Available to Application Services

Protected application workflows must receive authenticated user identity, roles, and organization scope.

### SEC-AUTH-005: Authentication Tokens Must Expire

Authentication tokens or sessions should have an expiration policy.

## 3.4 Future Authentication Strategy

Future phases may support:

- Azure Entra ID.
- Enterprise SSO.
- External identity providers.
- Permission groups.
- More advanced tenant access.
- Conditional access policies.

These are out of scope for the initial MVP unless formally approved in the roadmap.

---

# 4. Roles

## 4.1 MVP Roles

The MVP supports the following roles:

| Role | Description |
|---|---|
| Agent | Internal support user who asks questions, reviews answers, inspects citations, and submits feedback. |
| Supervisor | Operational leader who supports agents, reviews scoped chat history, feedback, and knowledge gap indicators. |
| KnowledgeAdmin | User responsible for uploading documents, reviewing processing status, and managing document retrieval eligibility. |
| Manager | Operations user who reviews dashboard metrics, usage, adoption, feedback, latency, cost, and knowledge gaps. |
| Admin | System administrator who manages users, roles, access boundaries, health, audit records, and administrative configuration. |

## 4.2 Role Principles

Roles define what actions a user may perform.

Quality Analyst and Trainer are business stakeholder personas, not dedicated MVP RBAC roles. Dedicated QA, Trainer, or Viewer roles are deferred; any exposed MVP review activity is granted only through one of the five roles above.

However, role permission is not enough by itself.

Every protected business action must consider:

```text
Authentication + Role Permission + Organization Scope
```

For example:

- A `Manager` may view dashboard metrics, but only for the organization scope they are authorized to access.
- A `KnowledgeAdmin` may upload documents, but only into their authorized organization scope.
- An `Agent` may ask questions, but retrieval must only use documents they are authorized to access.
- An `Admin` may manage users, but scope rules still apply unless a higher cross-organization administrative model is explicitly defined.

---

# 5. Permissions

## 5.1 Permission Naming Convention

Permissions should be named around business actions.

Recommended format:

```text
Resource.Action
```

Examples:

```text
Documents.Upload
Documents.View
Documents.Disable
Chat.AskQuestion
Chat.ViewOwnHistory
Dashboard.ViewOverview
Users.Manage
System.ViewHealthDetails
```

## 5.2 Permission Catalog

## 5.2.1 Authentication Permissions

| Permission | Description |
|---|---|
| Auth.Login | Allows user login. |
| Auth.ViewCurrentUser | Allows user to view their authenticated context. |

## 5.2.2 User Administration Permissions

| Permission | Description |
|---|---|
| Users.View | View users within authorized scope. |
| Users.Create | Create users. |
| Users.Update | Update users. |
| Users.Disable | Disable users. |
| Users.AssignRole | Assign roles to users. |
| Users.RemoveRole | Remove roles from users. |

## 5.2.3 Document Permissions

| Permission | Description |
|---|---|
| Documents.Upload | Upload internal documents. |
| Documents.View | View document metadata. |
| Documents.ViewProcessingStatus | View document processing status. |
| Documents.ViewChunks | View document chunks or previews. |
| Documents.Disable | Disable document from retrieval. |
| Documents.Enable | Re-enable document for retrieval when eligible (Processed status required). |
| Documents.RetryProcessing | Retry failed document processing (Phase 2). |
| Documents.ViewUsage | View document retrieval or citation usage. |

## 5.2.4 Chat Permissions

| Permission | Description |
|---|---|
| Chat.AskQuestion | Ask natural-language questions. |
| Chat.ViewOwnHistory | View own chat history. |
| Chat.ViewScopedHistory | View scoped team or organization chat history. |
| Chat.ViewInteraction | View a specific chat interaction. |
| Chat.ViewCitations | View citations for an answer. |

## 5.2.5 Feedback Permissions

| Permission | Description |
|---|---|
| Feedback.Submit | Submit useful / not useful feedback. |
| Feedback.UpdateOwn | Update own feedback when supported. |
| Feedback.ViewReviewData | View feedback review data. |

## 5.2.6 Dashboard Permissions

| Permission | Description |
|---|---|
| Dashboard.ViewOverview | View dashboard overview metrics. |
| Dashboard.ViewDocuments | View document processing metrics. |
| Dashboard.ViewChat | View chat usage, latency, cost, and insufficient-context metrics. |
| Dashboard.ViewFeedback | View feedback metrics. |

## 5.2.7 Knowledge Gap Review Permissions (Phase 2)

| Permission | Description |
|---|---|
| KnowledgeGaps.View | View knowledge gap indicators. |
| KnowledgeGaps.Review | Record knowledge gap review decisions. |
| KnowledgeGaps.Resolve | Mark knowledge gap signals as resolved where supported. |

These permissions are reserved for Phase 2. MVP captures scoped insufficient-context and `NotUseful` counts through dashboard data without a dedicated knowledge-gap workflow.

## 5.2.8 System and Audit Permissions

| Permission | Description |
|---|---|
| System.ViewBasicHealth | View basic health status. |
| System.ViewHealthDetails | View detailed health and dependency status. |
| System.ViewProcessingFailures | View recent processing failures. |
| Audit.View | View audit log entries. |

---

# 6. Permission Matrix

## 6.1 High-Level Permission Matrix

| Permission Area | Agent | Supervisor | KnowledgeAdmin | Manager | Admin |
|---|---:|---:|---:|---:|---:|
| Auth.Login | Yes | Yes | Yes | Yes | Yes |
| Auth.ViewCurrentUser | Yes | Yes | Yes | Yes | Yes |
| Users.View | No | No | No | No | Yes |
| Users.Create | No | No | No | No | Yes |
| Users.Update | No | No | No | No | Yes |
| Users.Disable | No | No | No | No | Yes |
| Users.AssignRole | No | No | No | No | Yes |
| Documents.Upload | No | No | Yes | No | Yes |
| Documents.View | No | No | Yes | Yes | Yes |
| Documents.ViewProcessingStatus | No | No | Yes | Yes | Yes |
| Documents.ViewChunks | No | No | Yes | No | Yes |
| Documents.Disable | No | No | Yes | No | Yes |
| Documents.Enable | No | No | Yes | No | Yes |
| Documents.RetryProcessing (Phase 2) | No | No | No | No | No |
| Documents.ViewUsage | No | No | Yes | Yes | Yes |
| Chat.AskQuestion | Yes | Yes | Yes | Yes | Yes |
| Chat.ViewOwnHistory | Yes | Yes | Yes | Yes | Yes |
| Chat.ViewScopedHistory | No | Yes | No | Yes | Yes |
| Chat.ViewInteraction | Own only | Scoped | Scoped | Scoped | Yes |
| Chat.ViewCitations | Own only | Scoped | Scoped | Scoped | Yes |
| Feedback.Submit | Yes | Yes | Yes | Yes | Yes |
| Feedback.UpdateOwn | Yes | Yes | Yes | Yes | Yes |
| Feedback.ViewReviewData | No | Yes | No | Yes | Yes |
| Dashboard.ViewOverview | No | No | Yes | Yes | Yes |
| Dashboard.ViewDocuments | No | No | Yes | Yes | Yes |
| Dashboard.ViewChat | No | No | No | Yes | Yes |
| Dashboard.ViewFeedback | No | Yes | No | Yes | Yes |
| KnowledgeGaps.View (Phase 2) | No | No | No | No | No |
| KnowledgeGaps.Review (Phase 2) | No | No | No | No | No |
| KnowledgeGaps.Resolve (Phase 2) | No | No | No | No | No |
| System.ViewBasicHealth | Yes | Yes | Yes | Yes | Yes |
| System.ViewHealthDetails | No | No | No | No | Yes |
| System.ViewProcessingFailures | No | No | Yes | No | Yes |
| Audit.View | No | No | No | No | Yes |

## 6.2 Notes on Scoped Permissions

| Term | Meaning |
|---|---|
| Own only | User can only access records they created or own. |
| Scoped | User can access records within assigned organization scope and role permissions. |

All MVP permission decisions are explicit in the matrix. Phase 2 permissions are denied in MVP until that workflow is documented and authorized.

---

# 7. Scope-Based Access Rules

## 7.1 Organization Scope

Most business data must be organization-scoped.

Organization-scoped data includes:

- Users.
- Documents.
- Document chunks.
- Embeddings.
- Chat sessions.
- Chat interactions.
- Retrieval results.
- Citations.
- Feedback.
- Knowledge gap signals.
- Dashboard metrics.
- Audit log entries when applicable.

## 7.2 Organization Scope Rule

A user may only access data belonging to their authorized organization scope unless cross-organization access is explicitly implemented and authorized.

For MVP, the default rule is:

```text
CurrentUser.OrganizationId must match Resource.OrganizationId.
```

## 7.3 Scope-Based Endpoint Behavior

| Resource | Scope Rule |
|---|---|
| Documents | User may only view documents in their organization scope. |
| Document Chunks | User may only view chunks for documents they are authorized to access. |
| Embeddings | Embeddings are internal and should not normally be exposed through public APIs. |
| Chat Sessions | Users may view their own sessions; `Supervisor`, `Manager`, and `Admin` may view scoped sessions. |
| Chat Interactions | Users may view their own interactions; `Supervisor`, `Manager`, and `Admin` may view scoped interactions. |
| Retrieval Results | Must only reference authorized chunks. |
| Citations | Must only expose sources the user is authorized to access. |
| Feedback | Must be scoped to the chat interaction and organization. |
| Dashboard Metrics | Must be filtered by organization scope. |
| Audit Logs | Must be restricted to Admin and scoped where applicable. |

## 7.4 Retrieval Scope Rule

Retrieval must apply authorization before selected chunks are used in a RAG prompt.

The retrieval pipeline must exclude:

- Documents outside the user’s organization scope.
- Documents the user is not authorized to access.
- Documents in `Uploaded` status.
- Documents in `Processing` status.
- Documents in `Failed` status.
- Documents where `is_retrieval_enabled = false`.
- Soft-deleted documents.
- Chunks without valid embeddings or searchable vector references.

## 7.5 Citation Scope Rule

Citations must not expose unauthorized documents.

When a citation is returned to a user, the backend must verify that the citation belongs to a chat interaction and source document visible to that user.

## 7.6 Dashboard Scope Rule

Dashboard metrics must be computed using only records visible to the requesting user’s role and organization scope.

The dashboard must not expose:

- Other organizations’ activity.
- Sensitive document content.
- Full prompt content.
- Full chunk text.
- Provider secrets.
- Unauthorized user-level details.

## 7.7 Admin Scope Rule

For MVP, Admin is a high-privilege role inside the current organization scope.

If future phases introduce cross-organization super administrators, that model must be explicitly documented before implementation.

---

# 8. Authorization Principles

## 8.1 Backend Is the Source of Truth

Frontend visibility is not authorization.

The frontend may hide buttons, menus, or screens based on roles, but the backend must enforce every protected action.

Examples:

- Hiding the document upload button from Agents is useful, but the API must still reject Agent upload attempts.
- Hiding dashboard navigation from Agents is useful, but the API must still reject dashboard requests from Agents.
- Hiding admin screens from non-admins is useful, but admin APIs must still enforce Admin permissions.

## 8.2 Deny by Default

If a user’s permission is unclear, the system must deny access.

The default behavior should be:

```text
No explicit permission = access denied.
```

## 8.3 Least Privilege

Users should receive only the permissions necessary for their role.

Examples:

- Agents do not need document upload permissions.
- Managers do not need user role assignment permissions.
- KnowledgeAdmins do not need full audit log access by default.
- Supervisors do not need detailed system health access.

## 8.4 Explicit Role Checks

Protected use cases should define required roles or permissions.

Example:

```text
Upload document requires Documents.Upload.
```

## 8.5 Organization Scope Checks

Protected queries must apply organization scope.

Example:

```text
GET /api/v1/documents/{documentId}
```

The backend must verify that the document belongs to the current user’s organization scope.

## 8.6 Resource Ownership Checks

Some resources require ownership checks.

Examples:

- Users may view their own chat history.
- Users may update their own feedback.
- Users may view their own chat interactions.

Supervisors, Managers, and Admins may have scoped review permissions depending on role.

## 8.7 Safe Failure Responses

Authorization failures must not leak sensitive information.

The API may return:

- `401 Unauthorized` for missing or invalid authentication.
- `403 Forbidden` for authenticated users without permission.
- `404 Not Found` when revealing the existence of a resource would leak cross-scope information.

## 8.8 No Authorization in UI Only

Any authorization rule implemented only in the frontend is incomplete and insecure.

All protected actions must be validated by backend authorization policies or application services.

## 8.9 No Provider-Based Authorization

AI providers, vector stores, or storage providers should not be trusted to enforce application authorization.

The application must enforce authorization before:

- Reading document metadata.
- Accessing chunks.
- Sending retrieved context to the AI provider.
- Returning citations.
- Displaying dashboard metrics.

## 8.10 Authorization Must Be Testable

Critical authorization behavior must have tests.

Required test categories:

- Unauthenticated access rejection.
- Role-based rejection.
- Organization-scope filtering.
- Cross-organization access denial.
- Retrieval authorization filtering.
- Citation access protection.
- Dashboard scope enforcement.
- Admin-only action rejection.

---

# 9. Audit-Sensitive Actions

## 9.1 Actions That Should Be Audited

The following actions should create audit or operational log records.

| Action | Reason |
|---|---|
| User login success | Security traceability. |
| User login failure | Security diagnostics. |
| Disabled user login attempt | Security diagnostics. |
| User created | Administrative traceability. |
| User updated | Administrative traceability. |
| User disabled | Administrative traceability. |
| Role assigned | Permission change traceability. |
| Role removed | Permission change traceability. |
| Document uploaded | Knowledge source traceability. |
| Document processing started | Ingestion lifecycle traceability. |
| Document processing completed | Ingestion lifecycle traceability. |
| Document processing failed | Operational diagnostics. |
| Document disabled from retrieval | Knowledge governance. |
| Document re-enabled for retrieval | Knowledge governance. |
| Chat question submitted | Usage traceability, without unnecessary sensitive content. |
| RAG answer generated | AI usage traceability. |
| Insufficient-context response generated | Knowledge gap visibility. |
| Feedback submitted | Quality review traceability. |
| Negative feedback submitted | Quality review signal. |
| Knowledge gap reviewed (Phase 2) | Continuous improvement traceability if the deferred review workflow is introduced. |
| Dashboard viewed | Optional operational visibility. |
| Authorization failure | Security diagnostics. |
| AI provider failure | Operational diagnostics. |
| Retrieval failure | Operational diagnostics. |
| Health details viewed | Administrative diagnostics. |
| Audit log viewed | Sensitive admin action. |

## 9.2 Audit Log Content

Audit log entries should include:

- Event type.
- Timestamp.
- User ID when available.
- Organization ID when applicable.
- Entity type.
- Entity ID.
- Safe message.
- Severity.
- Correlation ID.

Example:

```json
{
  "eventType": "DocumentProcessingFailed",
  "userId": null,
  "organizationId": "2c75ec98-80d9-44e2-8e65-7461df9e1001",
  "entityType": "Document",
  "entityId": "8c2d5e11-4d8d-4c41-a9c9-9d7e0f6f71a1",
  "severity": "Error",
  "message": "Document processing failed with safe reason: TextExtractionFailed.",
  "correlationId": "9cb05d602db247a88e1cf2e21b346f58",
  "createdAt": "2026-05-20T18:42:00Z"
}
```

## 9.3 Audit Content Restrictions

Audit logs must not include:

- Passwords.
- API keys.
- Provider credentials.
- Full document text.
- Full chunk text.
- Full prompt content unless explicitly protected and required.
- Sensitive customer data.
- Raw provider error payloads if they contain sensitive information.
- Stack traces in user-facing responses.

## 9.4 Audit vs Observability

Audit logs and observability logs are related but not identical.

| Type | Purpose |
|---|---|
| Audit log | Business/security traceability of important actions. |
| Observability log | Technical diagnostics, performance, failures, telemetry. |

Some events may appear in both, but audit logs should remain safe, stable, and reviewable.

---

# 10. Security Assumptions

The security model assumes:

1. Users are internal users of a contact center or support operation.
2. The MVP is not customer-facing.
3. Most users belong to one organization scope.
4. Documents may contain sensitive internal operational information.
5. Authentication is required for all business functionality.
6. Role-based permissions are required for restricted actions.
7. Organization scope is required for data isolation.
8. Frontend visibility does not provide security.
9. Backend APIs must enforce all protected access.
10. AI providers do not enforce application permissions.
11. Retrieval must filter unauthorized chunks before prompt construction.
12. Secrets are stored outside source control.
13. Logs must avoid unnecessary sensitive content.
14. Cost and latency metadata may be visible to managers and administrators but should not expose sensitive document content.
15. Cross-organization super-admin behavior is not part of MVP unless explicitly introduced later.

---

# 11. Security Constraints

## 11.1 MVP Security Constraints

The MVP must enforce:

- Authentication for protected endpoints.
- Role-based authorization.
- Organization-aware access filtering.
- Backend authorization as source of truth.
- Safe handling of authorization failures.
- Secure storage of password hashes if local auth is used.
- No hardcoded secrets.
- No raw provider credentials in logs.
- No retrieval from unauthorized documents.
- No citations exposing unauthorized sources.
- No dashboard metrics leaking other organizations.
- No AI prompt context from unauthorized documents.

## 11.2 AI Security Constraints

The AI workflow must enforce:

- Retrieval access checks before prompt construction.
- No unauthorized chunks in prompts.
- No use of failed, retrieval-disabled, soft-deleted, or unprocessed documents for retrieval.
- No unsupported policy claims when context is insufficient.
- No customer-facing chatbot behavior during MVP.
- No autonomous business actions during MVP.
- No final-authority behavior from the assistant.

## 11.3 Data Security Constraints

The data layer must support:

- Organization-scoped queries.
- Foreign key relationships for traceability.
- Audit fields.
- Soft delete where historical traceability matters.
- Exclusion of deleted, retrieval-disabled, failed, or unprocessed documents from retrieval.
- Nullable cost fields when cost is unavailable.
- Safe audit log messages.

## 11.4 Operational Security Constraints

Operational behavior must support:

- Logging authorization failures.
- Logging provider failures safely.
- Restricting health details to Admin.
- Restricting audit log access to Admin.
- Avoiding sensitive content in dashboard metrics.
- Avoiding raw exception details in user-facing responses.
- Using correlation IDs for diagnostics where practical.

---

# 12. Endpoint Authorization Reference

## 12.1 Authentication Endpoints

| Method | Endpoint | Required Permission |
|---|---|---|
| POST | `/api/v1/auth/login` | Public |
| POST | `/api/v1/auth/logout` | Authenticated |
| GET | `/api/v1/auth/me` | Auth.ViewCurrentUser |

## 12.2 User Administration Endpoints

| Method | Endpoint | Required Permission |
|---|---|---|
| GET | `/api/v1/users` | Users.View |
| POST | `/api/v1/users` | Users.Create |
| GET | `/api/v1/users/{userId}` | Users.View |
| PUT | `/api/v1/users/{userId}` | Users.Update |
| POST | `/api/v1/users/{userId}/roles` | Users.AssignRole |
| DELETE | `/api/v1/users/{userId}/roles/{roleName}` | Users.RemoveRole |

## 12.3 Document Endpoints

| Method | Endpoint | Required Permission |
|---|---|---|
| POST | `/api/v1/documents` | Documents.Upload |
| GET | `/api/v1/documents` | Documents.View |
| GET | `/api/v1/documents/{documentId}` | Documents.View |
| GET | `/api/v1/documents/{documentId}/processing-status` | Documents.ViewProcessingStatus |
| GET | `/api/v1/documents/{documentId}/chunks` | Documents.ViewChunks |
| POST | `/api/v1/documents/{documentId}/disable` | Documents.Disable |
| POST | `/api/v1/documents/{documentId}/enable` | Documents.Enable |
| POST | `/api/v1/documents/{documentId}/retry-processing` (Phase 2) | Documents.RetryProcessing |

MVP supports document retrieval disablement; re-enable and processing-retry operations are deferred to Phase 2.

## 12.4 Chat Endpoints

| Method | Endpoint | Required Permission |
|---|---|---|
| POST | `/api/v1/chat/questions` | Chat.AskQuestion |
| GET | `/api/v1/chat/sessions` | Chat.ViewOwnHistory |
| POST | `/api/v1/chat/sessions` | Chat.AskQuestion |
| GET | `/api/v1/chat/sessions/{chatSessionId}` | Chat.ViewOwnHistory or Chat.ViewScopedHistory |
| GET | `/api/v1/chat/interactions/{chatInteractionId}` | Chat.ViewInteraction |
| GET | `/api/v1/chat/interactions/{chatInteractionId}/citations` | Chat.ViewCitations |

## 12.5 Feedback Endpoints

| Method | Endpoint | Required Permission |
|---|---|---|
| POST | `/api/v1/chat/interactions/{chatInteractionId}/feedback` | Feedback.Submit |
| PUT | `/api/v1/chat/interactions/{chatInteractionId}/feedback` | Feedback.UpdateOwn |
| GET | `/api/v1/feedback` | Feedback.ViewReviewData |

## 12.6 Dashboard Endpoints

| Method | Endpoint | Required Permission |
|---|---|---|
| GET | `/api/v1/dashboard/overview` | Dashboard.ViewOverview |
| GET | `/api/v1/dashboard/documents` | Dashboard.ViewDocuments |
| GET | `/api/v1/dashboard/chat` | Dashboard.ViewChat |
| GET | `/api/v1/dashboard/feedback` | Dashboard.ViewFeedback |

## 12.7 Knowledge Gap Endpoints (Phase 2, Not MVP)

| Method | Endpoint | Required Permission |
|---|---|---|
| GET | `/api/v1/knowledge-gaps` | KnowledgeGaps.View |
| GET | `/api/v1/knowledge-gaps/{knowledgeGapId}` | KnowledgeGaps.View |
| POST | `/api/v1/knowledge-gaps/{knowledgeGapId}/review` | KnowledgeGaps.Review |

These endpoints are deferred to Phase 2 and have no allowed MVP role assignment.

## 12.8 Health and Audit Endpoints

| Method | Endpoint | Required Permission |
|---|---|---|
| GET | `/api/v1/health` | Public safe status or System.ViewBasicHealth according to deployment policy |
| GET | `/api/v1/health/details` | System.ViewHealthDetails |
| GET | `/api/v1/admin/processing-failures` | System.ViewProcessingFailures |
| GET | `/api/v1/admin/audit-log` | Audit.View |

---

# 13. Security Testing Expectations

Security tests should validate that backend authorization is enforced independently from frontend visibility.

## 13.1 Authentication Tests

- Unauthenticated users cannot access protected endpoints.
- Invalid credentials are rejected.
- Disabled users cannot authenticate or access protected endpoints.
- Current user context returns correct roles and organization scope.

## 13.2 Role Authorization Tests

- Agents cannot upload documents.
- Agents cannot view dashboard metrics.
- Agents cannot access user management.
- KnowledgeAdmins can upload documents.
- Managers can view dashboard metrics.
- Non-admins cannot manage users.
- Non-admins cannot view detailed health or audit logs.

## 13.3 Organization Scope Tests

- Users cannot access documents outside their organization.
- Users cannot access chat interactions outside their organization.
- Users cannot access feedback outside their organization.
- Dashboard metrics exclude other organizations.
- Citation access is blocked for unauthorized source documents.

## 13.4 Retrieval Security Tests

- Retrieval excludes unauthorized documents.
- Retrieval excludes failed documents.
- Retrieval excludes documents where `is_retrieval_enabled = false`.
- Retrieval excludes unprocessed documents.
- Prompt construction receives only authorized chunks.
- AI answer generation cannot use unauthorized source content.

## 13.5 Audit Tests

- Authorization failures are logged safely.
- Document upload events are logged.
- Processing failures are logged.
- Role changes are logged.
- Audit logs do not contain secrets.
- Audit logs do not contain full document text or sensitive prompt content.

---

# 14. Security Guidance for AI Agents

AI coding agents must use this document before implementing protected APIs, frontend visibility logic, database queries, retrieval logic, or dashboard metrics.

## 14.1 AI Agents Must

- Treat backend authorization as the source of truth.
- Enforce authentication on protected endpoints.
- Enforce role permissions.
- Enforce organization scope.
- Apply authorization before retrieval.
- Exclude unauthorized chunks before prompt construction.
- Protect citation access.
- Protect dashboard metrics.
- Keep audit logs safe.
- Avoid logging sensitive document or prompt content.
- Add tests for authorization and scope behavior.
- Keep security behavior aligned with business rules and API design.

## 14.2 AI Agents Must Not

- Rely only on frontend visibility for security.
- Add endpoints without authorization requirements.
- Allow arbitrary organization IDs to bypass current user scope.
- Return database entities directly from protected APIs.
- Retrieve chunks before applying authorization filters.
- Send unauthorized content to AI providers.
- Expose provider secrets in errors or logs.
- Expose raw provider errors to users.
- Add customer-facing chatbot access during MVP.
- Add real-time call transcription during MVP.
- Add autonomous business actions during MVP.
- Treat Admin as cross-organization super-admin unless explicitly documented.

---

# 15. Summary

This document defines the security and permission model for KnowledgeOps-AI.

The system uses authentication, role-based permissions, organization-aware access rules, backend authorization, audit-sensitive logging, and secure handling of AI workflows.

The most important security principle is:

```text
Frontend visibility is not security.
Backend authorization is the source of truth.
```

Every protected business workflow must validate authentication, permissions, and organization scope.

Retrieval must never include unauthorized documents, and AI prompts must never contain unauthorized context.

This security model must be implemented before protected business APIs are exposed.
