import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../../environments/environment';
import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  let service: DashboardService;
  let http: HttpTestingController;

  const overviewEndpoint = `${environment.apiBaseUrl}/dashboard/overview`;
  const documentsEndpoint = `${environment.apiBaseUrl}/dashboard/documents`;
  const chatEndpoint = `${environment.apiBaseUrl}/dashboard/chat`;
  const feedbackEndpoint = `${environment.apiBaseUrl}/dashboard/feedback`;

  const makePeriod = () => ({
    from: new Date(Date.now() - 30 * 86400_000).toISOString(),
    to: new Date().toISOString()
  });

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(DashboardService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // ── Overview ────────────────────────────────────────────────────────────────

  it('calls GET /dashboard/overview without params by default', () => {
    service.getOverview().subscribe();

    const req = http.expectOne(overviewEndpoint);
    expect(req.request.method).toBe('GET');
    req.flush({
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
      cost: { available: false, estimatedTotal: null }
    });
  });

  it('calls GET /dashboard/overview with from/to params when provided', () => {
    service.getOverview({ from: '2026-01-01', to: '2026-01-31' }).subscribe();

    const req = http.expectOne(r => r.url === overviewEndpoint);
    expect(req.request.params.get('from')).toBe('2026-01-01');
    expect(req.request.params.get('to')).toBe('2026-01-31');
    req.flush({
      period: { from: '2026-01-01T00:00:00Z', to: '2026-01-31T00:00:00Z' },
      questionsAsked: 0, activeUsers: 0,
      documentsUploaded: 0, documentsProcessed: 0, documentsFailed: 0,
      averageResponseLatencyMs: null, insufficientContextCount: 0,
      providerFailureCount: 0, usefulFeedbackCount: 0, notUsefulFeedbackCount: 0,
      cost: { available: false, estimatedTotal: null }
    });
  });

  it('parses overview response with null cost as unavailable', () => {
    let costAvailable: boolean | undefined;
    let costTotal: number | null | undefined;

    service.getOverview().subscribe(r => {
      costAvailable = r.cost.available;
      costTotal = r.cost.estimatedTotal;
    });

    http.expectOne(overviewEndpoint).flush({
      period: makePeriod(),
      questionsAsked: 5, activeUsers: 2,
      documentsUploaded: 3, documentsProcessed: 3, documentsFailed: 0,
      averageResponseLatencyMs: null, insufficientContextCount: 0,
      providerFailureCount: 0, usefulFeedbackCount: 1, notUsefulFeedbackCount: 0,
      cost: { available: false, estimatedTotal: null }
    });

    expect(costAvailable).toBe(false);
    expect(costTotal).toBeNull();
  });

  // ── Documents ────────────────────────────────────────────────────────────────

  it('calls GET /dashboard/documents', () => {
    service.getDocuments().subscribe();

    const req = http.expectOne(documentsEndpoint);
    expect(req.request.method).toBe('GET');
    req.flush({
      period: makePeriod(),
      uploaded: 2, processing: 1, processed: 10, failed: 3, retrievalDisabled: 5
    });
  });

  it('parses documents response counts', () => {
    let processed: number | undefined;

    service.getDocuments().subscribe(r => {
      processed = r.processed;
    });

    http.expectOne(documentsEndpoint).flush({
      period: makePeriod(),
      uploaded: 2, processing: 1, processed: 10, failed: 3, retrievalDisabled: 5
    });

    expect(processed).toBe(10);
  });

  // ── Chat ──────────────────────────────────────────────────────────────────

  it('calls GET /dashboard/chat', () => {
    service.getChat().subscribe();

    const req = http.expectOne(chatEndpoint);
    expect(req.request.method).toBe('GET');
    req.flush({
      period: makePeriod(),
      questionsAsked: 5, activeUsers: 2,
      averageResponseLatencyMs: null, retrievalLatencyMs: null,
      generationLatencyMs: null, totalRagLatencyMs: null,
      insufficientContextCount: 0, providerFailureCount: 0,
      tokens: { input: null, output: null, total: null },
      cost: { available: false, estimatedTotal: null }
    });
  });

  it('parses chat response with null latency as null', () => {
    let latency: number | null | undefined;

    service.getChat().subscribe(r => {
      latency = r.averageResponseLatencyMs;
    });

    http.expectOne(chatEndpoint).flush({
      period: makePeriod(),
      questionsAsked: 0, activeUsers: 0,
      averageResponseLatencyMs: null, retrievalLatencyMs: null,
      generationLatencyMs: null, totalRagLatencyMs: null,
      insufficientContextCount: 0, providerFailureCount: 0,
      tokens: { input: null, output: null, total: null },
      cost: { available: false, estimatedTotal: null }
    });

    expect(latency).toBeNull();
  });

  it('does not include organizationId in request body or params', () => {
    service.getOverview().subscribe();

    const req = http.expectOne(overviewEndpoint);
    expect(req.request.params.has('organizationId')).toBe(false);
    req.flush({
      period: makePeriod(), questionsAsked: 0, activeUsers: 0,
      documentsUploaded: 0, documentsProcessed: 0, documentsFailed: 0,
      averageResponseLatencyMs: null, insufficientContextCount: 0,
      providerFailureCount: 0, usefulFeedbackCount: 0, notUsefulFeedbackCount: 0,
      cost: { available: false, estimatedTotal: null }
    });
  });

  // ── Feedback ──────────────────────────────────────────────────────────────

  it('calls GET /dashboard/feedback', () => {
    service.getFeedback().subscribe();

    const req = http.expectOne(feedbackEndpoint);
    expect(req.request.method).toBe('GET');
    req.flush({ period: makePeriod(), useful: 7, notUseful: 3, total: 10 });
  });

  it('parses feedback totals correctly', () => {
    let total: number | undefined;

    service.getFeedback().subscribe(r => {
      total = r.total;
    });

    http.expectOne(feedbackEndpoint).flush({
      period: makePeriod(), useful: 7, notUseful: 3, total: 10
    });

    expect(total).toBe(10);
  });
});
