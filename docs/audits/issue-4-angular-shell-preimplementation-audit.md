# Issue #4 Angular Shell Pre-Implementation Audit

## 1. Purpose

This audit verifies the repository state and development environment readiness before scaffolding the Angular MVP application shell for Issue #4 (`feat: scaffold Angular MVP application shell`), which corresponds to Sprint 2 of the KnowledgeOps-AI MVP roadmap.

The audit confirms that the repository is a clean frontend slate, the Node/npm/Angular CLI environment is available and at compatible versions, the backend scaffold from Issue #3 is present and must not be touched, the canonical frontend structure and routing plan are decided, placeholder page intent is documented, API base URL and environment configuration are planned, HTTP client/interceptor/guard placeholder policies are established, and test strategy is clear. No Angular workspace, frontend files, source code changes, packages, lockfiles, Docker files, CI files, or diagram artifacts are created during this audit.

---

## 2. Classification

```text
Classification
- Task type: Pre-implementation audit
- Prompt level: Level 3
- Related sprint/issue: Sprint 2 / Issue #4
- Scope: Audit-only / Frontend foundation readiness
- Primary affected area: Angular MVP application shell readiness
- Security or organization-scope impact: None directly. The audit must confirm that
  frontend-as-UX-only and backend-authorization-as-source-of-truth boundaries are
  preserved. Pass-through interceptors and guards must not simulate real auth enforcement.
- AI/RAG impact: None directly. No RAG behavior, citation rendering connected to backend
  data, or AI provider behavior may be implemented in Issue #4. Placeholder pages must
  not assume completed backend workflows.
- Data or migration impact: None. No database, EF Core, or backend changes in Issue #4.

Reason
- Issue #4 scaffolds the Angular frontend shell. It crosses the frontend application
  boundary, establishes routing, environment config, HTTP client pattern, interceptor and
  guard placeholders, and loading/error conventions. It defines the frontend structural
  foundation for all subsequent feature sprints. Frontend architecture work of this
  kind is Level 3 per docs/agents/12-prompt-levels.md.

Required Context
- Agent context files: 00-agent-operating-protocol.md, 10-issue-execution-template.md,
  12-prompt-levels.md, 13-prompt-classifier.md, 06-frontend-context.md,
  05-testing-and-validation-context.md, 08-devops-context.md
- Canonical documents: docs/21-implementation-roadmap.md,
  docs/22-implementation-guardrails.md, docs/11-architecture-overview.md,
  docs/15-api-design.md, docs/16-security-and-permissions.md,
  docs/17-testing-strategy.md, docs/18-deployment-and-devops.md
- ADRs: ADR-003
- Progress files: current-state.md, decisions-log.md, open-risks.md, completed-issues.md

Recommended Subagents
- architecture-auditor (this audit fulfills that role)
- frontend-implementation-agent (for the follow-on implementation prompt)
- testing-agent (for Angular test wiring and build verification)
- verification-agent (to confirm build and test baseline after implementation)
```

---

## 3. Files And Context Reviewed

### Agent Harness Files

- `docs/agents/00-agent-operating-protocol.md`
- `docs/agents/10-issue-execution-template.md`
- `docs/agents/12-prompt-levels.md`
- `docs/agents/13-prompt-classifier.md`
- `docs/agents/06-frontend-context.md`
- `docs/agents/05-testing-and-validation-context.md`
- `docs/agents/08-devops-context.md`

### Progress Files

- `docs/agents/progress/current-state.md`
- `docs/agents/progress/decisions-log.md`
- `docs/agents/progress/open-risks.md`
- `docs/agents/progress/completed-issues.md`

### Canonical Implementation Planning Documents

- `docs/21-implementation-roadmap.md`
- `docs/22-implementation-guardrails.md`

### Architecture And Frontend Documents

- `docs/11-architecture-overview.md`
- `docs/15-api-design.md` (API base URL and route conventions)
- `docs/16-security-and-permissions.md` (frontend-as-UX-only confirmation)
- `docs/17-testing-strategy.md`
- `docs/18-deployment-and-devops.md`

