import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DocumentService } from './document.service';
import { environment } from '../../../environments/environment';

describe('DocumentService', () => {
  let service: DocumentService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiBaseUrl}/documents`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), DocumentService]
    });
    service = TestBed.inject(DocumentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() calls GET /documents', () => {
    service.list().subscribe();
    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('get() calls GET /documents/:id', () => {
    service.get('doc-1').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/doc-1`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('getProcessingStatus() calls GET /documents/:id/processing-status', () => {
    service.getProcessingStatus('doc-1').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/doc-1/processing-status`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('disableRetrieval() calls POST /documents/:id/disable', () => {
    service.disableRetrieval('doc-1').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/doc-1/disable`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
