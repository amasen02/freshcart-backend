import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { MultiFactorCodeRequiredDetail } from '../../core/auth/auth.store';
import { ApiRoutes } from '../../core/config/api-routes';
import { SignInPage } from './sign-in.page';

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));

describe('SignInPage', () => {
  let fixture: ComponentFixture<SignInPage>;
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
    fixture = TestBed.createComponent(SignInPage);
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

  it('blocksSubmissionAndShowsFieldErrorsWhileTheFormIsInvalid', async () => {
    submitForm();
    await fixture.whenStable();
    fixture.detectChanges();

    httpTesting.expectNone(ApiRoutes.auth.signIn);
    expect(pageElement().querySelector('#sign-in-email-errors')?.textContent).toContain('Email is required.');
    expect(pageElement().querySelector('#sign-in-password-errors')?.textContent).toContain(
      'Password is required.',
    );
  });

  it('revealsTheAuthenticatorCodeFieldWhenTheServerDemandsMultiFactor', async () => {
    enterText('#sign-in-email', 'shopper@freshcart.test');
    enterText('#sign-in-password', 'CorrectHorse!Battery9Staple');
    submitForm();
    await settle();

    httpTesting
      .expectOne(ApiRoutes.auth.signIn)
      .flush(
        { title: 'Bad request.', detail: MultiFactorCodeRequiredDetail },
        { status: 400, statusText: 'Bad Request' },
      );
    await settle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(pageElement().querySelector('#sign-in-multi-factor-code')).not.toBeNull();
    expect(pageElement().textContent).toContain('Enter the six-digit code from your authenticator app');
  });

  it('showsTheProblemDetailAsADismissibleAlertWhenCredentialsAreRejected', async () => {
    enterText('#sign-in-email', 'shopper@freshcart.test');
    enterText('#sign-in-password', 'wrong-password-attempt');
    submitForm();
    await settle();

    httpTesting
      .expectOne(ApiRoutes.auth.signIn)
      .flush(
        { title: 'Bad request.', detail: 'Email or password is incorrect.' },
        { status: 400, statusText: 'Bad Request' },
      );
    await settle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(pageElement().querySelector('ngb-alert')?.textContent).toContain('Email or password is incorrect.');
  });

  it('navigatesToTheSanitizedReturnUrlAfterASuccessfulSignIn', async () => {
    const navigateSpy = spyOn(router, 'navigateByUrl').and.resolveTo(true);
    fixture.componentRef.setInput('returnUrl', '/orders');
    fixture.detectChanges();

    enterText('#sign-in-email', 'shopper@freshcart.test');
    enterText('#sign-in-password', 'CorrectHorse!Battery9Staple');
    submitForm();
    await settle();

    httpTesting.expectOne(ApiRoutes.auth.signIn).flush({});
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

    expect(navigateSpy).toHaveBeenCalledWith('/orders');
  });

  it('fallsBackToHomeWhenTheReturnUrlIsNotASameOriginPath', async () => {
    const navigateSpy = spyOn(router, 'navigateByUrl').and.resolveTo(true);
    fixture.componentRef.setInput('returnUrl', 'https://evil.example.com/phish');
    fixture.detectChanges();

    enterText('#sign-in-email', 'shopper@freshcart.test');
    enterText('#sign-in-password', 'CorrectHorse!Battery9Staple');
    submitForm();
    await settle();

    httpTesting.expectOne(ApiRoutes.auth.signIn).flush({});
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
});
