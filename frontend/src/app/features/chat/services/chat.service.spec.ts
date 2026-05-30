import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../../environments/environment';
import { ChatService } from './chat.service';

describe('ChatService', () => {
  let service: ChatService;
  let http: HttpTestingController;
  const endpoint = `${environment.apiBaseUrl}/chat/questions`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ChatService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts a question without organization, provider, model, or debug fields', () => {
    service.askQuestion('How do I resolve a billing case?').subscribe();

    const req = http.expectOne(endpoint);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      questionText: 'How do I resolve a billing case?'
    });
    expect(req.request.body.organizationId).toBeUndefined();
    expect(req.request.body.provider).toBeUndefined();
    expect(req.request.body.model).toBeUndefined();
    expect(req.request.body.debug).toBeUndefined();
    req.flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'GroundedAnswer',
      answer: 'Use the approved billing workflow.',
      insufficientContext: false,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: null
    });
  });

  it('includes only chatSessionId when continuing the in-memory session', () => {
    service.askQuestion('What next?', 'session-1').subscribe();

    const req = http.expectOne(endpoint);
    expect(req.request.body).toEqual({
      questionText: 'What next?',
      chatSessionId: 'session-1'
    });
    req.flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-2',
      answerState: 'GroundedAnswer',
      answer: 'Continue with the next step.',
      insufficientContext: false,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: null
    });
  });

  it('parses grounded answers with citations', () => {
    let state: string | undefined;
    let title: string | null | undefined;
    let cost: number | null | undefined;

    service.askQuestion('What is covered?').subscribe(response => {
      state = response.answerState;
      title = response.citations[0].documentTitle;
      cost = response.metadata.estimatedCost;
    });

    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'GroundedAnswer',
      answer: 'Use the cited workflow.',
      insufficientContext: false,
      citations: [
        {
          citationId: 'citation-1',
          documentId: 'document-1',
          documentTitle: 'Workflow Guide',
          chunkId: 'chunk-1',
          pageNumber: 1,
          sectionLabel: 'Start',
          score: 0.7,
          rank: 1
        }
      ],
      metadata: { latencyMs: 42, retrievalResultCount: 1, estimatedCost: null },
      correlationId: 'corr-1'
    });

    expect(state).toBe('GroundedAnswer');
    expect(title).toBe('Workflow Guide');
    expect(cost).toBeNull();
  });

  it('parses insufficient context responses', () => {
    let state: string | undefined;

    service.askQuestion('Unknown?').subscribe(response => {
      state = response.answerState;
      expect(response.insufficientContext).toBe(true);
      expect(response.citations).toEqual([]);
    });

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

    expect(state).toBe('InsufficientContext');
  });

  it('parses provider failure responses', () => {
    let state: string | undefined;

    service.askQuestion('Try provider?').subscribe(response => {
      state = response.answerState;
      expect(response.answer).toBeNull();
      expect(response.citations).toEqual([]);
    });

    http.expectOne(endpoint).flush({
      chatSessionId: 'session-1',
      chatInteractionId: 'interaction-1',
      answerState: 'ProviderFailure',
      answer: null,
      insufficientContext: false,
      citations: [],
      metadata: { latencyMs: null, retrievalResultCount: 0, estimatedCost: null },
      correlationId: 'corr-provider'
    });

    expect(state).toBe('ProviderFailure');
  });
});
