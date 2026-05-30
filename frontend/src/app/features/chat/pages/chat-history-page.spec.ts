import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { ChatHistoryPage } from './chat-history-page';

const sessionsEndpoint = `${environment.apiBaseUrl}/chat/sessions`;

const makeSession = (id: string, title: string) => ({
  chatSessionId: id, title, status: 'Active',
  createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(),
  lastInteractionAt: null, interactionCount: 2
});

// Tests for Agent role (no scoped history)
describe('ChatHistoryPage (Agent)', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatHistoryPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
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
    const fixture = TestBed.createComponent(ChatHistoryPage);
    fixture.detectChanges(false);
    http.expectOne(sessionsEndpoint).flush([]);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows loading state initially', () => {
    const fixture = TestBed.createComponent(ChatHistoryPage);
    fixture.detectChanges(false);
    expect(fixture.componentInstance.loading).toBe(true);
    http.expectOne(sessionsEndpoint).flush([]);
  });

  it('sets sessions and clears loading after successful response', () => {
    const fixture = TestBed.createComponent(ChatHistoryPage);
    fixture.detectChanges(false);
    http.expectOne(sessionsEndpoint).flush([makeSession('s-1', 'Session 1')]);
    expect(fixture.componentInstance.sessions.length).toBe(1);
    expect(fixture.componentInstance.loading).toBe(false);
  });

  it('sets error state on API failure', () => {
    const fixture = TestBed.createComponent(ChatHistoryPage);
    fixture.detectChanges(false);
    http.expectOne(sessionsEndpoint).flush(
      { error: { correlationId: 'err-id' } },
      { status: 403, statusText: 'Forbidden' }
    );
    expect(fixture.componentInstance.error).not.toBeNull();
    expect(fixture.componentInstance.loading).toBe(false);
  });

  it('does not show scoped review button for Agent', () => {
    const fixture = TestBed.createComponent(ChatHistoryPage);
    fixture.detectChanges(false);
    http.expectOne(sessionsEndpoint).flush([]);
    fixture.detectChanges(false);
    expect((fixture.nativeElement as HTMLElement).querySelector('.scope-actions')).toBeFalsy();
  });
});

// Tests for Supervisor role (has scoped history)
describe('ChatHistoryPage (Supervisor)', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatHistoryPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: { isAuthenticated: () => true, getSession: () => ({ roles: ['Supervisor'] }) }
        }
      ]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('shows scoped review button for Supervisor', () => {
    const fixture = TestBed.createComponent(ChatHistoryPage);
    fixture.detectChanges(false);
    http.expectOne(sessionsEndpoint).flush([]);
    fixture.detectChanges(false);
    expect((fixture.nativeElement as HTMLElement).querySelector('.scope-actions')).toBeTruthy();
  });
});

// Tests for KnowledgeAdmin role (no scoped history)
describe('ChatHistoryPage (KnowledgeAdmin)', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatHistoryPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: { isAuthenticated: () => true, getSession: () => ({ roles: ['KnowledgeAdmin'] }) }
        }
      ]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('does not show scoped review button for KnowledgeAdmin', () => {
    const fixture = TestBed.createComponent(ChatHistoryPage);
    fixture.detectChanges(false);
    http.expectOne(sessionsEndpoint).flush([]);
    fixture.detectChanges(false);
    expect((fixture.nativeElement as HTMLElement).querySelector('.scope-actions')).toBeFalsy();
  });
});
