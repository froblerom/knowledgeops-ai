import { Injectable } from '@angular/core';

const ACCESS_TOKEN_KEY = 'ko_access_token';

export interface StoredSession {
  accessToken: string;
  expiresAt: string;
  userId: string;
  email: string;
  displayName: string;
  organizationId: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getSession(): StoredSession | null {
    const raw = localStorage.getItem(ACCESS_TOKEN_KEY + '_session');
    if (!raw) return null;
    try {
      return JSON.parse(raw) as StoredSession;
    } catch {
      return null;
    }
  }

  saveSession(session: StoredSession): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, session.accessToken);
    localStorage.setItem(ACCESS_TOKEN_KEY + '_session', JSON.stringify(session));
  }

  clearSession(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(ACCESS_TOKEN_KEY + '_session');
  }

  isAuthenticated(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;
    const session = this.getSession();
    if (!session) return false;
    return new Date(session.expiresAt) > new Date();
  }
}
