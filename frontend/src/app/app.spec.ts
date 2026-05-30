import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RoleVisibilityService } from './core/services/role-visibility.service';
import { App } from './app';

describe('App', () => {
  let roles: string[];

  beforeEach(async () => {
    roles = [];
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
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
    expect(compiled.querySelector('.app-title')?.textContent?.trim()).toContain('KnowledgeOps-AI');
  });

  it('shows Chat navigation for all MVP roles', () => {
    for (const role of ['Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin']) {
      roles = [role];
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const links = Array.from(compiled.querySelectorAll('a')).map(link => link.textContent?.trim());
      expect(links).toContain('Chat');
    }
  });
});
