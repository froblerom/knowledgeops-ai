import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type DocumentProcessingStatus = 'Uploaded' | 'Processing' | 'Processed' | 'Failed';

export interface ManagedDocument {
  documentId: string;
  fileName: string;
  title: string;
  contentType: string;
  fileSizeBytes: number;
  processingStatus: DocumentProcessingStatus;
  failureReason: string | null;
  isRetrievalEnabled: boolean;
  uploadedByUserId: string;
  uploadedAt: string;
  processingStartedAt: string | null;
  processedAt: string | null;
}

export interface DocumentProcessingStatusResponse {
  documentId: string;
  processingStatus: DocumentProcessingStatus;
  failureReason: string | null;
  isRetrievalEnabled: boolean;
  uploadedAt: string;
  processingStartedAt: string | null;
  processedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/documents`;

  list(): Observable<ManagedDocument[]> {
    return this.http.get<ManagedDocument[]>(this.baseUrl);
  }

  get(documentId: string): Observable<ManagedDocument> {
    return this.http.get<ManagedDocument>(`${this.baseUrl}/${documentId}`);
  }

  getProcessingStatus(documentId: string): Observable<DocumentProcessingStatusResponse> {
    return this.http.get<DocumentProcessingStatusResponse>(
      `${this.baseUrl}/${documentId}/processing-status`
    );
  }

  disableRetrieval(documentId: string): Observable<ManagedDocument> {
    return this.http.post<ManagedDocument>(
      `${this.baseUrl}/${documentId}/disable`,
      {}
    );
  }

  upload(title: string, file: File): Observable<ManagedDocument> {
    const formData = new FormData();
    formData.append('title', title);
    formData.append('file', file);
    // Do not set Content-Type manually; browser sets multipart/form-data with boundary.
    return this.http.post<ManagedDocument>(this.baseUrl, formData);
  }
}
