import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { RoleVisibilityService } from './core/services/role-visibility.service';
import { App } from './app';

describe('App', () => {
  let roles: string[];

  beforeEach(async () => {
    roles = [];
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => true,
            currentUser: () => ({ displayName: 'Test User' })
          }
        },
        {
          provide: RoleVisibilityService,
          useValue: {
            canAskChat: () =>
              roles.some(role =>
                ['Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin'].includes(role)
              ),
            canViewChatHistory: () =>
              roles.some(role =>
                ['Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin'].includes(role)
              ),
            canViewDocuments: () => false,
            canViewDashboard: () => false,
            canViewProcessingFailures: () => roles.some(role => ['KnowledgeAdmin', 'Admin'].includes(role)),
            canViewAuditLog: () => roles.includes('Admin'),
            canViewAdmin: () => false
          }
        }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the application title', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand span')?.textContent?.trim()).toContain('KnowledgeOps-AI');
  });

  it('shows Chat navigation for all MVP roles', () => {
    for (const role of ['Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin']) {
      roles = [role];
      const fixture = TestBed.createComponent(App);
      try {
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        const links = Array.from(compiled.querySelectorAll('a')).map(link => link.textContent?.trim());
        // mat-icon renders ligature text in jsdom, so link text is e.g. 'chat Chat' not 'Chat'
        expect(links.some(l => l?.includes('Chat'))).toBe(true);
      } finally {
        fixture.destroy();
      }
    }
  });
});
