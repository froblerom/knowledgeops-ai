import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type UserStatus = 'Pending' | 'Active' | 'Disabled';
export type UserRole = 'Agent' | 'Supervisor' | 'KnowledgeAdmin' | 'Manager' | 'Admin';

export interface ManagedUser {
  userId: string;
  displayName: string;
  email: string;
  organizationId: string;
  status: UserStatus;
  roles: UserRole[];
  createdAt: string;
  updatedAt: string;
  lastLoginAt: string | null;
}

export interface CreateManagedUserRequest {
  displayName: string;
  email: string;
  status: UserStatus;
  roles: UserRole[];
  initialPassword: string;
}

export interface UpdateManagedUserRequest {
  displayName: string;
  email: string;
  status: UserStatus;
}

@Injectable({ providedIn: 'root' })
export class UserAdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/users`;

  list(): Observable<ManagedUser[]> {
    return this.http.get<ManagedUser[]>(this.baseUrl);
  }

  get(userId: string): Observable<ManagedUser> {
    return this.http.get<ManagedUser>(`${this.baseUrl}/${userId}`);
  }

  create(request: CreateManagedUserRequest): Observable<ManagedUser> {
    return this.http.post<ManagedUser>(this.baseUrl, request);
  }

  update(userId: string, request: UpdateManagedUserRequest): Observable<ManagedUser> {
    return this.http.put<ManagedUser>(`${this.baseUrl}/${userId}`, request);
  }

  addRole(userId: string, roleName: UserRole): Observable<ManagedUser> {
    return this.http.post<ManagedUser>(`${this.baseUrl}/${userId}/roles`, { roleName });
  }

  removeRole(userId: string, roleName: UserRole): Observable<ManagedUser> {
    return this.http.delete<ManagedUser>(`${this.baseUrl}/${userId}/roles/${roleName}`);
  }
}
