import { Component, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { UserAdminService, ManagedUser, UserRole, UserStatus } from '../../../core/services/user-admin.service';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';

@Component({
  selector: 'app-admin-user-detail-page',
  imports: [ReactiveFormsModule, RouterLink, ErrorState, LoadingState],
  templateUrl: './admin-user-detail-page.html',
  styleUrl: './admin-user-form-page.scss'
})
export class AdminUserDetailPage implements OnInit {
  private readonly api = inject(UserAdminService);
  private readonly errors = inject(ApiErrorService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  readonly availableRoles: UserRole[] = ['Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin'];
  readonly statuses: UserStatus[] = ['Pending', 'Active', 'Disabled'];
  readonly userId = this.route.snapshot.paramMap.get('userId')!;
  user: ManagedUser | null = null;
  loading = true;
  saving = false;
  error: ApiRequestError | null = null;
  form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    status: ['Pending' as UserStatus, Validators.required],
    role: ['Agent' as UserRole, Validators.required]
  });

  ngOnInit(): void {
    this.api.get(this.userId).subscribe({
      next: user => this.setUser(user),
      error: response => this.fail(response)
    });
  }

  save(): void {
    if (this.form.invalid || this.saving) return;
    const value = this.form.getRawValue();
    this.saving = true;
    this.error = null;
    this.api.update(this.userId, {
      displayName: value.displayName,
      email: value.email,
      status: value.status
    }).subscribe({
      next: user => this.setUser(user),
      error: response => this.fail(response)
    });
  }

  addRole(): void {
    this.mutateRole(this.api.addRole(this.userId, this.form.getRawValue().role));
  }

  removeRole(role: UserRole): void {
    this.mutateRole(this.api.removeRole(this.userId, role));
  }

  private mutateRole(request: ReturnType<UserAdminService['addRole']>): void {
    this.saving = true;
    this.error = null;
    request.subscribe({
      next: user => this.setUser(user),
      error: response => this.fail(response)
    });
  }

  private setUser(user: ManagedUser): void {
    this.user = user;
    this.form.patchValue({
      displayName: user.displayName,
      email: user.email,
      status: user.status
    });
    this.loading = false;
    this.saving = false;
  }

  private fail(response: HttpErrorResponse): void {
    this.error = this.errors.fromHttpError(response);
    this.loading = false;
    this.saving = false;
  }
}
