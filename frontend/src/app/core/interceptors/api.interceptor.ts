import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiErrorService } from '../services/api-error.service';
import { AuthSessionService } from '../services/auth-session.service';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiBaseUrl)) {
    return next(req);
  }

  const session = inject(AuthSessionService);
  const errors = inject(ApiErrorService);
  const router = inject(Router);
  const token = session.getAccessToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((response: HttpErrorResponse) => {
      if (response.status === 401) {
        session.clearSession();
        void router.navigate(['/login']);
      }

      return throwError(() => errors.fromHttpError(response));
    })
  );
};
