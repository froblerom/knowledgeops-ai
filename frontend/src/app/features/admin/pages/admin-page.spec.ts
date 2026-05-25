import { TestBed } from '@angular/core/testing';
import { AdminPage } from './admin-page';

describe('AdminPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminPage]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(AdminPage);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
