import { TestBed } from '@angular/core/testing';
import { AuthSessionService, StoredSession } from './auth-session.service';

const makeSession = (expiresAt: Date = new Date(Date.now() + 3600_000)): StoredSession => ({
  accessToken: 'tok',
  expiresAt: expiresAt.toISOString(),
  userId: '00000000-0000-0000-0000-000000000001',
  email: 'a@example.com',
  displayName: 'A',
  organizationId: '00000000-0000-0000-0000-000000000002',
  roles: ['Agent']
});

describe('AuthSessionService', () => {
  let service: AuthSessionService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthSessionService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAccessToken returns null when storage is empty', () => {
    expect(service.getAccessToken()).toBeNull();
  });

  it('saveSession persists token and session', () => {
    service.saveSession(makeSession());
    expect(service.getAccessToken()).toBe('tok');
    expect(service.getSession()?.email).toBe('a@example.com');
  });

  it('clearSession removes token and session', () => {
    service.saveSession(makeSession());
    service.clearSession();
    expect(service.getAccessToken()).toBeNull();
    expect(service.getSession()).toBeNull();
  });

  it('isAuthenticated returns true for non-expired session', () => {
    service.saveSession(makeSession());
    expect(service.isAuthenticated()).toBe(true);
  });

  it('isAuthenticated returns false for expired session', () => {
    service.saveSession(makeSession(new Date(Date.now() - 1000)));
    expect(service.isAuthenticated()).toBe(false);
  });

  it('isAuthenticated returns false when no token', () => {
    expect(service.isAuthenticated()).toBe(false);
  });
});
