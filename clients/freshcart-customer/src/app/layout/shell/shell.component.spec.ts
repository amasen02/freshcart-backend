import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ShellComponent } from './shell.component';

describe('ShellComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
  });

  function renderShell(): HTMLElement {
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('rendersTheSkipLinkAsTheFirstFocusTarget', () => {
    const shellElement = renderShell();

    const skipLink = shellElement.querySelector<HTMLAnchorElement>('a[href="#main-content"]');
    expect(skipLink?.textContent).toContain('Skip to main content');
  });

  it('rendersTheBrandTheMainLandmarkAndTheFooter', () => {
    const shellElement = renderShell();

    expect(shellElement.querySelector('.navbar-brand')?.textContent).toContain('FreshCart');
    expect(shellElement.querySelector('main#main-content')).not.toBeNull();
    expect(shellElement.querySelector('footer')?.textContent).toContain(`${new Date().getFullYear()}`);
  });

  it('offersSignInAndSignUpToAnonymousVisitorsInsteadOfTheProfileMenu', () => {
    const shellElement = renderShell();

    expect(shellElement.querySelector('a[href="/auth/sign-in"]')?.textContent).toContain('Sign in');
    expect(shellElement.querySelector('a[href="/auth/sign-up"]')?.textContent).toContain('Create account');
    expect(shellElement.querySelector('a[href="/orders"]')).toBeNull();
  });

  it('hidesTheBasketBadgeWhileTheBasketIsEmpty', () => {
    const shellElement = renderShell();

    expect(shellElement.querySelector('a[href="/basket"]')).not.toBeNull();
    expect(shellElement.querySelector('a[href="/basket"] .badge')).toBeNull();
  });
});
