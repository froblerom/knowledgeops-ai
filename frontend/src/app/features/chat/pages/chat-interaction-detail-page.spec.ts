import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { ChatInteractionDetailPage } from './chat-interaction-detail-page';

describe('ChatInteractionDetailPage', () => {
  let http: HttpTestingController;
  const interactionId = 'test-interaction-001';
  const endpoint = `${environment.apiBaseUrl}/chat/interactions/${interactionId}`;

  const groundedResponse = () => ({
    chatInteractionId: interactionId,
    chatSessionId: 'session-1',
    answerState: 'GroundedAnswer',
    insufficientContext: false,
    questionText: 'What is the escalation policy?',
    answerText: 'Follow the documented escalation procedure.',
    promptVersion: 'rag-grounded-v1',
    correlationId: 'corr-1',
    metadata: {
      retrievalCandidateCount: 1,
      retrievalLatencyMs: 200, generationLatencyMs: 800, totalLatencyMs: 1000,
      tokenUsageInput: 100, tokenUsageOutput: 50, estimatedCost: null,
      aiProvider: 'TestProvider', aiModel: 'test-model-v1', providerFailureCode: null
    },
    citations: [{
      citationId: 'citation-1', chatInteractionId: interactionId,
      documentId: 'doc-1', chunkId: 'chunk-1', rank: 1,
      documentTitle: 'Escalation Policy', pageNumber: 2,
      sectionLabel: 'Escalation Steps', relevanceScore: 0.91
    }],
    createdAt: new Date().toISOString()
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatInteractionDetailPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => interactionId } } }
        },
        {
          provide: AuthSessionService,
          useValue: { isAuthenticated: () => true, getSession: () => ({ roles: ['Agent'] }) }
        }
      ]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should create', () => {
    const fixture = TestBed.createComponent(ChatInteractionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush('Not Found', { status: 404, statusText: 'Not Found' });
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('sets interaction and clears loading after successful response', () => {
    const fixture = TestBed.createComponent(ChatInteractionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush(groundedResponse());
    const component = fixture.componentInstance;
    expect(component.loading).toBe(false);
    expect(component.interaction).not.toBeNull();
    expect(component.interaction?.answerState).toBe('GroundedAnswer');
    expect(component.interaction?.insufficientContext).toBe(false);
  });

  it('sets question and answer text from response', () => {
    const fixture = TestBed.createComponent(ChatInteractionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush(groundedResponse());
    // Verify component state (DOM re-render requires a full zone cycle in this environment)
    const interaction = fixture.componentInstance.interaction;
    expect(interaction?.questionText).toBe('What is the escalation policy?');
    expect(interaction?.answerText).toBe('Follow the documented escalation procedure.');
    expect(interaction?.citations.length).toBe(1);
    expect(interaction?.citations[0].documentTitle).toBe('Escalation Policy');
  });

  it('sets insufficient context state correctly', () => {
    const fixture = TestBed.createComponent(ChatInteractionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush({
      chatInteractionId: interactionId, chatSessionId: 'session-1',
      answerState: 'InsufficientContext', insufficientContext: true,
      questionText: 'Unknown topic?', answerText: null,
      promptVersion: null, correlationId: null,
      metadata: {
        retrievalCandidateCount: 0,
        retrievalLatencyMs: null, generationLatencyMs: null, totalLatencyMs: null,
        tokenUsageInput: null, tokenUsageOutput: null, estimatedCost: null
      },
      citations: [], createdAt: new Date().toISOString()
    });
    const component = fixture.componentInstance;
    expect(component.interaction?.answerState).toBe('InsufficientContext');
    expect(component.interaction?.insufficientContext).toBe(true);
  });

  it('includes metadata in interaction detail', () => {
    const fixture = TestBed.createComponent(ChatInteractionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush(groundedResponse());
    const component = fixture.componentInstance;
    expect(component.interaction?.metadata.retrievalCandidateCount).toBe(1);
    expect(component.interaction?.metadata.totalLatencyMs).toBe(1000);
  });

  it('does not expose questionTextHash or retrievalQueryId in top-level interaction state', () => {
    const fixture = TestBed.createComponent(ChatInteractionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush(groundedResponse());
    const component = fixture.componentInstance;
    // questionTextHash and retrievalQueryId are internal fields never returned by the API
    expect((component.interaction as any)?.questionTextHash).toBeUndefined();
    expect((component.interaction as any)?.retrievalQueryId).toBeUndefined();
    // providerFailureCode is now intentionally in metadata (not a top-level field)
    expect((component.interaction as any)?.providerFailureCode).toBeUndefined();
    expect(component.interaction?.metadata.providerFailureCode).toBeNull();
  });

  it('shows providerFailureCode in metadata for ProviderFailure interactions', () => {
    const fixture = TestBed.createComponent(ChatInteractionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush({
      chatInteractionId: interactionId, chatSessionId: 'session-1',
      answerState: 'ProviderFailure', insufficientContext: false,
      questionText: 'What is the policy?', answerText: null,
      promptVersion: null, correlationId: null,
      metadata: {
        retrievalCandidateCount: 1,
        retrievalLatencyMs: 100, generationLatencyMs: 5000, totalLatencyMs: 5100,
        tokenUsageInput: null, tokenUsageOutput: null, estimatedCost: null,
        aiProvider: 'QwenLocal', aiModel: 'qwen3:8b',
        providerFailureCode: 'ProviderMalformedResponse'
      },
      citations: [], createdAt: new Date().toISOString()
    });
    const component = fixture.componentInstance;
    expect(component.interaction?.answerState).toBe('ProviderFailure');
    expect(component.interaction?.metadata.providerFailureCode).toBe('ProviderMalformedResponse');
    expect(component.interaction?.metadata.aiProvider).toBe('QwenLocal');
    expect(component.interaction?.metadata.aiModel).toBe('qwen3:8b');
  });

  it('shows aiProvider and aiModel in metadata for grounded interactions', () => {
    const fixture = TestBed.createComponent(ChatInteractionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush(groundedResponse());
    const component = fixture.componentInstance;
    expect(component.interaction?.metadata.aiProvider).toBe('TestProvider');
    expect(component.interaction?.metadata.aiModel).toBe('test-model-v1');
    expect(component.interaction?.metadata.providerFailureCode).toBeNull();
  });

  it('sets error state on API failure', () => {
    const fixture = TestBed.createComponent(ChatInteractionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush('Not Found', { status: 404, statusText: 'Not Found' });
    expect(fixture.componentInstance.error).not.toBeNull();
    expect(fixture.componentInstance.loading).toBe(false);
  });
});
