# Use Cases

## 1. Purpose

This document describes how actors interact with **KnowledgeOps-AI** to achieve business goals.

Use cases connect business actors, system behavior, business rules, and testable outcomes. They are a primary input for implementation issues, API design, frontend workflows, and testing strategy.

KnowledgeOps-AI is an enterprise AI-powered internal knowledge assistant for contact centers and support operations. The system allows authorized users to upload internal documents, process them into searchable knowledge, ask questions through a Retrieval-Augmented Generation (RAG) assistant, review citations, provide feedback, and monitor operational metrics.

---

## 2. Actors

The following actors are referenced in these use cases.

| Actor | Description |
|---|---|
| Support Agent | User who asks operational questions and uses answers during support work. |
| Supervisor | User who supports agents, reviews usage patterns, and identifies knowledge gaps. |
| Operations Manager | User who reviews dashboard metrics and business value indicators. |
| Knowledge Administrator | User who uploads and manages documents used by the assistant. |
| Quality Analyst | Business stakeholder persona who reviews answer quality; not a separate MVP RBAC role. |
| Trainer | Business stakeholder persona who improves training material; not a separate MVP RBAC role. |
| System Administrator | User who manages users, roles, access, and platform configuration. |
| AI Assistant | The system behavior that retrieves document context and generates grounded answers. |
| Background Processor | The system component that extracts, chunks, embeds, and indexes uploaded documents. |

