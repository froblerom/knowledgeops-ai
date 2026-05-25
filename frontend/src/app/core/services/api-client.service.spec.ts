import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ApiClientService } from './api-client.service';

describe('ApiClientService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient()]
    });
  });

  it('should be created', () => {
    const service = TestBed.inject(ApiClientService);
    expect(service).toBeTruthy();
  });

  it('should expose baseUrl from environment', () => {
    const service = TestBed.inject(ApiClientService);
    expect(service.baseUrl).toBeTruthy();
  });
});
