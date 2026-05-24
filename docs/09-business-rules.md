# Business Rules

## 1. Purpose

This document defines the explicit business rules that govern the behavior of **KnowledgeOps-AI**.

Business rules should be stable, traceable, and referenced by use cases, tests, domain logic, application services, implementation issues, and AI coding agents.

Rules must not be hidden inside controllers, UI components, database scripts, provider integrations, or undocumented assumptions.

KnowledgeOps-AI is an enterprise AI-powered internal knowledge assistant for contact centers and support operations. The system allows authorized users to upload internal documents, process them into searchable knowledge, ask questions through a Retrieval-Augmented Generation (RAG) assistant, receive source-grounded answers, review citations, provide feedback, and monitor operational metrics.

---

## 2. Rule Format

Each rule follows this format:

```text
BR-000: A specific business condition must result in a specific system behavior.
```

Each business rule should be:

- Explicit.
- Testable.
- Traceable.
- Stable.
- Independent from UI implementation details.
- Independent from a specific AI provider when possible.
- Enforced consistently across API, domain logic, application services, and background processes.

---

## 3. Rule Categories

The business rules are grouped into the following categories:

| Category | Description |
|---|---|
| Access and Security Rules | Rules for authentication, authorization, roles, and organization boundaries. |
| Document Lifecycle Rules | Rules for upload, processing, status, failure, and retrieval eligibility. |
| Retrieval and RAG Rules | Rules for semantic retrieval, answer generation, grounding, citations, and insufficient context. |
| Feedback and Review Rules | Rules for answer feedback, review signals, and knowledge gap visibility. |
| Dashboard and Metrics Rules | Rules for operational metrics, cost, latency, and scoped reporting. |
| Logging and Observability Rules | Rules for traceability, diagnostics, and sensitive data protection. |
| Administration Rules | Rules for user, role, document, and platform management. |
| AI Governance Rules | Rules that prevent unsafe or unsupported AI behavior. |
| Scope Control Rules | Rules that protect the project from uncontrolled expansion. |

---

# 4. Access and Security Rules

## BR-001: Authenticated Access Required

Protected system functionality must only be accessible to authenticated users.

### System Behavior

The system must reject unauthenticated requests to protected endpoints, screens, workflows, and data resources.

### Applies To

- Document upload.
- Document listing.
- Document details.
- Chat assistant.
- Chat history.
- Feedback.
- Dashboard.
- User management.
- System health views.
- Administration features.

### Test Expectations

- Unauthenticated requests to protected APIs are rejected.
- Authenticated requests include user identity context.
- Public endpoints are explicitly documented if any exist.

---

## BR-002: Role Permissions Apply

A user may only perform actions allowed by their assigned role.

### System Behavior

The system must evaluate the user’s role before allowing restricted actions.

### Applies To

- Agent chat usage.
- Knowledge administrator document upload.
- Manager dashboard access.
- Admin user management.
- Health or operational monitoring.
- Document disablement.
- Scoped review views.

### Test Expectations

- Users with valid roles can perform allowed actions.
- Users without required roles are rejected.
- Role-restricted APIs return authorization failures when accessed by unauthorized roles.

---

## BR-003: Organization Boundaries Apply

A user may only access data within their authorized organization or equivalent access scope.

### System Behavior

The system must filter or reject access to documents, chats, citations, feedback, retrieval results, dashboard metrics, and administration records outside the user’s authorized scope.

### Applies To

- Documents.
- Document chunks.
- Embeddings.
- Retrieval results.
- Chat history.
- Feedback.
- Dashboard metrics.
- User management.
- Citation access.

### Test Expectations

- Users cannot access documents outside their organization scope.
- Users cannot retrieve chunks from unauthorized documents.
- Dashboard data is scoped by organization.
- Cross-organization access attempts are rejected or filtered safely.

---

## BR-004: Unauthorized Access Must Be Rejected Safely

When a user attempts an unauthorized action, the system must reject the action without exposing sensitive data.

### System Behavior

The system must return a safe authorization failure and must not reveal protected document content, prompt content, internal metadata, or other sensitive details.

