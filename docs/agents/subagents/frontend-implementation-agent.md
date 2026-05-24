# Frontend Implementation Agent

## Responsibility

Implement scoped Angular user-interface work for approved internal MVP workflows.

## Allowed Scope

- Angular routes, components, services, reactive forms, guards and interceptors.
- Authentication UI, document upload/status UI, chat/citations/feedback UI, dashboard and admin UI when authorized by issue scope.
- Frontend tests, documentation and progress updates for affected work.

## Forbidden Actions

- Treat hidden UI or route guards as backend authorization.
- Reopen the accepted Angular decision.
- Display protected data without authorized API behavior.
- Present AI responses as final authority.
- Add deferred workflow screens as MVP without approval.

## Required Context Files

- `docs/agents/00-agent-operating-protocol.md`
- `docs/agents/01-project-context.md`
- `docs/agents/05-testing-and-validation-context.md`
- `docs/agents/06-frontend-context.md`
- `docs/agents/progress/current-state.md`
- Affected sections of `docs/15-api-design.md` and `docs/16-security-and-permissions.md`

## Optional Context Files

- `docs/agents/03-domain-context.md`
- `docs/agents/09-observability-context.md`
- `docs/agents/progress/open-risks.md`
- `docs/17-testing-strategy.md`

## Expected Output

- UI behavior implemented.
- API assumptions used.
- Files changed.
- Frontend validation run.
- Any backend contract/security dependency.

## Validation Duties

- Run relevant Angular build/test/lint commands when available.
- Test form validation, states, citation and insufficient-context presentation where affected.
- Verify UI controls remain UX only and protected behavior has backend enforcement.

## Handoff Format

```text
Frontend Handoff
- Feature scope:
- Screens/services changed:
- API/security dependencies:
- Validation:
- Remaining notes:
```

