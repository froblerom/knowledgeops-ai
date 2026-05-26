import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  DocumentProcessingStatusResponse,
  DocumentService,
  ManagedDocument
} from '../../../core/services/document.service';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { RoleVisibilityService } from '../../../core/services/role-visibility.service';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import { ErrorState } from '../../../shared/components/error-state/error-state';

@Component({
  selector: 'app-document-detail-page',
  imports: [RouterLink, DatePipe, LoadingState, ErrorState],
  templateUrl: './document-detail-page.html',
  styleUrl: './document-detail-page.scss'
})
export class DocumentDetailPage implements OnInit {
  private readonly documentsApi = inject(DocumentService);
  private readonly apiError = inject(ApiErrorService);
  private readonly roles = inject(RoleVisibilityService);
  private readonly route = inject(ActivatedRoute);

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
      },
      error: response => {
        this.error = this.apiError.fromHttpError(response);
        this.loading = false;
      }
    });
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
}