### Applies To

- API responses.
- Frontend error states.
- Logs.
- Retrieval.
- Dashboard access.
- Citation access.

### Test Expectations

- Unauthorized requests fail safely.
- Error responses do not expose sensitive content.
- Authorization failures are logged without leaking protected data.

---

## BR-005: User Identity Must Be Available for Protected Actions

Every protected action must be associated with an authenticated user identity.

### System Behavior

The system must associate protected requests with the current user identifier, role, and organization scope.

### Applies To

- Uploads.
- Chat questions.
- Feedback.
- Admin actions.
- Dashboard views.
- Document disablement.
- Chat history access.

### Test Expectations

- Protected actions include current user context.
- Stored records include user references where required.
- Missing identity context prevents protected actions.

---

# 5. Document Lifecycle Rules

## BR-006: Uploaded Documents Must Have Metadata

Every uploaded document must have required metadata before it can enter the processing lifecycle.

### System Behavior

The system must reject document uploads that are missing required metadata.

### Required Metadata

Minimum required metadata should include:

- Document identifier.
- File name.
- Content type.
- File size.
- Upload timestamp.
- Uploaded by user identifier.
- Organization identifier.
- Processing status.

### Test Expectations

- Uploads without required metadata are rejected.
- Valid uploads store metadata.
- Stored metadata is retrievable by authorized users.

---

## BR-007: Unsupported File Types Must Be Rejected

A document with an unsupported file type must not be accepted for processing.

### System Behavior

The system must validate uploaded file type before storage or processing and reject unsupported formats with a clear validation message.

### Applies To

Initial supported formats may include:

- PDF.
- TXT.
- Markdown.
- DOCX if practical for the MVP.

### Test Expectations

- Supported file types are accepted.
- Unsupported file types are rejected.
- Rejected files do not enter the processing lifecycle.

---

## BR-008: Documents Must Enter a Processing Lifecycle After Upload

A valid uploaded document must enter the document processing lifecycle.

### System Behavior

After upload, the system must create a document record and assign an initial processing status.

### Minimum Statuses

- Uploaded.
- Processing.
- Processed.
- Failed.

Retrieval availability is not a processing status. It is recorded independently through `is_retrieval_enabled`.

### Test Expectations

- Valid uploads create document records.
- Uploaded documents receive the correct initial status.
- Processing status changes are persisted.

---

## BR-009: Documents Must Be Processed Before Retrieval

A document must reach `Processed` status before its chunks can be used for retrieval.

### System Behavior

The retrieval pipeline must exclude documents that have not completed processing.

### Applies To

- Uploaded documents.
- Processing documents.
- Failed documents.
- Documents where `is_retrieval_enabled = false`.
- Partially processed documents.

### Test Expectations

- Unprocessed documents are excluded from retrieval.
- Processing documents are excluded from retrieval.
- Only processed, retrieval-enabled, non-soft-deleted, organization-authorized documents can contribute chunks to retrieval.

---

## BR-010: Failed Documents Are Not Searchable

A document in `Failed` status must not be used as a retrieval source.

### System Behavior

The retrieval pipeline must exclude failed documents and their chunks.

### Test Expectations

- Failed documents do not appear in retrieval results.
- Failed documents do not contribute citations.
- Failed status is visible to authorized users.

---

## BR-011: Retrieval-Disabled Documents Are Not Searchable

A document where `is_retrieval_enabled = false` must not be used as a retrieval source.

### System Behavior

When a document is disabled from retrieval, the system must set `is_retrieval_enabled = false` and exclude its chunks from future retrieval without changing its processing outcome.

### Test Expectations

- Retrieval-disabled documents are excluded from retrieval.
- Retrieval-disabled documents cannot be used to generate new grounded answers.
- Historical citations may remain available according to retention rules.

---

## BR-012: Processing Failures Must Store a Reason

When document processing fails, the system must store a failure reason visible to authorized users.

### System Behavior

The system must record why processing failed when practical.

### Possible Failure Reasons

