import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { DocumentsPage } from './documents-page';

describe('DocumentsPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentsPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(DocumentsPage);
    fixture.detectChanges(false);
    httpMock.expectOne(request => request.url.endsWith('/documents')).flush([]);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders canonical document metadata and retrieval state', () => {
    const fixture = TestBed.createComponent(DocumentsPage);
    fixture.componentInstance.documents = [
      {
        documentId: 'd1',
        fileName: 'knowledge.pdf',
        title: 'Knowledge Base',
        contentType: 'application/pdf',
        fileSizeBytes: 42,
        processingStatus: 'Processed',
        failureReason: null,
        isRetrievalEnabled: true,
        uploadedByUserId: 'u1',
        uploadedAt: '2026-01-01T00:00:00Z',
        processingStartedAt: null,
        processedAt: '2026-01-01T00:01:00Z'
      }
    ];
    fixture.componentInstance.loading = false;
    fixture.detectChanges();
    expect(fixture.componentInstance.documents[0].title).toBe('Knowledge Base');
    expect(fixture.nativeElement.textContent).toContain('knowledge.pdf');
    expect(fixture.nativeElement.textContent).toContain('Enabled');
    httpMock.expectOne(request => request.url.endsWith('/documents')).flush([]);
  });

  it('shows error state on API failure', () => {
    const fixture = TestBed.createComponent(DocumentsPage);
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.endsWith('/documents')).flush(
      { error: { correlationId: 'err-id' } },
      { status: 403, statusText: 'Forbidden' }
    );
    expect(fixture.componentInstance.error).not.toBeNull();
    expect(fixture.componentInstance.loading).toBe(false);
  });
});
