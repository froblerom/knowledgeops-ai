# Implementation Decisions Log

This log tracks implementation-time and harness-routing decisions. Accepted ADRs remain authoritative for architecture decisions.

| Date | Decision | Rationale | Affected Area | ADR Needed |
| --- | --- | --- | --- | --- |
| 2026-05-24 | Use `docs/agents/` as the agent context harness root. | Multiple shared contexts, specialist definitions and progress files require a plural modular location. | Agent harness | No |
| 2026-05-24 | Use prompt Levels 0 through 3 only. | Level 3 covers cross-layer, security-sensitive, harness and release work for current MVP. | Prompt routing | No |
| 2026-05-24 | Add dedicated `rag-implementation-agent`. | RAG safety crosses authorization, prompts, citations, provider isolation and sensitive AI telemetry. | Agent harness | No |
| 2026-05-24 | Use progress files before implementation prompts begin. | Prompts require current state, decisions, risks and verified completion history. | Execution workflow | No |
| 2026-05-24 | Mermaid Markdown remains diagram source of truth. | Accepted ADR-009; rendered images remain artifacts. | Documentation/diagrams | No |
| 2026-05-24 | Angular is selected for MVP frontend. | Accepted ADR-003. | Frontend | No |
| 2026-05-24 | `docs/09-business-rules.md` is canonical for `BR-###`. | Prevent conflicting rule identifiers and traceability drift. | Rules/traceability | No |
| 2026-05-24 | Document disablement uses `is_retrieval_enabled = false`, not a `Disabled` processing status. | Keeps processing outcome separate from retrieval availability. | Documents/retrieval | No |

## Update Rule

Add a concise entry when a future issue makes a material implementation choice. If a choice changes an accepted architecture decision, identify the required ADR action rather than treating this log as approval.

