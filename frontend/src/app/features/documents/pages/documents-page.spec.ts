import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { RoleVisibilityService } from '../../../core/services/role-visibility.service';
import { ManagedDocument } from '../../../core/services/document.service';
import { DocumentsPage } from './documents-page';

const makeDoc = (overrides: Partial<ManagedDocument> = {}): ManagedDocument => ({
  documentId: 'd1',
  fileName: 'knowledge.pdf',
  title: 'Knowledge Base',
  contentType: 'application/pdf',
  fileSizeBytes: 42,
  processingStatus: 'Processed',
  failureReason: null,
  isRetrievalEnabled: false,
  uploadedByUserId: 'u1',
  uploadedAt: '2026-01-01T00:00:00Z',
  processingStartedAt: null,
  processedAt: '2026-01-01T00:01:00Z',
  ...overrides
});

describe('DocumentsPage', () => {
  let httpMock: HttpTestingController;
  let canEnable: boolean;
  let canDisable: boolean;

  const makeVisibility = () => ({
    canUploadDocuments: () => false,
    canEnableDocumentRetrieval: () => canEnable,
    canDisableDocumentRetrieval: () => canDisable
  });

  beforeEach(async () => {
    canEnable = true;
    canDisable = true;
    await TestBed.configureTestingModule({
      imports: [DocumentsPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: RoleVisibilityService, useFactory: makeVisibility }
      ]
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
    fixture.componentInstance.documents = [makeDoc({ isRetrievalEnabled: true })];
    fixture.componentInstance.loading = false;
    fixture.detectChanges();
    expect(fixture.componentInstance.documents[0].title).toBe('Knowledge Base');
    expect(fixture.nativeElement.textContent).toContain('knowledge.pdf');
    expect(fixture.nativeElement.textContent).toContain('Enabled');
    httpMock.expectOne(request => request.url.endsWith('/documents')).flush([]);
  });

  it('shows Enable button for Processed+Disabled document when role allows', () => {
    const fixture = TestBed.createComponent(DocumentsPage);
    fixture.componentInstance.documents = [makeDoc({ processingStatus: 'Processed', isRetrievalEnabled: false })];
    fixture.componentInstance.loading = false;
    fixture.detectChanges();
    const buttons = fixture.nativeElement.querySelectorAll('button');
    const enableBtn = Array.from(buttons as NodeListOf<HTMLButtonElement>)
      .find(b => b.textContent?.includes('Enable'));
    expect(enableBtn).toBeTruthy();
    httpMock.expectOne(request => request.url.endsWith('/documents')).flush([]);
  });

  it('shows Disable button for Processed+Enabled document when role allows', () => {
    const fixture = TestBed.createComponent(DocumentsPage);
    fixture.componentInstance.documents = [makeDoc({ processingStatus: 'Processed', isRetrievalEnabled: true })];
    fixture.componentInstance.loading = false;
    fixture.detectChanges();
    const buttons = fixture.nativeElement.querySelectorAll('button');
    const disableBtn = Array.from(buttons as NodeListOf<HTMLButtonElement>)
      .find(b => b.textContent?.includes('Disable'));
    expect(disableBtn).toBeTruthy();
    httpMock.expectOne(request => request.url.endsWith('/documents')).flush([]);
  });

  it('does not show action buttons for Failed document', () => {
    const fixture = TestBed.createComponent(DocumentsPage);
    fixture.componentInstance.documents = [makeDoc({ processingStatus: 'Failed', isRetrievalEnabled: false })];
    fixture.componentInstance.loading = false;
    fixture.detectChanges();
    const buttons = fixture.nativeElement.querySelectorAll('button');
    const actionBtns = Array.from(buttons as NodeListOf<HTMLButtonElement>)
      .filter(b => b.textContent?.includes('Enable') || b.textContent?.includes('Disable'));
    expect(actionBtns.length).toBe(0);
    httpMock.expectOne(request => request.url.endsWith('/documents')).flush([]);
  });

  it('calls enable endpoint and updates row on success', () => {
    const fixture = TestBed.createComponent(DocumentsPage);
    const doc = makeDoc({ processingStatus: 'Processed', isRetrievalEnabled: false });
    fixture.componentInstance.documents = [doc];
    fixture.componentInstance.loading = false;
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.endsWith('/documents')).flush([doc]);

    const buttons = fixture.nativeElement.querySelectorAll('button');
    const enableBtn = Array.from(buttons as NodeListOf<HTMLButtonElement>)
      .find(b => b.textContent?.includes('Enable'));
    enableBtn!.click();

    const req = httpMock.expectOne(r => r.url.endsWith('/documents/d1/enable'));
    expect(req.request.method).toBe('POST');
    req.flush({ ...doc, isRetrievalEnabled: true });

    expect(fixture.componentInstance.documents[0].isRetrievalEnabled).toBe(true);
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
