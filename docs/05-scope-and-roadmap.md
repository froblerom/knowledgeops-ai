# Scope and Roadmap

## 1. Purpose

This document defines what will be built now, what will be deferred, and what will not be built for **KnowledgeOps-AI**.

The purpose of this document is to protect the project from uncontrolled expansion. KnowledgeOps-AI is an enterprise AI-powered internal knowledge assistant for contact centers and support operations. Because AI knowledge platforms can easily grow into many adjacent areas, this document establishes clear delivery boundaries for the MVP and future phases.

This document should be used by:

- Human developers.
- AI coding agents.
- Technical reviewers.
- Product stakeholders.
- Portfolio reviewers.
- Future maintainers.

Any proposed feature should be checked against this document before implementation. If a feature does not support the approved MVP, Phase 2, or Phase 3 scope, it should be deferred or rejected unless the scope is formally updated.

---

## 2. Product Direction

KnowledgeOps-AI exists to help contact center organizations transform internal documentation into reliable, searchable, conversational, cited, and measurable knowledge.

The system is not intended to be a generic chatbot. It is designed as a controlled business knowledge assistant that uses internal documents as the grounding source for AI-generated answers.

The long-term product direction is to support:

- Document ingestion.
- Text extraction.
- Document chunking.
- Embedding generation.
- Semantic retrieval.
- Retrieval-Augmented Generation.
- Source citations.
- Chat history.
- User feedback.
- Role-aware access control.
- Operational dashboard metrics.
- AI usage observability.
- Continuous knowledge improvement.

The roadmap is divided into controlled phases to avoid overbuilding too early.

---

## 3. Scope Control Principles

The project must follow these scope control principles.

### 3.1 Build the Business Workflow Before Advanced AI Features

The first priority is to prove that users can upload documents, ask questions, receive grounded answers, review citations, and provide feedback.

Advanced AI features should not be added before the core workflow is stable.

### 3.2 Prefer a Focused MVP Over a Large Unfinished Platform

The MVP should demonstrate the central value proposition clearly.

The goal is not to build every possible knowledge management feature. The goal is to validate that AI-assisted document retrieval can solve a real contact center knowledge problem.

### 3.3 Keep AI Grounded and Auditable

AI-generated answers must be connected to retrieved document context whenever possible.

The system should avoid unsupported answers and should clearly indicate when it does not have enough information.

### 3.4 Keep Access Control in Scope from the Beginning

Because internal documents may contain sensitive information, authentication, roles, and organization-aware access are part of the MVP.

Security should not be treated as a later enhancement.

### 3.5 Defer Workflow Automation Until the Knowledge Assistant Works

The system should first become a reliable assistant for knowledge access.

Automation features such as automatic policy enforcement, ticket actions, live agent assist, or autonomous workflows should be deferred.

### 3.6 Design for Future Expansion Without Building Everything Now

The architecture should support future growth, but the MVP should avoid unnecessary complexity.

Interfaces and boundaries are encouraged. Premature enterprise-scale complexity is not.

---

## 4. MVP Scope

The MVP defines the minimum valuable product required to prove the core business and technical value of KnowledgeOps-AI.

The MVP should demonstrate that a contact center user can upload internal documents, process them into searchable knowledge, ask questions, receive grounded answers with citations, provide feedback, and view basic operational metrics.

---

## 4.1 MVP Business Scope

The MVP focuses on the following business outcomes:

- Help support agents find internal knowledge faster.
- Reduce dependency on supervisors for repeated documented questions.
- Improve trust in AI responses through source citations.
- Give administrators visibility into document processing.
- Give managers basic visibility into usage, feedback, latency, and estimated cost.
- Demonstrate safe and controlled AI adoption in a contact center context.

The MVP should be usable as a realistic demonstration of an internal AI knowledge assistant for support operations.

---

## 4.2 MVP User Scope

The MVP should support the following user roles:

### Support Agent

Can ask questions, receive answers, review citations, and submit feedback.

### Supervisor

Can use the assistant, review answer usefulness, and observe repeated questions or usage signals where available.

### Knowledge Administrator

Can upload documents, review metadata, and check document processing status.

### Operations Manager

Can review basic dashboard metrics related to usage, latency, cost, documents, and feedback.

