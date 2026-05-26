import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { RoleVisibilityService } from '../../../core/services/role-visibility.service';
import {
  DocumentProcessingStatusResponse,
  ManagedDocument
} from '../../../core/services/document.service';
import { DocumentDetailPage } from './document-detail-page';

describe('DocumentDetailPage', () => {
  let httpMock: HttpTestingController;
  let canDisable: boolean;
  const visibility = { canDisableDocumentRetrieval: () => canDisable };
  const document: ManagedDocument = {
    documentId: 'doc-1',
    fileName: 'policy.pdf',
    title: 'Knowledge Policy',
    contentType: 'application/pdf',
    fileSizeBytes: 42,
    processingStatus: 'Failed',
    failureReason: 'Extraction failed.',
    isRetrievalEnabled: true,
    uploadedByUserId: 'user-1',
    uploadedAt: '2026-01-01T00:00:00Z',
    processingStartedAt: null,
    processedAt: null
  };
  const status: DocumentProcessingStatusResponse = {
    documentId: 'doc-1',
    processingStatus: 'Failed',
    failureReason: 'Extraction failed.',
    isRetrievalEnabled: true,
    uploadedAt: '2026-01-01T00:00:00Z',
    processingStartedAt: '2026-01-01T00:01:00Z',
    processedAt: null
  };

  beforeEach(async () => {
    canDisable = true;
    await TestBed.configureTestingModule({
      imports: [DocumentDetailPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ documentId: 'doc-1' }) } }
        },
        { provide: RoleVisibilityService, useValue: visibility }
      ]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('renders canonical metadata, status, failure reason, and no upload input', () => {
    const fixture = TestBed.createComponent(DocumentDetailPage);
    fixture.componentInstance.document = document;
    fixture.componentInstance.status = status;
    fixture.componentInstance.loading = false;
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Knowledge Policy');
    expect(text).toContain('policy.pdf');
    expect(text).toContain('application/pdf');
    expect(text).toContain('Failed');
    expect(text).toContain('Extraction failed.');
    expect(fixture.nativeElement.querySelector('input[type="file"]')).toBeNull();
    httpMock.expectOne(request => request.url.endsWith('/documents/doc-1')).flush(document);
    httpMock.expectOne(request => request.url.endsWith('/documents/doc-1/processing-status')).flush(status);
  });

  it('shows disable action for permitted role and calls canonical route', () => {
    const fixture = TestBed.createComponent(DocumentDetailPage);
    fixture.componentInstance.document = document;
    fixture.componentInstance.status = status;
    fixture.componentInstance.loading = false;
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.endsWith('/documents/doc-1')).flush(document);
    httpMock.expectOne(request => request.url.endsWith('/documents/doc-1/processing-status')).flush(status);

    fixture.nativeElement.querySelector('button').click();
    const request = httpMock.expectOne(req => req.url.endsWith('/documents/doc-1/disable'));
    expect(request.request.method).toBe('POST');
    request.flush({ ...document, isRetrievalEnabled: false });

    expect(fixture.componentInstance.document?.isRetrievalEnabled).toBe(false);
  });

  it('hides disable action when role helper denies it', () => {
    canDisable = false;
    const fixture = TestBed.createComponent(DocumentDetailPage);
    fixture.componentInstance.document = document;
    fixture.componentInstance.status = status;
    fixture.componentInstance.loading = false;
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('button')).toBeNull();
    httpMock.expectOne(request => request.url.endsWith('/documents/doc-1')).flush(document);
    httpMock.expectOne(request => request.url.endsWith('/documents/doc-1/processing-status')).flush(status);
  });
});
