# Testing Strategy

## 1. Purpose

This document defines the testing strategy for **KnowledgeOps-AI**.

Testing must be connected to:

- Requirements.
- Use cases.
- Business rules.
- Architecture decisions.
- API contracts.
- Security rules.
- Acceptance criteria.

KnowledgeOps-AI is an enterprise AI-powered internal knowledge assistant for contact centers and support operations. The system allows authorized users to upload internal documents, process them into searchable knowledge, ask questions through a Retrieval-Augmented Generation workflow, receive grounded answers with citations, submit feedback, and monitor operational metrics.

The goal of this testing strategy is to ensure that the system is not only technically correct, but also aligned with the business behavior already documented.

Testing should validate that the system:

- Enforces authentication and authorization.
- Protects organization-scoped data.
- Processes documents correctly.
- Excludes failed, retrieval-disabled, soft-deleted, or unprocessed documents from retrieval.
- Retrieves only authorized chunks.
- Generates grounded answers with citations.
- Handles insufficient context safely.
- Stores chat history and feedback.
- Exposes accurate dashboard metrics.
- Avoids leaking sensitive content.
- Preserves MVP scope.

---

## 2. Testing Principles

## 2.1 Tests Must Follow Business Intent

Tests should validate documented business behavior.

A test should usually trace back to at least one of the following:

- Functional requirement.
- Non-functional requirement.
- Business rule.
- Use case.
- Acceptance criterion.
- Security constraint.
- Architecture decision.

Tests should not be written only around implementation details.

---

## 2.2 Business Rules Must Be Testable

Business rules must not be hidden in controllers, UI components, database scripts, or provider adapters.

If a business rule exists, there should be at least one test that proves it is enforced.

Example:

```text
BR-015: Retrieval must respect authorization.
```

Testing expectation:

```text
A user from Organization A must not retrieve chunks from documents belonging to Organization B.
```

---

## 2.3 Backend Authorization Is the Source of Truth

Frontend visibility is not security.

The frontend may hide buttons, menu items, or pages, but backend tests must prove that protected APIs reject unauthorized requests.

Example:

```text
An Agent should not see the Upload Document button in the UI.
```

This is useful for UX, but not enough.

Required backend test:

```text
POST /api/v1/documents must reject an Agent even if the Agent manually sends the request.
```

---

## 2.4 AI Behavior Must Be Deterministic in Tests

Most automated tests must not depend on live AI provider calls.

Use fake, stub, or mock implementations for:

- Embedding provider.
- AI answer generator.
- Retrieval provider.
- Cost estimator.

Live Azure OpenAI or OpenAI calls should be reserved for optional manual validation, demo validation, or controlled integration checks.

---

## 2.5 Retrieval Must Be Tested Before Generation

The system must prove that retrieval happens before answer generation.

A valid RAG test should verify:

1. The user asks a question.
2. Retrieval is executed using the user’s organization scope.
3. Retrieved chunks are passed to the prompt builder.
4. The answer generator receives grounded context.
5. Citations are mapped from retrieved chunks.
6. The interaction is stored.

---

## 2.6 Tests Must Protect Against Scope Creep

Tests should reinforce MVP boundaries.

The MVP must not include:

- Customer-facing chatbot behavior.
- Real-time call transcription.
- Autonomous ticket actions.
- Automatic policy enforcement.
- Custom model training.
- Enterprise SSO unless formally approved.

If code introduces these behaviors accidentally, tests or review checks should catch the drift.

---

## 2.7 Tests Should Be Layered

Different test types should validate different risks.

| Test Type | Main Purpose |
|---|---|
| Unit tests | Validate isolated business logic and application services. |
| Integration tests | Validate database, persistence, authorization, and workflow behavior. |
| API tests | Validate HTTP contracts and error responses. |
| Frontend tests | Validate UI behavior, forms, state, guards, and user flows. |
| End-to-end tests | Validate critical user journeys across frontend and backend. |
| UAT scenarios | Validate business usefulness from stakeholder perspective. |

---

## 2.8 Tests Should Be Repeatable

Automated tests should be deterministic and repeatable in local development and CI.

Tests should avoid:

- Live AI calls by default.
- External network dependencies.
- Shared mutable external state.
- Time-sensitive assertions without a controllable clock.
- Random generated data without controlled seeds.
- Relying on test execution order.

---

## 2.9 Tests Should Use Seeded Personas

Integration, API, frontend, and E2E tests should use known test personas.

Recommended seeded users:

| Persona | Role | Organization | Purpose |
|---|---|---|---|
| Agent A | Agent | Org A | Ask questions, review own history, submit feedback. |
| Supervisor A | Supervisor | Org A | Review scoped activity and knowledge gaps. |
| Knowledge Admin A | KnowledgeAdmin | Org A | Upload and manage documents. |
| Manager A | Manager | Org A | Review dashboard metrics. |
| Admin A | Admin | Org A | Manage users, roles, health, audit. |
| Agent B | Agent | Org B | Validate cross-organization isolation. |
| Admin B | Admin | Org B | Validate organization-scoped admin behavior. |

