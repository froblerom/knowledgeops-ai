import { TestBed } from '@angular/core/testing';
import { LoadingState } from './loading-state';

describe('LoadingState', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoadingState]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(LoadingState);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
