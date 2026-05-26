import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  provideHttpClient,
  withInterceptors
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { apiInterceptor } from './api.interceptor';
import { AuthSessionService } from '../services/auth-session.service';
import { provideRouter, Router } from '@angular/router';
import { ApiRequestError } from '../services/api-error.service';

const API_URL = 'http://localhost:5194/api/v1/some-endpoint';
const EXTERNAL_URL = 'https://external.example.com/data';

describe('apiInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let session: AuthSessionService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiInterceptor])),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('attaches Authorization header to api requests when token present', () => {
    session.saveSession({
      accessToken: 'my-token',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
      userId: '1',
      email: 'a@example.com',
      displayName: 'A',
      organizationId: '2',
      roles: []
    });

    http.get(API_URL).subscribe();

    const req = httpMock.expectOne(API_URL);
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-token');
    req.flush({});
  });

  it('does not attach Authorization header when no token', () => {
    http.get(API_URL).subscribe();

    const req = httpMock.expectOne(API_URL);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('does not attach Authorization header to non-api requests', () => {
    session.saveSession({
      accessToken: 'my-token',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
      userId: '1',
      email: 'a@example.com',
      displayName: 'A',
      organizationId: '2',
      roles: []
    });

    http.get(EXTERNAL_URL).subscribe();

    const req = httpMock.expectOne(EXTERNAL_URL);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('clears the session and redirects to login on an API 401', () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');
    session.saveSession({
      accessToken: 'my-token',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
      userId: '1',
      email: 'a@example.com',
      displayName: 'A',
      organizationId: '2',
      roles: []
    });

    let receivedError: ApiRequestError | undefined;
    http.get(API_URL).subscribe({ error: error => receivedError = error });
    httpMock.expectOne(API_URL).flush(
      { error: { correlationId: 'body-id', message: 'detail' } },
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(session.getAccessToken()).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
    expect(receivedError?.message).toBe('Your session has expired. Please sign in again.');
  });

  it('maps an API 403 without clearing the session', () => {
    session.saveSession({
      accessToken: 'my-token',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
      userId: '1',
      email: 'a@example.com',
      displayName: 'A',
      organizationId: '2',
      roles: []
    });

    let receivedError: ApiRequestError | undefined;
    http.get(API_URL).subscribe({ error: error => receivedError = error });
    httpMock.expectOne(API_URL).flush(
      { error: { message: 'unsafe detail' } },
      { status: 403, statusText: 'Forbidden' }
    );

    expect(session.getAccessToken()).toBe('my-token');
    expect(receivedError?.message).toBe('You do not have access to perform this action.');
  });
});