---

# 3. Unit Tests

## 3.1 Purpose

Unit tests validate isolated business logic without requiring live infrastructure.

They should be fast, deterministic, and easy to run during development.

Unit tests should cover:

- Domain rules.
- Application service logic.
- Validators.
- Prompt construction.
- Citation mapping.
- Retrieval eligibility checks.
- Permission checks.
- Cost estimation logic.
- Dashboard metric calculations.
- Error mapping.
- State transitions.

---

## 3.2 Recommended Unit Test Areas

## 3.2.1 Document Validation Tests

Validate:

- Required metadata.
- Supported file types.
- File size rules.
- Initial processing status.
- Organization assignment.
- Upload permission assumptions.

Example tests:

```text
UploadDocumentValidator_RejectsMissingTitle
UploadDocumentValidator_RejectsUnsupportedFileType
UploadDocumentValidator_RejectsOversizedFile
UploadDocumentService_AssignsUploadedStatus
UploadDocumentService_AssignsCurrentUserOrganization
```

---

## 3.2.2 Document Lifecycle Tests

Validate:

- Uploaded to Processing transition.
- Processing to Processed transition.
- Processing to Failed transition.
- Failure reason storage.
- Retrieval-disabled documents excluded from retrieval without changing processing status.
- Failed documents excluded from retrieval.

Example tests:

```text
DocumentLifecycle_CanMoveFromUploadedToProcessing
DocumentLifecycle_CanMoveFromProcessingToProcessed
DocumentLifecycle_CanMoveFromProcessingToFailedWithReason
DocumentEligibility_FailedDocumentIsNotRetrievable
DocumentEligibility_RetrievalDisabledDocumentIsNotRetrievable
```

---

## 3.2.3 Chunking Tests

Validate:

- Extracted text is split into chunks.
- Empty chunks are not stored.
- Chunk indexes are assigned consistently.
- Chunk source document relationship is preserved.
- Page or section metadata is preserved when available.

Example tests:

```text
ChunkingService_SplitsLongTextIntoChunks
ChunkingService_DoesNotCreateEmptyChunks
ChunkingService_AssignsSequentialChunkIndexes
ChunkingService_PreservesDocumentReference
```

---

## 3.2.4 Retrieval Eligibility Tests

Validate:

- Retrieval excludes failed documents.
- Retrieval excludes documents where `is_retrieval_enabled = false`.
- Retrieval excludes soft-deleted documents.
- Retrieval excludes unprocessed documents.
- Retrieval excludes unauthorized documents.
- Retrieval includes only processed, retrieval-enabled, non-soft-deleted, authorized documents.

Example tests:

```text
RetrievalEligibility_ExcludesFailedDocuments
RetrievalEligibility_ExcludesRetrievalDisabledDocuments
RetrievalEligibility_ExcludesSoftDeletedDocuments
RetrievalEligibility_ExcludesProcessingDocuments
RetrievalEligibility_ExcludesCrossOrganizationDocuments
RetrievalEligibility_IncludesProcessedEnabledAuthorizedDocuments
```

---

## 3.2.5 Prompt Builder Tests

Validate:

- Retrieved context is included.
- Grounding instructions are included.
- Insufficient-context behavior is included.
- Prompt version is assigned.
- Prompt size limits are respected.

Example tests:

```text
PromptBuilder_IncludesRetrievedContext
PromptBuilder_IncludesGroundingInstructions
PromptBuilder_IncludesInsufficientContextInstruction
PromptBuilder_TracksPromptVersion
PromptBuilder_RespectsMaxContextLimit
```

---

## 3.2.6 Citation Mapper Tests

Validate:

- Retrieval results map to citations.
- Citations include document reference.
- Citations include chunk reference.
- Citations include page or section when available.
- Unauthorized source data is not exposed.

Example tests:

```text
CitationMapper_MapsDocumentAndChunkReferences
CitationMapper_IncludesPageNumberWhenAvailable
CitationMapper_IncludesSectionLabelWhenAvailable
CitationMapper_DoesNotExposeUnauthorizedSource
```

---

## 3.2.7 RAG Orchestration Tests

Validate:

- Retrieval happens before generation.
- AI generation is skipped when context is insufficient.
- Answer is stored.
- Citations are stored.
- Metadata is stored.
- Insufficient context is stored.

Example tests:

```text
RagChatService_RetrievesBeforeGeneratingAnswer
RagChatService_ReturnsCitationsForGroundedAnswer
RagChatService_DoesNotCallGeneratorWhenContextInsufficient
RagChatService_StoresInsufficientContextInteraction
RagChatService_StoresLatencyAndCostMetadata
```

