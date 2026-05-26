import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
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
  readonly roleVisibility = inject(RoleVisibilityService);

  documents: ManagedDocument[] = [];
  loading = true;
  error: ApiRequestError | null = null;

  ngOnInit(): void {
    this.documentsApi.list().subscribe({
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
}
