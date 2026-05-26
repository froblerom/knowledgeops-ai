import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { AuthSessionService } from './auth-session.service';

const loginResponse = {
  accessToken: 'test-token',
  expiresAt: new Date(Date.now() + 3600_000).toISOString(),
  user: {
    userId: '00000000-0000-0000-0000-000000000001',
    email: 'agent@example.com',
    displayName: 'Agent',
    organizationId: '00000000-0000-0000-0000-000000000002',
    roles: ['Agent'],
    status: 'Active'
  }
};

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let sessionService: AuthSessionService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    sessionService = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('login saves session on success', () => {
    service.login({ email: 'agent@example.com', password: 'p' }).subscribe();

    const req = httpMock.expectOne(r => r.url.includes('/auth/login'));
    req.flush(loginResponse);

    expect(sessionService.getAccessToken()).toBe('test-token');
    expect(sessionService.getSession()?.email).toBe('agent@example.com');
  });

  it('logout clears session', () => {
    sessionService.saveSession({
      accessToken: 'test-token',
      expiresAt: loginResponse.expiresAt,
      userId: loginResponse.user.userId,
      email: loginResponse.user.email,
      displayName: loginResponse.user.displayName,
      organizationId: loginResponse.user.organizationId,
      roles: loginResponse.user.roles
    });

    service.logout().subscribe();
    httpMock.expectOne(r => r.url.includes('/auth/logout')).flush({});

    expect(sessionService.getAccessToken()).toBeNull();
  });

  it('isAuthenticated returns false when no session', () => {
    expect(service.isAuthenticated()).toBe(false);
  });

  it('isAuthenticated returns true after login', () => {
    service.login({ email: 'agent@example.com', password: 'p' }).subscribe();
    httpMock.expectOne(r => r.url.includes('/auth/login')).flush(loginResponse);

    expect(service.isAuthenticated()).toBe(true);
  });
});