---

## 3.2.8 Feedback Tests

Validate:

- Feedback belongs to chat interaction.
- Useful feedback is stored.
- Not useful feedback is stored.
- Duplicate feedback does not inflate metrics.
- Feedback respects organization scope.

Example tests:

```text
FeedbackService_StoresUsefulFeedback
FeedbackService_StoresNotUsefulFeedback
FeedbackService_RejectsFeedbackWithoutChatInteraction
FeedbackService_PreventsDuplicateFeedbackInflation
FeedbackService_RejectsCrossOrganizationFeedback
```

---

## 3.2.9 Dashboard Metric Tests

Validate:

- Question counts.
- Feedback counts.
- Document counts.
- Failed processing counts.
- Insufficient-context counts.
- Average latency.
- Estimated cost when available.
- Cost unavailable behavior.

Example tests:

```text
DashboardMetrics_ComputesQuestionCount
DashboardMetrics_ComputesFeedbackCounts
DashboardMetrics_ComputesProcessedDocumentCount
DashboardMetrics_ComputesFailedDocumentCount
DashboardMetrics_ComputesInsufficientContextCount
DashboardMetrics_DoesNotTreatUnavailableCostAsZero
```

---

## 3.2.10 Authorization Unit Tests

Validate permission logic independently where applicable.

Example tests:

```text
PermissionService_AgentCannotUploadDocuments
PermissionService_KnowledgeAdminCanUploadDocuments
PermissionService_ManagerCanViewDashboard
PermissionService_NonAdminCannotManageUsers
PermissionService_AdminCanViewHealthDetails
```

---

# 4. Integration Tests

## 4.1 Purpose

Integration tests validate behavior across multiple backend layers.

They should verify:

- Application services.
- Persistence.
- Entity Framework Core mappings.
- Database constraints.
- Authorization behavior.
- Document lifecycle transitions.
- API-adjacent workflows.
- Organization-scoped queries.

Integration tests may use:

- Testcontainers with SQL Server.
- Local test database.
- In-memory fakes only when relational behavior is not important.

For database behavior, prefer a real relational database test environment over EF Core InMemory.

---

## 4.2 Recommended Integration Test Areas

## 4.2.1 Persistence Integration Tests

Validate:

- Users persist with roles.
- Documents persist with metadata.
- Chunks preserve document relationship.
- Chat interactions store citations.
- Feedback stores correctly.
- Audit entries store safely.
- Index assumptions support key queries.

Example tests:

```text
Persistence_SavesUserWithRoles
Persistence_SavesDocumentWithOrganizationScope
Persistence_SavesChunksWithSourceDocument
Persistence_SavesChatInteractionWithCitations
Persistence_SavesFeedbackForChatInteraction
Persistence_SavesAuditLogEntryWithoutSensitiveContent
```

---

## 4.2.2 Organization Scope Integration Tests

Validate:

- Org A user cannot access Org B documents.
- Org A retrieval excludes Org B chunks.
- Org A dashboard excludes Org B metrics.
- Org A feedback review excludes Org B feedback.

Example tests:

```text
OrganizationScope_DocumentsAreFilteredByOrganization
OrganizationScope_RetrievalExcludesOtherOrganizationChunks
OrganizationScope_DashboardExcludesOtherOrganizationData
OrganizationScope_FeedbackReviewExcludesOtherOrganizationData
```

---

## 4.2.3 Document Processing Integration Tests

Validate:

- Upload creates document record.
- Background processing updates status.
- Processing failure stores reason.
- Processed documents produce chunks and embeddings.
- Failed documents remain excluded from retrieval.

Example tests:

```text
DocumentProcessing_ValidDocumentMovesToProcessed
DocumentProcessing_TextExtractionFailureMovesToFailed
DocumentProcessing_EmptyTextMovesToFailed
DocumentProcessing_StoresChunksAndEmbeddings
DocumentProcessing_FailedDocumentNotRetrievable
```

---

## 4.2.4 RAG Workflow Integration Tests

Use fake AI providers and fake retrieval/embedding services where appropriate.

Validate:

- Chat question creates interaction.
- Retrieval results are stored.
- Citations are stored.
- Answer metadata is stored.
- Insufficient context is stored.
- Provider failure is handled safely.

Example tests:

```text
RagWorkflow_StoresAnsweredInteractionWithCitations
RagWorkflow_StoresRetrievalResults
RagWorkflow_StoresInsufficientContextInteraction
RagWorkflow_HandlesAiProviderFailureSafely
RagWorkflow_DoesNotPersistUnauthorizedRetrievalResults
```

---

## 4.2.5 Dashboard Integration Tests

Validate metrics against real stored records.

Example tests:

