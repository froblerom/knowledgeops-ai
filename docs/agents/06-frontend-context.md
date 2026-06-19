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

## Testing Gotchas

Non-obvious Angular/Vitest patterns discovered during implementation. A future spec will re-hit these without this guidance.

### `mat-icon` renders ligature text in jsdom

In a jsdom test environment, `mat-icon` outputs the icon name as visible text content. A link or button containing `<mat-icon>chat</mat-icon>Chat` produces `textContent = 'chat Chat'`, not `'Chat'`. Assertions on link or button text arrays must use:

```typescript
expect(links.some(l => l?.includes('Chat'))).toBe(true);
// NOT: expect(links).toContain('Chat');
```

### `protected` component members in spec files

Angular component members declared `protected` are not accessible from spec files by TypeScript. Cast to bypass:

```typescript
(componentInstance as any).form.setValue({ ... });
(componentInstance as any).submit();
(componentInstance as any).errorMessage();
```

This applies to any form field, method, or signal marked `protected` in the component class.

### Signal properties must be called as functions

A Signal property (e.g. `isSubmitting = signal(false)`) must be invoked as a function to read its value in tests. Accessing it as a plain property returns the `WritableSignal` object, not the value.

```typescript
expect((componentInstance as any).isSubmitting()).toBe(false); // correct
expect((componentInstance as any).isSubmitting).toBe(false);   // wrong — returns signal object
```

### `vi.spyOn` on the injected Router instance (not DI token override)

Overriding the `Router` DI token with a plain object breaks Angular's router factory. Spy on the real injected instance instead:

```typescript
vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
```

## Sources

- ADR-003, `docs/11-architecture-overview.md`, `docs/15-api-design.md`, `docs/16-security-and-permissions.md`, `docs/17-testing-strategy.md`, `docs/22-implementation-guardrails.md`.

