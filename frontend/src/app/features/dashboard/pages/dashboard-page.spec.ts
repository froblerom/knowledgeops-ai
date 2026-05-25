import { TestBed } from '@angular/core/testing';
import { DashboardPage } from './dashboard-page';

describe('DashboardPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPage]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(DashboardPage);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
