import { DecimalPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import { ChatInteractionDetail } from '../models/chat.models';
import { ChatService } from '../services/chat.service';

@Component({
  selector: 'app-chat-interaction-detail-page',
  imports: [DecimalPipe, DatePipe, RouterLink, ErrorState, LoadingState],
  templateUrl: './chat-interaction-detail-page.html',
  styleUrl: './chat-interaction-detail-page.scss'
})
export class ChatInteractionDetailPage implements OnInit {
  private readonly chat = inject(ChatService);
  private readonly apiError = inject(ApiErrorService);
  private readonly route = inject(ActivatedRoute);

  interaction: ChatInteractionDetail | null = null;
  loading = true;
  error: ApiRequestError | null = null;

  ngOnInit(): void {
    const interactionId = this.route.snapshot.paramMap.get('chatInteractionId');
    if (!interactionId) {
      this.loading = false;
      return;
    }

    this.chat.getInteraction(interactionId).subscribe({
      next: interaction => {
        this.interaction = interaction;
        this.loading = false;
      },
      error: (err: ApiRequestError | HttpErrorResponse) => {
        this.error = err instanceof ApiRequestError ? err : this.apiError.fromHttpError(err);
        this.loading = false;
      }
    });
  }
}
