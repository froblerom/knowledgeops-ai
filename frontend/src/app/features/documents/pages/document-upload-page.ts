import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DocumentService } from '../../../core/services/document.service';
import { RoleVisibilityService } from '../../../core/services/role-visibility.service';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';

@Component({
  selector: 'app-document-upload-page',
  imports: [FormsModule, RouterLink, ErrorState],
  templateUrl: './document-upload-page.html',
  styleUrl: './document-upload-page.scss'
})
export class DocumentUploadPage {
  private readonly documentsApi = inject(DocumentService);
  private readonly router = inject(Router);
  private readonly apiError = inject(ApiErrorService);
  readonly roleVisibility = inject(RoleVisibilityService);

  title = '';
  selectedFile: File | null = null;
  submitting = false;
  validationErrors: string[] = [];
  error: ApiRequestError | null = null;

  // UX-only: matches backend AllowedExtensions. Backend is authoritative.
  private static readonly AllowedExtensions = ['.pdf', '.txt', '.md', '.markdown', '.docx'];
  private static readonly MaxFileSizeBytes = 10 * 1024 * 1024;

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  onSubmit(): void {
    this.validationErrors = [];
    this.error = null;

    const errors: string[] = [];

    if (!this.title.trim()) {
      errors.push('Title is required.');
    }

    if (!this.selectedFile) {
      errors.push('File is required.');
    } else {
      const nameParts = this.selectedFile.name.split('.');
      const ext = nameParts.length > 1 ? '.' + nameParts.pop()!.toLowerCase() : '';
      if (!DocumentUploadPage.AllowedExtensions.includes(ext)) {
        errors.push('File type is not supported. Allowed formats: PDF, TXT, MD, DOCX.');
      }
      if (this.selectedFile.size > DocumentUploadPage.MaxFileSizeBytes) {
        errors.push('File size must not exceed 10 MB.');
      }
      if (this.selectedFile.size === 0) {
        errors.push('File must not be empty.');
      }
    }

    if (errors.length > 0) {
      this.validationErrors = errors;
      return;
    }

    this.submitting = true;
    this.documentsApi.upload(this.title.trim(), this.selectedFile!).subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigate(['/documents']);
      },
      error: response => {
        this.submitting = false;
        this.error = this.apiError.fromHttpError(response);
      }
    });
  }
}
