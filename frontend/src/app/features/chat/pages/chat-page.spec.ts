import { TestBed } from '@angular/core/testing';
import { ChatPage } from './chat-page';

describe('ChatPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatPage]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(ChatPage);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
