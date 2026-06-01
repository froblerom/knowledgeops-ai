import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  DashboardChatResponse,
  DashboardDateParams,
  DashboardDocumentsResponse,
  DashboardFeedbackResponse,
  DashboardOverviewResponse
} from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly dashboardBaseUrl = `${environment.apiBaseUrl}/dashboard`;

  getOverview(params?: DashboardDateParams): Observable<DashboardOverviewResponse> {
    return this.http.get<DashboardOverviewResponse>(
      `${this.dashboardBaseUrl}/overview`,
      { params: this.buildParams(params) }
    );
  }

  getDocuments(params?: DashboardDateParams): Observable<DashboardDocumentsResponse> {
    return this.http.get<DashboardDocumentsResponse>(
      `${this.dashboardBaseUrl}/documents`,
      { params: this.buildParams(params) }
    );
  }

  getChat(params?: DashboardDateParams): Observable<DashboardChatResponse> {
    return this.http.get<DashboardChatResponse>(
      `${this.dashboardBaseUrl}/chat`,
      { params: this.buildParams(params) }
    );
  }

  getFeedback(params?: DashboardDateParams): Observable<DashboardFeedbackResponse> {
    return this.http.get<DashboardFeedbackResponse>(
      `${this.dashboardBaseUrl}/feedback`,
      { params: this.buildParams(params) }
    );
  }

  private buildParams(params?: DashboardDateParams): HttpParams {
    let httpParams = new HttpParams();
    if (params?.from) {
      httpParams = httpParams.set('from', params.from);
    }
    if (params?.to) {
      httpParams = httpParams.set('to', params.to);
    }
    return httpParams;
  }
}