- Unsupported or corrupted file.
- Text extraction failed.
- Extracted text was empty.
- Chunking failed.
- Embedding generation failed.
- Storage failure.
- Provider failure.

### Test Expectations

- Failed documents include a failure reason.
- Authorized users can view the failure reason.
- Failure reasons do not expose sensitive content unnecessarily.

---

## BR-013: Document Chunks Must Preserve Source Relationship

Every stored document chunk must maintain a relationship to its source document.

### System Behavior

Chunks must reference the source document and organization scope.

### Test Expectations

- Each chunk has a document reference.
- Each chunk has organization scope.
- Citations can be traced back to source documents.

---

## BR-014: Empty Chunks Must Not Be Stored

The system must not store empty or meaningless chunks.

### System Behavior

The chunking process must exclude chunks that have no usable text content.

### Test Expectations

- Empty extracted text does not produce chunks.
- Empty chunks are filtered out.
- Documents with no usable text fail safely or are marked according to implementation design.

---

# 6. Retrieval and RAG Rules

## BR-015: Retrieval Must Respect Authorization

The retrieval process must only consider chunks from documents the user is authorized to access.

### System Behavior

The retrieval pipeline must apply organization and permission filtering before returning candidate chunks for answer generation.

### Test Expectations

- Unauthorized chunks are excluded from retrieval.
- Retrieval does not leak unauthorized document metadata.
- Cross-organization retrieval is blocked.

---

## BR-016: Retrieval Must Exclude Ineligible Documents

The retrieval process must exclude documents that are not eligible for retrieval.

### System Behavior

Retrieval must only use chunks from documents that are:

- Processed.
- Retrieval-enabled (`is_retrieval_enabled = true`).
- Not soft-deleted (`deleted_at IS NULL`).
- Within the user’s organization scope.
- Authorized for the user.

### Test Expectations

- Uploaded, processing, failed, retrieval-disabled, and soft-deleted documents are excluded.
- Unauthorized documents are excluded.
- Retrieval results contain only eligible chunks.

---

## BR-017: Retrieved Context Must Be Passed to the RAG Prompt

When relevant chunks are found, the system must include retrieved document context in the RAG prompt.

### System Behavior

The prompt construction process must include selected retrieved chunks and appropriate instructions for grounded answer generation.

### Test Expectations

- Prompt construction includes retrieved context.
- Prompt construction references relevant sources.
- Prompt templates are testable without live AI calls.

---

## BR-018: Answers Should Be Grounded in Retrieved Sources

Generated answers should be based on retrieved document chunks when sources are found.

### System Behavior

The AI assistant must be instructed to answer using the provided document context and avoid unsupported business claims.

### Test Expectations

- RAG answers are generated only after retrieval.
- Prompt instructions require grounding.
- Answers are associated with retrieved context metadata.

---

## BR-019: Citations Are Required for Grounded Answers

When an answer is generated from retrieved document chunks, the system must include source citations.

### System Behavior

The response must identify the sources used to support the answer.

### Citation Data Should Include

- Document identifier.
- Document title or name.
- Chunk identifier or reference.
- Page or section when available.
- Relevance score when practical.

### Test Expectations

- Grounded answers include citations.
- Citations reference source documents.
- Citations reference supporting chunks or equivalent references.

---

## BR-020: Insufficient Context Must Be Disclosed

When the system does not find enough relevant context, it must disclose that the available documents are insufficient.

### System Behavior

The assistant must return a safe insufficient-context response instead of inventing unsupported information.

### Test Expectations

- Questions with no relevant chunks receive insufficient-context responses.
- Weak retrieval results do not produce unsupported policy claims.
- Insufficient-context events are stored for review.

---

## BR-021: Unsupported Policy Claims Must Not Be Presented as Official Guidance

The system must not present AI-generated content as official policy unless the answer is grounded in retrieved approved sources.

### System Behavior

When no sufficient source exists, the assistant must avoid definitive operational, HR, legal, compliance, or policy claims.

### Test Expectations

- Unsupported answers are not marked as official.
- Insufficient context is handled safely.
- The assistant recommends human escalation when appropriate.

