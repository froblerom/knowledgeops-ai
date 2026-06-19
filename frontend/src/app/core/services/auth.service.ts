import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthSessionService, StoredSession } from './auth-session.service';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface CurrentUserResponse {
  userId: string;
  email: string;
  displayName: string;
  organizationId: string;
  roles: string[];
  status: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: CurrentUserResponse;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AuthSessionService);
  private readonly baseUrl = environment.apiBaseUrl;

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.baseUrl}/auth/login`, request)
      .pipe(
        tap(response => {
          const stored: StoredSession = {
            accessToken: response.accessToken,
            expiresAt: response.expiresAt,
            userId: response.user.userId,
            email: response.user.email,
            displayName: response.user.displayName,
            organizationId: response.user.organizationId,
            roles: response.user.roles
          };
          this.session.saveSession(stored);
        })
      );
  }

  logout(): Observable<unknown> {
    return this.http
      .post(`${this.baseUrl}/auth/logout`, {})
      .pipe(tap(() => this.session.clearSession()));
  }

  me(): Observable<CurrentUserResponse> {
    return this.http.get<CurrentUserResponse>(`${this.baseUrl}/auth/me`);
  }

  isAuthenticated(): boolean {
    return this.session.isAuthenticated();
  }

  currentUser() {
    return this.session.getSession();
  }
}
