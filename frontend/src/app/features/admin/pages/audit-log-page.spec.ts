import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { AuditLogEntry } from '../models/admin-support.models';
import { AdminSupportService } from '../services/admin-support.service';
import { AuditLogPage } from './audit-log-page';

describe('AuditLogPage', () => {
  let entries$: Observable<AuditLogEntry[]>;

  beforeEach(async () => {
    entries$ = of([]);
    await TestBed.configureTestingModule({
      imports: [AuditLogPage],
      providers: [
        {
          provide: AdminSupportService,
          useValue: {
            getAuditLog: () => entries$
          }
        }
      ]
    }).compileComponents();
  });

  it('shows loading state', () => {
    entries$ = NEVER;
    const fixture = TestBed.createComponent(AuditLogPage);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Loading');
  });

  it('shows error state', () => {
    entries$ = throwError(() => new HttpErrorResponse({
      status: 403,
      statusText: 'Forbidden',
      headers: new HttpHeaders({ 'X-Correlation-ID': 'audit-err' })
    }));
    const fixture = TestBed.createComponent(AuditLogPage);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('You do not have access');
    expect(fixture.nativeElement.textContent).toContain('audit-err');
  });

  it('shows empty state', () => {
    entries$ = of([]);
    const fixture = TestBed.createComponent(AuditLogPage);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No audit log entries found.');
  });

  it('renders safe audit fields', () => {
    entries$ = of([
      {
        auditLogEntryId: 'audit-1',
        eventType: 'DocumentUploadAccepted',
        message: 'Document upload accepted. DocumentId=doc-1.',
        severity: 'Info',
        userId: 'user-1',
        entityType: 'Document',
        entityId: 'doc-1',
        correlationId: 'safe-correlation',
        createdAt: '2026-06-01T12:00:00Z'
      }
    ]);
    const fixture = TestBed.createComponent(AuditLogPage);

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('DocumentUploadAccepted');
    expect(text).toContain('Document upload accepted');
    expect(text).toContain('safe-correlation');
    expect(text).not.toContain('provider payload');
    expect(text).not.toContain('chunk text');
    expect(text).not.toContain('prompt text');
  });
});