MVP technical authorization uses only `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, and `Admin`. Where Quality Analyst or Trainer activities are exposed in MVP, an approved technical role is assigned; dedicated roles and the full review workflow are deferred.

---

## 3. Use Case Overview

| Use Case ID | Name | Primary Actor |
|---|---|---|
| UC-001 | Authenticate User | Support Agent / Any User |
| UC-002 | Manage Users and Roles | System Administrator |
| UC-003 | Upload Internal Document | Knowledge Administrator |
| UC-004 | Process Uploaded Document | Background Processor |
| UC-005 | Review Document Processing Status | Knowledge Administrator |
| UC-006 | Ask Knowledge Question | Support Agent |
| UC-007 | Generate RAG Answer with Citations | AI Assistant |
| UC-008 | Handle Insufficient Context | AI Assistant |
| UC-009 | Review Source Citations | Support Agent |
| UC-010 | Submit Answer Feedback | Support Agent |
| UC-011 | Review Chat History | Support Agent / Supervisor |
| UC-012 | Review Operational Dashboard | Operations Manager |
| UC-013 | Review Knowledge Gaps (Phase 2 workflow; MVP metrics only) | Supervisor / Manager / KnowledgeAdmin / Admin |
| UC-014 | Disable Document from Retrieval | Knowledge Administrator |
| UC-015 | Monitor System Health and Failures | System Administrator |
| UC-016 | Validate Access Boundaries | System Administrator |

---

# UC-001: Authenticate User

## Use Case ID

UC-001

## Name

Authenticate User

## Primary Actor

Support Agent / Any User

## Goal

Allow a registered user to access KnowledgeOps-AI securely.

## Preconditions

- The user exists in the system.
- The user has valid credentials or an approved authentication method.
- The user has at least one assigned role.
- The user belongs to an organization or approved access scope.

## Main Flow

1. The user opens the application.
2. The system displays the login interface.
3. The user enters authentication credentials.
4. The system validates the credentials.
5. The system identifies the user.
6. The system loads the user’s roles and organization scope.
7. The system creates an authenticated session or returns an authentication token.
8. The user is redirected to the appropriate application area based on role.

## Alternative Flows

### AF-001.1: Invalid Credentials

1. The user enters invalid credentials.
2. The system rejects the login attempt.
3. The system displays a generic authentication error.
4. The system does not reveal whether the username or password was incorrect.

### AF-001.2: User Has No Role

1. The user authenticates successfully.
2. The system detects that the user has no assigned role.
3. The system blocks access to protected functionality.
4. The system displays an access configuration error.

### AF-001.3: User Is Disabled

1. The user attempts to log in.
2. The system detects that the account is disabled.
3. The system rejects access.
4. The system logs the failed access attempt.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The user is authenticated.
- The system knows the user identity, roles, and organization scope.
- Protected functionality becomes available according to permissions.

## Acceptance Criteria

- Valid users can log in successfully.
- Invalid credentials are rejected.
- Disabled users cannot access the system.
- The authenticated user context includes user ID, role, and organization scope.
- Protected endpoints reject unauthenticated requests.

---

# UC-002: Manage Users and Roles

## Use Case ID

UC-002

## Name

Manage Users and Roles

## Primary Actor

System Administrator

## Goal

Allow authorized administrators to manage users, roles, and access boundaries.

## Preconditions

- The actor is authenticated.
- The actor has the `Admin` role.
- The system has role and organization concepts configured.

## Main Flow

1. The System Administrator opens the user management area.
2. The system displays users within the administrator’s allowed scope.
3. The administrator selects a user.
4. The system displays user details, assigned roles, and organization scope.
5. The administrator updates the user’s role or organization access.
6. The system validates the requested changes.
7. The system saves the updated user configuration.
8. The system logs the administrative change.

## Alternative Flows

### AF-002.1: Unauthorized Administrator Action

1. A user without administrator permissions attempts to manage users.
2. The system rejects the request.
3. The system returns an authorization failure.

### AF-002.2: Invalid Role Assignment

1. The administrator attempts to assign an invalid role.
2. The system rejects the change.
3. The system displays a validation error.

### AF-002.3: Cross-Organization Access Violation

1. The administrator attempts to manage a user outside their allowed scope.
2. The system rejects the request.
3. The system logs the authorization failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- User role or organization access is updated when valid.
- Invalid or unauthorized changes are rejected.
- Administrative changes are logged.

## Acceptance Criteria

- Admin users can view users within scope.
- Admin users can assign supported roles.
- Non-admin users cannot manage users.
- Invalid role assignments are rejected.
- Cross-organization user management is blocked.

---

# UC-003: Upload Internal Document

## Use Case ID

UC-003

## Name

Upload Internal Document

## Primary Actor

Knowledge Administrator

## Goal

Allow authorized users to upload internal documents that will later be processed and made available for retrieval.

## Preconditions

- The actor is authenticated.
- The actor has KnowledgeAdmin or Admin permission.
- The document file exists and is accessible to the actor.
- The document format is supported by the MVP.
- The document belongs to an organization or approved access scope.

## Main Flow

1. The Knowledge Administrator opens the document management area.
2. The system displays the document upload interface.
3. The actor selects a document file.
4. The actor enters or confirms required metadata.
5. The system validates the file type, file size, and metadata.
6. The system stores the uploaded file using the configured storage provider.
7. The system creates a document metadata record.
8. The system assigns the document to the actor’s organization or selected authorized scope.
9. The system sets the document status to `Uploaded`.
10. The system schedules the document for background processing.
11. The system displays the uploaded document in the document list.

## Alternative Flows

### AF-003.1: Unsupported File Type

1. The actor selects an unsupported file type.
2. The system rejects the upload.
3. The system displays a clear validation message.

### AF-003.2: Missing Metadata

1. The actor submits the upload without required metadata.
2. The system rejects the request.
3. The system identifies missing fields.

### AF-003.3: Unauthorized Upload

1. A user without document upload permission attempts to upload a document.
2. The system rejects the request.
3. The system logs the authorization failure.

### AF-003.4: Storage Failure

1. The system validates the upload.
2. The file storage provider fails.
3. The system rejects the upload or marks it as failed according to implementation design.
4. The system logs the failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The document file is stored.
- Document metadata is created.
- The document is scoped to an organization.
- The document is scheduled for processing.
- The document is not yet available for retrieval until processed.

## Acceptance Criteria

- Authorized users can upload supported documents.
- Unsupported formats are rejected.
- Uploaded document metadata is stored.
- Uploaded documents appear in the document list.
- Uploaded documents are assigned to an organization scope.
- Unauthorized users cannot upload documents.

---

# UC-004: Process Uploaded Document

## Use Case ID

UC-004

## Name

Process Uploaded Document

## Primary Actor

Background Processor

## Goal

Transform an uploaded document into searchable chunks with embeddings.

## Preconditions

- A document exists with status `Uploaded`.
- The document file is available in storage.
- The document has valid metadata.
- The document format is supported.
- The AI embedding provider or compatible embedding service is configured.

## Main Flow

1. The Background Processor selects a document pending processing.
2. The system changes the document status to `Processing`.
3. The system retrieves the stored document file.
4. The system extracts text from the document.
5. The system validates that extracted text is not empty.
6. The system splits the extracted text into chunks.
7. The system stores chunk records with source document references.
8. The system generates embeddings for each chunk.
9. The system stores embedding data or searchable vector references.
10. The system marks the document as `Processed`.
11. The system logs the processing completion.

## Alternative Flows

### AF-004.1: Text Extraction Fails

1. The processor retrieves the file.
2. Text extraction fails.
3. The system marks the document as `Failed`.
4. The system stores a failure reason.
5. The system logs the failure.

### AF-004.2: Extracted Text Is Empty

1. The processor extracts text.
2. The extracted text is empty or below a minimum useful threshold.
3. The system marks the document as `Failed`.
4. The system stores a failure reason.

### AF-004.3: Embedding Provider Fails

1. The system creates chunks.
2. Embedding generation fails.
3. The system marks the document as `Failed` or stores failed chunk embedding state according to implementation design.
4. The system logs the provider failure.

### AF-004.4: Partial Processing Failure

1. Some chunks are created successfully.
2. A later processing step fails.
3. The system prevents incomplete or invalid chunks from being used for retrieval.
4. The system records failure status or remediation metadata.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- If successful, the document status is `Processed`.
- If successful, chunks and embeddings are available for retrieval.
- If failed, the document status is `Failed`.
- Failure reasons are visible to authorized users.
- Failed or incomplete documents are not searchable.

## Acceptance Criteria

- Uploaded documents move through processing states.
- Supported documents produce stored chunks.
- Chunks maintain source document references.
- Embeddings or searchable vector references are created.
- Failed processing attempts store failure reasons.
- Failed documents are excluded from retrieval.

---

# UC-005: Review Document Processing Status

## Use Case ID

UC-005

## Name

Review Document Processing Status

## Primary Actor

Knowledge Administrator

## Goal

Allow authorized users to verify whether uploaded documents are ready for retrieval or require attention.

## Preconditions

- The actor is authenticated.
- The actor has the `KnowledgeAdmin` or `Admin` role.
- At least one document exists within the actor’s organization scope.

## Main Flow

1. The Knowledge Administrator opens the document management area.
2. The system retrieves documents within the actor’s organization scope.
3. The system displays document metadata and processing status.
4. The actor selects a document.
5. The system displays document details.
6. The system displays processing status, timestamps, and failure reason if applicable.
7. The actor determines the processing status and whether retrieval is enabled.

## Alternative Flows

### AF-005.1: No Documents Exist

1. The actor opens the document management area.
2. The system finds no documents within scope.
3. The system displays an empty state.

### AF-005.2: Document Failed Processing

1. The actor opens a failed document.
2. The system displays the failure status and reason.
3. The actor may decide to replace the document or retry processing if supported in a later phase.

### AF-005.3: Unauthorized Document View

1. The actor attempts to view a document outside their organization scope.
2. The system rejects the request.
3. The system logs the authorization failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The actor understands the document’s processing state.
- Failed documents can be identified.
- Processed documents can be retrieved only when retrieval is enabled and access rules allow it.
- Unauthorized document access is blocked.

## Acceptance Criteria

- Authorized users can view document processing status.
- Failed documents display failure reason.
- Processed documents are clearly marked.
- Retrieval-disabled documents are clearly marked separately from processing status.
- Document lists are scoped by organization.
- Unauthorized document access is rejected.

---

# UC-006: Ask Knowledge Question

## Use Case ID

UC-006

## Name

Ask Knowledge Question

## Primary Actor

Support Agent

## Goal

Allow a support user to ask a natural-language question about internal operational knowledge.

## Preconditions

- The actor is authenticated.
- The actor has permission to use the chat assistant.
- At least one relevant processed document exists in the actor’s organization scope.
- The chat assistant and retrieval services are available.

## Main Flow

1. The Support Agent opens the chat interface.
2. The system displays the input field for a question.
3. The agent enters a natural-language question.
4. The system validates the question.
5. The system identifies the actor’s user ID, role, and organization scope.
6. The system sends the question to the RAG orchestration workflow.
7. The system retrieves relevant chunks.
8. The system generates an answer using retrieved context.
9. The system returns the answer with citations.
10. The system stores the chat interaction.
11. The system displays the answer to the agent.

## Alternative Flows

### AF-006.1: Empty Question

1. The agent submits an empty question.
2. The system rejects the request.
3. The system asks the agent to enter a valid question.

### AF-006.2: No Relevant Context

1. The agent asks a question.
2. The system does not find enough relevant context.
3. The system returns an insufficient-context response.
4. The system stores the interaction as insufficient context.

### AF-006.3: AI Provider Failure

1. The system retrieves context.
2. The AI generation provider fails.
3. The system returns a safe error message.
4. The system logs the failure.

### AF-006.4: Unauthorized Chat Access

1. A user without chat permission attempts to ask a question.
2. The system rejects the request.
3. The system logs the authorization failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The question is processed.
- The answer is displayed when generation succeeds.
- Citations are displayed when sources are used.
- The chat interaction is stored.
- Latency and evaluation metadata are captured when available.

## Acceptance Criteria

- Authenticated users can submit valid questions.
- Empty questions are rejected.
- Retrieval runs before answer generation.
- Answers are displayed with citations when sources are found.
- Insufficient context is handled safely.
- Chat interactions are persisted.

---

# UC-007: Generate RAG Answer with Citations

## Use Case ID

UC-007

## Name

Generate RAG Answer with Citations

## Primary Actor

AI Assistant

## Goal

Generate an answer grounded in retrieved internal document chunks and return source citations.

## Preconditions

- The user has submitted a valid question.
- The user is authenticated.
- Retrieval has returned relevant document chunks.
- The AI generation provider is available.
- Retrieved chunks are within the user’s authorized scope.

## Main Flow

1. The system receives the validated user question.
2. The system retrieves relevant chunks.
3. The system ranks or selects the top chunks according to retrieval configuration.
4. The system builds a prompt using the user question and retrieved context.
5. The system includes instructions to answer only from available context.
6. The system calls the AI generation provider.
7. The AI provider returns a generated answer.
8. The system maps retrieved chunks to citations.
9. The system returns the answer and citations to the user.
10. The system stores prompt, response, retrieval, latency, and estimated cost metadata where available.

## Alternative Flows

### AF-007.1: Retrieved Context Is Weak

1. Retrieval returns chunks below an acceptable relevance threshold.
2. The system treats the situation as insufficient context.
3. The system avoids generating an unsupported answer.

### AF-007.2: Provider Timeout

1. The system sends the prompt to the AI generation provider.
2. The provider times out.
3. The system returns a safe failure message.
4. The system logs the timeout.

### AF-007.3: Missing Citation Metadata

1. The answer is generated from retrieved chunks.
2. Some citation metadata is incomplete.
3. The system returns available citation fields.
4. The system does not hide the fact that metadata is incomplete.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The user receives a grounded answer.
- The user receives citations.
- The interaction is stored.
- Evaluation metadata is available for later review.

## Acceptance Criteria

- RAG answers use retrieved context.
- Prompt construction includes retrieved chunks.
- Returned answers include citations.
- Retrieved sources are authorized for the user.
- The system records retrieval and generation metadata.
- The system does not generate unsupported policy claims when context is weak.

---

# UC-008: Handle Insufficient Context

## Use Case ID

UC-008

## Name

Handle Insufficient Context

## Primary Actor

AI Assistant

## Goal

Respond safely when available documents do not contain enough information to answer a user question.

## Preconditions

- A user has submitted a question.
- Retrieval returns no chunks or weakly relevant chunks.
- The system determines that context is insufficient.

## Main Flow

1. The system receives the user question.
2. The system attempts retrieval.
3. The system finds no relevant chunks or insufficiently relevant chunks.
4. The system avoids generating unsupported operational guidance.
5. The system returns a response explaining that available documents do not contain enough information.
6. The system may recommend contacting a supervisor or knowledge administrator.
7. The system stores the interaction with an insufficient-context marker.
8. The system includes the event in dashboard or knowledge gap metrics.

## Alternative Flows

### AF-008.1: Partial Context Exists

1. Retrieval returns some related context but not enough to answer fully.
2. The system provides a limited answer only for the supported part.
3. The system clearly states what is not covered by the available documents.

### AF-008.2: User Rephrases Question

1. The user asks a follow-up or rephrased question.
2. The system performs retrieval again.
3. If relevant context is found, the system proceeds with normal RAG answer generation.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The system does not invent unsupported information.
- The user is informed that context is insufficient.
- The question is stored for review.
- The event may contribute to knowledge gap metrics.

## Acceptance Criteria

- The system detects when no relevant source is found.
- The system returns a safe insufficient-context message.
- The system does not fabricate policy details.
- The interaction is stored.
- The insufficient-context event is available for dashboard or review.

---

# UC-009: Review Source Citations

## Use Case ID

UC-009

## Name

Review Source Citations

## Primary Actor

Support Agent

## Goal

Allow users to inspect which documents supported an AI-generated answer.

## Preconditions

- The actor is authenticated.
- The actor submitted or can view a chat response.
- The answer includes source citations.
- The cited documents are within the actor’s authorized access scope.

## Main Flow

1. The system displays the generated answer.
2. The system displays source citations associated with the answer.
3. The actor selects or expands a citation.
4. The system displays citation details.
5. Citation details may include document name, chunk reference, page or section reference, and relevance metadata.
6. The actor uses the citation to validate trust in the answer.

## Alternative Flows

### AF-009.1: Citation Metadata Is Partial

1. The actor opens a citation.
2. Some metadata, such as page number, is not available.
3. The system displays available citation data.
4. The system avoids implying missing metadata exists.

### AF-009.2: Source Document Is No Longer Available

1. The actor opens a citation from historical chat.
2. The source document has been disabled or removed from active retrieval.
3. The system displays available historical citation metadata according to retention rules.
4. The system prevents unauthorized or invalid source access.

### AF-009.3: Unauthorized Citation Access

1. The actor attempts to open a citation outside their access scope.
2. The system blocks access.
3. The system logs the authorization failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The actor can identify the sources used for the answer.
- The actor can judge whether the answer is trustworthy.
- Unauthorized citation access is blocked.

## Acceptance Criteria

- Citations are visible for grounded answers.
- Citations identify source documents.
- Citations identify chunks or equivalent references.
- Page or section references appear when available.
- Unauthorized source access is blocked.

---

# UC-010: Submit Answer Feedback

## Use Case ID

UC-010

## Name

Submit Answer Feedback

## Primary Actor

Support Agent

## Goal

Allow users to mark an AI-generated answer as useful or not useful.

## Preconditions

- The actor is authenticated.
- A chat interaction exists.
- The actor is authorized to provide feedback on the answer.

## Main Flow

1. The system displays an answer.
2. The system displays feedback options.
3. The actor selects `Useful` or `Not Useful`.
4. The system validates that the actor may provide feedback.
5. The system stores the feedback.
6. The system associates the feedback with the chat interaction.
7. The system updates feedback metrics.
8. The system confirms feedback submission.

## Alternative Flows

### AF-010.1: Duplicate Feedback

1. The actor submits feedback for an answer already rated by the same actor.
2. The system either rejects the duplicate or updates the existing feedback according to implementation design.
3. The system avoids counting duplicate feedback incorrectly.

### AF-010.2: Unauthorized Feedback

1. A user attempts to submit feedback for an answer outside their access scope.
2. The system rejects the request.
3. The system logs the authorization failure.

### AF-010.3: Feedback Storage Failure

1. The actor submits feedback.
2. The system fails to store the feedback.
3. The system displays an error message.
4. The system logs the failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- Feedback is stored.
- Feedback is linked to the chat interaction.
- Feedback metrics are updated.
- Feedback can be used for dashboard and quality review.

## Acceptance Criteria

- Users can mark answers as useful.
- Users can mark answers as not useful.
- Feedback is associated with the correct chat interaction.
- Duplicate feedback does not inflate metrics.
- Unauthorized feedback is rejected.

---

# UC-011: Review Chat History

## Use Case ID

UC-011

## Name

Review Chat History

## Primary Actor

Support Agent / Supervisor

## Goal

Allow users to review prior chat interactions according to their permissions.

## Preconditions

- The actor is authenticated.
- Chat interactions exist.
- The actor has permission to view the requested chat history.

## Main Flow

1. The actor opens the chat history view.
2. The system identifies the actor’s role and organization scope.
3. The system retrieves chat interactions allowed for the actor.
4. The system displays questions, answers, timestamps, and feedback status.
5. The actor selects a specific interaction.
6. The system displays answer details and citations.
7. The actor reviews the prior answer.

## Alternative Flows

### AF-011.1: No Chat History Exists

1. The actor opens chat history.
2. The system finds no allowed chat interactions.
3. The system displays an empty state.

### AF-011.2: Supervisor Reviews Team History

1. A supervisor opens team chat history.
2. The system retrieves interactions within the supervisor’s authorized organization scope.
3. The system displays allowed interactions only.

### AF-011.3: Unauthorized History Access

1. The actor attempts to access chat history outside their scope.
2. The system rejects the request.
3. The system logs the authorization failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The actor can review allowed chat history.
- Unauthorized history remains inaccessible.
- Historical citations remain associated with the interaction.

## Acceptance Criteria

- Users can view their own chat history.
- Authorized `Supervisor`, `Manager`, and `Admin` users can view scoped team history within organization scope.
- Chat history includes question, answer, timestamp, and citation references.
- Unauthorized history access is blocked.

---

# UC-012: Review Operational Dashboard

## Use Case ID

UC-012

## Name

Review Operational Dashboard

## Primary Actor

Operations Manager

## Goal

Allow managers to review operational metrics related to usage, documents, latency, cost, feedback, and insufficient context.

## Preconditions

- The actor is authenticated.
- The actor has Manager, Admin, or equivalent dashboard permission.
- Dashboard data exists or can be computed.
- Metrics are scoped to the actor’s organization or allowed boundary.

## Main Flow

1. The Operations Manager opens the dashboard.
2. The system validates dashboard permission.
3. The system determines the actor’s organization scope.
4. The system retrieves dashboard metrics.
5. The system displays question count.
6. The system displays active user count when available.
7. The system displays average response latency.
8. The system displays estimated AI cost when available.
9. The system displays document counts and processing status counts.
10. The system displays feedback counts.
11. The system displays insufficient-context counts.
12. The actor reviews metrics for operational insight.

## Alternative Flows

### AF-012.1: No Metrics Available

1. The actor opens the dashboard.
2. The system finds no data.
3. The system displays an empty or zero-state dashboard.

### AF-012.2: Cost Metadata Unavailable

1. The dashboard loads.
2. Estimated cost metadata is not available.
3. The system displays the metric as unavailable instead of zero if zero would be misleading.

### AF-012.3: Unauthorized Dashboard Access

1. A user without dashboard permission opens the dashboard.
2. The system rejects the request.
3. The system logs the authorization failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The actor can review scoped operational metrics.
- Dashboard data supports operational decisions.
- Unauthorized dashboard access is blocked.

## Acceptance Criteria

- Authorized managers can view dashboard metrics.
- Dashboard metrics are scoped by organization.
- Metrics include questions, documents, feedback, latency, and insufficient context.
- Estimated cost is shown when available.
- Unauthorized users cannot view dashboard data.

---

# UC-013: Review Knowledge Gaps

## Use Case ID

UC-013

## Name

Review Knowledge Gaps (Phase 2 Workflow)

## Primary Actor

Supervisor / Manager / KnowledgeAdmin / Admin

## Goal

In Phase 2, identify repeated questions, insufficient-context events, and low-quality answer patterns that indicate documentation or training gaps.

## MVP Scope Boundary

The MVP stores insufficient-context events and `NotUseful` feedback and exposes basic scoped counts through dashboard metrics. It does not require a dedicated knowledge-gap queue, categorization, assignment, review decisions, resolution workflow, clustering, or dedicated QA/Trainer role.

## Preconditions

- The actor is authenticated.
- The actor has permission to review knowledge gap indicators.
- Chat, feedback, or insufficient-context data exists.
- The data belongs to the actor’s organization scope.

## Main Flow

1. The actor opens a dashboard or review view.
2. The system validates the actor’s role and organization scope.
3. The system retrieves insufficient-context questions.
4. The system retrieves not useful feedback counts or patterns.
5. The system retrieves repeated questions when available.
6. The system displays knowledge gap indicators.
7. The actor reviews the indicators.
8. The actor determines whether documentation, training, or process clarification is needed.

## Alternative Flows

### AF-013.1: No Knowledge Gaps Detected

1. The actor opens the review view.
2. The system finds no insufficient-context events or negative feedback.
3. The system displays an empty or healthy state.

### AF-013.2: Similar Question Grouping Not Available

1. The actor opens the review view.
2. The system has individual insufficient-context events but no clustering.
3. The system displays individual events.
4. Grouping may be deferred to a future phase.

### AF-013.3: Unauthorized Review Access

1. A user without review permission attempts to access knowledge gap data.
2. The system rejects the request.
3. The system logs the authorization failure.

## Business Rules

Canonical business-rule applicability for this Phase 2 workflow is defined in `docs/09-business-rules.md`, Section 13.1. MVP implements only the underlying scoped feedback and insufficient-context metrics.

## Postconditions

- The actor can identify potential knowledge gaps.
- Documentation or training improvements can be planned.
- Review data remains scoped and protected.

## Acceptance Criteria

- Authorized users can view insufficient-context counts or records.
- Not useful feedback is available for review.
- Review data is organization-scoped.
- Unauthorized users cannot access review data.
- Knowledge gap review can inform training or document updates.

---

# UC-014: Disable Document from Retrieval

## Use Case ID

UC-014

## Name

Disable Document from Retrieval

## Primary Actor

Knowledge Administrator

## Goal

Allow authorized users to prevent a document from being used in future retrieval.

## Preconditions

- The actor is authenticated.
- The actor has the `KnowledgeAdmin` or `Admin` role.
- The document exists.
- The document belongs to the actor’s authorized organization scope.

## Main Flow

1. The Knowledge Administrator opens the document management area.
2. The actor selects a document.
3. The system displays document details.
4. The actor chooses to disable the document from retrieval.
5. The system asks for confirmation.
6. The actor confirms the action.
7. The system sets `is_retrieval_enabled = false` and preserves the existing processing status.
8. The system prevents the document’s chunks from appearing in future retrieval.
9. The system logs the document disable action.

## Alternative Flows

### AF-014.1: Actor Cancels Disable Action

1. The actor chooses to disable a document.
2. The system asks for confirmation.
3. The actor cancels.
4. The system leaves the document unchanged.

### AF-014.2: Unauthorized Disable Action

1. A user without permission attempts to disable a document.
2. The system rejects the request.
3. The system logs the authorization failure.

### AF-014.3: Document Outside Scope

1. The actor attempts to disable a document outside their organization scope.
2. The system rejects the request.
3. The system logs the authorization failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- The document is excluded from future retrieval.
- Historical chat citations may remain available according to retention rules.
- The disable action is logged.

## Acceptance Criteria

- Authorized users can disable documents from retrieval.
- Retrieval-disabled documents are excluded from retrieval results.
- Unauthorized users cannot disable documents.
- Organization scope is enforced.
- The disable action is logged.

---

# UC-015: Monitor System Health and Failures

## Use Case ID

UC-015

## Name

Monitor System Health and Failures

## Primary Actor

System Administrator

## Goal

Allow administrators to monitor platform health, failures, and important operational events.

## Preconditions

- The actor is authenticated.
- The actor has Admin or equivalent operational permission.
- The system emits logs, health information, or operational status data.

## Main Flow

1. The System Administrator opens a health or operations view.
2. The system validates administrator permission.
3. The system displays basic system health.
4. The system displays document processing failure counts or recent failures.
5. The system displays AI provider failure indicators when available.
6. The system displays background processing status when available.
7. The administrator reviews operational status.
8. The administrator investigates issues as needed.

## Alternative Flows

### AF-015.1: Health Data Unavailable

1. The administrator opens the health view.
2. The system cannot retrieve health data.
3. The system displays a safe error message.
4. The system logs the failure.

### AF-015.2: AI Provider Failure

1. The system detects AI provider failure.
2. The system logs the failure.
3. The health view or logs expose the failure to authorized administrators.

### AF-015.3: Unauthorized Health Access

1. A non-admin user attempts to access health information.
2. The system rejects the request.
3. The system logs the authorization failure.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- Administrators can understand system health at a basic level.
- Operational failures are visible to authorized users.
- Sensitive content remains protected.

## Acceptance Criteria

- Authorized administrators can view basic health information.
- Document processing failures are visible.
- AI provider failures are logged.
- Unauthorized health access is blocked.
- Health information avoids exposing sensitive document or prompt content.

---

# UC-016: Validate Access Boundaries

## Use Case ID

UC-016

## Name

Validate Access Boundaries

## Primary Actor

System Administrator

## Goal

Ensure that users can only access documents, chats, citations, feedback, retrieval results, and dashboard metrics within their authorized role and organization scope.

## Preconditions

- Users exist across one or more organization scopes.
- Documents, chats, feedback, and metrics exist across organization scopes.
- Role-based permissions are configured.
- The actor is authorized to validate system configuration.

## Main Flow

1. The System Administrator reviews user roles and organization assignments.
2. The system displays configured access boundaries.
3. The administrator verifies access behavior through available admin views or tests.
4. The system enforces access restrictions for documents.
5. The system enforces access restrictions for retrieval.
6. The system enforces access restrictions for chat history.
7. The system enforces access restrictions for feedback.
8. The system enforces access restrictions for dashboard metrics.
9. Authorization failures are logged.

## Alternative Flows

### AF-016.1: User Attempts Cross-Organization Document Access

1. A user requests a document outside their organization.
2. The system rejects the request.
3. The system logs the authorization failure.

### AF-016.2: Retrieval Attempts to Use Unauthorized Chunk

1. A user asks a question.
2. Retrieval would otherwise match a chunk from an unauthorized document.
3. The system excludes the unauthorized chunk.
4. The system continues retrieval using authorized chunks only.

### AF-016.3: Dashboard Cross-Scope Attempt

1. A user requests dashboard data outside their scope.
2. The system rejects or filters the data according to permissions.
3. The system logs unauthorized access when applicable.

## Business Rules

Canonical business-rule applicability for this use case is defined in `docs/09-business-rules.md`, Section 13.1.

## Postconditions

- Access boundaries are enforced.
- Unauthorized access is blocked.
- Retrieval does not leak unauthorized documents.
- Dashboard metrics remain scoped.
- Failures are logged for review.

## Acceptance Criteria

- Users cannot access documents outside their organization scope.
- Users cannot retrieve chunks from unauthorized documents.
- Users cannot view chat history outside their permitted scope.
- Users cannot submit feedback outside their permitted scope.
- Dashboard metrics are filtered by organization.
- Authorization failures are logged.

---

# 4. Use Case to Requirement Traceability

| Use Case | Related Functional Requirements |
|---|---|
| UC-001 Authenticate User | FR-001 to FR-005, FR-092 to FR-099 |
| UC-002 Manage Users and Roles | FR-006 to FR-010, FR-087 to FR-089 |
| UC-003 Upload Internal Document | FR-011 to FR-018, FR-092 |
| UC-004 Process Uploaded Document | FR-019 to FR-038, FR-093 |
| UC-005 Review Document Processing Status | FR-015, FR-016, FR-021, FR-024, FR-091 |
| UC-006 Ask Knowledge Question | FR-039 to FR-057, FR-064 |
| UC-007 Generate RAG Answer with Citations | FR-047 to FR-063, FR-073, FR-074 |
| UC-008 Handle Insufficient Context | FR-055 to FR-057, FR-075, FR-084 |
| UC-009 Review Source Citations | FR-058 to FR-063, FR-067 |
| UC-010 Submit Answer Feedback | FR-068 to FR-075, FR-083 |
| UC-011 Review Chat History | FR-064 to FR-067 |
| UC-012 Review Operational Dashboard | FR-076 to FR-086 |
| UC-013 Review Knowledge Gaps | FR-072, FR-075, FR-083, FR-084 |
| UC-014 Disable Document from Retrieval | FR-027, FR-045 |
| UC-015 Monitor System Health and Failures | FR-090 to FR-099 |
| UC-016 Validate Access Boundaries | FR-002, FR-006 to FR-010, FR-043, FR-046, FR-085, FR-086 |

---

# 5. Use Case to Business Rule Traceability

Canonical rule-to-use-case traceability is maintained in `docs/09-business-rules.md`, Section 13.1. That catalog is the only source of `BR-###` meanings and mappings; this use-case document does not duplicate it.

---

# 6. Summary

These use cases define how KnowledgeOps-AI actors interact with the system to achieve business outcomes.

The core MVP workflow is:

1. Users authenticate.
2. Knowledge administrators upload documents.
3. The system processes documents asynchronously.
4. The system creates chunks and embeddings.
5. Support agents ask questions.
6. The RAG assistant retrieves relevant chunks.
7. The assistant generates grounded answers with citations.
8. Users review citations and submit feedback.
9. Managers and administrators review operational metrics.
10. The system enforces access boundaries throughout the workflow.

These use cases should guide implementation issues, API contracts, frontend screens, test planning, and future roadmap decisions.