```text
Dashboard_ReturnsQuestionCountFromChatInteractions
Dashboard_ReturnsFeedbackCountsFromAnswerFeedback
Dashboard_ReturnsDocumentCountsByStatus
Dashboard_ReturnsAverageLatencyFromChatInteractions
Dashboard_ReturnsScopedMetricsOnly
```

---

# 5. API Tests

## 5.1 Purpose

API tests validate HTTP behavior, contracts, status codes, authorization, request validation, and response models.

They should prove that controllers expose application behavior correctly without inventing rules.

API tests should cover:

- Request model validation.
- Response model shape.
- Error response shape.
- Authorization requirements.
- Organization scope.
- Status codes.
- Pagination and filtering.
- Business rule enforcement through API boundaries.

---

## 5.2 Authentication API Tests

Endpoints:

```text
POST /api/v1/auth/login
GET  /api/v1/auth/me
```

Test cases:

```text
POST_login_WithValidCredentials_ReturnsTokenAndUserContext
POST_login_WithInvalidCredentials_Returns401
POST_login_WithDisabledUser_Returns403
GET_me_WithValidToken_ReturnsCurrentUser
GET_me_WithoutToken_Returns401
```

---

## 5.3 User Administration API Tests

Endpoints:

```text
GET    /api/v1/users
POST   /api/v1/users
GET    /api/v1/users/{userId}
PUT    /api/v1/users/{userId}
POST   /api/v1/users/{userId}/roles
DELETE /api/v1/users/{userId}/roles/{roleName}
```

Test cases:

```text
GET_users_AsAdmin_ReturnsScopedUsers
GET_users_AsAgent_Returns403
POST_users_AsAdmin_CreatesUser
POST_users_AsNonAdmin_Returns403
POST_userRole_WithInvalidRole_Returns400
DELETE_userRole_AsAdmin_RemovesRole
```

---

## 5.4 Document API Tests

Endpoints:

```text
POST /api/v1/documents
GET  /api/v1/documents
GET  /api/v1/documents/{documentId}
GET  /api/v1/documents/{documentId}/processing-status
POST /api/v1/documents/{documentId}/disable
```

Test cases:

```text
POST_documents_AsKnowledgeAdmin_WithValidFile_Returns201
POST_documents_AsAgent_Returns403
POST_documents_WithUnsupportedFileType_Returns415
POST_documents_WithMissingTitle_Returns400
GET_documents_ReturnsOnlyCurrentOrganizationDocuments
GET_document_ByCrossOrganizationId_Returns404Or403
POST_disableDocument_AsKnowledgeAdmin_DisablesRetrieval
POST_disableDocument_AsAgent_Returns403
```

---

## 5.5 Chat API Tests

Endpoint:

```text
POST /api/v1/chat/questions
```

Test cases:

```text
POST_chatQuestion_AsAgent_WithValidQuestion_ReturnsAnswer
POST_chatQuestion_WithEmptyQuestion_Returns400
POST_chatQuestion_RetrievalFindsSources_ReturnsCitations
POST_chatQuestion_NoRelevantSources_ReturnsInsufficientContext
POST_chatQuestion_CrossOrganizationChunksAreExcluded
POST_chatQuestion_AiProviderFailure_ReturnsSafeProviderError
```

---

## 5.6 Citation API Tests

Endpoints:

```text
GET /api/v1/chat/interactions/{chatInteractionId}/citations
GET /api/v1/citations/{citationId}
```

Test cases:

```text
GET_citations_ForOwnInteraction_ReturnsCitations
GET_citations_ForUnauthorizedInteraction_Returns404Or403
GET_citation_DoesNotExposeUnauthorizedDocument
```

---

## 5.7 Feedback API Tests

Endpoint:

```text
POST /api/v1/chat/interactions/{chatInteractionId}/feedback
```

Test cases:

```text
POST_feedback_WithUsefulRating_ReturnsCreatedOrOk
POST_feedback_WithNotUsefulRating_ReturnsCreatedOrOk
POST_feedback_ForMissingInteraction_Returns404
POST_feedback_ForCrossOrganizationInteraction_Returns404Or403
POST_feedback_DuplicateDoesNotInflateMetrics
POST_feedback_WithInvalidRating_Returns400
```

---

## 5.8 Dashboard API Tests

Endpoint:

```text
GET /api/v1/dashboard/overview
```

Test cases:

```text
GET_dashboard_AsManager_ReturnsOverviewMetrics
GET_dashboard_AsAgent_Returns403
GET_dashboard_ReturnsOrganizationScopedMetrics
GET_dashboard_WhenCostUnavailable_DoesNotReturnMisleadingZero
GET_dashboard_IncludesQuestionFeedbackDocumentLatencyMetrics
```

---

## 5.9 Health and Audit API Tests

Endpoints:

