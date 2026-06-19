import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { LoginPage } from './login-page';
import { ComponentFixture } from '@angular/core/testing';

const loginResponse = {
  accessToken: 'test-token',
  expiresAt: new Date(Date.now() + 3600_000).toISOString(),
  user: {
    userId: '1',
    email: 'agent@example.com',
    displayName: 'Agent',
    organizationId: '2',
    roles: ['Agent'],
    status: 'Active'
  }
};

describe('LoginPage', () => {
  let fixture: ComponentFixture<LoginPage>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'dashboard', redirectTo: '' }])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginPage);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('submit is disabled when form is invalid', () => {
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBe(true);
  });

  it('sets errorMessage on failed login', () => {
    const instance = fixture.componentInstance as any;

    instance.form.setValue({ email: 'agent@example.com', password: 'wrong' });
    instance.submit();

    httpMock.expectOne(r => r.url.includes('/auth/login')).flush(
      { message: 'Invalid credentials.' },
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(instance.errorMessage()).toBe('Invalid email or password.');
    expect(instance.isSubmitting()).toBe(false);
  });

  it('navigates to chat on successful login', async () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');
    const { nativeElement, componentInstance } = fixture;

    (componentInstance as any).form.setValue({ email: 'agent@example.com', password: 'correct' });
    fixture.detectChanges();

    nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    httpMock.expectOne(r => r.url.includes('/auth/login')).flush(loginResponse);
    fixture.detectChanges();

    expect(navigateSpy).toHaveBeenCalledWith('/chat');
  });
});