### System Administrator

Can manage users, roles, and basic access boundaries.

The MVP does not need to fully optimize every workflow for every stakeholder. It should support enough role coverage to demonstrate business value and access control.

---

## 4.3 MVP Functional Scope

The MVP includes the following functional capabilities.

### 4.3.1 Authentication and Authorization

The system should support:

- User login.
- Authenticated API access.
- Role-based access control.
- Organization-aware access boundaries.
- Protection of document and chat endpoints.

Minimum roles may include:

- Agent.
- Supervisor.
- KnowledgeAdmin.
- Manager.
- Admin.

### 4.3.2 Document Upload

The system should support:

- Uploading internal documents.
- Storing document metadata.
- Assigning documents to an organization or access scope.
- Recording upload user and upload timestamp.
- Displaying uploaded documents in an admin or knowledge management view.

Supported document formats may start with a limited set such as:

- PDF.
- TXT.
- Markdown.
- DOCX, if practical for the MVP.

The MVP should prioritize reliable ingestion over broad file format support.

### 4.3.3 Document Processing

The system should process uploaded documents asynchronously.

Processing should include:

- Text extraction.
- Chunk generation.
- Embedding generation.
- Storage of document chunks.
- Storage of processing status.
- Storage of processing failure reason when processing fails.

Document status values may include:

- Uploaded.
- Processing.
- Processed.
- Failed.

Processing status is separate from retrieval availability. The MVP stores `is_retrieval_enabled` so an authorized Knowledge Administrator or Admin can exclude a processed document from future retrieval without changing its processing outcome. A document is retrievable only when it is `Processed`, retrieval-enabled, not soft-deleted, and authorized for the current organization scope.

### 4.3.4 Document Chunking

The system should split extracted text into chunks suitable for retrieval.

The MVP should store:

- Chunk text.
- Chunk index.
- Document reference.
- Page or section reference when available.
- Token or character length when practical.
- Embedding reference or vector data.
- Created timestamp.

Chunking does not need to be perfect in the MVP, but it must be consistent and testable.

### 4.3.5 Embedding Generation

The system should generate embeddings for document chunks.

The MVP should:

- Use Azure OpenAI, OpenAI API, or a compatible embedding provider.
- Isolate embedding calls behind an application interface.
- Store embedding results in a searchable form.
- Track embedding errors during processing.

### 4.3.6 Vector or Semantic Retrieval

The system should retrieve relevant document chunks for user questions.

The MVP should support:

- Query embedding generation.
- Similarity search against indexed chunks.
- Retrieval of top relevant chunks.
- Filtering by organization or access scope.
- Returning retrieval metadata for answer generation.

The initial retrieval implementation may use a practical vector-capable storage approach suitable for the project stage.

### 4.3.7 RAG Chat

The system should provide a chat endpoint and user interface that allow users to ask questions against the indexed knowledge base.

The RAG flow should:

1. Receive the user question.
2. Validate user access.
3. Generate or prepare the query representation.
4. Retrieve relevant chunks.
5. Build a prompt using retrieved context.
6. Call the AI generation provider.
7. Return a grounded answer.
8. Include citations to source documents.
9. Store chat and evaluation metadata.

### 4.3.8 Source Citations

The MVP must include citations when relevant sources are found.

A citation should identify:

- Document title or name.
- Document identifier.
- Chunk reference.
- Page or section reference when available.
- Relevance score when practical.

The UI should make it clear which sources were used to generate the answer.

### 4.3.9 Insufficient Context Handling

The MVP should avoid pretending to know answers that are not supported by retrieved documents.

When no relevant source is found, the assistant should respond safely.

Possible behavior:

- State that the available documents do not contain enough information.
- Suggest contacting a supervisor or knowledge administrator.
- Avoid inventing unsupported policy details.
- Record the question as an insufficient-context event.

### 4.3.10 Chat History

The system should store chat interactions.

Stored data may include:

- User question.
- Generated answer.
- Timestamp.
- User identifier.
- Organization identifier.
- Sources used.
- Retrieval metadata.
- Latency.
- Estimated cost.
- Feedback status.

### 4.3.11 User Feedback

The MVP should allow users to mark answers as:

- Useful.
- Not useful.

Optional feedback notes may be deferred unless simple to include.

Feedback should support future evaluation and dashboard metrics.

### 4.3.12 Basic Operational Dashboard

The MVP should provide a basic dashboard with metrics such as:

- Number of questions asked.
- Number of active users.
- Average response latency.
- Estimated AI cost.
- Number of documents uploaded.
- Number of documents processed.
- Number of failed document processing attempts.
- Useful feedback count.
- Not useful feedback count.
- Questions with insufficient context.

The dashboard does not need advanced analytics in the MVP.

### 4.3.13 Logging and Observability

The MVP should include structured logging for important workflows.

Logging should cover:

- Document upload.
- Document processing started.
- Document processing completed.
- Document processing failed.
- Chat question received.
- Retrieval completed.
- AI generation completed.
- AI generation failed.
- Feedback submitted.
- Authorization failures.
- Background job failures.

Sensitive content should not be logged unnecessarily.

### 4.3.14 Testing

The MVP should include tests for critical behavior.

Testing scope should include:

- Unit tests for application services.
- Authorization behavior where practical.
- Document processing status transitions.
- Chunking behavior.
- Retrieval service abstractions.
- Chat orchestration behavior.
- Feedback capture.
- Dashboard query behavior.
- Basic integration tests for key API workflows.

---

## 4.4 MVP Technical Scope

The MVP technical stack includes:

### Backend

- .NET 10.
- ASP.NET Core Web API.
- Clean Architecture.
- SQL Server.
- Entity Framework Core.
- Background services.
- Authentication and authorization.
- Structured logging.
- Application configuration.
- AI/search provider abstractions.

### AI and Retrieval

- Embeddings.
- Semantic retrieval.
- RAG prompt construction.
- Azure OpenAI or OpenAI API.
- Source citations.
- Basic response evaluation metadata.

### Frontend

The frontend uses Angular, as accepted in ADR-003.

MVP frontend screens should include:

- Login.
- Chat interface.
- Source citation display.
- Feedback controls.
- Document upload.
- Document processing status.
- Basic admin or dashboard view.

### Cloud and DevOps

The MVP should support:

- Docker.
- Environment-based configuration.
- GitHub Actions.
- Local development setup.
- Secure secrets handling.
- Azure-ready service design.

Full cloud deployment may be completed in a later milestone if not required for the first MVP demonstration.

---

## 5. Phase 2 Scope

Phase 2 expands the MVP into a stronger operational platform after the core RAG workflow is working.

Phase 2 should focus on improving governance, usability, quality review, and operational insight.

---

## 5.1 Phase 2 Business Scope

Phase 2 should improve:

- Knowledge administration.
- Answer quality review.
- Supervisor and manager visibility.
- Training and onboarding insights.
- Document governance.
- AI usage review.

The goal of Phase 2 is to make KnowledgeOps-AI more useful as an internal operational tool, not just a proof of concept.

---

## 5.2 Phase 2 Functional Scope

Phase 2 may include:

### 5.2.1 Advanced Document Management

- Document replacement.
- Extended document availability history and re-enable administration.
- Basic document version tracking.
- Document tags.
- Document categories.
- Document owner field.
- Effective date and expiration date.
- Better metadata filtering.

### 5.2.2 Improved Admin Workflows

- Admin review of failed document processing.
- Retry document processing.
- View chunk counts per document.
- View last processed timestamp.
- View embedding status.
- View document usage counts.

### 5.2.3 Enhanced Feedback

- Optional feedback comments.
- Feedback reason categories.
- Quality review status.
- Ability to flag an answer for review.
- Supervisor or QA review notes.

### 5.2.4 Improved Dashboard

- Frequently asked topics.
- Most used documents.
- Least used documents.
- Questions with no answer.
- Feedback trends.
- Cost over time.
- Latency over time.
- Usage by role or organization.
- Document processing trends.

### 5.2.5 Better Citation Experience

- Citation preview.
- Highlighted chunk text.
- Page number display when available.
- Link from citation to document details.
- Citation confidence indicators.

### 5.2.6 Knowledge Gap Review

- List of questions with insufficient context.
- Group similar unanswered questions.
- Assign knowledge gaps to administrators or trainers.
- Mark gaps as resolved after document updates.