```text
GET /api/v1/health
GET /api/v1/health/details
GET /api/v1/admin/audit-log
```

Test cases:

```text
GET_health_ReturnsBasicStatus
GET_healthDetails_AsAdmin_ReturnsDependencyStatus
GET_healthDetails_AsAgent_Returns403
GET_auditLog_AsAdmin_ReturnsAuditEntries
GET_auditLog_AsNonAdmin_Returns403
GET_auditLog_DoesNotExposeSecretsOrSensitiveContent
```

---

# 6. Frontend Tests

## 6.1 Purpose

Frontend tests validate that the UI supports documented workflows and does not mislead users.

Frontend tests do not replace backend authorization tests.

Frontend tests should cover:

- Login flow.
- Role-aware navigation.
- Document upload form.
- Document status display.
- Chat question flow.
- Citation display.
- Feedback controls.
- Dashboard rendering.
- Error states.
- Loading states.
- Empty states.

---

## 6.2 Recommended Frontend Test Types

Depending on frontend framework and tooling, use:

- Component tests.
- Service tests.
- Route guard tests.
- Form validation tests.
- UI state tests.
- Mock API interaction tests.

Because Angular is the accepted frontend framework, likely tools include:

- Jasmine/Karma or Jest.
- Angular Testing Library.
- Playwright or Cypress for E2E.

---

## 6.3 Frontend Authentication Tests

Test cases:

```text
LoginForm_RequiresEmailAndPassword
LoginForm_ShowsGenericErrorOnInvalidLogin
AuthGuard_RedirectsUnauthenticatedUserToLogin
AuthService_StoresAuthenticatedUserContext
Navigation_ShowsRoleAwareMenuItems
```

---

## 6.4 Frontend Document Tests

Test cases:

```text
DocumentUploadForm_RequiresTitleAndFile
DocumentUploadForm_DisallowsUnsupportedFileBeforeSubmit
DocumentUploadPage_HiddenForAgentRole
DocumentList_DisplaysProcessingStatus
DocumentDetails_DisplaysFailureReasonWhenFailed
DocumentDisableButton_VisibleOnlyForKnowledgeAdminOrAdmin
```

---

## 6.5 Frontend Chat Tests

Test cases:

```text
ChatInput_RejectsEmptyQuestion
ChatPage_DisplaysAnswer
ChatPage_DisplaysCitations
ChatPage_DisplaysInsufficientContextMessage
ChatPage_DisplaysProviderErrorSafely
ChatPage_AllowsFeedbackSubmission
```

---

## 6.6 Frontend Dashboard Tests

Test cases:

```text
DashboardPage_VisibleForManager
DashboardPage_HiddenForAgent
DashboardPage_DisplaysQuestionCount
DashboardPage_DisplaysFeedbackCounts
DashboardPage_DisplaysDocumentCounts
DashboardPage_DisplaysLatencyMetrics
DashboardPage_ShowsCostUnavailableState
```

---

## 6.7 Frontend Error State Tests

Test cases:

```text
ApiError_ValidationError_DisplaysFieldMessage
ApiError_Forbidden_DisplaysAccessDenied
ApiError_NotFound_DisplaysSafeNotFound
ApiError_ProviderError_DisplaysRetryMessage
```

---

# 7. End-to-End Tests

## 7.1 Purpose

End-to-end tests validate complete workflows from the user interface through backend APIs and persistence.

E2E tests should be limited to high-value critical paths because they are more expensive to run and maintain.

E2E tests should use:

- Seeded users.
- Seeded organizations.
- Fake AI provider where possible.
- Controlled test documents.
- Deterministic retrieval behavior.

---

## 7.2 Recommended E2E Scenarios

## 7.2.1 Agent Asks Question and Receives Cited Answer

Flow:

1. Agent logs in.
2. Agent opens chat.
3. Agent asks a question.
4. System returns answer.
5. System displays citations.
6. Agent submits useful feedback.

Expected result:

- Answer is displayed.
- Citations are displayed.
- Feedback is accepted.
- Chat history contains the interaction.

---

## 7.2.2 KnowledgeAdmin Uploads Document and Reviews Status

Flow:

1. KnowledgeAdmin logs in.
2. KnowledgeAdmin opens document upload.
3. KnowledgeAdmin uploads valid document.
4. System accepts upload.
5. System displays document status.
6. Background processing completes.
7. Document status changes to Processed.

Expected result:

- Document appears in document list.
- Processing status is visible.
- Processed document is eligible for retrieval.

---

## 7.2.3 Manager Reviews Dashboard

Flow:

1. Manager logs in.
2. Manager opens dashboard.
3. System displays overview metrics.

Expected result:

- Question count appears.
- Document counts appear.
- Feedback counts appear.
- Latency appears.
- Cost appears or is marked unavailable.

---

