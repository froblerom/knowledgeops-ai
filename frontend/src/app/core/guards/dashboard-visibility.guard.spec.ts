import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { dashboardVisibilityGuard } from './dashboard-visibility.guard';
import { AuthSessionService } from '../services/auth-session.service';

@Component({ template: '<p>dashboard</p>' })
class DashboardStub {}
@Component({ template: '<p>chat</p>' })
class ChatStub {}

describe('dashboardVisibilityGuard', () => {
  let session: AuthSessionService;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([
        { path: 'chat', component: ChatStub },
        { path: 'dashboard', component: DashboardStub, canActivate: [dashboardVisibilityGuard] }
      ])]
    });
    session = TestBed.inject(AuthSessionService);
    router = TestBed.inject(Router);
  });

  afterEach(() => localStorage.clear());

  function save(roles: string[]): void {
    session.saveSession({
      accessToken: 'token',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
      userId: 'u',
      email: 'test@example.test',
      displayName: 'User',
      organizationId: 'o',
      roles
    });
  }

  it('allows KnowledgeAdmin navigation as UX guidance', async () => {
    save(['KnowledgeAdmin']);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard');
    expect(router.url).toBe('/dashboard');
  });

  it('allows Manager navigation as UX guidance', async () => {
    save(['Manager']);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard');
    expect(router.url).toBe('/dashboard');
  });

  it('allows Admin navigation as UX guidance', async () => {
    save(['Admin']);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard');
    expect(router.url).toBe('/dashboard');
  });

  it('redirects Agent while backend remains authoritative', async () => {
    save(['Agent']);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard');
    expect(router.url).toBe('/chat');
  });

  it('redirects Supervisor while backend remains authoritative', async () => {
    save(['Supervisor']);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard');
    expect(router.url).toBe('/chat');
  });
});