### 5.2.7 Prompt and Retrieval Review

- Store prompt template version.
- Store retrieval configuration version.
- Review retrieved chunks per answer.
- Compare useful and not useful answer patterns.

### 5.2.8 Notification or Review Workflow

Basic internal notifications may be introduced for:

- Failed document processing.
- Repeated unanswered questions.
- Answers flagged for review.
- Documents nearing expiration.

This should remain lightweight and should not become a full workflow engine.

---

## 5.3 Phase 2 Technical Scope

Phase 2 may include:

- More complete API validation.
- Improved integration tests.
- Better background job resilience.
- Retry policies for transient AI provider failures.
- Better retrieval filtering.
- Improved chunking strategy.
- More detailed metrics.
- Better frontend dashboard components.
- Application Insights integration.
- More complete Docker Compose setup.
- Initial Azure deployment readiness.

---

## 6. Phase 3 Scope

Phase 3 extends KnowledgeOps-AI toward a more mature enterprise AI knowledge platform.

Phase 3 should only begin after MVP and Phase 2 capabilities are stable.

---

## 6.1 Phase 3 Business Scope

Phase 3 should focus on:

- Enterprise readiness.
- Advanced analytics.
- Multi-organization scaling.
- Better governance.
- Deeper AI evaluation.
- Cloud deployment maturity.
- Integration with external enterprise systems.

The goal of Phase 3 is to demonstrate how the system could evolve beyond a portfolio MVP into a more realistic enterprise platform.

---

## 6.2 Phase 3 Functional Scope

Phase 3 may include:

### 6.2.1 Enterprise Authentication

- Azure Entra ID integration.
- SSO.
- More advanced tenant or organization management.
- Permission groups.
- Role assignment workflows.

### 6.2.2 Advanced Knowledge Governance

- Document approval workflows.
- Content owner review.
- Policy expiration workflows.
- Full document version history.
- Audit trail for document lifecycle changes.
- Review and publishing states.

### 6.2.3 Advanced AI Evaluation

- Evaluation datasets.
- Golden question-answer pairs.
- Automated regression evaluation.
- Retrieval quality scoring.
- Answer faithfulness checks.
- Citation accuracy checks.
- Human review queues.
- A/B comparison of prompt versions.

### 6.2.4 Advanced Analytics

- Topic clustering.
- Trend analysis.
- Knowledge gap heatmaps.
- Adoption reports.
- Cost forecasting.
- Latency breakdowns.
- Team-level and role-level insights.
- Exportable reports.

### 6.2.5 Enterprise Integrations

Possible integrations may include:

- SharePoint.
- Microsoft Teams.
- Confluence.
- ServiceNow.
- Jira Service Management.
- Zendesk.
- Salesforce Service Cloud.
- Internal ticketing systems.
- Existing document repositories.

These integrations should not be part of the MVP unless specifically required.

### 6.2.6 Real-Time Agent Assist

Possible future capabilities may include:

- Live support suggestions.
- Transcript-based retrieval.
- Suggested responses.
- Call or chat context summarization.
- Real-time escalation recommendations.

This is explicitly deferred until the document-based assistant is stable.

### 6.2.7 Advanced Security and Compliance

- More detailed audit logs.
- Data retention policies.
- Sensitive data detection.
- Document classification.
- Access reviews.
- Enterprise compliance reporting.
- Data loss prevention integration.

### 6.2.8 Multi-Language Support

- Multi-language document ingestion.
- Multi-language retrieval.
- Multi-language responses.
- Translation-aware prompt templates.
- Language-specific evaluation.

This should be introduced only when the core English or primary-language flow is already validated.

---

## 6.3 Phase 3 Technical Scope

Phase 3 may include:

- Production-grade Azure deployment.
- Infrastructure as Code.
- Advanced CI/CD.
- Blue/green or staged deployment strategy.
- Robust secrets management.
- Scalable vector search service.
- Queue-based document processing.
- Distributed background workers.
- Full observability dashboards.
- Advanced caching strategies.
- Load testing.
- Security testing.
- Disaster recovery planning.
- Provider abstraction hardening.

---

## 7. Out of Scope

