import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { AdminUserDetailPage } from './admin-user-detail-page';

describe('AdminUserDetailPage', () => {
  let http: HttpTestingController;

  const user = {
    userId: 'user-1',
    displayName: 'Agent One',
    email: 'agent@example.test',
    organizationId: 'org-1',
    status: 'Active',
    roles: ['Agent'],
    createdAt: '2026-06-01T00:00:00Z',
    updatedAt: '2026-06-01T00:00:00Z',
    lastLoginAt: null
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminUserDetailPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ userId: 'user-1' }) } }
        }
      ]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads user detail and stores scoped metadata and roles', () => {
    const fixture = TestBed.createComponent(AdminUserDetailPage);
    fixture.componentInstance.ngOnInit();

    const request = http.expectOne(req => req.url.endsWith('/users/user-1'));
    expect(request.request.method).toBe('GET');
    request.flush(user);

    expect(fixture.componentInstance.loading).toBe(false);
    expect(fixture.componentInstance.form.getRawValue().email).toBe('agent@example.test');
    expect(fixture.componentInstance.user?.displayName).toBe('Agent One');
    expect(fixture.componentInstance.user?.organizationId).toBe('org-1');
    expect(fixture.componentInstance.user?.roles).toEqual(['Agent']);
  });

  it('updates editable fields without submitting organization, roles, or password data', () => {
    const fixture = TestBed.createComponent(AdminUserDetailPage);
    fixture.componentInstance.ngOnInit();
    http.expectOne(req => req.url.endsWith('/users/user-1')).flush(user);

    fixture.componentInstance.form.patchValue({
      displayName: 'Agent Updated',
      email: 'updated@example.test',
      status: 'Disabled'
    });
    fixture.componentInstance.save();

    const request = http.expectOne(req => req.url.endsWith('/users/user-1'));
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      displayName: 'Agent Updated',
      email: 'updated@example.test',
      status: 'Disabled'
    });
    expect(request.request.body.organizationId).toBeUndefined();
    expect(request.request.body.roles).toBeUndefined();
    expect(request.request.body.initialPassword).toBeUndefined();
    request.flush({ ...user, displayName: 'Agent Updated', email: 'updated@example.test', status: 'Disabled' });

    expect(fixture.componentInstance.saving).toBe(false);
    expect(fixture.componentInstance.user?.status).toBe('Disabled');
  });

  it('adds and removes roles through canonical role endpoints', () => {
    const fixture = TestBed.createComponent(AdminUserDetailPage);
    fixture.componentInstance.ngOnInit();
    http.expectOne(req => req.url.endsWith('/users/user-1')).flush(user);

    fixture.componentInstance.form.patchValue({ role: 'Manager' });
    fixture.componentInstance.addRole();
    const add = http.expectOne(req => req.url.endsWith('/users/user-1/roles'));
    expect(add.request.method).toBe('POST');
    expect(add.request.body).toEqual({ roleName: 'Manager' });
    add.flush({ ...user, roles: ['Agent', 'Manager'] });

    fixture.componentInstance.removeRole('Agent');
    const remove = http.expectOne(req => req.url.endsWith('/users/user-1/roles/Agent'));
    expect(remove.request.method).toBe('DELETE');
    remove.flush({ ...user, roles: ['Manager'] });

    expect(fixture.componentInstance.user?.roles).toEqual(['Manager']);
  });

  it('surfaces safe error IDs for failed detail loading', () => {
    const fixture = TestBed.createComponent(AdminUserDetailPage);
    fixture.componentInstance.ngOnInit();

    http.expectOne(req => req.url.endsWith('/users/user-1')).flush(
      { error: { correlationId: 'user-detail-error' } },
      { status: 404, statusText: 'Not Found' }
    );

    expect(fixture.componentInstance.loading).toBe(false);
    expect(fixture.componentInstance.error?.errorId).toBe('user-detail-error');
    expect(fixture.componentInstance.error?.message).toContain('could not be found');
  });
});
