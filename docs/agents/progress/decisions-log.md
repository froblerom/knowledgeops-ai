# Implementation Decisions Log

This log tracks implementation-time and harness-routing decisions. Accepted ADRs remain authoritative for architecture decisions.

| Date | Decision | Rationale | Affected Area | ADR Needed |
| --- | --- | --- | --- | --- |
| 2026-05-24 | Use `docs/agents/` as the canonical agent context harness root; do not use `docs/agent/`. | Multiple shared contexts, specialist definitions and progress files require one unambiguous plural modular location. | Agent harness | No |
| 2026-05-24 | Use prompt Levels 0 through 3 only. | Level 3 covers cross-layer, security-sensitive, harness and release work for current MVP. | Prompt routing | No |
| 2026-05-24 | Add dedicated `rag-implementation-agent`. | RAG safety crosses authorization, prompts, citations, provider isolation and sensitive AI telemetry. | Agent harness | No |
| 2026-05-24 | Use progress files before implementation prompts begin. | Prompts require current state, decisions, risks and verified completion history. | Execution workflow | No |
| 2026-05-24 | Mermaid Markdown remains diagram source of truth. | Accepted ADR-009; rendered images remain artifacts. | Documentation/diagrams | No |
| 2026-05-24 | Angular is selected for MVP frontend. | Accepted ADR-003. | Frontend | No |
| 2026-05-24 | `docs/09-business-rules.md` is canonical for `BR-###`. | Prevent conflicting rule identifiers and traceability drift. | Rules/traceability | No |
| 2026-05-24 | Document disablement uses `is_retrieval_enabled = false`, not a `Disabled` processing status. | Keeps processing outcome separate from retrieval availability. | Documents/retrieval | No |
| 2026-05-24 | Future implementation prompts must classify first using `docs/agents/13-prompt-classifier.md`. | Ensures scope, required context, risk and validation expectations are declared before implementation begins. | Execution workflow | No |
| 2026-05-25 | Use `KnowledgeOps` for code projects and namespaces, with `KnowledgeOpsAI.sln` as the solution name while retaining KnowledgeOps-AI branding. | Matches the approved scaffold audit and keeps code identifiers consistent without changing product branding. | Backend solution structure | No |
| 2026-05-25 | Target `net10.0` and pin the installed .NET SDK `10.0.204` in `global.json` with .NET 10 minor roll-forward. | Establishes a reproducible backend foundation without downgrading the approved target framework. | Backend toolchain | No |
| 2026-05-25 | Defer `KnowledgeOps.E2ETests` beyond Issue #3. | Issue #3 requires only the four approved scaffold test projects; E2E workflow coverage belongs to a later authorized sprint. | Testing structure | No |
| 2026-05-25 | Keep Issue #3 dependencies limited to host/DI abstractions and xUnit template support. | Persistence, security, provider SDKs, observability integrations and container testing are outside scaffold scope. | Backend dependency surface | No |

## Update Rule

Add a concise entry when a future issue makes a material implementation choice. If a choice changes an accepted architecture decision, identify the required ADR action rather than treating this log as approval.