This section defines features that should not be built unless the project scope is formally revised.

The following items are out of scope for the approved project direction.

---

## 7.1 Out of Scope for MVP

The MVP must not include:

- Real-time call transcription.
- Live agent assist.
- Autonomous ticket handling.
- Customer-facing chatbot behavior.
- Automatic policy enforcement.
- Advanced workflow automation.
- Complex approval processes.
- Full document lifecycle governance.
- Enterprise SSO.
- Full multi-tenant enterprise isolation.
- Custom LLM training.
- Custom embedding model training.
- Advanced MLOps.
- Predictive workforce management.
- Omnichannel routing.
- Full contact center platform replacement.
- Complex compliance certification.
- Full analytics suite.
- Advanced topic modeling.
- External enterprise system integrations.
- Mobile application.
- Browser extension.
- Native desktop application.

---

## 7.2 Out of Scope for the Overall Product Direction

The project is not intended to become:

- A complete contact center platform.
- A ticketing system.
- A workforce management system.
- A CRM.
- A legal advisory system.
- A compliance certification authority.
- A fully autonomous business decision-maker.
- A generic public chatbot.
- A replacement for human supervisors, managers, trainers, or quality analysts.
- A model training platform.
- A general-purpose document management system unrelated to AI retrieval.
- A social collaboration platform.
- A project management tool.

---

## 7.3 Out of Scope for AI Behavior

The AI assistant must not:

- Invent policies when no source exists.
- Present unsupported answers as official guidance.
- Override business rules.
- Make final HR, legal, compliance, or financial decisions.
- Expose documents the user is not authorized to access.
- Ignore document access boundaries during retrieval.
- Replace human review for sensitive cases.
- Claim certainty when the retrieved context is insufficient.
- Use hidden external knowledge as the source of business policy.

---

## 8. Assumptions

The roadmap is based on the following assumptions.

### 8.1 Business Assumptions

- Contact centers have useful internal knowledge stored in documents.
- Users need faster access to internal policies, procedures, and troubleshooting information.
- Organizations value source-grounded AI more than generic chatbot responses.
- Supervisors and managers benefit from visibility into repeated questions and knowledge gaps.
- Knowledge administrators can provide documents for ingestion.
- Role-based access is required from the beginning.
- The MVP can demonstrate business value without advanced integrations.

### 8.2 Technical Assumptions

- The system can extract usable text from supported document formats.
- Documents can be chunked into retrievable sections.
- Embeddings can be generated using Azure OpenAI, OpenAI API, or a compatible provider.
- A vector-capable retrieval approach can be implemented for the project.
- Background processing can handle ingestion asynchronously.
- SQL Server can support core metadata, chat history, feedback, and operational records.
- The selected frontend framework can support chat, upload, admin, and dashboard workflows.
- Docker and GitHub Actions are sufficient for initial DevOps maturity.
- Azure services can be integrated gradually.

### 8.3 AI Assumptions

- Retrieval quality depends heavily on document quality, chunking strategy, metadata, and filtering.
- Not every question will have enough context to answer.
- Source citations are necessary for trust.
- Feedback is required for continuous improvement.
- Cost and latency must be measured early.
- Provider-specific implementation should be isolated behind interfaces where practical.

### 8.4 Portfolio Assumptions

- The project is designed to demonstrate enterprise-grade applied AI development.
- Documentation is part of the product.
- Scope clarity is necessary for human and AI-assisted implementation.
- Recruiters and reviewers should be able to understand the value quickly.
- The repository should demonstrate both technical depth and business reasoning.

---

## 9. Release Milestones

The project should be delivered through controlled release milestones.

Each milestone should produce a coherent increment of value and avoid drifting into features that belong to later phases.

---

## 9.1 Release 0 — Foundation and Documentation

### Goal

Establish business, product, architecture, and implementation context before writing major application code.

### Scope

- Executive Summary.
- Business Context.
- Business Case.
- Project Charter.
- Stakeholder Map.
- Scope and Roadmap.
- Initial README direction.
- Initial architecture assumptions.
- Initial technology stack decision.
- Repository structure plan.

### Exit Criteria