---

## BR-022: Human Escalation Should Be Suggested When Context Is Insufficient

When the available documents do not support an answer, the system should suggest contacting a supervisor, knowledge administrator, or appropriate human owner.

### System Behavior

The assistant should provide a safe next step instead of fabricating an answer.

### Test Expectations

- Insufficient-context responses include a safe next-step message where appropriate.
- The response does not invent a business policy.
- The question is captured for review.

---

# 7. Feedback and Review Rules

## BR-023: Feedback Must Belong to a Chat Interaction

User feedback must be associated with a stored chat interaction.

### System Behavior

The system must not store orphan feedback that cannot be traced to a question and answer.

### Test Expectations

- Feedback requires a valid chat interaction.
- Feedback is linked to the correct interaction.
- Feedback can be used in dashboard metrics.

---

## BR-024: Duplicate Feedback Must Not Inflate Metrics

The same user must not be able to inflate feedback metrics by repeatedly rating the same answer.

### System Behavior

The system should either reject duplicate feedback from the same user for the same answer or update the existing feedback.

### Test Expectations

- Duplicate feedback does not create duplicate metric counts.
- Feedback update behavior is consistent if supported.
- Feedback metrics remain accurate.

---

## BR-025: Negative Feedback Must Be Available for Review

Answers marked as not useful must be available to authorized reviewers.

### System Behavior

The system must allow authorized roles to inspect negative feedback according to role and organization scope.

### Applies To

- Supervisors.
- Operations Managers.
- Knowledge Administrators where appropriate.
- Admins.

Quality Analysts and Trainers are business stakeholder personas, not dedicated MVP RBAC roles. If they perform this MVP activity, they use an approved role with review permission.

### Test Expectations

- Negative feedback can be queried by authorized users.
- Negative feedback is organization-scoped.
- Unauthorized users cannot access feedback review data.

---

## BR-026: Insufficient-Context Questions Must Contribute to MVP Metrics

Questions that cannot be answered due to insufficient context must be stored and counted in authorized MVP dashboard metrics.

### System Behavior

The system must record insufficient-context events and expose scoped counts through the MVP dashboard. A dedicated knowledge-gap queue, categorization, assignment, review decision, resolution workflow, or clustering capability is Phase 2.

### Test Expectations

- Insufficient-context events are persisted.
- Authorized dashboard users can view scoped counts.
- Any future review data remains scoped by organization.

---

## BR-027: Review Signals Must Respect Access Boundaries

Feedback, insufficient-context event metrics, and any future review signals must only be visible to authorized users.

### System Behavior

The system must enforce role and organization scope when exposing MVP metric data or future review data.

### Test Expectations

- MVP feedback and insufficient-context metrics are organization-scoped.
- Unauthorized users cannot access metric or feedback data.
- Future review APIs must enforce access rules if introduced in Phase 2.

---

# 8. Dashboard and Metrics Rules

## BR-028: Metrics Must Respect Access Boundaries

Dashboard metrics must be scoped according to the viewer’s role and organization permissions.

### System Behavior

The dashboard must not expose data outside the user’s authorized scope.

### Test Expectations

- Dashboard metrics are organization-scoped.
- Unauthorized dashboard access is rejected.
- Cross-organization metrics are not leaked.

---

## BR-029: Chat Activity Must Update Usage Metrics

Stored chat interactions must contribute to usage metrics.

### System Behavior

The system must use stored chat records to calculate metrics such as question count and active usage.

### Test Expectations

- New chat interactions increase question counts.
- Dashboard reflects chat activity.
- Metrics are scoped correctly.

---

## BR-030: Feedback Must Update Feedback Metrics

Stored answer feedback must contribute to feedback metrics.

### System Behavior

The system must use feedback records to calculate useful and not useful counts.

### Test Expectations

- Useful feedback updates useful count.
- Not useful feedback updates not useful count.
- Feedback metrics are scoped correctly.

---

## BR-031: Document Processing Status Must Update Document Metrics

Document status changes must contribute to document metrics.

### System Behavior