### Architecture Decision Records

- `docs/decisions/README.md`
- `docs/decisions/ADR-003-use-angular.md`

### Repository Files And Environment Inspected

- Repository root directory contents
- Presence/absence of `frontend/`, `angular.json`, `package.json`, lockfiles
- Presence/absence of `.nvmrc`, `.node-version`
- Backend scaffold directories and files from Issue #3
- `src/KnowledgeOps.Api/Properties/launchSettings.json` (API port convention)
- `.gitignore` (Angular-pattern coverage)
- `node --version`, `npm --version`, `npx --version`
- `npx @angular/cli version` output

---

## 4. Repository State

### Root Files And Folders

| Item | Present |
| --- | --- |
| `.claude/` | Yes |
| `.github/` | Yes |
| `docs/` | Yes |
| `src/` | Yes — backend scaffold from Issue #3 |
| `tests/` | Yes — backend test projects from Issue #3 |
| `KnowledgeOpsAI.sln` | Yes — backend solution from Issue #3 |
| `global.json` | Yes — from Issue #3, pins SDK 10.0.204 |
| `Directory.Build.props` | Yes — from Issue #3, sets `net10.0` |
| `.gitignore` | Yes — Visual Studio template; includes `node_modules/` globally |
| `LICENSE` | Yes |
| `README.md` | Yes |

### Frontend State

| Item | Present |
| --- | --- |
| `frontend/` | **No** — clean frontend slate |
| `angular.json` | **No** |
| `package.json` (repo root) | **No** |
| `package.json` (frontend root) | **No** |
| `package-lock.json` | **No** |
| `yarn.lock` | **No** |
| `pnpm-lock.yaml` | **No** |
| `.angular/` | **No** |
| `.nvmrc` or `.node-version` | **No** |

**Finding**: The frontend is a clean slate. No conflicts, no existing package manager convention from a frontend perspective.

### Backend Scaffold Presence (Issue #3)

| Backend Item | Present |
| --- | --- |
| `KnowledgeOpsAI.sln` | Yes |
| `src/KnowledgeOps.Api/` | Yes |
| `src/KnowledgeOps.Application/` | Yes |
| `src/KnowledgeOps.Domain/` | Yes |
| `src/KnowledgeOps.Infrastructure/` | Yes |
| `src/KnowledgeOps.Worker/` | Yes |
| `tests/KnowledgeOps.Domain.Tests/` | Yes |
| `tests/KnowledgeOps.Application.Tests/` | Yes |
| `tests/KnowledgeOps.Api.Tests/` | Yes |
| `tests/KnowledgeOps.IntegrationTests/` | Yes |

**Finding**: Issue #3 is verified complete per `completed-issues.md` (2026-05-25). The backend scaffold must not be touched during Issue #4 implementation.

### Established Backend API Port

`src/KnowledgeOps.Api/Properties/launchSettings.json` specifies:

```json
"http": { "applicationUrl": "http://localhost:5194" }
"https": { "applicationUrl": "https://localhost:7136;http://localhost:5194" }
```

**Finding**: HTTP port `5194` is established. The Angular development environment API base URL should use `http://localhost:5194/api/v1`.

### .gitignore Frontend Coverage

The root `.gitignore` is a Visual Studio template that includes `node_modules/` globally. This covers `frontend/node_modules/` as-is.

**Finding**: The `.gitignore` does **not** include Angular-specific patterns `.angular/` (CLI build cache) or `frontend/dist/` (build output). The Angular CLI generates its own `.gitignore` inside `frontend/` when scaffolded with `--skip-git`. The implementation must verify this covers `.angular/` and `dist/`; if not, the root `.gitignore` must be updated.

### Progress File State

| File | Status As Of |
| --- | --- |
| `current-state.md` | 2026-05-25: Sprint 1 complete; no frontend scaffold; Issue #4 is next recommended action |
| `decisions-log.md` | 2026-05-25: Sprint 1 decisions logged; no frontend conventions established yet |
| `open-risks.md` | 2026-05-25: Sprint 1 disposition noted; five original risks remain open |
| `completed-issues.md` | Issues #2 and #3 complete; Issue #4 not listed |

