import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AnswerFeedbackRating,
  AnswerFeedbackResponse,
  AnswerFeedbackReviewResponse,
  AskChatQuestionRequest,
  AskChatQuestionResponse
} from '../models/chat.models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/chat/questions`;
  private readonly feedbackUrl = `${environment.apiBaseUrl}/feedback`;
  private readonly chatInteractionsUrl = `${environment.apiBaseUrl}/chat/interactions`;

  askQuestion(questionText: string, chatSessionId?: string | null): Observable<AskChatQuestionResponse> {
    const request: AskChatQuestionRequest = chatSessionId
      ? { questionText, chatSessionId }
      : { questionText };

    return this.http.post<AskChatQuestionResponse>(this.baseUrl, request);
  }

  submitFeedback(
    chatInteractionId: string,
    rating: AnswerFeedbackRating
  ): Observable<AnswerFeedbackResponse> {
    return this.http.post<AnswerFeedbackResponse>(`${this.chatInteractionsUrl}/${chatInteractionId}/feedback`, {
      rating
    });
  }

  updateFeedback(
    chatInteractionId: string,
    rating: AnswerFeedbackRating
  ): Observable<AnswerFeedbackResponse> {
    return this.http.put<AnswerFeedbackResponse>(`${this.chatInteractionsUrl}/${chatInteractionId}/feedback`, {
      rating
    });
  }

  getFeedbackReviewData(): Observable<AnswerFeedbackReviewResponse> {
    return this.http.get<AnswerFeedbackReviewResponse>(this.feedbackUrl);
  }
}
