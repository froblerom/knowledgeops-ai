import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { adminVisibilityGuard } from './admin-visibility.guard';
import { AuthSessionService } from '../services/auth-session.service';

@Component({ template: '<p>admin</p>' })
class AdminStub {}
@Component({ template: '<p>dashboard</p>' })
class DashboardStub {}

describe('adminVisibilityGuard', () => {
  let session: AuthSessionService;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([
        { path: 'dashboard', component: DashboardStub },
        { path: 'admin', component: AdminStub, canActivate: [adminVisibilityGuard] }
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

  it('allows Admin navigation as UX guidance', async () => {
    save(['Admin']);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/admin');
    expect(router.url).toBe('/admin');
  });

  it('redirects non-Admin navigation while backend remains authoritative', async () => {
    save(['Agent']);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/admin');
    expect(router.url).toBe('/dashboard');
  });
});
