# Business Rules Context

## Canonical Rule Ownership

`docs/09-business-rules.md` is the only canonical source of `BR-###` meanings. Do not redefine or renumber business rules in agent prompts, context summaries, code comments or supporting documents.

Open the exact business-rule sections whenever an issue implements, changes or tests governed behavior.

## Rules To Protect

### Access And Security

- Protected operations require authentication, allowed role permission and authorized organization scope.
- Backend authorization is authoritative; UI visibility is not authorization.
- Deny safely without disclosing protected existence or content.
- Audit sensitive access failures and privileged operations safely.

### Documents And Processing

- Uploaded documents must be validated, stored and processed asynchronously.
- Processing status values are `Uploaded`, `Processing`, `Processed` and `Failed`.
- Store a safe failure reason for failed processing where applicable.
- Disabling future retrieval uses `is_retrieval_enabled = false`, not a processing-status change.

### Retrieval

- Retrieve only processed, retrieval-enabled, non-soft-deleted and organization-authorized documents.
- Retrieval candidates, stored results and prompt context must not include unauthorized chunks.
- Preserve document/chunk traceability for retrieval results and citations.

### RAG And Citations

- Retrieve authorized context before answer generation.
- Grounded answers must include source citations.
- When context is insufficient, disclose that outcome safely instead of inventing support.
- AI responses are not final business authority.

### Feedback And Dashboard

- Feedback is `Useful` or `NotUseful` and belongs to a stored chat interaction.
- Prevent duplicate feedback from misleading metrics.
- Dashboard data is permission- and organization-scoped.
- MVP exposes basic insufficient-context and `NotUseful` signals, not a full knowledge-gap workflow.
- Nullable cost remains unavailable when no estimate exists; do not report unavailable cost as zero.

### Safe Operations And Scope

- Logs and errors must not expose secrets, full prompts or protected source content.
- Keep the MVP internal and document-based.
- Do not implement deferred adjacent operational automation or additional RBAC roles without approved scope change.

## Related Sources

- Requirements: `docs/06-requirements.md`.
- Use cases: `docs/07-use-cases.md`.
- Guardrails: `docs/22-implementation-guardrails.md`.

