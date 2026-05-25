import { TestBed } from '@angular/core/testing';
import { ErrorState } from './error-state';

describe('ErrorState', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErrorState]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(ErrorState);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the message input', () => {
    const fixture = TestBed.createComponent(ErrorState);
    fixture.componentInstance.message = 'Test error';
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.error-message')?.textContent).toContain('Test error');
  });
});