---

## 5. Node / npm / Angular CLI Readiness

### Detected Versions

| Tool | Version |
| --- | --- |
| Node.js | **v24.15.0** |
| npm | **11.12.1** |
| npx | **11.12.1** |

### Angular CLI

Angular CLI is **not globally installed**. When invoked via `npx @angular/cli`, npm downloads and runs version `21.2.12`:

```
Angular CLI       : 21.2.12
Node.js           : 24.15.0
Package Manager   : npm 11.12.1
Operating System  : win32 x64
```

**Finding**: Angular CLI 21.2.12 is available via npx. Node 24.15.0 and npm 11.12.1 are compatible with Angular 21. No global CLI installation is required or expected.

### npm Version Notice

npx output reported: `npm notice: New minor version of npm available! 11.12.1 -> 11.15.0`. This is informational only and is not a blocker.

### Implementation Readiness

**READY.** Node, npm, npx, and Angular CLI are all confirmed functional. No blockers exist.

### Angular CLI Version Implications

Angular CLI 21.2.12 generates:

- Standalone components by default (Angular 17+ default; no `NgModule` generated)
- `src/app/app.component.ts` with standalone decorator
- `src/app/app.routes.ts` for route definitions
- `src/app/app.config.ts` with `provideRouter(routes)` and `provideHttpClient()`
- esbuild builder by default (fast builds)
- SSR must be explicitly disabled with `--no-ssr`

The `--routing` flag remains supported and ensures routing configuration is included explicitly.

### Test Runner Note

Angular 19+ deprecated Karma. Angular 21 may use the Angular-native test runner, Vitest, or a similar browser-native approach rather than Karma + ChromeHeadless. The implementation must inspect what test runner is generated after `ng new` and document the exact `npm test` behavior. If a browser dependency (ChromeHeadless) is required, its availability in the environment must be confirmed before claiming tests pass.

---

## 6. Scaffold Decisions Confirmed

| Decision | Confirmed Value |
| --- | --- |
| Frontend framework | Angular only (ADR-003; do not introduce React, Next.js, Vue, Svelte, or any other framework) |
| Angular root directory | `frontend/` |
| Package manager | **npm** (no existing repo package manager convention; npm matches Node toolchain) |
| Routing | **Enabled** via `--routing` flag |
| Style format | **SCSS** via `--style=scss` |
| Standalone components | **Yes** (Angular 21 default) |
| Server-side rendering | **Disabled** via `--no-ssr` |
| UI component library | **None in Issue #4** (Angular Material, Tailwind, Bootstrap deferred) |
| NgRx or state management | **None in Issue #4** |
| Auth libraries / JWT helpers | **None in Issue #4** |
| Placeholder pages only | Yes — no business workflow implementation |
| Backend projects touched | **None** — src/, tests/, KnowledgeOpsAI.sln, global.json, Directory.Build.props must not be modified |
| Docker/CI implementation | **None in Issue #4** |
| E2E tests | **None in Issue #4** |

---

## 7. Planned Frontend Structure

```text
frontend/
  src/
    app/
      core/
        services/
          api-client.service.ts     (base HTTP client service)
        interceptors/
          api.interceptor.ts        (pass-through; no JWT)
        guards/
          auth.guard.ts             (pass-through; no real auth)
      shared/
        components/
          loading/
            loading.component.ts
            loading.component.html
            loading.component.scss
          error/
            error.component.ts
            error.component.html
            error.component.scss
      features/
        auth/
          login/
            login.component.ts      (placeholder page)
            login.component.html
            login.component.scss
        documents/
          documents/
            documents.component.ts  (placeholder page)
            documents.component.html
            documents.component.scss
        chat/
          chat/
            chat.component.ts       (placeholder page)
            chat.component.html
            chat.component.scss
        dashboard/
          dashboard/
            dashboard.component.ts  (placeholder page)
            dashboard.component.html
            dashboard.component.scss
        admin/
          admin/
            admin.component.ts      (placeholder page)
            admin.component.html
            admin.component.scss
      app.component.ts              (layout shell with nav + router-outlet)
      app.component.html
      app.component.scss
      app.routes.ts                 (route definitions)
      app.config.ts                 (provideRouter, provideHttpClient, withInterceptors)
    environments/
      environment.ts                (production env, apiBaseUrl: '/api/v1')
      environment.development.ts    (development env, apiBaseUrl: 'http://localhost:5194/api/v1')
    index.html
    main.ts
    styles.scss
  angular.json
  package.json
  package-lock.json
  tsconfig.json
  tsconfig.app.json
  tsconfig.spec.json
  .gitignore                        (verify Angular CLI generates this; covers .angular/, dist/)
```