## 7.2.4 Agent Receives Insufficient Context Response

Flow:

1. Agent logs in.
2. Agent asks a question not covered by available documents.
3. System returns insufficient-context response.

Expected result:

- System does not fabricate policy.
- System suggests human escalation.
- Interaction is stored.
- Dashboard insufficient-context count updates.

---

## 7.2.5 Cross-Organization Access Is Blocked

Flow:

1. Agent A logs in under Organization A.
2. Agent A attempts to access Organization B document or chat by ID.
3. System blocks access.

Expected result:

- UI does not display unauthorized data.
- API returns safe 404 or 403.
- Authorization failure is logged safely.

---

# 8. UAT Scenarios

## 8.1 Purpose

User Acceptance Testing validates that the system supports real stakeholder goals.

UAT scenarios should be written from the perspective of business users, not technical implementation.

---

## 8.2 UAT-001: Support Agent Finds Procedure Faster

### Actor

Support Agent.

### Scenario

A support agent needs to know how to escalate a critical customer complaint.

### Steps

1. Log in as Agent.
2. Open the chat assistant.
3. Ask: “What is the escalation process for a critical customer complaint?”
4. Review the answer.
5. Review the source citations.
6. Mark the answer as useful.

### Acceptance Criteria

- The agent receives a direct answer.
- The answer includes at least one source citation.
- The citation references an approved internal document.
- The agent can submit feedback.
- The system stores the interaction.

---

## 8.3 UAT-002: Knowledge Administrator Uploads New Policy

### Actor

Knowledge Administrator.

### Scenario

A knowledge administrator uploads a new internal support policy.

### Steps

1. Log in as KnowledgeAdmin.
2. Open document management.
3. Upload a supported file with a title.
4. Review document status.
5. Wait for processing completion or trigger test worker flow.
6. Confirm document reaches Processed status.

### Acceptance Criteria

- Upload succeeds.
- Document metadata is stored.
- Processing status is visible.
- Processed document becomes eligible for retrieval.
- Failure reason is visible if processing fails.

---

## 8.4 UAT-003: Operations Manager Reviews Value Metrics

### Actor

Operations Manager.

### Scenario

An operations manager wants to understand assistant usage and answer usefulness.

### Steps

1. Log in as Manager.
2. Open dashboard.
3. Review question count.
4. Review feedback counts.
5. Review document processing counts.
6. Review average latency.
7. Review estimated cost or unavailable cost state.

### Acceptance Criteria

- Dashboard loads successfully.
- Metrics are scoped to the manager’s organization.
- Metrics do not expose sensitive document content.
- Cost unavailable state is clear when cost cannot be estimated.

---

## 8.5 UAT-004: Manager Reviews Basic Knowledge Gap Indicators

### Actor

Manager.

### Scenario

A manager wants to review MVP insufficient-context and `NotUseful` counts without requiring a dedicated knowledge-gap review workflow.

### Steps

1. Log in as Manager.
2. Open the dashboard.
3. Review the scoped insufficient-context count.
4. Review the scoped `NotUseful` feedback count.

### Acceptance Criteria

- Basic insufficient-context and `NotUseful` counts are visible to the authorized manager.
- Dashboard data is organization-scoped.
- The system does not expose other organizations’ information.
- No dedicated queue, categorization, assignment, or resolution workflow is required for MVP.

---

## 8.6 UAT-005: Unauthorized User Cannot Upload Document

### Actor

Support Agent.

### Scenario

An Agent attempts to upload a document, which should not be allowed.

### Steps

1. Log in as Agent.
2. Attempt to access document upload screen.
3. If UI hides upload, attempt direct API request manually.

### Acceptance Criteria

- Upload UI is not available to Agent.
- Backend rejects direct upload request.
- System returns a safe authorization error.
- No document is created.
- Authorization failure is logged safely.

---

## 8.7 UAT-006: AI Does Not Answer Without Sources

### Actor

Support Agent.

### Scenario

An agent asks a question that is not covered by uploaded documents.

### Steps

1. Log in as Agent.
2. Ask a question outside the knowledge base.
3. Review the assistant response.

### Acceptance Criteria

- The assistant states that available documents do not contain enough information.
- The assistant does not invent official policy.
- The assistant suggests contacting a supervisor or knowledge administrator.
- The interaction is stored as insufficient context.

---

# 9. Traceability to Use Cases

## 9.1 Use Case to Test Type Traceability

