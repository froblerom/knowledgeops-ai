import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { RoleVisibilityService } from '../../../core/services/role-visibility.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import { ChatSessionSummary } from '../models/chat.models';
import { ChatService } from '../services/chat.service';

@Component({
  selector: 'app-chat-history-page',
  imports: [DatePipe, RouterLink, ErrorState, LoadingState],
  templateUrl: './chat-history-page.html',
  styleUrl: './chat-history-page.scss'
})
export class ChatHistoryPage implements OnInit {
  private readonly chat = inject(ChatService);
  private readonly apiError = inject(ApiErrorService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly roleVisibility = inject(RoleVisibilityService);
  private readonly router = inject(Router);

  sessions: ChatSessionSummary[] = [];
  loading = true;
  error: ApiRequestError | null = null;

  get canViewScoped(): boolean {
    return this.roleVisibility.canViewScopedChatHistory();
  }

  ngOnInit(): void {
    this.loadSessions(false);
  }

  loadSessions(scoped: boolean): void {
    this.loading = true;
    this.error = null;

    this.chat.getSessions(scoped).pipe(
      finalize(() => this.cdr.markForCheck())
    ).subscribe({
      next: sessions => {
        this.sessions = sessions;
        this.loading = false;
      },
      error: err => {
        this.error = this.toApiRequestError(err);
        this.loading = false;
      }
    });
  }

  openSession(sessionId: string): void {
    this.router.navigate(['/chat/history', sessionId]);
  }

  private toApiRequestError(err: ApiRequestError | HttpErrorResponse): ApiRequestError {
    if (err instanceof ApiRequestError) return err;
    return this.apiError.fromHttpError(err);
  }
}
