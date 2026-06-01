import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { ChatSessionDetailPage } from './chat-session-detail-page';

describe('ChatSessionDetailPage', () => {
  let http: HttpTestingController;
  const sessionId = 'test-session-001';
  const endpoint = `${environment.apiBaseUrl}/chat/sessions/${sessionId}`;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatSessionDetailPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => sessionId } } }
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
    const fixture = TestBed.createComponent(ChatSessionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush({
      chatSessionId: sessionId, title: 'Test Session', status: 'Active',
      createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(),
      lastInteractionAt: null, interactions: []
    });
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('sets session and clears loading after successful response', () => {
    const fixture = TestBed.createComponent(ChatSessionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush({
      chatSessionId: sessionId, title: 'Policy Session', status: 'Active',
      createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(),
      lastInteractionAt: null,
      interactions: [{
        chatInteractionId: 'i-1', answerState: 'GroundedAnswer',
        insufficientContext: false, createdAt: new Date().toISOString()
      }]
    });
    const component = fixture.componentInstance;
    expect(component.loading).toBe(false);
    expect(component.session).not.toBeNull();
    expect(component.session?.interactions.length).toBe(1);
  });

  it('sets session title from response', () => {
    const fixture = TestBed.createComponent(ChatSessionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush({
      chatSessionId: sessionId, title: 'Policy Session', status: 'Active',
      createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(),
      lastInteractionAt: null,
      interactions: [{
        chatInteractionId: 'i-1', answerState: 'GroundedAnswer',
        insufficientContext: false, createdAt: new Date().toISOString()
      }]
    });
    // Verify component state (DOM re-render requires a full zone cycle in this environment)
    expect(fixture.componentInstance.session?.title).toBe('Policy Session');
    expect(fixture.componentInstance.session?.interactions.length).toBe(1);
  });

  it('sets error state on API failure', () => {
    const fixture = TestBed.createComponent(ChatSessionDetailPage);
    fixture.detectChanges(false);
    http.expectOne(endpoint).flush('Not Found', { status: 404, statusText: 'Not Found' });
    expect(fixture.componentInstance.error).not.toBeNull();
    expect(fixture.componentInstance.loading).toBe(false);
  });
});
