import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { credentialsInterceptor } from './credentials.interceptor';

describe('credentialsInterceptor', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([credentialsInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    httpClient = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  function withCredentialsFlagFor(url: string): boolean {
    httpClient.get(url).subscribe();
    const testRequest = httpTesting.expectOne(url);
    testRequest.flush({});
    return testRequest.request.withCredentials;
  }

  it('attachesCredentialsToRelativeApiRequests', () => {
    expect(withCredentialsFlagFor('/api/products')).toBeTrue();
  });

  it('attachesCredentialsToRelativeHubRequests', () => {
    expect(withCredentialsFlagFor('/hubs/notifications')).toBeTrue();
  });

  it('leavesAbsoluteUrlsWithoutCredentials', () => {
    expect(withCredentialsFlagFor('https://external.example.com/api/data')).toBeFalse();
  });

  it('leavesOtherRelativePathsWithoutCredentials', () => {
    expect(withCredentialsFlagFor('/assets/translations.json')).toBeFalse();
  });
});
