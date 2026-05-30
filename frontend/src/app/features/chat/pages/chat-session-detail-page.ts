import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import { ChatSessionDetail } from '../models/chat.models';
import { ChatService } from '../services/chat.service';

@Component({
  selector: 'app-chat-session-detail-page',
  imports: [DatePipe, RouterLink, ErrorState, LoadingState],
  templateUrl: './chat-session-detail-page.html',
  styleUrl: './chat-session-detail-page.scss'
})
export class ChatSessionDetailPage implements OnInit {
  private readonly chat = inject(ChatService);
  private readonly apiError = inject(ApiErrorService);
  private readonly route = inject(ActivatedRoute);

  session: ChatSessionDetail | null = null;
  loading = true;
  error: ApiRequestError | null = null;

  ngOnInit(): void {
    const sessionId = this.route.snapshot.paramMap.get('chatSessionId');
    if (!sessionId) {
      this.loading = false;
      return;
    }

    this.chat.getSession(sessionId).subscribe({
      next: session => {
        this.session = session;
        this.loading = false;
      },
      error: (err: ApiRequestError | HttpErrorResponse) => {
        this.error = err instanceof ApiRequestError ? err : this.apiError.fromHttpError(err);
        this.loading = false;
      }
    });
  }
}
