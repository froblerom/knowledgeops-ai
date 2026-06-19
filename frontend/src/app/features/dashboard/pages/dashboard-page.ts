import { NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { ApiErrorService, ApiRequestError } from '../../../core/services/api-error.service';
import { ErrorState } from '../../../shared/components/error-state/error-state';
import { LoadingState } from '../../../shared/components/loading-state/loading-state';
import {
  DashboardChatResponse,
  DashboardDocumentsResponse,
  DashboardFeedbackResponse,
  DashboardOverviewResponse
} from '../models/dashboard.models';
import { DashboardService } from '../services/dashboard.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [NgIf, ErrorState, LoadingState],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss'
})
export class DashboardPage implements OnInit {
  private readonly dashboard = inject(DashboardService);
  private readonly apiError = inject(ApiErrorService);
  private readonly cdr = inject(ChangeDetectorRef);

  // Overview section
  overviewLoading = true;
  overviewError: ApiRequestError | null = null;
  overview: DashboardOverviewResponse | null = null;

  // Documents section
  documentsLoading = true;
  documentsError: ApiRequestError | null = null;
  documents: DashboardDocumentsResponse | null = null;

  // Chat section
  chatLoading = true;
  chatError: ApiRequestError | null = null;
  chat: DashboardChatResponse | null = null;

  // Feedback section
  feedbackLoading = true;
  feedbackError: ApiRequestError | null = null;
  feedback: DashboardFeedbackResponse | null = null;

  get overviewEmpty(): boolean {
    return !!this.overview &&
      this.overview.questionsAsked === 0 &&
      this.overview.documentsUploaded === 0 &&
      this.overview.usefulFeedbackCount === 0 &&
      this.overview.notUsefulFeedbackCount === 0;
  }

  get documentsEmpty(): boolean {
    return !!this.documents &&
      this.documents.uploaded === 0 &&
      this.documents.processing === 0 &&
      this.documents.processed === 0 &&
      this.documents.failed === 0;
  }

  get chatEmpty(): boolean {
    return !!this.chat && this.chat.questionsAsked === 0;
  }

  get feedbackEmpty(): boolean {
    return !!this.feedback && this.feedback.total === 0;
  }

  ngOnInit(): void {
    this.loadOverview();
    this.loadDocuments();
    this.loadChat();
    this.loadFeedback();
  }

  loadOverview(): void {
    this.overviewLoading = true;
    this.overviewError = null;
    this.dashboard.getOverview().pipe(
      finalize(() => this.cdr.markForCheck())
    ).subscribe({
      next: result => {
        this.overview = result;
        this.overviewLoading = false;
      },
      error: err => {
        this.overviewError = this.toApiRequestError(err);
        this.overviewLoading = false;
      }
    });
  }

  loadDocuments(): void {
    this.documentsLoading = true;
    this.documentsError = null;
    this.dashboard.getDocuments().pipe(
      finalize(() => this.cdr.markForCheck())
    ).subscribe({
      next: result => {
        this.documents = result;
        this.documentsLoading = false;
      },
      error: err => {
        this.documentsError = this.toApiRequestError(err);
        this.documentsLoading = false;
      }
    });
  }

  loadChat(): void {
    this.chatLoading = true;
    this.chatError = null;
    this.dashboard.getChat().pipe(
      finalize(() => this.cdr.markForCheck())
    ).subscribe({
      next: result => {
        this.chat = result;
        this.chatLoading = false;
      },
      error: err => {
        this.chatError = this.toApiRequestError(err);
        this.chatLoading = false;
      }
    });
  }

  loadFeedback(): void {
    this.feedbackLoading = true;
    this.feedbackError = null;
    this.dashboard.getFeedback().pipe(
      finalize(() => this.cdr.markForCheck())
    ).subscribe({
      next: result => {
        this.feedback = result;
        this.feedbackLoading = false;
      },
      error: err => {
        this.feedbackError = this.toApiRequestError(err);
        this.feedbackLoading = false;
      }
    });
  }

  private toApiRequestError(err: ApiRequestError | HttpErrorResponse): ApiRequestError {
    if (err instanceof ApiRequestError) return err;
    return this.apiError.fromHttpError(err);
  }
}