**Notes on Angular 21 generation:**
- The CLI generates `src/environments/` files only when `ng generate environments` is run or when manually created. The implementation must run this command or create files manually.
- Placeholder components should use `@Component({ standalone: true, ... })`.
- The `app.component.html` provides the layout shell: a simple nav list linking to all placeholder routes with `<router-outlet>` below.

---

## 8. Routing And Placeholder Page Plan

### Route Table

| Route | Component | Intent |
| --- | --- | --- |
| `/login` | `LoginComponent` | Placeholder for Sprint 6 authentication |
| `/documents` | `DocumentsComponent` | Placeholder for Sprint 10–11 document management |
| `/chat` | `ChatComponent` | Placeholder for Sprint 20 RAG chat UI |
| `/dashboard` | `DashboardComponent` | Placeholder for Sprint 23 metrics dashboard |
| `/admin` | `AdminComponent` | Placeholder for Sprint 9 administration |
| `''` (default) | redirect | Redirect to `/login` |
| `**` (wildcard) | redirect | Redirect to `/login` |

### Default Route Decision: `/login`

**Decision**: Default redirect to `/login`.

**Justification**: The canonical MVP workflow starts with authentication (Sprint 6). Establishing `/login` as the entry point from Sprint 2 ensures the routing intent is correct from the start and avoids a route restructure when auth is implemented. During the placeholder phase, `/login` is a simple placeholder page that does not implement authentication. All placeholder routes remain publicly accessible in the scaffold; route guarding will be activated in Sprint 6.

### Route Implementation (Angular 21 standalone)

```typescript
// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'documents',
    loadComponent: () =>
      import('./features/documents/documents/documents.component')
        .then(m => m.DocumentsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'chat',
    loadComponent: () =>
      import('./features/chat/chat/chat.component').then(m => m.ChatComponent),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard/dashboard.component')
        .then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./features/admin/admin/admin.component').then(m => m.AdminComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: 'login' }
];
```

---

## 9. Environment And API Configuration Plan

### Environment Files

Angular CLI 21 may not generate environment files by default. The implementation must run:

```bash
cd frontend
npx ng generate environments
```

Or manually create:

```
src/environments/environment.ts
src/environments/environment.development.ts
```

### Environment File Content

```typescript
// src/environments/environment.ts (production)
export const environment = {
  production: true,
  apiBaseUrl: '/api/v1'
};
```

```typescript
// src/environments/environment.development.ts (development)
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5194/api/v1'
};
```

**Notes:**
- Port `5194` is established from `src/KnowledgeOps.Api/Properties/launchSettings.json`.
- The production `apiBaseUrl` uses a relative path (`/api/v1`) to support proxied or co-hosted deployments without hardcoding a host.
- HTTPS (port 7136) is available but development defaults to HTTP for local simplicity. HTTPS can be enabled when the Docker/SSL environment is established in Sprint 3.
- The `fileReplacements` configuration in `angular.json` must be verified to swap `environment.ts` with `environment.development.ts` during `ng serve`/`ng test`.
- Do not commit production API keys, provider URLs, tokens, or real credentials.

---

## 10. HTTP Client, Interceptor, And Guard Placeholder Plan

### HTTP Client Service

```typescript
// src/app/core/services/api-client.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiClientService {
  protected readonly http = inject(HttpClient);
  protected readonly baseUrl = environment.apiBaseUrl;
}
```

