import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminPage } from './admin-page';

describe('AdminPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(AdminPage);
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.endsWith('/users')).flush([]);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('stores scoped users returned by the API for display', () => {
    const fixture = TestBed.createComponent(AdminPage);
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.endsWith('/users')).flush([{
      userId: 'u1',
      displayName: 'Admin User',
      email: 'admin@example.test',
      organizationId: 'o1',
      status: 'Active',
      roles: ['Admin'],
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z',
      lastLoginAt: null
    }]);
    expect(fixture.componentInstance.users[0].email).toBe('admin@example.test');
    expect(fixture.componentInstance.users[0].status).toBe('Active');
    expect(fixture.componentInstance.loading).toBe(false);
  });

  it('passes the safe Error ID to its error state on failed loading', () => {
    const fixture = TestBed.createComponent(AdminPage);
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.endsWith('/users')).flush(
      { error: { correlationId: 'safe-error-id' } },
      { status: 403, statusText: 'Forbidden' }
    );
    expect(fixture.componentInstance.error?.errorId).toBe('safe-error-id');
    expect(fixture.componentInstance.error?.message).toContain('You do not have access');
  });
});
