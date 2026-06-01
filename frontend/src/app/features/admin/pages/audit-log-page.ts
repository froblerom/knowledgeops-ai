import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import { AuditLogEntry } from '../models/admin-support.models';
import { AdminSupportService } from '../services/admin-support.service';

@Component({
  selector: 'app-audit-log-page',
  imports: [ReactiveFormsModule, ErrorState, LoadingState],
  templateUrl: './audit-log-page.html',
  styleUrl: './admin-page.scss'
})
export class AuditLogPage implements OnInit {
  private readonly support = inject(AdminSupportService);
  private readonly apiError = inject(ApiErrorService);
  private readonly formBuilder = inject(FormBuilder);

  readonly filters = this.formBuilder.group({
    from: [''],
    to: [''],
    eventType: ['']
  });

  entries: AuditLogEntry[] = [];
  loading = true;
  error: ApiRequestError | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    const raw = this.filters.getRawValue();
    this.support.getAuditLog({
      from: raw.from || undefined,
      to: raw.to || undefined,
      eventType: raw.eventType?.trim() || undefined
    }).subscribe({
      next: entries => {
        this.entries = entries;
        this.loading = false;
      },
      error: response => {
        this.error = this.apiError.fromHttpError(response);
        this.loading = false;
      }
    });
  }
}
