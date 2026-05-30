import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { RoleVisibilityService } from '../../../core/services/role-visibility.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import {
  AskChatQuestionResponse,
  ChatAnswerState,
  ChatCitation
} from '../models/chat.models';
import { ChatService } from '../services/chat.service';

interface ChatTranscriptItem {
  id: string;
  questionText: string;
  answerState: ChatAnswerState | 'Pending';
  answer: string | null;
  insufficientContext: boolean;
  citations: ChatCitation[];
}

@Component({
  selector: 'app-chat-page',
  imports: [DecimalPipe, FormsModule, ErrorState, LoadingState],
  templateUrl: './chat-page.html',
  styleUrl: './chat-page.scss'
})
export class ChatPage {
  private readonly chat = inject(ChatService);
  private readonly apiError = inject(ApiErrorService);
  readonly roleVisibility = inject(RoleVisibilityService);

  questionText = '';
  submitting = false;
  validationError: string | null = null;
  error: ApiRequestError | null = null;
  transcript: ChatTranscriptItem[] = [];
  private chatSessionId: string | null = null;

  get canSubmit(): boolean {
    return this.questionText.trim().length > 0 && !this.submitting;
  }

  onSubmit(): void {
    const questionText = this.questionText.trim();
    this.validationError = null;
    this.error = null;

    if (!questionText) {
      this.validationError = 'Enter a question before asking.';
      return;
    }

    if (!this.roleVisibility.canAskChat() || this.submitting) return;

    const pendingId = `pending-${this.transcript.length + 1}`;
    this.transcript = [
      ...this.transcript,
      {
        id: pendingId,
        questionText,
        answerState: 'Pending',
        answer: null,
        insufficientContext: false,
        citations: []
      }
    ];
    this.questionText = '';
    this.submitting = true;

    this.chat.askQuestion(questionText, this.chatSessionId ?? undefined).subscribe({
      next: response => {
        this.chatSessionId = response.chatSessionId;
        this.replacePendingItem(pendingId, questionText, response);
        this.submitting = false;
      },
      error: response => {
        this.error = this.toApiRequestError(response);
        this.removePendingItem(pendingId);
        this.questionText = questionText;
        this.submitting = false;
      }
    });
  }

  private replacePendingItem(
    pendingId: string,
    questionText: string,
    response: AskChatQuestionResponse
  ): void {
    const answered: ChatTranscriptItem = {
      id: response.chatInteractionId,
      questionText,
      answerState: response.answerState,
      answer: response.answer,
      insufficientContext: response.insufficientContext,
      citations: response.citations ?? []
    };

    this.transcript = this.transcript.map(item =>
      item.id === pendingId ? answered : item
    );
  }

  private removePendingItem(pendingId: string): void {
    this.transcript = this.transcript.filter(item => item.id !== pendingId);
  }

  private toApiRequestError(response: ApiRequestError | HttpErrorResponse): ApiRequestError {
    if (response instanceof ApiRequestError) {
      return response;
    }

    return this.apiError.fromHttpError(response);
  }
}
