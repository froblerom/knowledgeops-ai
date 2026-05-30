import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AskChatQuestionRequest,
  AskChatQuestionResponse
} from '../models/chat.models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/chat/questions`;

  askQuestion(questionText: string, chatSessionId?: string | null): Observable<AskChatQuestionResponse> {
    const request: AskChatQuestionRequest = chatSessionId
      ? { questionText, chatSessionId }
      : { questionText };

    return this.http.post<AskChatQuestionResponse>(this.baseUrl, request);
  }
}
