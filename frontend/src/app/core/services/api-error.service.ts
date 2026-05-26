import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

interface ApiErrorEnvelope {
  error?: {
    correlationId?: string;
  };
}

export class ApiRequestError {
  constructor(
    readonly status: number,
    readonly message: string,
    readonly errorId: string | null
  ) {}
}

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  fromHttpError(response: HttpErrorResponse): ApiRequestError {
    return new ApiRequestError(
      response.status,
      this.messageForStatus(response.status),
      this.errorIdFrom(response)
    );
  }

  private errorIdFrom(response: HttpErrorResponse): string | null {
    const headerId = response.headers.get('X-Correlation-ID');
    if (headerId) return headerId;

    const body = response.error as ApiErrorEnvelope | null;
    return body?.error?.correlationId ?? null;
  }

  private messageForStatus(status: number): string {
    switch (status) {
      case 401:
        return 'Your session has expired. Please sign in again.';
      case 403:
        return 'You do not have access to perform this action.';
      case 404:
        return 'The requested item could not be found.';
      case 500:
        return 'Something went wrong. Please try again.';
      case 503:
        return 'The service is temporarily unavailable. Please try again later.';
      default:
        return 'An unexpected error occurred.';
    }
  }
}
