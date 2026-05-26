import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Component } from '@angular/core';
import { provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthSessionService } from '../services/auth-session.service';

@Component({ standalone: true, template: '<p>protected</p>' })
class ProtectedPage {}

@Component({ standalone: true, template: '<p>login</p>' })
class LoginStub {}

describe('authGuard', () => {
  let session: AuthSessionService;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'login', component: LoginStub },
          { path: 'protected', component: ProtectedPage, canActivate: [authGuard] }
        ])
      ]
    });
    session = TestBed.inject(AuthSessionService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('allows navigation when session is valid', async () => {
    session.saveSession({
      accessToken: 'tok',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
      userId: '1',
      email: 'a@example.com',
      displayName: 'A',
      organizationId: '2',
      roles: []
    });

    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/protected');

    expect(router.url).toBe('/protected');
  });

  it('redirects to /login when not authenticated', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/protected');

    expect(router.url).toBe('/login');
  });
});