The dashboard must reflect document counts by status where applicable.

### Test Expectations

- Uploaded documents increase document counts.
- Processed documents increase processed counts.
- Failed documents increase failed counts.
- Retrieval-disabled documents are represented consistently if availability counts are included.

---

## BR-032: Latency Must Be Captured for Chat Interactions

The system must capture response latency for chat interactions.

### System Behavior

The system must record timing metadata for chat workflows.

### Test Expectations

- Chat interactions include latency metadata when available.
- Average latency can be computed.
- Latency metrics are shown to authorized dashboard users.

---

## BR-033: Estimated AI Cost Should Be Captured When Available

The system should capture estimated AI usage cost when provider metadata or internal calculation is available.

### System Behavior

The system should store cost metadata or mark it as unavailable when cost cannot be estimated.

### Test Expectations

- Cost is displayed when available.
- Cost is not misleadingly shown as zero when unavailable.
- Cost metrics are scoped correctly.

---

# 9. Logging and Observability Rules

## BR-034: Important Business Events Must Be Logged

The system must log important business workflow events.

### Events Should Include

- Document upload.
- Document processing started.
- Document processing completed.
- Document processing failed.
- Chat question received.
- Retrieval completed.
- AI generation completed.
- AI generation failed.
- Feedback submitted.
- Authorization failure.

### Test Expectations

- Important events produce logs.
- Logs support operational diagnosis.
- Logs avoid unnecessary sensitive content.

---

## BR-035: Authorization Failures Must Be Logged

Authorization failures must be logged for operational and security review.

### System Behavior

The system must log rejected unauthorized access attempts without exposing sensitive content.

### Test Expectations

- Unauthorized requests generate safe logs.
- Logs include enough context for diagnosis.
- Logs do not leak protected data.

---

## BR-036: AI Provider Failures Must Be Logged

Failures from AI generation, embedding, or retrieval provider integrations must be logged.

### System Behavior

The system must log provider failures and return safe user-facing responses.

### Test Expectations

- AI generation failures are logged.
- Embedding failures are logged.
- Provider error details are handled safely.
- User-facing messages avoid provider-sensitive details.

---

## BR-037: Sensitive Content Must Be Protected

Sensitive document content, prompts, responses, secrets, and credentials must not be exposed unnecessarily through logs, errors, metrics, or dashboard views.

### System Behavior

The system must avoid logging or displaying sensitive content unless explicitly required and properly authorized.

### Test Expectations

- Logs avoid full document content.
- Error messages avoid sensitive content.
- Dashboard metrics do not expose private document text.
- Secrets are not logged.

---

# 10. Administration Rules

## BR-038: Only Authorized Administrators May Manage Users

User management actions must only be available to authorized administrators.

### System Behavior

The system must restrict user creation, role assignment, and access updates to authorized roles.

### Test Expectations

- Admin users can manage users within scope.
- Non-admin users cannot manage users.
- User management actions are logged.

---

## BR-039: Only Authorized Roles May Upload Documents

Document upload must only be available to authorized roles.

### System Behavior

The system must restrict document upload to `KnowledgeAdmin` or `Admin`.

### Test Expectations

- Authorized users can upload documents.
- Unauthorized users cannot upload documents.
- Upload attempts are logged.

---

## BR-040: Only Authorized Roles May Disable Documents From Retrieval

Document disablement from retrieval must only be available to authorized roles.

### System Behavior

The system must restrict setting `is_retrieval_enabled = false` to `KnowledgeAdmin` or `Admin` roles and retain the document processing status.

### Test Expectations

- Authorized users can disable documents from retrieval.
- Unauthorized users cannot disable documents.
- Retrieval-disabled documents are excluded from retrieval.

---

## BR-041: Detailed System Health Information Must Be Restricted

Detailed system health and operational diagnostics must only be available to authorized administrative users. A basic safe health status may be public or authenticated according to deployment policy.

### System Behavior

The system must restrict `/api/v1/health/details` to `Admin`. `/api/v1/health` must expose only safe basic status and must not expose dependency details or sensitive configuration.

