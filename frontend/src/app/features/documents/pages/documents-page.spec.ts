import { TestBed } from '@angular/core/testing';
import { DocumentsPage } from './documents-page';

describe('DocumentsPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentsPage]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(DocumentsPage);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
