import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { UserAdminService, UserRole, UserStatus } from '../../../core/services/user-admin.service';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';

@Component({
  selector: 'app-admin-user-create-page',
  imports: [ReactiveFormsModule, RouterLink, ErrorState],
  templateUrl: './admin-user-create-page.html',
  styleUrl: './admin-user-form-page.scss'
})
export class AdminUserCreatePage {
  private readonly api = inject(UserAdminService);
  private readonly errors = inject(ApiErrorService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly roles: UserRole[] = ['Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin'];
  readonly statuses: UserStatus[] = ['Pending', 'Active', 'Disabled'];
  loading = false;
  error: ApiRequestError | null = null;
  form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    status: ['Pending' as UserStatus, Validators.required],
    role: ['' as UserRole | ''],
    initialPassword: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid || this.loading) return;
    const value = this.form.getRawValue();
    this.loading = true;
    this.error = null;
    this.api.create({
      displayName: value.displayName,
      email: value.email,
      status: value.status,
      roles: value.role ? [value.role] : [],
      initialPassword: value.initialPassword
    }).subscribe({
      next: user => this.router.navigate(['/admin/users', user.userId]),
      error: response => {
        this.error = this.errors.fromHttpError(response);
        this.loading = false;
      }
    });
  }
}
