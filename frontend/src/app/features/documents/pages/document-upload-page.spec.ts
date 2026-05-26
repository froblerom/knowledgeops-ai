import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';
import { DocumentUploadPage } from './document-upload-page';
import { AuthSessionService } from '../../../core/services/auth-session.service';

function makeFile(name: string, type: string, size = 1024): File {
  return new File([new ArrayBuffer(size)], name, { type });
}

describe('DocumentUploadPage', () => {
  let httpMock: HttpTestingController;

  function setup(roles: string[] = ['KnowledgeAdmin']) {
    TestBed.configureTestingModule({
      imports: [DocumentUploadPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            getSession: () => ({ roles }),
            isAuthenticated: () => roles.length > 0
          }
        }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    return TestBed.createComponent(DocumentUploadPage);
  }

  afterEach(() => httpMock.verify());

  it('renders upload form for KnowledgeAdmin', () => {
    const fixture = setup(['KnowledgeAdmin']);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
  });

  it('renders upload form for Admin', () => {
    const fixture = setup(['Admin']);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
  });

  it('shows access-denied message for Agent', () => {
    const fixture = setup(['Agent']);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('permission');
  });

  it('shows access-denied message for Supervisor', () => {
    const fixture = setup(['Supervisor']);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
  });

  it('shows access-denied message for Manager', () => {
    const fixture = setup(['Manager']);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
  });

  it('shows title required error on submit with empty title', async () => {
    const fixture = setup();
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.selectedFile = makeFile('policy.pdf', 'application/pdf');
    await fixture.whenStable();

    component.onSubmit();
    fixture.detectChanges();

    expect(component.validationErrors).toContain('Title is required.');
  });

  it('shows file required error on submit with no file', async () => {
    const fixture = setup();
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.title = 'My Policy';
    await fixture.whenStable();

    component.onSubmit();
    fixture.detectChanges();

    expect(component.validationErrors).toContain('File is required.');
  });

  it('shows unsupported extension error on submit', async () => {
    const fixture = setup();
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.title = 'My Policy';
    component.selectedFile = makeFile('malware.exe', 'application/octet-stream');
    await fixture.whenStable();

    component.onSubmit();
    fixture.detectChanges();

    expect(component.validationErrors.some(e => e.toLowerCase().includes('not supported'))).toBeTruthy();
  });

  it('shows oversized file error on submit', async () => {
    const fixture = setup();
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.title = 'Big File';
    component.selectedFile = makeFile('big.pdf', 'application/pdf', 10 * 1024 * 1024 + 1);
    await fixture.whenStable();

    component.onSubmit();
    fixture.detectChanges();

    expect(component.validationErrors.some(e => e.includes('10 MB'))).toBeTruthy();
  });

  it('upload() posts FormData to POST /api/v1/documents', () => {
    const fixture = setup();
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.title = 'Policy';
    component.selectedFile = makeFile('policy.pdf', 'application/pdf');
    component.onSubmit();

    const req = httpMock.expectOne(req => req.url.endsWith('/documents') && req.method === 'POST');
    expect(req.request.body instanceof FormData).toBeTruthy();
    req.flush({ documentId: 'doc-1', processingStatus: 'Uploaded', isRetrievalEnabled: false });
  });

  it('does not display storageLocation in template', () => {
    const fixture = setup();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent.toLowerCase()).not.toContain('storagelocation');
    expect(fixture.nativeElement.textContent.toLowerCase()).not.toContain('local://');
  });

  it('does not include text extraction or RAG elements in template', () => {
    const fixture = setup();
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent.toLowerCase();
    expect(text).not.toContain('extract');
    expect(text).not.toContain('chunk');
    expect(text).not.toContain('embed');
    expect(text).not.toContain('retrieval');
    expect(text).not.toContain('rag');
  });

  it('shows api error message on upload failure', async () => {
    const fixture = setup();
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.title = 'Policy';
    component.selectedFile = makeFile('policy.pdf', 'application/pdf');
    component.onSubmit();

    httpMock.expectOne(req => req.url.endsWith('/documents') && req.method === 'POST').flush(
      { error: { correlationId: 'err-1' } },
      { status: 503, statusText: 'Service Unavailable' }
    );

    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.error).not.toBeNull();
    expect(component.submitting).toBeFalsy();
  });
});