| Use Case | Unit Tests | Integration Tests | API Tests | Frontend Tests | E2E Tests | UAT |
|---|---|---|---|---|---|---|
| UC-001 Authenticate User | Permission logic | User/role persistence | Login, me endpoint | Login form, route guards | Login flow | Access validation |
| UC-002 Manage Users and Roles | Role assignment logic | User-role persistence | User admin endpoints | Admin screens | Admin manages user | Admin validation |
| UC-003 Upload Internal Document | Upload validation | Document persistence | Upload endpoint | Upload form | Upload document flow | UAT-002, UAT-005 |
| UC-004 Process Uploaded Document | Lifecycle, chunking | Worker processing | Processing status endpoint | Status display | Process document flow | UAT-002 |
| UC-005 Review Document Processing Status | Status rules | Status persistence | Document status endpoint | Document status UI | Upload/status flow | UAT-002 |
| UC-006 Ask Knowledge Question | Chat validation | Chat persistence | Chat endpoint | Chat UI | Ask question flow | UAT-001 |
| UC-007 Generate RAG Answer with Citations | Prompt, citation mapping | RAG workflow | Chat response contract | Citation display | Cited answer flow | UAT-001 |
| UC-008 Handle Insufficient Context | Insufficient context logic | Stored marker | Chat insufficient response | Safe message UI | No-source question flow | UAT-006 |
| UC-009 Review Source Citations | Citation mapper | Citation persistence | Citation endpoint | Citation display | Cited answer flow | UAT-001 |
| UC-010 Submit Answer Feedback | Feedback rules | Feedback persistence | Feedback endpoint | Feedback controls | Feedback flow | UAT-001 |
| UC-011 Review Chat History | Ownership rules | Chat query persistence | Chat history endpoint | History UI | Review history flow | Optional |
| UC-012 Review Operational Dashboard | Metric calculations | Metric queries | Dashboard endpoint | Dashboard UI | Manager dashboard flow | UAT-003 |
| UC-013 Review Knowledge Gaps (Phase 2) | Deferred | Deferred | Deferred | Deferred | Deferred | MVP dashboard counts covered by UAT-004 |
| UC-014 Disable Document from Retrieval | Eligibility rules | Retrieval-enabled flag update | Disable endpoint | Disable action UI | Optional | Optional |
| UC-015 Monitor System Health and Failures | Health logic | Audit/health persistence | Health endpoints | Health admin UI | Optional | Optional |
| UC-016 Validate Access Boundaries | Permission/scope logic | Cross-org data tests | Authorization API tests | Role-aware UI | Cross-org block flow | UAT-005 |

---

# 10. Traceability to Business Rules

## 10.1 Business Rule to Test Strategy

| Business Rule Area | Required Test Coverage |
|---|---|
| BR-001 to BR-005 Access and security | Authorization unit tests, API tests, integration scope tests. |
| BR-006 to BR-014 Document lifecycle | Unit tests for validation/status, integration tests for processing, API upload tests. |
| BR-015 to BR-022 Retrieval and RAG | Unit tests for eligibility/prompting, integration RAG tests, API chat tests. |
| BR-023 to BR-027 Feedback and review | Feedback unit tests, API feedback tests, dashboard/review integration tests. |
| BR-028 to BR-033 Dashboard and metrics | Metric unit tests, dashboard integration tests, API dashboard tests. |
| BR-034 to BR-037 Logging and observability | Audit integration tests, safe logging tests, health API tests. |
| BR-038 to BR-041 Administration | Admin API tests, permission tests, audit tests. |
| BR-042 to BR-045 AI governance | RAG orchestration tests, insufficient-context tests, provider isolation tests. |
| BR-046 to BR-049 Scope control | Review checklist, architecture tests where applicable, no out-of-scope endpoints. |

---

# 11. Definition of Done

## 11.1 General Definition of Done

A feature is considered done when:

- It maps to an approved use case, requirement, or business rule.
- It does not introduce out-of-scope behavior.
- Backend authorization is enforced.
- Organization scope is enforced where applicable.
- Request and response models are stable.
- Error responses are safe and consistent.
- Unit tests cover core logic.
- Integration or API tests cover critical workflows.
- Frontend behavior is tested when UI is involved.
- Sensitive content is not logged unnecessarily.
- Documentation is updated when behavior changes.
- Existing tests pass.
- New tests pass.

---

## 11.2 Backend Definition of Done

A backend change is done when:

- Application service or handler implements the use case.
- Controller remains thin.
- Business rules are not hidden in the controller.
- Domain/application logic is covered by unit tests.
- Persistence behavior is covered by integration tests where needed.
- API contract is covered by API tests.
- Authorization is tested.
- Organization scope is tested.
- Errors use the standard error response model.
- Provider-specific logic stays in Infrastructure.
- Live AI calls are not required for automated test success.

---

## 11.3 Frontend Definition of Done

A frontend change is done when:

- UI supports the documented workflow.
- Form validation is implemented.
- Loading, empty, and error states are handled.
- Role-aware navigation is implemented.
- Backend authorization is still assumed to be source of truth.
- API client uses documented contracts.
- Component or flow tests are added where appropriate.
- The UI does not expose misleading AI behavior.
- Citations and insufficient-context messages are displayed clearly.

