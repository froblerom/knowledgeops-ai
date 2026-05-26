import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ApiErrorService } from './api-error.service';

describe('ApiErrorService', () => {
  let service: ApiErrorService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ApiErrorService);
  });

  it('prefers the response header Error ID over the body fallback', () => {
    const error = service.fromHttpError(new HttpErrorResponse({
      status: 500,
      headers: new HttpHeaders({ 'X-Correlation-ID': 'header-id' }),
      error: { error: { correlationId: 'body-id', message: 'sensitive' } }
    }));

    expect(error.errorId).toBe('header-id');
    expect(error.message).toBe('Something went wrong. Please try again.');
  });

  it('reads the body Error ID when no response header is supplied', () => {
    const error = service.fromHttpError(new HttpErrorResponse({
      status: 503,
      error: { error: { correlationId: 'body-id' } }
    }));

    expect(error.errorId).toBe('body-id');
    expect(error.message).toBe('The service is temporarily unavailable. Please try again later.');
  });

  it.each([
    [400, 'Please review the supplied values and try again.'],
    [401, 'Your session has expired. Please sign in again.'],
    [403, 'You do not have access to perform this action.'],
    [404, 'The requested item could not be found.'],
    [409, 'The requested change conflicts with the current state.']
  ])('maps status %i to a safe message', (status, message) => {
    const error = service.fromHttpError(new HttpErrorResponse({
      status,
      error: { error: { message: 'server detail must not be displayed' } }
    }));

    expect(error.message).toBe(message);
    expect(error.message).not.toContain('server detail');
  });
});