### Test Expectations

- Authorized administrators can view detailed health information.
- Non-admin users cannot view detailed health information.
- Basic and detailed health information avoids exposing sensitive content.

---

# 11. AI Governance Rules

## BR-042: AI Is Not Final Business Authority

The AI assistant must support human decision-making and must not act as the final authority for business, legal, HR, compliance, or operational policy decisions.

### System Behavior

The assistant must avoid presenting itself as the final decision-maker.

### Test Expectations

- System prompts and UI language describe the assistant as decision support.
- Insufficient-context cases suggest human escalation.
- AI answers do not override business rules.

---

## BR-043: Provider Details Must Not Drive Business Rules

Business rules must not depend directly on a specific AI provider implementation.

### System Behavior

Provider-specific behavior must be isolated behind infrastructure or provider interfaces where practical.

### Applies To

- AI generation.
- Embeddings.
- Vector search.
- File storage.
- Secrets.
- Cloud telemetry.

### Test Expectations

- Domain and application rules do not depend on provider-specific SDK types.
- Provider implementations are replaceable behind abstractions.
- Business behavior remains stable if provider implementation changes.

---

## BR-044: AI Responses Must Be Traceable

AI-generated answers must be traceable to their input question, retrieved context, citations, and metadata where available.

### System Behavior

The system must store enough metadata to review how a response was produced.

### Test Expectations

- Chat records include question and answer.
- Source citations are preserved.
- Retrieval metadata is stored where practical.
- Latency and cost metadata are stored where available.

---

## BR-045: AI Must Handle Missing Knowledge Safely

The AI assistant must not invent knowledge when required information is missing from available documents.

### System Behavior

The assistant must return an insufficient-context response or limited supported answer when retrieved context is missing or weak.

### Test Expectations

- Missing context produces safe responses.
- Unsupported claims are avoided.
- Missing knowledge is recorded for review.

---

# 12. Scope Control Rules

## BR-046: MVP Must Prioritize the Core Knowledge Assistant Workflow

The MVP must prioritize document upload, document processing, retrieval, RAG answers, citations, feedback, access control, and basic metrics.

### System Behavior

Implementation work must favor the approved MVP workflow over advanced or unrelated features.

### Test Expectations

- MVP implementation maps to approved scope.
- Out-of-scope features are not introduced without scope revision.
- Implementation issues reference approved requirements or use cases.

---

## BR-047: Real-Time Agent Assist Is Out of Scope for MVP

Real-time call transcription, live agent assist, and transcript-based suggestions must not be implemented in the MVP.

### System Behavior

The system must remain focused on document-based knowledge retrieval during MVP.

### Test Expectations

- MVP contains no real-time call transcription.
- MVP contains no live agent assist workflow.
- MVP contains no transcript-based automation.

---

## BR-048: Customer-Facing Chatbot Behavior Is Out of Scope for MVP

The MVP must not expose the assistant as a customer-facing chatbot.

### System Behavior

The assistant must be designed for internal users only during MVP.

### Test Expectations

- Chat access requires authenticated internal users.
- No public customer-facing chat endpoint is introduced.
- UI language describes the assistant as internal.

---

## BR-049: Autonomous Business Actions Are Out of Scope for MVP

The AI assistant must not perform autonomous business actions such as ticket updates, policy enforcement, account changes, customer messaging, or workflow execution during MVP.

### System Behavior

The assistant may provide decision support but must not execute autonomous operational actions.

### Test Expectations

- AI responses do not trigger automatic business changes.
- No autonomous ticket or CRM actions exist in MVP.
- Human users remain responsible for operational decisions.

---

# 13. Rule Traceability

## 13.1 Rule-to-Use-Case Traceability