---

## 11.4 RAG Feature Definition of Done

A RAG-related feature is done when:

- Retrieval happens before generation.
- Retrieval applies organization and authorization filters.
- Failed, retrieval-disabled, soft-deleted, unprocessed, and unauthorized documents are excluded.
- Prompt includes retrieved context when context exists.
- Insufficient context is detected and handled safely.
- Grounded answers include citations.
- Chat interaction is stored.
- Retrieval metadata is stored where required.
- Latency and cost metadata are captured where available.
- Tests use fake providers by default.
- The assistant does not present unsupported answers as official policy.

---

## 11.5 Security Feature Definition of Done

A security-sensitive feature is done when:

- Authentication is required where appropriate.
- Role permissions are enforced.
- Organization scope is enforced.
- Unauthorized requests fail safely.
- Authorization failures are logged safely.
- Sensitive data is not exposed in errors or logs.
- Tests cover unauthenticated access.
- Tests cover unauthorized role access.
- Tests cover cross-organization access.
- Tests prove frontend visibility is not the only protection.

---

## 11.6 Documentation Definition of Done

Documentation is done when:

- New behavior is reflected in relevant docs.
- Requirements are updated when system behavior changes.
- Use cases are updated when user flows change.
- Business rules are updated when stable rules change.
- API design is updated when contracts change.
- Security documentation is updated when permissions change.
- Architecture docs are updated when module boundaries change.
- ADRs are added or superseded for significant architecture decisions.

---

# 12. Recommended Test Project Structure

## 12.1 Backend Test Structure

Recommended structure:

```text
tests/
  KnowledgeOps.UnitTests/
    Documents/
    Processing/
    Retrieval/
    Chat/
    Feedback/
    Dashboard/
    Security/
    Common/

  KnowledgeOps.IntegrationTests/
    Persistence/
    Documents/
    Processing/
    Retrieval/
    Chat/
    Feedback/
    Dashboard/
    Security/
    Api/

  KnowledgeOps.ApiTests/
    Auth/
    Users/
    Documents/
    Chat/
    Feedback/
    Dashboard/
    Health/
```

Depending on implementation preference, `ApiTests` may be part of `IntegrationTests`.

---

## 12.2 Frontend Test Structure

Recommended Angular-style structure:

```text
frontend/
  src/
    app/
      auth/
      documents/
      chat/
      dashboard/
      admin/
      shared/

  tests/
    unit/
    integration/
    e2e/
```

Alternative structure may be used if the frontend tooling recommends it.

---

# 13. CI Testing Expectations

## 13.1 Pull Request Validation

At minimum, CI should run:

- Backend build.
- Backend unit tests.
- Backend integration tests that are practical for CI.
- Frontend build.
- Frontend unit tests.
- Linting or formatting checks if configured.

## 13.2 Optional CI Stages

Later phases may add:

- API contract tests.
- E2E smoke tests.
- Docker Compose validation.
- Migration validation.
- Security scanning.
- Dependency vulnerability scanning.
- Mermaid diagram validation.
- Documentation link checks.

---

# 14. AI Agent Guidance

AI coding agents must use this testing strategy before implementing features.

## 14.1 AI Agents Must

- Map tests to requirements, use cases, or business rules.
- Add unit tests for business logic.
- Add API or integration tests for protected workflows.
- Test authorization and organization scope.
- Use fake AI providers for automated tests.
- Test retrieval before generation.
- Test citations for grounded answers.
- Test insufficient-context behavior.
- Test feedback and dashboard metrics when affected.
- Update tests when changing API contracts.
- Update documentation when changing tested behavior.

## 14.2 AI Agents Must Not

- Depend on live AI provider calls for normal test success.
- Add features without corresponding tests.
- Skip authorization tests.
- Skip organization scope tests.
- Hide business rule behavior in controllers or UI components.
- Treat frontend visibility as security.
- Remove insufficient-context tests.
- Remove citation tests.
- Add out-of-scope MVP features without updating roadmap and ADRs.
- Invent tests for undocumented behavior without first updating requirements or business rules.

---

# 15. Summary

This testing strategy connects KnowledgeOps-AI validation to the project’s documented requirements, use cases, business rules, architecture, API design, and security model.

Testing must prove that the system works as a business-driven AI knowledge assistant, not merely as a collection of technical components.

The most important testing concerns are:

- Secure access.
- Organization-scoped data isolation.
- Document lifecycle correctness.
- Retrieval eligibility.
- RAG grounding.
- Source citations.
- Insufficient-context safety.
- Feedback traceability.
- Dashboard accuracy.
- Safe observability.
- MVP scope control.

A feature is not complete unless its business behavior is tested, its security boundaries are enforced, and its implementation remains aligned with the documented product intent.
