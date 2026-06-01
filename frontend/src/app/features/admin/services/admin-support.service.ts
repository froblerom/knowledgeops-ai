import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuditLogEntry, AuditLogFilters, ProcessingFailure } from '../models/admin-support.models';

@Injectable({ providedIn: 'root' })
export class AdminSupportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/admin`;

  getProcessingFailures(limit?: number): Observable<ProcessingFailure[]> {
    let params = new HttpParams();
    if (limit !== undefined) {
      params = params.set('limit', String(limit));
    }

    return this.http.get<ProcessingFailure[]>(`${this.baseUrl}/processing-failures`, { params });
  }

  getAuditLog(filters: AuditLogFilters = {}): Observable<AuditLogEntry[]> {
    let params = new HttpParams();
    if (filters.from) params = params.set('from', filters.from);
    if (filters.to) params = params.set('to', filters.to);
    if (filters.eventType) params = params.set('eventType', filters.eventType);

    return this.http.get<AuditLogEntry[]>(`${this.baseUrl}/audit-log`, { params });
  }
}
