import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { ApiRoutes } from '../../core/config/api-routes';
import { SignUpPage } from './sign-up.page';

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));

describe('SignUpPage', () => {
  let fixture: ComponentFixture<SignUpPage>;
  let httpTesting: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(SignUpPage);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    httpTesting.verify();
  });

  function pageElement(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function enterText(selector: string, value: string): void {
    const inputElement = pageElement().querySelector<HTMLInputElement>(selector);
    if (!inputElement) {
      throw new Error(`No input found for selector ${selector}`);
    }
    inputElement.value = value;
    inputElement.dispatchEvent(new Event('input'));
  }

  function submitForm(): void {
    pageElement().querySelector<HTMLButtonElement>('button[type="submit"]')?.click();
  }

  it('rejectsPasswordsThatDoNotMeetTheIdentityComplexityPolicy', async () => {
    enterText('#sign-up-display-name', 'Sam Shopper');
    enterText('#sign-up-email', 'shopper@freshcart.test');
    enterText('#sign-up-password', 'too-simple');
    enterText('#sign-up-confirm-password', 'too-simple');
    submitForm();
    await fixture.whenStable();
    fixture.detectChanges();

    httpTesting.expectNone(ApiRoutes.auth.signUp);
    expect(pageElement().querySelector('#sign-up-password-errors')?.textContent).toContain(
      'Password must be at least 12 characters.',
    );
  });

  it('rejectsSubmissionWhenThePasswordsDoNotMatch', async () => {
    enterText('#sign-up-display-name', 'Sam Shopper');
    enterText('#sign-up-email', 'shopper@freshcart.test');
    enterText('#sign-up-password', 'CorrectHorse!Battery9Staple');
    enterText('#sign-up-confirm-password', 'DifferentHorse!Battery9Staple');
    submitForm();
    await fixture.whenStable();
    fixture.detectChanges();

    httpTesting.expectNone(ApiRoutes.auth.signUp);
    expect(pageElement().querySelector('#sign-up-confirm-password-errors')?.textContent).toContain(
      'Passwords do not match.',
    );
  });

  it('registersSignsInAndNavigatesHomeOnSuccess', async () => {
    const navigateSpy = spyOn(router, 'navigateByUrl').and.resolveTo(true);

    enterText('#sign-up-display-name', 'Sam Shopper');
    enterText('#sign-up-email', 'shopper@freshcart.test');
    enterText('#sign-up-password', 'CorrectHorse!Battery9Staple');
    enterText('#sign-up-confirm-password', 'CorrectHorse!Battery9Staple');
    submitForm();
    await settle();

    const signUpRequest = httpTesting.expectOne(ApiRoutes.auth.signUp);
    expect(signUpRequest.request.body).toEqual({
      email: 'shopper@freshcart.test',
      password: 'CorrectHorse!Battery9Staple',
      displayName: 'Sam Shopper',
      marketingConsent: false,
      signInImmediately: true,
      useCookie: true,
    });
    signUpRequest.flush({}, { status: 201, statusText: 'Created' });
    await settle();

    httpTesting.expectOne(ApiRoutes.auth.antiForgeryToken).flush(null, { status: 204, statusText: 'No Content' });
    await settle();
    httpTesting.expectOne(ApiRoutes.account.me).flush({
      userId: 'b3f2c1a0-0000-4000-8000-000000000001',
      email: 'shopper@freshcart.test',
      displayName: 'Sam Shopper',
      roles: ['Customer'],
      multiFactorEnabled: false,
    });
    await settle();

    expect(navigateSpy).toHaveBeenCalledWith('/');
  });

  it('mapsServerValidationErrorsOntoTheMatchingControls', async () => {
    enterText('#sign-up-display-name', 'Sam Shopper');
    enterText('#sign-up-email', 'shopper@freshcart.test');
    enterText('#sign-up-password', 'CorrectHorse!Battery9Staple');
    enterText('#sign-up-confirm-password', 'CorrectHorse!Battery9Staple');
    submitForm();
    await settle();

    httpTesting.expectOne(ApiRoutes.auth.signUp).flush(
      {
        title: 'One or more validation errors occurred.',
        detail: 'Validation failed.',
        validationErrors: { Email: ['Email is already registered.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(pageElement().querySelector('#sign-up-email-errors')?.textContent).toContain(
      'Email is already registered.',
    );
  });
});
