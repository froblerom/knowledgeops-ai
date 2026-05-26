import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subject, forkJoin, interval } from 'rxjs';
import { switchMap, takeUntil } from 'rxjs/operators';
import {
  DocumentProcessingStatus,
  DocumentProcessingStatusResponse,
  DocumentService,
  ManagedDocument
} from '../../../core/services/document.service';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { RoleVisibilityService } from '../../../core/services/role-visibility.service';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import { ErrorState } from '../../../shared/components/error-state/error-state';

const POLL_INTERVAL_MS = 5000;

@Component({
  selector: 'app-document-detail-page',
  imports: [RouterLink, DatePipe, LoadingState, ErrorState],
  templateUrl: './document-detail-page.html',
  styleUrl: './document-detail-page.scss'
})
export class DocumentDetailPage implements OnInit, OnDestroy {
  private readonly documentsApi = inject(DocumentService);
  private readonly apiError = inject(ApiErrorService);
  private readonly roles = inject(RoleVisibilityService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroy$ = new Subject<void>();

  readonly documentId = this.route.snapshot.paramMap.get('documentId')!;
  readonly canDisable = this.roles.canDisableDocumentRetrieval();
  document: ManagedDocument | null = null;
  status: DocumentProcessingStatusResponse | null = null;
  loading = true;
  saving = false;
  error: ApiRequestError | null = null;

  ngOnInit(): void {
    forkJoin({
      document: this.documentsApi.get(this.documentId),
      status: this.documentsApi.getProcessingStatus(this.documentId)
    }).subscribe({
      next: result => {
        this.document = result.document;
        this.status = result.status;
        this.loading = false;
        this.startPollingIfPending(result.status.processingStatus);
      },
      error: response => {
        this.error = this.apiError.fromHttpError(response);
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  disableRetrieval(): void {
    if (!this.document || this.saving) return;

    this.saving = true;
    this.error = null;
    this.documentsApi.disableRetrieval(this.documentId).subscribe({
      next: document => {
        this.document = document;
        if (this.status) {
          this.status = { ...this.status, isRetrievalEnabled: document.isRetrievalEnabled };
        }
        this.saving = false;
      },
      error: response => {
        this.error = this.apiError.fromHttpError(response);
        this.saving = false;
      }
    });
  }

  private isTerminal(processingStatus: DocumentProcessingStatus): boolean {
    return processingStatus === 'Processed' || processingStatus === 'Failed';
  }

  private startPollingIfPending(processingStatus: DocumentProcessingStatus): void {
    if (this.isTerminal(processingStatus)) return;

    interval(POLL_INTERVAL_MS)
      .pipe(
        takeUntil(this.destroy$),
        switchMap(() => this.documentsApi.getProcessingStatus(this.documentId))
      )
      .subscribe({
        next: statusResponse => {
          this.status = statusResponse;
          if (statusResponse.isRetrievalEnabled !== undefined && this.document) {
            this.document = { ...this.document, isRetrievalEnabled: statusResponse.isRetrievalEnabled };
          }
          if (this.isTerminal(statusResponse.processingStatus)) {
            this.destroy$.next();
          }
        },
        error: () => {
          // Polling errors are silent; the last known status remains displayed.
        }
      });
  }
}