| Business Rule | Related Use Cases |
|---|---|
| BR-001 | UC-001, UC-002, UC-003, UC-006, UC-010, UC-011, UC-012, UC-015, UC-016 |
| BR-002 | UC-001, UC-002, UC-003, UC-005, UC-006, UC-011, UC-012, UC-013, UC-014, UC-015, UC-016 |
| BR-003 | UC-001, UC-002, UC-003, UC-005, UC-006, UC-009, UC-010, UC-011, UC-012, UC-013, UC-014, UC-016 |
| BR-004 | UC-001, UC-002, UC-006, UC-009, UC-010, UC-012, UC-015, UC-016 |
| BR-005 | UC-001, UC-003, UC-006, UC-010, UC-012, UC-014, UC-016 |
| BR-006 | UC-003 |
| BR-007 | UC-003 |
| BR-008 | UC-003, UC-004, UC-005 |
| BR-009 | UC-004, UC-005, UC-006, UC-007 |
| BR-010 | UC-004, UC-005, UC-007 |
| BR-011 | UC-005, UC-014 |
| BR-012 | UC-004, UC-005, UC-015 |
| BR-013 | UC-004, UC-007, UC-009 |
| BR-014 | UC-004 |
| BR-015 | UC-006, UC-007, UC-009, UC-016 |
| BR-016 | UC-006, UC-007, UC-014, UC-016 |
| BR-017 | UC-006, UC-007 |
| BR-018 | UC-006, UC-007 |
| BR-019 | UC-007, UC-009 |
| BR-020 | UC-006, UC-008, UC-013 |
| BR-021 | UC-006, UC-007, UC-008 |
| BR-022 | UC-008 |
| BR-023 | UC-010 |
| BR-024 | UC-010 |
| BR-025 | UC-010, UC-013 |
| BR-026 | UC-008, UC-013 |
| BR-027 | UC-010, UC-011, UC-012, UC-013 |
| BR-028 | UC-012, UC-013, UC-016 |
| BR-029 | UC-006, UC-012 |
| BR-030 | UC-010, UC-012 |
| BR-031 | UC-003, UC-004, UC-005, UC-012 |
| BR-032 | UC-006, UC-007, UC-012 |
| BR-033 | UC-006, UC-007, UC-012 |
| BR-034 | UC-003, UC-004, UC-006, UC-010, UC-015 |
| BR-035 | UC-001, UC-002, UC-003, UC-009, UC-012, UC-014, UC-015, UC-016 |
| BR-036 | UC-004, UC-006, UC-007, UC-015 |
| BR-037 | UC-001, UC-002, UC-003, UC-006, UC-009, UC-011, UC-012, UC-015, UC-016 |
| BR-038 | UC-002 |
| BR-039 | UC-003 |
| BR-040 | UC-014 |
| BR-041 | UC-015 |
| BR-042 | UC-006, UC-007, UC-008 |
| BR-043 | UC-004, UC-007, UC-015 |
| BR-044 | UC-007, UC-009, UC-011, UC-013 |
| BR-045 | UC-006, UC-007, UC-008 |
| BR-046 | UC-003, UC-004, UC-006, UC-007, UC-010, UC-012 |
| BR-047 | Scope governance |
| BR-048 | Scope governance |
| BR-049 | Scope governance |

---

## 13.2 Rule-to-Requirement Traceability

