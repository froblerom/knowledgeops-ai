import { Component, OnInit, inject } from '@angular/core';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import { ProcessingFailure } from '../models/admin-support.models';
import { AdminSupportService } from '../services/admin-support.service';

@Component({
  selector: 'app-processing-failures-page',
  imports: [ErrorState, LoadingState],
  templateUrl: './processing-failures-page.html',
  styleUrl: './admin-page.scss'
})
export class ProcessingFailuresPage implements OnInit {
  private readonly support = inject(AdminSupportService);
  private readonly apiError = inject(ApiErrorService);

  failures: ProcessingFailure[] = [];
  loading = true;
  error: ApiRequestError | null = null;

  ngOnInit(): void {
    this.support.getProcessingFailures().subscribe({
      next: failures => {
        this.failures = failures;
        this.loading = false;
      },
      error: response => {
        this.error = this.apiError.fromHttpError(response);
        this.loading = false;
      }
    });
  }
}