- Project purpose is clear.
- Business domain is defined.
- Stakeholders are identified.
- MVP scope is documented.
- Out-of-scope boundaries are explicit.
- AI agents can understand project intent before implementation.

---

## 9.2 Release 1 — Application Skeleton

### Goal

Create the initial enterprise application structure.

### Scope

- Backend solution setup.
- Clean Architecture project structure.
- API project.
- Application layer.
- Domain layer.
- Infrastructure layer.
- Persistence setup.
- Initial SQL Server configuration.
- Initial authentication setup.
- Basic health endpoint.
- Docker development setup.
- Initial CI workflow.

### Exit Criteria

- Application builds successfully.
- API runs locally.
- Clean Architecture boundaries exist.
- Database connection is configured.
- Basic authentication foundation exists.
- CI validates build and tests.
- Docker setup supports local development.

---

## 9.3 Release 2 — Identity, Roles, and Access Boundaries

### Goal

Implement the security foundation required before document access and chat features.

### Scope

- User model.
- Organization or account scope model.
- Role model.
- Login flow.
- JWT or selected authentication approach.
- Role-based authorization.
- Organization-aware access checks.
- Seed users for demonstration.
- Authorization tests.

### Exit Criteria

- Users can authenticate.
- Roles are enforced.
- Organization-aware access can be validated.
- Unauthorized access is blocked.
- Tests cover critical access rules.

---

## 9.4 Release 3 — Document Upload and Metadata

### Goal

Allow authorized users to upload documents and store document records.

### Scope

- Document upload endpoint.
- Document metadata model.
- File storage abstraction.
- Initial local or cloud-compatible storage implementation.
- Document list endpoint.
- Document detail endpoint.
- Basic admin UI for upload and listing.
- Metadata persistence.
- Upload validation.
- Upload tests.

### Exit Criteria

- Authorized users can upload documents.
- Document metadata is stored.
- Uploaded documents are visible in the UI.
- Invalid uploads are handled safely.
- Access rules are enforced.
- Tests validate upload behavior.

---

## 9.5 Release 4 — Document Processing Pipeline

### Goal

Process uploaded documents into retrievable text chunks.

### Scope

- Background processing service.
- Text extraction for selected formats.
- Chunking service.
- Document processing states.
- Failure reason storage.
- Chunk persistence.
- Admin visibility into processing status.
- Processing tests.

### Exit Criteria

- Uploaded documents move through processing states.
- Text extraction works for supported formats.
- Chunks are stored.
- Failures are visible.
- Processing runs asynchronously.
- Tests validate status transitions and chunk creation.

---

## 9.6 Release 5 — Embeddings and Retrieval

### Goal

Enable semantic retrieval over processed document chunks.

### Scope

- Embedding provider interface.
- Embedding generation implementation.
- Embedding storage approach.
- Query embedding generation.
- Retrieval service.
- Top-K chunk retrieval.
- Organization-aware retrieval filtering.
- Retrieval metadata.
- Retrieval tests.

### Exit Criteria

- Document chunks have embeddings.
- User questions can retrieve relevant chunks.
- Retrieval respects access boundaries.
- Retrieval metadata is available.
- Tests validate retrieval workflow.

---

## 9.7 Release 6 — RAG Chat Assistant

### Goal

Deliver the core assistant experience.

### Scope

- Chat endpoint.
- Prompt template.
- RAG orchestration service.
- AI generation provider interface.
- Grounded answer generation.
- Source citations.
- Insufficient-context handling.
- Chat history persistence.
- Chat UI.
- Citation display.
- Chat tests.

### Exit Criteria

- Users can ask questions.
- Relevant sources are retrieved.
- Answers are generated from retrieved context.
- Citations are returned.
- Chat history is stored.
- Insufficient context is handled safely.
- Tests validate the core RAG flow.

---

## 9.8 Release 7 — Feedback and Evaluation Metadata

### Goal

Capture feedback and operational metadata for answer evaluation.

### Scope

- Useful / not useful feedback.
- Feedback persistence.
- Prompt and response metadata.
- Retrieval context metadata.
- Latency tracking.
- Estimated cost tracking.
- Token usage tracking where available.
- Feedback UI.
- Evaluation metadata tests.

### Exit Criteria

