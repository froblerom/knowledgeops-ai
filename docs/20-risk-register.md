# Risk Register

## 1. Purpose

This risk register identifies risks to **KnowledgeOps-AI**, the internal document-based RAG knowledge assistant for contact centers and support operations.

The MVP centers on uploaded internal documents, asynchronous processing, chunking, embeddings, authorized semantic retrieval, cited RAG answers, insufficient-context handling, feedback, basic metrics, authentication, RBAC, organization-scoped access, observability, and Azure-ready delivery.

## 2. Risk Ratings

| Rating | Meaning |
|---|---|
| Critical | Could cause unauthorized disclosure, unsafe AI behavior, or loss of essential trust and must be addressed before release. |
| High | Could materially damage MVP correctness, security, reliability, or delivery. |
| Medium | Could materially reduce usability, operational clarity, or maintainability. |
| Low | Limited effect with manageable workaround. |

| Status | Meaning |
|---|---|
| Open | Identified and requiring planned mitigation. |
| Mitigating | Mitigation activity is in progress. |
| Accepted | Exposure is consciously accepted with documented rationale. |
| Closed | Mitigation has been verified or risk no longer applies. |

## 3. Risk Register

| ID | Risk | Impact | Probability | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|
| RISK-001 | AI produces unsupported or misleading answers despite insufficient approved context. | Critical | Medium | Enforce retrieval-before-generation, insufficient-context disclosure, human-authority messaging, citations, and fake-provider tests for safe behavior. | AI / Backend Lead | Open |
| RISK-002 | Weak retrieval quality causes relevant approved content not to be selected. | High | Medium | Track retrieval metadata, evaluate representative questions, expose insufficient-context outcomes, and tune retrieval within the provider abstraction. | AI Lead | Open |
| RISK-003 | Chunking strategy loses relevant context or creates unusable retrieval units. | High | Medium | Keep chunking deterministic and testable, preserve source/page metadata where available, and validate with sample internal documents. | AI / Backend Lead | Open |
| RISK-004 | Citations are missing, incorrect, or do not trace to the retrieved supporting content. | Critical | Medium | Store retrieval-to-citation relationships, require citations for grounded answers, and test citation mapping and authorized access. | Backend Lead | Open |
| RISK-005 | Unauthorized users access protected document content or citations. | Critical | Medium | Enforce backend authentication, RBAC, organization filtering, safe error handling, and authorization tests. | Security Owner | Open |
| RISK-006 | Retrieval leaks chunks across organization boundaries and injects unauthorized content into prompts. | Critical | Medium | Apply organization filters before retrieval and prompt construction; test cross-organization data and audit failures. | Security / Backend Lead | Open |
| RISK-007 | Prompt, retrieved context, or stored response metadata exposes sensitive internal information through logs or diagnostics. | Critical | Medium | Minimize logged content, restrict operational data, sanitize errors, and review telemetry fields. | Security Owner | Open |
| RISK-008 | Provider keys, connection strings, or deployment secrets leak through source control, logs, or health responses. | Critical | Low | Use environment/secret storage, prohibit hardcoded secrets, scan configuration review, and sanitize health/log responses. | DevOps / Security Owner | Open |
| RISK-009 | AI provider usage costs become materially higher than expected. | High | Medium | Capture token usage and estimated cost when available, monitor trends, bound retrieved context, and configure provider usage carefully. | Product / AI Lead | Open |
| RISK-010 | Retrieval or AI generation latency makes the assistant impractical during support work. | High | Medium | Capture retrieval, generation, and total RAG latency; investigate slow dependencies and keep ingestion asynchronous. | Backend / DevOps Lead | Open |
| RISK-011 | AI or embedding provider downtime prevents processing or answer generation. | High | Medium | Isolate providers behind abstractions, return safe failures, log provider faults, and use fakes in normal CI. | AI / DevOps Lead | Open |
| RISK-012 | Document upload or processing failures leave expected knowledge unavailable. | High | Medium | Store processing status and safe failure reason, expose failure metrics, and document support investigation. | KnowledgeAdmin / Backend Lead | Open |
| RISK-013 | Text extraction fails silently or accepts unusable text. | High | Medium | Treat empty/invalid extraction as failure, store safe failure reason, and keep failed documents non-retrievable. | Backend Lead | Open |
| RISK-014 | Embedding generation failure or incomplete vector data makes chunks incorrectly retrievable. | High | Medium | Require completed processing and valid searchable representation before retrieval; log embedding failure and test partial failure. | AI / Backend Lead | Open |
| RISK-015 | Vector/index state becomes inconsistent with documents, chunks, or retrieval availability. | High | Medium | Preserve identifiers and scope, filter using canonical eligibility rules, and test retrieval-disabled/deleted/failed source exclusion. | Backend Lead | Open |
| RISK-016 | Processing lifecycle and retrieval eligibility are implemented inconsistently. | High | Medium | Use statuses `Uploaded`, `Processing`, `Processed`, `Failed`; use `is_retrieval_enabled` and `deleted_at` independently; test the canonical retrieval predicate. | Backend Lead | Open |
| RISK-017 | Dashboard metrics misrepresent latency, cost, feedback, or insufficient-context outcomes. | High | Medium | Define metric sources, represent unavailable cost honestly, scope queries by organization, and test aggregations. | Backend / Product Lead | Open |
| RISK-018 | Insufficient-context handling fails and presents unsupported output as authoritative guidance. | Critical | Medium | Require safe fallback behavior, test weak/no-context paths, and retain human decision authority language. | AI / Product Lead | Open |
| RISK-019 | MVP scope expands into a customer-facing chatbot, live agent assist, real-time transcription, autonomous action automation, ticketing, or a full contact center platform. | High | Medium | Enforce scope rules and ADR review in planning; keep these capabilities deferred or out of scope. | Product Owner | Open |
| RISK-020 | Application or domain logic becomes coupled directly to an AI provider SDK. | High | Medium | Preserve the Azure OpenAI-compatible abstraction decision and use fake implementations for tests and CI. | Architecture Owner | Open |
| RISK-021 | Database migrations or schema changes compromise document, citation, feedback, or audit traceability. | High | Medium | Review EF Core migrations carefully, back up environments as appropriate, validate rollback strategy, and preserve relationships. | Backend / DevOps Lead | Open |
| RISK-022 | Audit logging is incomplete for role changes, retrieval-availability changes, access failures, or sensitive admin visibility. | High | Medium | Define audit-sensitive events, store safe append-oriented records, and test audit creation without sensitive content leakage. | Security / Backend Lead | Open |
| RISK-023 | Frontend visibility controls are mistaken for authorization. | Critical | Medium | Treat backend authorization as authoritative, keep Angular route visibility supplemental, and test direct API denial. | Security / Frontend Lead | Open |
| RISK-024 | Health endpoints expose sensitive dependency or provider details. | High | Low | Keep `/api/v1/health` basic and safe; restrict `/api/v1/health/details` to `Admin`; sanitize detail responses. | DevOps / Security Owner | Open |
| RISK-025 | CI relies on live AI calls and becomes expensive, flaky, or non-repeatable. | High | Medium | Use fake AI providers by default in CI and reserve live validation for explicitly controlled testing. | DevOps / AI Lead | Open |
| RISK-026 | Docker or Azure-ready configuration diverges across environments. | Medium | Medium | Use environment-based configuration, document deployment assumptions, validate health and migrations, and prohibit embedded secrets. | DevOps Lead | Open |
| RISK-027 | Documentation, ADRs, API contracts, and tests drift from the approved internal RAG product boundary. | High | Medium | Re-run documentation consistency review at major planning transitions and update canonical documents together. | Product / Architecture Owner | Open |