**Rules for Issue #4:**
- No feature workflow methods (no login, upload, chat, dashboard, feedback, or admin calls).
- No backend assumptions beyond `apiBaseUrl`.
- Child services in future sprints may extend or inject this service.
- `HttpClient` must be provided via `provideHttpClient()` in `app.config.ts`.

### Interceptor Placeholder

Angular 21 uses function-based interceptors (`HttpInterceptorFn`).

```typescript
// src/app/core/interceptors/api.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';

// Pass-through interceptor.
// Backend authorization remains the source of truth.
// Authentication header injection will be added in Sprint 6.
export const apiInterceptor: HttpInterceptorFn = (req, next) => next(req);
```

**Forbidden in Issue #4:**
- No JWT persistence or reading from `localStorage`.
- No `Authorization` header injection.
- No token refresh logic.
- No session state management.

### Guard Placeholder

Angular 21 uses function-based guards (`CanActivateFn`).

```typescript
// src/app/core/guards/auth.guard.ts
import { CanActivateFn } from '@angular/router';

// Pass-through guard — all routes accessible during placeholder phase.
// Frontend route guards are UX guidance only.
// Backend authorization is the source of truth for all protected behavior.
// Real auth enforcement will be added in Sprint 6.
export const authGuard: CanActivateFn = () => true;
```

**Forbidden in Issue #4:**
- No real role checks.
- No real auth state inspection.
- No route blocking.
- No redirection to login based on authentication state.

### Wiring in app.config.ts

```typescript
// src/app/app.config.ts
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { apiInterceptor } from './core/interceptors/api.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([apiInterceptor]))
  ]
};
```

---

## 11. Loading/Error Convention Plan

### Recommended Approach

Create two minimal shared standalone components to establish a consistent loading/error presentation convention. These are presentational components only — no data fetching, no API calls.

```typescript
// src/app/shared/components/loading/loading.component.ts
import { Component } from '@angular/core';

@Component({
  standalone: true,
  selector: 'app-loading',
  template: `<p>Loading...</p>`,
  styles: [`p { color: #666; }`]
})
export class LoadingComponent {}
```

```typescript
// src/app/shared/components/error/error.component.ts
import { Component, Input } from '@angular/core';

