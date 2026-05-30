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

  it('posts the first question and renders only allowed citation metadata', () => {
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
    expect(text).toContain('Rank 1');
    expect(text).toContain('Page 3');
    expect(text).toContain('Section Priority cases');
    expect(text).toContain('Score 0.88');
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
});
