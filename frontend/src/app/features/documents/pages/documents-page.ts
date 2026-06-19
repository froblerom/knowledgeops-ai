import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { DocumentService, ManagedDocument } from '../../../core/services/document.service';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { RoleVisibilityService } from '../../../core/services/role-visibility.service';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import { ErrorState } from '../../../shared/components/error-state/error-state';

@Component({
  selector: 'app-documents-page',
  imports: [RouterLink, DatePipe, LoadingState, ErrorState],
  templateUrl: './documents-page.html',
  styleUrl: './documents-page.scss'
})
export class DocumentsPage implements OnInit {
  private readonly documentsApi = inject(DocumentService);
  private readonly apiError = inject(ApiErrorService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly roleVisibility = inject(RoleVisibilityService);

  documents: ManagedDocument[] = [];
  loading = true;
  error: ApiRequestError | null = null;

  // Per-row action state: documentId → 'enabling' | 'disabling' | null
  actionInProgress: Record<string, 'enabling' | 'disabling' | null> = {};
  rowErrors: Record<string, ApiRequestError | null> = {};

  ngOnInit(): void {
    this.documentsApi.list().pipe(
      finalize(() => this.cdr.markForCheck())
    ).subscribe({
      next: docs => {
        this.documents = docs;
        this.loading = false;
      },
      error: response => {
        this.error = this.apiError.fromHttpError(response);
        this.loading = false;
      }
    });
  }

  isActionInProgress(documentId: string): boolean {
    return this.actionInProgress[documentId] != null;
  }

  enableRetrieval(doc: ManagedDocument): void {
    if (this.isActionInProgress(doc.documentId)) return;

    this.actionInProgress[doc.documentId] = 'enabling';
    this.rowErrors[doc.documentId] = null;
    this.documentsApi.enableRetrieval(doc.documentId).pipe(
      finalize(() => this.cdr.markForCheck())
    ).subscribe({
      next: updated => {
        const idx = this.documents.findIndex(d => d.documentId === doc.documentId);
        if (idx >= 0) this.documents[idx] = updated;
        this.actionInProgress[doc.documentId] = null;
      },
      error: response => {
        this.rowErrors[doc.documentId] = this.apiError.fromHttpError(response);
        this.actionInProgress[doc.documentId] = null;
      }
    });
  }

  disableRetrieval(doc: ManagedDocument): void {
    if (this.isActionInProgress(doc.documentId)) return;

    this.actionInProgress[doc.documentId] = 'disabling';
    this.rowErrors[doc.documentId] = null;
    this.documentsApi.disableRetrieval(doc.documentId).pipe(
      finalize(() => this.cdr.markForCheck())
    ).subscribe({
      next: updated => {
        const idx = this.documents.findIndex(d => d.documentId === doc.documentId);
        if (idx >= 0) this.documents[idx] = updated;
        this.actionInProgress[doc.documentId] = null;
      },
      error: response => {
        this.rowErrors[doc.documentId] = this.apiError.fromHttpError(response);
        this.actionInProgress[doc.documentId] = null;
      }
    });
  }
}
