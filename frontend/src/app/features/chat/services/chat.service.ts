import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AskChatQuestionRequest,
  AskChatQuestionResponse,
  ChatInteractionDetail,
  ChatCitationHistory,
  ChatSessionDetail,
  ChatSessionSummary,
  CreateChatSessionRequest,
  CreateChatSessionResponse
} from '../models/chat.models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly chatBaseUrl = `${environment.apiBaseUrl}/chat`;

  askQuestion(questionText: string, chatSessionId?: string | null): Observable<AskChatQuestionResponse> {
    const request: AskChatQuestionRequest = chatSessionId
      ? { questionText, chatSessionId }
      : { questionText };

    return this.http.post<AskChatQuestionResponse>(`${this.chatBaseUrl}/questions`, request);
  }

  getSessions(scoped = false): Observable<ChatSessionSummary[]> {
    const params = scoped ? '?scoped=true' : '';
    return this.http.get<ChatSessionSummary[]>(`${this.chatBaseUrl}/sessions${params}`);
  }

  createSession(title?: string): Observable<CreateChatSessionResponse> {
    const body: CreateChatSessionRequest = title ? { title } : {};
    return this.http.post<CreateChatSessionResponse>(`${this.chatBaseUrl}/sessions`, body);
  }

  getSession(chatSessionId: string): Observable<ChatSessionDetail> {
    return this.http.get<ChatSessionDetail>(`${this.chatBaseUrl}/sessions/${chatSessionId}`);
  }

  getInteraction(chatInteractionId: string): Observable<ChatInteractionDetail> {
    return this.http.get<ChatInteractionDetail>(`${this.chatBaseUrl}/interactions/${chatInteractionId}`);
  }

  getInteractionCitations(chatInteractionId: string): Observable<ChatCitationHistory[]> {
    return this.http.get<ChatCitationHistory[]>(`${this.chatBaseUrl}/interactions/${chatInteractionId}/citations`);
  }
}
