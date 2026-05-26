import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UserAdminService, ManagedUser } from '../../../core/services/user-admin.service';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import { ErrorState } from '../../../shared/components/error-state/error-state';

@Component({
  selector: 'app-admin-page',
  imports: [RouterLink, LoadingState, ErrorState],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.scss'
})
export class AdminPage implements OnInit {
  private readonly usersApi = inject(UserAdminService);
  private readonly apiError = inject(ApiErrorService);

  users: ManagedUser[] = [];
  loading = true;
  error: ApiRequestError | null = null;

  ngOnInit(): void {
    this.usersApi.list().subscribe({
      next: users => {
        this.users = users;
        this.loading = false;
      },
      error: response => {
        this.error = this.apiError.fromHttpError(response);
        this.loading = false;
      }
    });
  }
}
