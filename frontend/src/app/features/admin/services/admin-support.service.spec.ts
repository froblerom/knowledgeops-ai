import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminSupportService } from './admin-support.service';

describe('AdminSupportService', () => {
  let service: AdminSupportService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AdminSupportService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getProcessingFailures calls expected endpoint', () => {
    service.getProcessingFailures().subscribe();

    const request = http.expectOne(req => req.url.endsWith('/admin/processing-failures'));
    expect(request.request.method).toBe('GET');
  });

  it('getAuditLog calls expected endpoint with filters', () => {
    service.getAuditLog({
      from: '2026-06-01T00:00:00Z',
      to: '2026-06-01T23:59:59Z',
      eventType: 'DocumentUploadAccepted'
    }).subscribe();

    const request = http.expectOne(req => req.url.endsWith('/admin/audit-log'));
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('from')).toBe('2026-06-01T00:00:00Z');
    expect(request.request.params.get('to')).toBe('2026-06-01T23:59:59Z');
    expect(request.request.params.get('eventType')).toBe('DocumentUploadAccepted');
  });
});