- Users can submit feedback.
- Feedback is stored.
- Latency is tracked.
- Estimated cost is tracked.
- Prompt/response metadata is available for review.
- Tests validate feedback and metadata capture.

---

## 9.9 Release 8 — Operational Dashboard

### Goal

Provide basic operational visibility for managers and administrators.

### Scope

- Dashboard API.
- Question count.
- Active user count.
- Average response latency.
- Estimated AI cost.
- Document count.
- Processed document count.
- Failed document processing count.
- Feedback counts.
- Insufficient-context count.
- Dashboard UI.
- Dashboard query tests.

### Exit Criteria

- Managers can view core usage metrics.
- Administrators can view document processing metrics.
- Feedback trends are visible at a basic level.
- Dashboard respects access rules.
- Tests validate dashboard data.

---

## 9.10 Release 9 — MVP Hardening and Portfolio Readiness

### Goal

Stabilize the MVP for demonstration, review, and portfolio presentation.

### Scope

- End-to-end workflow testing.
- Integration tests for critical APIs.
- UI polish.
- README update.
- Demo script.
- Architecture overview.
- Known limitations document.
- Deployment notes.
- CI stabilization.
- Docker validation.
- Sample documents for demo.
- Screenshots or demo assets.

### Exit Criteria

- MVP flow works end to end.
- Repository is understandable.
- Documentation supports human and AI review.
- Tests pass.
- Demo can be executed reliably.
- Portfolio value is clear.

---

## 10. Roadmap Summary

| Phase | Focus | Main Outcome |
|---|---|---|
| Release 0 | Documentation and context | Clear business and implementation direction. |
| Release 1 | Application skeleton | Enterprise-ready project foundation. |
| Release 2 | Identity and access | Secure access boundaries. |
| Release 3 | Document upload | Documents can enter the system. |
| Release 4 | Processing pipeline | Documents become searchable chunks. |
| Release 5 | Embeddings and retrieval | Semantic search becomes available. |
| Release 6 | RAG chat | Users receive grounded answers with citations. |
| Release 7 | Feedback and evaluation | AI answers become measurable. |
| Release 8 | Dashboard | Operational visibility becomes available. |
| Release 9 | MVP hardening | Project becomes demo-ready and portfolio-ready. |
| Phase 2 | Governance and quality | Stronger admin, QA, feedback, and insight workflows. |
| Phase 3 | Enterprise maturity | Advanced analytics, integrations, governance, and cloud readiness. |

---

## 11. Scope Governance for AI Agents

AI coding agents must follow this document when generating implementation plans, code, tests, or recommendations.

### 11.1 AI Agent Rules

AI agents must:

- Respect MVP boundaries.
- Avoid adding out-of-scope features.
- Prefer business-aligned implementation over generic technical expansion.
- Preserve authentication and access control expectations.
- Keep AI behavior grounded in retrieved documents.
- Avoid unsupported answer behavior.
- Keep provider-specific logic behind infrastructure interfaces where practical.
- Include tests for critical workflows.
- Update documentation when behavior, scope, or architecture changes.
- Ask for scope clarification when a requested feature conflicts with this roadmap.

### 11.2 AI Agent Must Not

AI agents must not:

- Add real-time call transcription during MVP.
- Add autonomous ticket actions during MVP.
- Add customer-facing chatbot behavior during MVP.
- Add complex enterprise SSO during MVP.
- Add model training or fine-tuning during MVP.
- Remove source citation requirements.
- Bypass document access boundaries.
- Treat AI answers as final business authority.
- Expand the system into a full contact center platform.
- Generate code for features that are explicitly out of scope unless the scope document is revised.

---

## 12. Summary

This Scope and Roadmap document defines the controlled delivery path for KnowledgeOps-AI.

The MVP focuses on the essential value proposition: upload internal documents, process them, retrieve relevant knowledge, generate grounded AI answers with citations, capture feedback, and expose basic operational metrics.

Phase 2 improves governance, quality review, dashboard insight, and knowledge administration.

Phase 3 explores enterprise maturity, advanced analytics, integrations, stronger governance, and production-grade cloud readiness.

This phased roadmap protects the project from uncontrolled expansion and ensures that both human developers and AI agents build toward the same business-driven product vision.
