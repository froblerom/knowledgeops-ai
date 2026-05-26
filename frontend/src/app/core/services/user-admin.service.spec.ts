import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserAdminService } from './user-admin.service';

describe('UserAdminService', () => {
  let service: UserAdminService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(UserAdminService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses the protected users API for list and role mutations', () => {
    service.list().subscribe();
    expect(http.expectOne(request => request.url.endsWith('/users')).request.method).toBe('GET');

    service.addRole('user-1', 'Admin').subscribe();
    const add = http.expectOne(request => request.url.endsWith('/users/user-1/roles'));
    expect(add.request.method).toBe('POST');
    expect(add.request.body).toEqual({ roleName: 'Admin' });

    service.removeRole('user-1', 'Admin').subscribe();
    expect(http.expectOne(request => request.url.endsWith('/users/user-1/roles/Admin')).request.method).toBe('DELETE');
  });

  it('sends initialPassword only on create', () => {
    service.create({
      displayName: 'New User',
      email: 'new@example.test',
      status: 'Pending',
      roles: [],
      initialPassword: 'bootstrap'
    }).subscribe();

    const create = http.expectOne(request => request.url.endsWith('/users'));
    expect(create.request.body.initialPassword).toBe('bootstrap');
    expect(create.request.body.organizationId).toBeUndefined();
  });
});
