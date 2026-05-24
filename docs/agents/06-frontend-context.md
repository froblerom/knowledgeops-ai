# Frontend Context

## Frontend Decision

Angular is selected for the MVP. Use TypeScript, RxJS, Angular Router, Reactive Forms, guards where appropriate and interceptors where appropriate. Do not reopen framework selection through implementation work.

## Intended Structure

```text
frontend/
  src/
    app/
      core/
      shared/
      features/
        auth/
        documents/
        chat/
        dashboard/
        admin/
```

## MVP UI Areas

- Login/session handling and protected navigation.
- Document upload, document listing and processing-status visibility for authorized roles.
- Chat question entry and answer presentation.
- Source citation display for grounded answers.
- Clear insufficient-context response display.
- `Useful`/`NotUseful` feedback controls.
- Basic scoped dashboard metrics.
- Admin user/role and safe operational views only where API/security contracts authorize them.

## Security Rules

- Role-aware navigation, route guards and hidden actions improve UX only.
- Backend authorization and organization scope remain authoritative.
- Do not render protected data that the API has not authorized.
- UI copy must not present AI answers as final authority.

## Frontend Validation

- Validate forms, loading/error states, guards/interceptors and typed API handling.
- Test role-aware visibility without treating it as security proof.
- Test citation, feedback and insufficient-context presentation where affected.
- Read `docs/15-api-design.md` and `docs/16-security-and-permissions.md` for protected API screens.

## Sources

- ADR-003, `docs/11-architecture-overview.md`, `docs/15-api-design.md`, `docs/16-security-and-permissions.md`, `docs/17-testing-strategy.md`, `docs/22-implementation-guardrails.md`.

