import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
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
  let canEnable: boolean;
  const visibility = {
    canDisableDocumentRetrieval: () => canDisable,
    canEnableDocumentRetrieval: () => canEnable
  };
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
    canEnable = true;
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

  it('shows enable action for Processed+Disabled document and calls canonical route', () => {
    const processedDisabledDoc: ManagedDocument = {
      ...document, processingStatus: 'Processed', isRetrievalEnabled: false, failureReason: null
    };
    const processedDisabledStatus: DocumentProcessingStatusResponse = {
      ...status, processingStatus: 'Processed', isRetrievalEnabled: false, failureReason: null,
      processedAt: '2026-01-01T00:02:00Z'
    };
    const fixture = TestBed.createComponent(DocumentDetailPage);
    fixture.componentInstance.document = processedDisabledDoc;
    fixture.componentInstance.status = processedDisabledStatus;
    fixture.componentInstance.loading = false;
    fixture.detectChanges();
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(processedDisabledDoc);
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(processedDisabledStatus);

    const enableBtn = fixture.nativeElement.querySelector('button');
    expect(enableBtn).not.toBeNull();
    expect(enableBtn.textContent).toContain('Enable retrieval');
    enableBtn.click();

    const request = httpMock.expectOne(req => req.url.endsWith('/documents/doc-1/enable'));
    expect(request.request.method).toBe('POST');
    request.flush({ ...processedDisabledDoc, isRetrievalEnabled: true });

    expect(fixture.componentInstance.document?.isRetrievalEnabled).toBe(true);
  });

  it('shows disable action for Processed+Enabled document and calls canonical route', () => {
    const processedEnabledDoc: ManagedDocument = {
      ...document, processingStatus: 'Processed', isRetrievalEnabled: true, failureReason: null
    };
    const processedEnabledStatus: DocumentProcessingStatusResponse = {
      ...status, processingStatus: 'Processed', isRetrievalEnabled: true, failureReason: null,
      processedAt: '2026-01-01T00:02:00Z'
    };
    const fixture = TestBed.createComponent(DocumentDetailPage);
    fixture.componentInstance.document = processedEnabledDoc;
    fixture.componentInstance.status = processedEnabledStatus;
    fixture.componentInstance.loading = false;
    fixture.detectChanges();
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(processedEnabledDoc);
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(processedEnabledStatus);

    const disableBtn = fixture.nativeElement.querySelector('button');
    expect(disableBtn).not.toBeNull();
    expect(disableBtn.textContent).toContain('Disable retrieval');
    disableBtn.click();

    const request = httpMock.expectOne(req => req.url.endsWith('/documents/doc-1/disable'));
    expect(request.request.method).toBe('POST');
    request.flush({ ...processedEnabledDoc, isRetrievalEnabled: false });

    expect(fixture.componentInstance.document?.isRetrievalEnabled).toBe(false);
  });

  it('shows retrieval hint for Failed document instead of action buttons', () => {
    canEnable = true;
    canDisable = true;
    const fixture = TestBed.createComponent(DocumentDetailPage);
    fixture.componentInstance.document = document;
    fixture.componentInstance.status = status;
    fixture.componentInstance.loading = false;
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('button')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Only processed documents can be enabled');
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(document);
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(status);
  });

  it('hides action buttons when role denies both enable and disable', () => {
    canEnable = false;
    canDisable = false;
    const processedDoc: ManagedDocument = {
      ...document, processingStatus: 'Processed', failureReason: null
    };
    const processedStatus: DocumentProcessingStatusResponse = {
      ...status, processingStatus: 'Processed', failureReason: null,
      processedAt: '2026-01-01T00:02:00Z'
    };
    const fixture = TestBed.createComponent(DocumentDetailPage);
    fixture.componentInstance.document = processedDoc;
    fixture.componentInstance.status = processedStatus;
    fixture.componentInstance.loading = false;
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('button')).toBeNull();
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(processedDoc);
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(processedStatus);
  });

  it('does not poll when initial status is terminal (Failed)', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    try {
      const fixture = TestBed.createComponent(DocumentDetailPage);
      fixture.detectChanges();

      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(document);
      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(status);

      await vi.advanceTimersByTimeAsync(10000);
      httpMock.expectNone(r => r.url.endsWith('/documents/doc-1/processing-status'));

      fixture.componentInstance.ngOnDestroy();
    } finally {
      vi.useRealTimers();
    }
  });

  it('does not poll when initial status is terminal (Processed)', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    try {
      const processedStatus: DocumentProcessingStatusResponse = {
        ...status,
        processingStatus: 'Processed',
        failureReason: null,
        processedAt: '2026-01-01T00:02:00Z'
      };
      const processedDocument: ManagedDocument = { ...document, processingStatus: 'Processed' };

      const fixture = TestBed.createComponent(DocumentDetailPage);
      fixture.detectChanges();

      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(processedDocument);
      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(processedStatus);

      await vi.advanceTimersByTimeAsync(10000);
      httpMock.expectNone(r => r.url.endsWith('/documents/doc-1/processing-status'));

      fixture.componentInstance.ngOnDestroy();
    } finally {
      vi.useRealTimers();
    }
  });

  it('starts polling when initial status is Uploaded and stops on Processed', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    try {
      const uploadedStatus: DocumentProcessingStatusResponse = {
        ...status,
        processingStatus: 'Uploaded',
        failureReason: null
      };
      const uploadedDocument: ManagedDocument = { ...document, processingStatus: 'Uploaded', isRetrievalEnabled: false };

      const fixture = TestBed.createComponent(DocumentDetailPage);
      fixture.detectChanges();

      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(uploadedDocument);
      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(uploadedStatus);

      // First poll tick
      await vi.advanceTimersByTimeAsync(5000);
      const poll1 = httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status'));
      poll1.flush({ ...uploadedStatus, processingStatus: 'Processing' });

      // Second poll tick — terminal response stops polling
      await vi.advanceTimersByTimeAsync(5000);
      const poll2 = httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status'));
      poll2.flush({ ...uploadedStatus, processingStatus: 'Processed', processedAt: '2026-01-01T00:02:00Z', failureReason: null });

      await vi.advanceTimersByTimeAsync(10000);
      httpMock.expectNone(r => r.url.endsWith('/documents/doc-1/processing-status'));

      fixture.componentInstance.ngOnDestroy();
    } finally {
      vi.useRealTimers();
    }
  });

  it('starts polling when initial status is Processing and stops on Failed', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    try {
      const processingStatus: DocumentProcessingStatusResponse = {
        ...status,
        processingStatus: 'Processing',
        failureReason: null
      };
      const processingDocument: ManagedDocument = { ...document, processingStatus: 'Processing', isRetrievalEnabled: false };

      const fixture = TestBed.createComponent(DocumentDetailPage);
      fixture.detectChanges();

      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(processingDocument);
      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(processingStatus);

      await vi.advanceTimersByTimeAsync(5000);
      const poll = httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status'));
      poll.flush({ ...processingStatus, processingStatus: 'Failed', failureReason: 'Unsupported encoding.' });

      await vi.advanceTimersByTimeAsync(10000);
      httpMock.expectNone(r => r.url.endsWith('/documents/doc-1/processing-status'));

      fixture.componentInstance.ngOnDestroy();
    } finally {
      vi.useRealTimers();
    }
  });

  it('hides failure reason when status is not Failed', () => {
    const uploadedStatus: DocumentProcessingStatusResponse = {
      ...status,
      processingStatus: 'Uploaded',
      failureReason: null
    };

    const fixture = TestBed.createComponent(DocumentDetailPage);
    fixture.componentInstance.document = { ...document, processingStatus: 'Uploaded' };
    fixture.componentInstance.status = uploadedStatus;
    fixture.componentInstance.loading = false;
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Failure reason');

    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(document);
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(uploadedStatus);
    fixture.componentInstance.ngOnDestroy(); // clean up polling
  });

  it('shows failure reason when status is Failed', () => {
    const failedStatus: DocumentProcessingStatusResponse = {
      ...status,
      processingStatus: 'Failed',
      failureReason: 'Bad encoding.'
    };

    const fixture = TestBed.createComponent(DocumentDetailPage);
    fixture.componentInstance.document = { ...document, processingStatus: 'Failed' };
    fixture.componentInstance.status = failedStatus;
    fixture.componentInstance.loading = false;
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Failure reason');
    expect(fixture.nativeElement.textContent).toContain('Bad encoding.');

    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(document);
    httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(failedStatus);
  });

  it('cancels polling on ngOnDestroy before terminal status', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    try {
      const uploadedStatus: DocumentProcessingStatusResponse = {
        ...status,
        processingStatus: 'Uploaded',
        failureReason: null
      };
      const uploadedDocument: ManagedDocument = { ...document, processingStatus: 'Uploaded', isRetrievalEnabled: false };

      const fixture = TestBed.createComponent(DocumentDetailPage);
      fixture.detectChanges();

      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1')).flush(uploadedDocument);
      httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status')).flush(uploadedStatus);

      await vi.advanceTimersByTimeAsync(5000);
      const poll = httpMock.expectOne(r => r.url.endsWith('/documents/doc-1/processing-status'));
      poll.flush(uploadedStatus); // still pending

      // Destroy before the next poll fires
      fixture.componentInstance.ngOnDestroy();

      await vi.advanceTimersByTimeAsync(10000);
      httpMock.expectNone(r => r.url.endsWith('/documents/doc-1/processing-status'));
    } finally {
      vi.useRealTimers();
    }
  });
});