## 4. Highest Priority Risks Before Implementation Planning

| Risk | Reason |
|---|---|
| RISK-005 / RISK-006 | Document and cross-organization retrieval exposure are unacceptable security failures. |
| RISK-001 / RISK-004 / RISK-018 | Grounding, citations, and insufficient-context handling determine user trust and AI safety. |
| RISK-016 | One lifecycle and eligibility contract is required for schema, APIs, and tests. |
| RISK-019 | The MVP must remain an internal document-based knowledge assistant. |
| RISK-023 | Backend enforcement is essential for protected content. |

## 5. Risk Review Process

Risk review should occur:

- before the Implementation Roadmap is drafted;
- when an ADR, scope boundary, security contract, data model, or API surface changes;
- before MVP release and before an Azure deployment;
- after material provider, retrieval, authorization, or processing incidents.

For each review:

1. Confirm the risk remains applicable to current scope.
2. Confirm impact, probability, owner, and mitigation remain accurate.
3. Link mitigation evidence to requirements, ADRs, tests, observability, or deployment controls where appropriate.
4. Change status only when mitigation or acceptance is documented.

## 6. Scope And Governance Notes

- KnowledgeOps-AI remains internal-only and document-based for MVP.
- Angular is the accepted frontend framework.
- MVP technical roles remain `Agent`, `Supervisor`, `KnowledgeAdmin`, `Manager`, and `Admin`.
- `docs/09-business-rules.md` is the canonical source for business rule identifiers.
- Full knowledge-gap review workflows are Phase 2; MVP stores events and exposes basic counts.
- Accepted ADRs govern clean architecture, SQL Server, EF Core, AI provider isolation, RAG citations, asynchronous processing, Mermaid diagrams, and organization scope.

## 7. Acceptance Criteria

- Risks address document processing, retrieval, RAG behavior, citations, provider dependencies, security, observability, delivery, and scope control.
- Security risks explicitly include unauthorized document exposure and cross-organization retrieval leakage.
- AI risks explicitly include hallucination, citations, insufficient context, cost, and latency.
- The register contains no required MVP workflow outside the internal document-based knowledge assistant.