@Component({
  standalone: true,
  selector: 'app-error',
  template: `<p>{{ message }}</p>`,
  styles: [`p { color: red; }`]
})
export class ErrorComponent {
  @Input() message = 'An error occurred.';
}
```

**Rules for Issue #4:**
- No design system or component library.
- No complex CSS or brand styles — visual polish is deferred.
- Minimal markup is sufficient to establish the convention; future sprints will style and expand these components.
- These components are to be used by placeholder pages as needed and wired into feature components in later sprints.

---

## 12. Testing Plan

### Expected Test Commands After Implementation

```bash
cd frontend
npm run build         # Verify the application compiles without errors
npm test -- --watch=false  # Run unit tests once in non-watch mode
```

Or equivalently:

```bash
cd frontend
npx ng build
npx ng test --watch=false
```

### Test Runner Note

**Angular 21 test runner must be confirmed after scaffolding.** Angular 19+ deprecated Karma; Angular 21 may default to a non-Karma test runner (likely the Angular-native test runner or a similar browser-based runner without ChromeHeadless dependency). The implementation must:

1. Inspect the generated `angular.json` to identify the configured test runner.
2. Confirm the `npm test` / `ng test` command and required browser/environment.
3. If ChromeHeadless is required, verify it is available in the environment.
4. Run `ng test --watch=false` and report exactly what passes or fails.
5. Do not claim tests passed unless the command completed successfully.

### Placeholder Component Tests

- The Angular CLI generates a minimal `app.component.spec.ts` with a basic `AppComponent` creation test.
- Additional generated specs for placeholder components should be minimal (one creation test per component is sufficient).
- No business logic to test in Issue #4 — tests confirm components initialize without error.
- Failed specs due to missing browser dependencies must be reported clearly and not treated as passing.

### E2E Tests

None in Issue #4. E2E testing is deferred to Sprint 26.

---

## 13. Package Policy

### Allowed Packages

| Package / Category | Source | Justification |
| --- | --- | --- |
| `@angular/core`, `@angular/common`, `@angular/router`, `@angular/forms`, `@angular/platform-browser`, `@angular/compiler` | CLI template | Core Angular framework |
| `rxjs` | CLI template | Required by Angular |
| `typescript` | CLI template | Angular TypeScript dependency |
| `zone.js` | CLI template | Angular change detection |
| `@angular/cli` | devDependency from CLI | Build tooling |
| `@angular/build` or `@angular-devkit/build-angular` | CLI template | Build system |
| `karma`, `karma-*`, `jasmine-*` | CLI template (if generated) | Test runner (if Karma still default) |
| Angular test runner packages | CLI template (if new runner) | Test runner (if Karma deprecated) |

All packages above are CLI-generated template defaults. They are allowed.

### Forbidden Packages In Issue #4

| Package / Category | Reason |
| --- | --- |
| `react`, `react-dom`, `next`, `vue`, `svelte`, `solid-js` | Forbidden; Angular is the only MVP frontend framework (ADR-003) |
| `@angular/material`, `@angular/cdk` | Deferred; no UI component library in Issue #4 |
| `tailwindcss`, `@tailwindcss/*` | Deferred; no CSS framework in Issue #4 |
| `bootstrap`, `ng-bootstrap` | Deferred |
| `@ngrx/*`, `akita`, `ngxs` | Deferred; no state management library in Issue #4 |
| `jwt-decode`, `angular-jwt`, `@auth0/*`, `@okta/*` | Deferred to Sprint 6 auth implementation |
| `chart.js`, `d3`, `ngx-charts`, `highcharts` | Deferred to Sprint 23 dashboard implementation |
| `openai`, `@azure/openai`, `ai` | Never in frontend; provider SDKs remain in backend Infrastructure |
| Any vector database or retrieval SDK | Never in frontend |

---

## 14. Out Of Scope

The following must not be implemented in Issue #4:

- Real login behavior or authentication state (Sprint 6)
- JWT storage, localStorage token handling, or session persistence (Sprint 6)
- User session management or authenticated user context (Sprint 6)
- Document upload workflow (Sprint 11)
- Document processing status display with live data (Sprint 12)
- Chat question/answer workflow (Sprint 20)
- Citation rendering connected to backend data (Sprint 19–20)
- Insufficient-context response display connected to backend (Sprint 18–20)
- Answer feedback controls (Sprint 22)
- Dashboard metrics display (Sprint 23)
- Admin user management UI (Sprint 9)
- Any backend API implementation
- Real API calls from the Angular application (no backend assumed running)
- Angular Material, Tailwind, Bootstrap, or any UI library
- NgRx or state management
- Production UI polish or brand styling
- React or any non-Angular frontend framework
- Docker Compose or Dockerfile for frontend (Sprint 3)
- GitHub Actions CI for frontend (Sprint 27)
- E2E tests (Sprint 26)
- Secrets, real API keys, production URLs, tokens, or committed credentials
- Server-side rendering (not in MVP scope)
- Any backend source files (src/, tests/, KnowledgeOpsAI.sln, global.json, Directory.Build.props)

---

## 15. Implementation Plan For Next Prompt

The following is an executable scaffold plan. Commands are not executed during this audit. All commands run from the repository root unless noted.

### Step 1: Scaffold Angular Application

```bash
npx @angular/cli@21.2.12 new frontend --routing --style=scss --no-ssr --skip-git
```

This creates `frontend/` with the Angular workspace, installs npm dependencies, and generates:
- `frontend/angular.json`
- `frontend/package.json` + `frontend/package-lock.json`
- `frontend/src/app/app.component.*`
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/app.config.ts`
- `frontend/tsconfig.json`, `tsconfig.app.json`, `tsconfig.spec.json`

### Step 2: Verify .gitignore Coverage

Inspect `frontend/.gitignore` generated by the CLI. Confirm it includes `.angular` and `dist`. If the Angular CLI does not generate `frontend/.gitignore` (depends on CLI version), add to the **root** `.gitignore`:

```
# Angular frontend
frontend/.angular/
frontend/dist/
```

Note: `node_modules/` is already globally excluded by the root `.gitignore`.

### Step 3: Create Environment Files

```bash
cd frontend
npx ng generate environments
```

Then add `apiBaseUrl` to each:
- `src/environments/environment.ts`: `apiBaseUrl: '/api/v1'`
- `src/environments/environment.development.ts`: `apiBaseUrl: 'http://localhost:5194/api/v1'`

Verify `angular.json` contains a `fileReplacements` entry swapping `environment.ts` for `environment.development.ts` during `development` configuration.

### Step 4: Delete Template Sample Content

Remove or empty:
- Any template-generated weather forecast sample or similar demo component.
- Template `app.component.html` content (replace with layout shell).

### Step 5: Create Application Layout Shell

Update `src/app/app.component.html` with a minimal nav and router outlet:

```html
<nav>
  <a routerLink="/chat">Chat</a>
  <a routerLink="/documents">Documents</a>
  <a routerLink="/dashboard">Dashboard</a>
  <a routerLink="/admin">Admin</a>
  <a routerLink="/login">Login</a>
</nav>
<router-outlet />
```

Add `RouterModule` or `RouterLink`, `RouterOutlet` to `app.component.ts` imports array.

### Step 6: Create Core Services, Interceptor, Guard

```bash
cd frontend
npx ng generate service core/services/api-client
npx ng generate interceptor core/interceptors/api --functional
npx ng generate guard core/guards/auth --functional
```

Apply pass-through implementations as specified in Section 10.

Wire interceptor into `app.config.ts` via `withInterceptors([apiInterceptor])`.

### Step 7: Create Placeholder Pages

```bash
cd frontend
npx ng generate component features/auth/login --standalone
npx ng generate component features/documents/documents --standalone
npx ng generate component features/chat/chat --standalone
npx ng generate component features/dashboard/dashboard --standalone
npx ng generate component features/admin/admin --standalone
```

Each placeholder page should display only its name and "placeholder" note in the template.

### Step 8: Create Shared Loading And Error Components

```bash
cd frontend
npx ng generate component shared/components/loading --standalone
npx ng generate component shared/components/error --standalone
```

Apply minimal implementations as specified in Section 11.

### Step 9: Update Routes

Replace the generated `src/app/app.routes.ts` with the route table from Section 8, using lazy-loaded `loadComponent()` for each placeholder.

### Step 10: Verify Environment Wiring

In `app.config.ts`, confirm `provideHttpClient(withInterceptors([apiInterceptor]))` is present.

### Step 11: Run Validation

See Section 16.

---

## 16. Validation Plan For Implementation

Run these commands after implementation from the `frontend/` directory:

```bash
# 1. Confirm Node/npm environment
node --version
npm --version

# 2. Confirm installed Angular CLI version
npx ng version

# 3. Build the Angular application
npx ng build

# 4. Run unit tests (non-watch mode)
npx ng test --watch=false

# 5. Confirm no unexpected packages were added
# Inspect package.json and flag any non-CLI-generated packages
cat package.json
```

### Expected Validation Outcomes

| Command | Expected Result |
| --- | --- |
| `ng build` | Build completes without errors; output in `dist/frontend/` |
| `ng test --watch=false` | All generated specs pass; test runner reported clearly |
| `package.json` inspection | Only CLI-generated packages; no forbidden libraries |
| Route inspection | Default redirects to `/login`; all five feature routes accessible |

### Known Limitations To Report

- If Angular 21 requires ChromeHeadless or a browser for `ng test` and no browser is available, report the exact error. Do not claim tests passed.
- If `ng generate environments` does not produce the expected files, document the manual alternative.
- If the `--routing` flag behavior differs from documented (e.g., no `app.routes.ts` generated), document the actual generated structure.
- If the test runner is not Karma (likely for Angular 21), document the actual runner and configuration.

### Backend Validation

No backend validation is required for Issue #4. The backend scaffold from Issue #3 should remain unchanged. Optionally confirm with `dotnet build KnowledgeOpsAI.sln` that the backend still builds cleanly after frontend files are added to the repository.

---

## 17. Risks And Blockers

### Blockers

| Blocker | Status |
| --- | --- |
| Node.js unavailable | **Not a blocker.** v24.15.0 confirmed. |
| npm unavailable | **Not a blocker.** 11.12.1 confirmed. |
| Angular CLI unavailable | **Not a blocker.** 21.2.12 available via npx. |
| Frontend conflicts | **Not a blocker.** Repository is a clean frontend slate. |

**No blockers found.** Implementation can proceed.

### Risks

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Angular 21 test runner differs from Karma | Low | Inspect generated `angular.json` after scaffold; run `ng test --watch=false` and report actual runner; do not claim tests passed without running them. |
| `ng new` may not include `--routing` in the same form as Angular 16 | Low | Verify `app.routes.ts` and `provideRouter()` are generated; add them manually if missing. |
| Environment files not auto-generated by `ng new` | Low | Run `ng generate environments` as a second step after scaffold; confirm `fileReplacements` in `angular.json`. |
| Angular CLI generates `.gitignore` inside `frontend/` inconsistently | Low | Inspect after scaffold; update root `.gitignore` with `.angular/` and `frontend/dist/` if not covered. |
| API port `5194` may change in Sprint 3 Docker setup | Very Low | Record `5194` as the current development default in the environment file and note it may be revised during Sprint 3 or when Docker Compose is established. |
| npm notices about version updates (11.12.1 → 11.15.0) | Informational | Not a blocker; update npm if desired but not required for Issue #4. |

### Progress File Update Recommendation

The following updates are recommended for the **implementation prompt** (not this audit):

- `current-state.md`: Update to reflect Sprint 2 / Issue #4 as active implementation.
- `decisions-log.md`: Add entry for npm as the selected package manager, `frontend/` as Angular root, port `5194` as established API base, and `--no-ssr` / SCSS / standalone as accepted Angular scaffold options.
- `open-risks.md`: No new blockers or material risks discovered requiring an immediate entry. Existing risks remain unchanged.
- `completed-issues.md`: Update only after Issue #4 is verified complete.

**open-risks.md is not updated during this audit** because no new blocker or materially new risk was discovered.

---

## 18. Readiness Recommendation

**READY FOR IMPLEMENTATION**

- Node 24.15.0, npm 11.12.1, and Angular CLI 21.2.12 are confirmed available.
- The repository is a clean frontend slate — no conflicts, no prior package manager convention.
- The backend scaffold from Issue #3 is present and verified; it must not be touched.
- The API port convention is established at `5194`.
- All canonical decisions are settled.
- No blockers exist.

---

## 19. Recommended Next Step

Generate the Issue #4 implementation prompt using the following canonical sources:

1. This audit report as the pre-implementation checklist.
2. `docs/agents/10-issue-execution-template.md` as the implementation template.
3. `docs/agents/13-prompt-classifier.md` as the classification entry.
4. `docs/22-implementation-guardrails.md` and `docs/21-implementation-roadmap.md` as scope authorities.
5. The scaffold plan in Section 15, routing in Section 8, environment plan in Section 9, and validation plan in Section 16 as the implementation blueprint.

The implementation prompt should:
- Classify the task as Level 3 per the harness.
- Execute the scaffold steps in Section 15 in order.
- Remove template-generated sample content.
- Add the layout shell, placeholder pages, core services, interceptor, guard, and shared components.
- Run validation commands in Section 16.
- Inspect and document the test runner generated by Angular 21.
- Update `current-state.md`, `decisions-log.md`, and `completed-issues.md` after verified implementation.
- Not mark Issue #4 complete until `ng build` and `ng test --watch=false` pass.
- Not touch the backend scaffold (src/, tests/, KnowledgeOpsAI.sln, global.json, Directory.Build.props).
