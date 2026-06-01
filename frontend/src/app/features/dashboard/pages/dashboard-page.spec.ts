import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { DashboardPage } from './dashboard-page';

const overviewEndpoint = `${environment.apiBaseUrl}/dashboard/overview`;
const documentsEndpoint = `${environment.apiBaseUrl}/dashboard/documents`;
const chatEndpoint = `${environment.apiBaseUrl}/dashboard/chat`;
const feedbackEndpoint = `${environment.apiBaseUrl}/dashboard/feedback`;

const makePeriod = () => ({
  from: new Date(Date.now() - 30 * 86400_000).toISOString(),
  to: new Date().toISOString()
});

const makeOverview = (overrides: object = {}) => ({
  period: makePeriod(),
  questionsAsked: 10,
  activeUsers: 3,
  documentsUploaded: 5,
  documentsProcessed: 4,
  documentsFailed: 1,
  averageResponseLatencyMs: 200,
  insufficientContextCount: 1,
  providerFailureCount: 0,
  usefulFeedbackCount: 7,
  notUsefulFeedbackCount: 2,
  cost: { available: false, estimatedTotal: null },
  ...overrides
});

const makeDocuments = (overrides: object = {}) => ({
  period: makePeriod(),
  uploaded: 2, processing: 1, processed: 10, failed: 3, retrievalDisabled: 5,
  ...overrides
});

const makeChat = (overrides: object = {}) => ({
  period: makePeriod(),
  questionsAsked: 5,
  activeUsers: 2,
  averageResponseLatencyMs: 200,
  retrievalLatencyMs: 50,
  generationLatencyMs: 150,
  totalRagLatencyMs: 200,
  insufficientContextCount: 1,
  providerFailureCount: 0,
  tokens: { input: 1000, output: 500, total: 1500 },
  cost: { available: false, estimatedTotal: null },
  ...overrides
});

const makeFeedback = (overrides: object = {}) => ({
  period: makePeriod(), useful: 7, notUseful: 3, total: 10,
  ...overrides
});

function flushAll(http: HttpTestingController, overrides: {
  overview?: object; documents?: object; chat?: object; feedback?: object;
} = {}): void {
  http.expectOne(overviewEndpoint).flush(makeOverview(overrides.overview ?? {}));
  http.expectOne(documentsEndpoint).flush(makeDocuments(overrides.documents ?? {}));
  http.expectOne(chatEndpoint).flush(makeChat(overrides.chat ?? {}));
  http.expectOne(feedbackEndpoint).flush(makeFeedback(overrides.feedback ?? {}));
}

describe('DashboardPage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should create', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);
    flushAll(http);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows loading state initially for all sections', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);

    expect(fixture.componentInstance.overviewLoading).toBe(true);
    expect(fixture.componentInstance.documentsLoading).toBe(true);
    expect(fixture.componentInstance.chatLoading).toBe(true);
    expect(fixture.componentInstance.feedbackLoading).toBe(true);

    flushAll(http);
  });

  it('clears loading and sets overview data after successful response', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);

    http.expectOne(overviewEndpoint).flush(makeOverview());
    http.expectOne(documentsEndpoint).flush(makeDocuments());
    http.expectOne(chatEndpoint).flush(makeChat());
    http.expectOne(feedbackEndpoint).flush(makeFeedback());

    expect(fixture.componentInstance.overviewLoading).toBe(false);
    expect(fixture.componentInstance.overview?.questionsAsked).toBe(10);
    expect(fixture.componentInstance.overview?.activeUsers).toBe(3);
  });

  it('sets overview error when request fails', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);

    http.expectOne(overviewEndpoint).error(new ProgressEvent('error'));
    http.expectOne(documentsEndpoint).flush(makeDocuments());
    http.expectOne(chatEndpoint).flush(makeChat());
    http.expectOne(feedbackEndpoint).flush(makeFeedback());

    expect(fixture.componentInstance.overviewLoading).toBe(false);
    expect(fixture.componentInstance.overviewError).not.toBeNull();
    expect(fixture.componentInstance.overview).toBeNull();
  });

  it('shows cost as not-available when cost.available === false', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);

    http.expectOne(overviewEndpoint).flush(makeOverview({ cost: { available: false, estimatedTotal: null } }));
    http.expectOne(documentsEndpoint).flush(makeDocuments());
    http.expectOne(chatEndpoint).flush(makeChat({ cost: { available: false, estimatedTotal: null } }));
    http.expectOne(feedbackEndpoint).flush(makeFeedback());

    expect(fixture.componentInstance.overview?.cost.available).toBe(false);
    expect(fixture.componentInstance.overview?.cost.estimatedTotal).toBeNull();

    // Trigger change detection without strict checks (the component state changed in the same tick)
    fixture.changeDetectorRef.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    const costTexts = Array.from(el.querySelectorAll('.metric-value')).map(n => n.textContent?.trim() ?? '');
    // Should NOT contain '$0.00' for unavailable cost
    expect(costTexts.some(t => t === '$0.00' || t === '$0.0000')).toBe(false);
    // Should contain "Not available"
    expect(costTexts.some(t => t.includes('Not available'))).toBe(true);
  });

  it('shows N/A for null latency fields', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);

    http.expectOne(overviewEndpoint).flush(makeOverview({ averageResponseLatencyMs: null }));
    http.expectOne(documentsEndpoint).flush(makeDocuments());
    http.expectOne(chatEndpoint).flush(makeChat({ averageResponseLatencyMs: null, retrievalLatencyMs: null }));
    http.expectOne(feedbackEndpoint).flush(makeFeedback());

    fixture.changeDetectorRef.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    const metricValues = Array.from(el.querySelectorAll('.metric-value')).map(n => n.textContent?.trim() ?? '');
    expect(metricValues.some(t => t === 'N/A')).toBe(true);
  });

  it('shows documents metrics', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);
    flushAll(http);

    expect(fixture.componentInstance.documents?.processed).toBe(10);
    expect(fixture.componentInstance.documents?.retrievalDisabled).toBe(5);
  });

  it('shows feedback counts', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);
    flushAll(http);

    expect(fixture.componentInstance.feedback?.useful).toBe(7);
    expect(fixture.componentInstance.feedback?.notUseful).toBe(3);
    expect(fixture.componentInstance.feedback?.total).toBe(10);
  });

  it('shows empty state when overview has all-zero counts', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);

    http.expectOne(overviewEndpoint).flush(makeOverview({
      questionsAsked: 0, activeUsers: 0, documentsUploaded: 0,
      usefulFeedbackCount: 0, notUsefulFeedbackCount: 0
    }));
    http.expectOne(documentsEndpoint).flush(makeDocuments());
    http.expectOne(chatEndpoint).flush(makeChat());
    http.expectOne(feedbackEndpoint).flush(makeFeedback());

    expect(fixture.componentInstance.overviewEmpty).toBe(true);
  });

  it('does not include organizationId in any request', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges(false);

    [overviewEndpoint, documentsEndpoint, chatEndpoint, feedbackEndpoint].forEach(url => {
      const req = http.expectOne(url);
      expect(req.request.params.has('organizationId')).toBe(false);
      req.flush(url === overviewEndpoint ? makeOverview() :
               url === documentsEndpoint ? makeDocuments() :
               url === chatEndpoint ? makeChat() :
               makeFeedback());
    });
  });
});
