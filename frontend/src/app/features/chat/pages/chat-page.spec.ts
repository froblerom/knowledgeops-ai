import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { ChatPage } from './chat-page';

describe('ChatPage', () => {
  let http: HttpTestingController;
  const endpoint = `${environment.apiBaseUrl}/chat/questions`;
  const interactionFeedbackEndpoint = `${environment.apiBaseUrl}/chat/interactions/interaction-1/feedback`;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatPage, FormsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AuthSessionService,
          useValue: {
            isAuthenticated: () => true,
            getSession: () => ({ roles: ['Agent'] })
          }
        }
      ]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(ChatPage);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('posts the first question and renders Sources heading without Rank or Score', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = 'What is the escalation policy?';

    fixture.componentInstance.onSubmit();

    const req = http.expectOne(endpoint);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ questionText: 'What is the escalation policy?' });

    req.flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'GroundedAnswer',
      answer: 'Follow the approved escalation policy.',
      insufficientContext: false,
      citations: [
        {
          citationId: 'citation-1',
          documentId: 'document-1',
          documentTitle: 'Escalation Policy',
          chunkId: 'chunk-1',
          pageNumber: 3,
          sectionLabel: 'Priority cases',
          score: 0.876,
          rank: 1
        }
      ],
      metadata: { latencyMs: 12, retrievalResultCount: 1, estimatedCost: null },
      correlationId: 'corr-1'
    });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Follow the approved escalation policy.');
    expect(text).toContain('Escalation Policy');
    expect(text).toContain('Sources');
    expect(text).not.toContain('Citations');
    expect(text).toContain('Page 3');
    expect(text).toContain('Priority cases');
    // Rank and score are hidden in the main chat view — kept only in Chat History detail
    expect(text).not.toContain('Rank 1');
    expect(text).not.toContain('Score 0.88');
    expect(text).not.toContain('document-1');
    expect(text).not.toContain('chunk-1');
    expect(text).not.toContain('citation-1');
    expect(text).not.toContain('corr-1');
  });

  it('prevents empty submit', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = '   ';

    fixture.componentInstance.onSubmit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Enter a question before asking.');
    http.expectNone(endpoint);
  });

  it('shows loading state while a question is pending', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = 'What is pending?';

    fixture.componentInstance.onSubmit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Loading');
    expect(fixture.componentInstance.submitting).toBe(true);

    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'GroundedAnswer',
      answer: 'Loaded answer',
      insufficientContext: false,
      citations: [
        {
          citationId: 'citation-1',
          documentId: 'document-1',
          documentTitle: 'Policy',
          chunkId: 'chunk-1',
          pageNumber: null,
          sectionLabel: null,
          score: null,
          rank: 1
        }
      ],
      metadata: { latencyMs: null, retrievalResultCount: 1, estimatedCost: null },
      correlationId: null
    });
  });

  it('continues with the returned chatSessionId without exposing session controls', () => {
    const fixture = TestBed.createComponent(ChatPage);

    fixture.componentInstance.questionText = 'First question';
    fixture.componentInstance.onSubmit();
    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'GroundedAnswer',
      answer: 'First answer',
      insufficientContext: false,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: null
    });

    fixture.componentInstance.questionText = 'Second question';
    fixture.componentInstance.onSubmit();
    const second = http.expectOne(endpoint);
    expect(second.request.body).toEqual({
      questionText: 'Second question',
      chatSessionId: 'session-1'
    });
    second.flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-2',
      answerState: 'GroundedAnswer',
      answer: 'Second answer',
      insufficientContext: false,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: null
    });
  });

  it('shows the internal assistant disclaimer and insufficient context response', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = 'Can you answer from unknown docs?';

    fixture.componentInstance.onSubmit();
    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'InsufficientContext',
      answer: null,
      insufficientContext: true,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: null
    });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Internal assistant responses are for operational support');
    expect(text).toContain('I do not have enough approved context to answer that question.');
  });

  it('renders provider failure safely', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = 'Will the provider answer?';

    fixture.componentInstance.onSubmit();
    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'ProviderFailure',
      answer: 'raw provider failure detail',
      insufficientContext: false,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: 'corr-provider'
    });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('The assistant could not generate an answer. Please try again.');
    expect(text).not.toContain('raw provider failure detail');
    expect(text).not.toContain('corr-provider');
    expect(text).not.toContain('exception');
  });

  it('renders a safe transport error', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = 'Will transport fail?';

    fixture.componentInstance.onSubmit();
    http.expectOne(endpoint).flush(
      { error: { correlationId: 'corr-transport' } },
      { status: 503, statusText: 'Service Unavailable' }
    );
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('The service is temporarily unavailable. Please try again later.');
    expect(text).toContain('corr-transport');
    expect(text).not.toContain('Service Unavailable');
  });

  it('does not render an uncited grounded answer as safe', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = 'Will citations be missing?';

    fixture.componentInstance.onSubmit();
    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'GroundedAnswer',
      answer: 'This answer should not be trusted without citations.',
      insufficientContext: false,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 1, estimatedCost: null },
      correlationId: null
    });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('The assistant could not return a safe cited answer.');
    expect(text).not.toContain('This answer should not be trusted without citations.');
  });

  it('submits useful feedback for a completed answer', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = 'Was this useful?';

    fixture.componentInstance.onSubmit();
    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'GroundedAnswer',
      answer: 'Use the documented process.',
      insufficientContext: false,
      citations: [
        {
          citationId: 'citation-1',
          documentId: 'document-1',
          documentTitle: 'Policy',
          chunkId: 'chunk-1',
          pageNumber: null,
          sectionLabel: null,
          score: null,
          rank: 1
        }
      ],
      metadata: { latencyMs: null, retrievalResultCount: 1, estimatedCost: null },
      correlationId: null
    });
    fixture.detectChanges(false);

    fixture.componentInstance.submitFeedback(fixture.componentInstance.transcript[0], 'Useful');

    expect(fixture.componentInstance.transcript[0].feedbackSubmitting).toBe(true);

    const req = http.expectOne(interactionFeedbackEndpoint);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ rating: 'Useful' });
    expect(req.request.body.answer).toBeUndefined();
    expect(req.request.body.question).toBeUndefined();
    req.flush({
      feedbackId: 'feedback-1',
      chatInteractionId: 'interaction-1',
      userId: 'user-1',
      rating: 'Useful',
      createdAt: '2026-05-30T00:00:00Z',
      updatedAt: '2026-05-30T00:00:00Z'
    });
    fixture.detectChanges(false);

    expect(fixture.componentInstance.transcript[0].feedbackMessage).toBe('Submitted');
  });

  it('updates existing feedback instead of posting a duplicate', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = 'Was this not useful?';

    fixture.componentInstance.onSubmit();
    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'InsufficientContext',
      answer: null,
      insufficientContext: true,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: null
    });
    fixture.detectChanges(false);

    fixture.componentInstance.submitFeedback(fixture.componentInstance.transcript[0], 'Useful');
    http.expectOne(interactionFeedbackEndpoint).flush({
      feedbackId: 'feedback-1',
      chatInteractionId: 'interaction-1',
      userId: 'user-1',
      rating: 'Useful',
      createdAt: '2026-05-30T00:00:00Z',
      updatedAt: '2026-05-30T00:00:00Z'
    });
    fixture.detectChanges(false);

    fixture.componentInstance.submitFeedback(fixture.componentInstance.transcript[0], 'NotUseful');
    const req = http.expectOne(interactionFeedbackEndpoint);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ rating: 'NotUseful' });
    req.flush({
      feedbackId: 'feedback-1',
      chatInteractionId: 'interaction-1',
      userId: 'user-1',
      rating: 'NotUseful',
      createdAt: '2026-05-30T00:00:00Z',
      updatedAt: '2026-05-30T00:01:00Z'
    });
    fixture.detectChanges(false);

    expect(fixture.componentInstance.transcript[0].feedbackRating).toBe('NotUseful');
    expect(fixture.componentInstance.transcript[0].feedbackMessage).toBe('Updated');
  });

  it('shows a safe feedback error state', () => {
    const fixture = TestBed.createComponent(ChatPage);
    fixture.componentInstance.questionText = 'Will feedback fail?';

    fixture.componentInstance.onSubmit();
    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'InsufficientContext',
      answer: null,
      insufficientContext: true,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: null
    });
    fixture.detectChanges(false);

    fixture.componentInstance.submitFeedback(fixture.componentInstance.transcript[0], 'Useful');
    http.expectOne(interactionFeedbackEndpoint).flush(
      { error: { correlationId: 'feedback-corr' } },
      { status: 503, statusText: 'Service Unavailable' }
    );
    fixture.detectChanges(false);

    const error = fixture.componentInstance.transcript[0].feedbackError!;
    expect(error).toContain('The service is temporarily unavailable. Please try again later.');
    expect(error).toContain('feedback-corr');
    expect(error).not.toContain('Service Unavailable');
  });

  it('hides feedback controls when UX visibility says feedback is unavailable', () => {
    const fixture = TestBed.createComponent(ChatPage);
    vi.spyOn(fixture.componentInstance.roleVisibility, 'canSubmitFeedback').mockReturnValue(false);
    fixture.componentInstance.questionText = 'Can I rate this?';

    fixture.componentInstance.onSubmit();
    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'InsufficientContext',
      answer: null,
      insufficientContext: true,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: null
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.feedback-controls')).toBeNull();
  });
});
