import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { ProcessingFailure } from '../models/admin-support.models';
import { AdminSupportService } from '../services/admin-support.service';
import { ProcessingFailuresPage } from './processing-failures-page';

describe('ProcessingFailuresPage', () => {
  let failures$: Observable<ProcessingFailure[]>;

  beforeEach(async () => {
    failures$ = of([]);
    await TestBed.configureTestingModule({
      imports: [ProcessingFailuresPage],
      providers: [
        {
          provide: AdminSupportService,
          useValue: {
            getProcessingFailures: () => failures$
          }
        }
      ]
    }).compileComponents();
  });

  it('shows loading state', () => {
    failures$ = NEVER;
    const fixture = TestBed.createComponent(ProcessingFailuresPage);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Loading');
  });

  it('shows error state', () => {
    failures$ = throwError(() => new HttpErrorResponse({
      status: 403,
      statusText: 'Forbidden',
      headers: new HttpHeaders({ 'X-Correlation-ID': 'err-1' })
    }));
    const fixture = TestBed.createComponent(ProcessingFailuresPage);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('You do not have access');
    expect(fixture.nativeElement.textContent).toContain('err-1');
  });

  it('shows empty state', () => {
    failures$ = of([]);
    const fixture = TestBed.createComponent(ProcessingFailuresPage);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No processing failures found.');
  });

  it('renders safe failure fields', () => {
    failures$ = of([
      {
        documentId: 'doc-1',
        title: 'Example policy',
        processingStatus: 'Failed',
        failureReason: 'TextExtractionFailed',
        failedAt: '2026-06-01T12:00:00Z'
      }
    ]);
    const fixture = TestBed.createComponent(ProcessingFailuresPage);

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Example policy');
    expect(text).toContain('TextExtractionFailed');
    expect(text).not.toContain('storageLocation');
    expect(text).not.toContain('local://');
  });
});
