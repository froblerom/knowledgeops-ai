# Pull Request Review Template

## Summary

- What changed:
- Why:
- Related roadmap sprint:

## Related Issue

- Issue:
- PR closes issue: yes/no
- Closure reference:
- Branch follows issue naming convention: yes/no

## Files Changed

- Files changed:

## Scope Confirmation

- Intended scope:
- Out-of-scope items confirmed absent:
- Deferred behavior accidentally introduced: yes/no

## Architecture Boundary Check

- [ ] Controllers remain thin where endpoints changed.
- [ ] Domain/Application remain independent of EF Core and provider SDK implementations.
- [ ] Infrastructure contains technical adapters.
- [ ] ADRs remain aligned or explicit ADR/document updates are included.

## Security And Scope Check

- [ ] Backend authorization protects affected operations.
- [ ] Organization scope is applied where protected data is read or written.
- [ ] Direct API denial and cross-scope cases are tested where applicable.
- [ ] Sensitive responses/logs/health output reveal no protected data or secrets.

## AI / RAG Safety Check When Applicable

- [ ] Retrieval occurs before generation.
- [ ] Only authorized eligible chunks may reach prompts.
- [ ] Grounded answers include citations.
- [ ] Insufficient-context behavior is safe.
- [ ] Provider adapters remain isolated and fake providers cover normal tests.

## Testing And Validation

- Commands run:
- Results:
- Tests added or updated:
- Validation not run and reason:
- Remaining test gaps:

## Documentation Updates

- Documentation updated:
- Progress records updated:
- ADR or contract change required:
- Deferred follow-up, if applicable:

## Risk / Follow-Up Notes

- Residual risk:
- Follow-up work:

## Checklist

- [ ] Issue scope and acceptance criteria are met.
- [ ] No unapproved MVP expansion is present.
- [ ] Relevant validation evidence is reported honestly.
- [ ] Implementation guardrails and Definition of Done are satisfied.
- [ ] No secrets, credentials, real data, or production connection strings were committed.
- [ ] Related issue is referenced and closed by this pull request.
- [ ] Merge recommendation is stated with any blockers.