| Business Rule | Related Requirements |
|---|---|
| BR-001 | FR-001, FR-002, NFR-001 |
| BR-002 | FR-006, FR-007, FR-009, NFR-002 |
| BR-003 | FR-005, FR-010, FR-043, FR-046, FR-086, NFR-003, NFR-007 |
| BR-004 | FR-009, FR-097, FR-099, NFR-006 |
| BR-005 | FR-004, FR-005 |
| BR-006 | FR-012, FR-013 |
| BR-007 | FR-013, FR-014 |
| BR-008 | FR-019, FR-021 |
| BR-009 | FR-026, FR-041, FR-045, FR-046 |
| BR-010 | FR-021, FR-024, FR-026, FR-045 |
| BR-011 | FR-027, FR-045 |
| BR-012 | FR-024, FR-091, NFR-008 |
| BR-013 | FR-030, FR-031, FR-058 to FR-063 |
| BR-014 | FR-032, FR-033 |
| BR-015 | FR-043, FR-046, NFR-003, NFR-007 |
| BR-016 | FR-026, FR-027, FR-038, FR-045, FR-046 |
| BR-017 | FR-048, FR-050, FR-051 |
| BR-018 | FR-052, NFR-034 |
| BR-019 | FR-058 to FR-063, NFR-037 |
| BR-020 | FR-055, FR-075, NFR-035 |
| BR-021 | FR-056, NFR-036 |
| BR-022 | FR-057 |
| BR-023 | FR-068 to FR-071 |
| BR-024 | FR-070, FR-072 |
| BR-025 | FR-066, FR-072, FR-083 |
| BR-026 | FR-075, FR-084 |
| BR-027 | FR-010, FR-066, FR-085, FR-086 |
| BR-028 | FR-085, FR-086, NFR-003 |
| BR-029 | FR-064, FR-076 |
| BR-030 | FR-068 to FR-072, FR-083 |
| BR-031 | FR-021, FR-080 to FR-082 |
| BR-032 | FR-064, FR-078, NFR-013 |
| BR-033 | FR-079, NFR-028 |
| BR-034 | FR-092 to FR-098, NFR-024 |
| BR-035 | FR-097, NFR-024, NFR-027 |
| BR-036 | FR-037, FR-096, NFR-010 |
| BR-037 | FR-099, NFR-004, NFR-006 |
| BR-038 | FR-087 to FR-089 |
| BR-039 | FR-011, FR-013, FR-018 |
| BR-040 | FR-027, FR-045 |
| BR-041 | FR-090, FR-091 |
| BR-042 | FR-055 to FR-057, NFR-038 |
| BR-043 | FR-035, FR-049, NFR-020 |
| BR-044 | FR-064, FR-067, FR-073, FR-074 |
| BR-045 | FR-055, FR-056, FR-075, NFR-035, NFR-036 |
| BR-046 | MVP scope requirements |
| BR-047 | Scope and Roadmap |
| BR-048 | Scope and Roadmap |
| BR-049 | Scope and Roadmap |

---

# 14. Enforcement Guidance

Business rules should be enforced in the most appropriate layer of the application.

| Rule Type | Preferred Enforcement Location |
|---|---|
| Authentication and authorization | API middleware, authorization policies, application services |
| Organization scope | Application services, query filters, retrieval filters |
| Document status eligibility | Domain/application services, retrieval services |
| Upload validation | Application services, API validation |
| Chunk and embedding validity | Document processing services |
| RAG grounding behavior | Prompt orchestration, RAG application services |
| Citation requirement | RAG orchestration and response mapping |
| Feedback ownership | Application services and persistence rules |
| Metrics scope | Dashboard application services and queries |
| Sensitive logging rules | Logging policy and infrastructure services |
| Provider isolation | Application interfaces and infrastructure implementations |
| Scope control | Documentation, implementation issues, pull request review |

---

# 15. AI Agent Guidance

AI coding agents must use this document as a source of truth when generating implementation plans, code, tests, refactors, or documentation updates.

AI agents must:

- Reference relevant business rules when implementing features.
- Avoid hiding business rules inside UI components.
- Avoid bypassing authorization or organization boundaries.
- Avoid adding out-of-scope features.
- Keep AI provider details outside business rule logic.
- Preserve citation and insufficient-context behavior.
- Add or update tests when business rules are implemented.
- Update this document if a new stable business rule is introduced.

AI agents must not:

- Invent undocumented business rules.
- Remove source citation requirements.
- Treat AI answers as final business authority.
- Add customer-facing chatbot behavior during MVP.
- Add real-time call transcription during MVP.
- Add autonomous operational actions during MVP.
- Expose unauthorized documents through retrieval or citations.

---

# 16. Summary

This document defines the explicit business rules that govern KnowledgeOps-AI.

The rules ensure that the system remains secure, source-grounded, auditable, measurable, and aligned with contact center operations.

They protect the project from hidden logic, inconsistent implementation, unsafe AI behavior, and uncontrolled scope expansion.

These business rules should be referenced by use cases, requirements, domain logic, application services, tests, implementation issues, pull requests, and AI coding agents.
