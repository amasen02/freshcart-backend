import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NgbAlert } from '@ng-bootstrap/ng-bootstrap';

import { AuthStore } from '../../core/auth/auth.store';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';
import {
  ServerValidationErrorKey,
  applyServerValidationErrors,
  clearServerValidationErrors,
} from '../../shared/forms/server-validation';

type SignInControlName = 'email' | 'password' | 'multiFactorCode';

const SignInErrorMessages: Readonly<Record<SignInControlName, Readonly<Record<string, string>>>> = {
  email: {
    required: 'Email is required.',
    email: 'Enter a valid email address.',
  },
  password: {
    required: 'Password is required.',
  },
  multiFactorCode: {
    required: 'Enter the six-digit code from your authenticator app.',
  },
};

@Component({
  selector: 'fc-sign-in-page',
  imports: [ReactiveFormsModule, RouterLink, NgbAlert, AutofocusDirective],
  templateUrl: './sign-in.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInPage {
  protected readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly returnUrl = input('/');

  protected readonly signInForm = this.formBuilder.group({
    email: this.formBuilder.control('', [Validators.required, Validators.email]),
    password: this.formBuilder.control('', [Validators.required]),
    multiFactorCode: this.formBuilder.control(''),
    rememberMe: this.formBuilder.control(false),
  });

  private readonly multiFactorRevealed = signal(false);
  protected readonly showMultiFactorCode = computed(
    () => this.multiFactorRevealed() || this.authStore.multiFactorChallengeRequired(),
  );
  protected readonly isAuthenticating = computed(() => this.authStore.status() === 'authenticating');

  // Only same-origin paths are honored so a crafted link cannot redirect users off-site after sign-in.
  private readonly safeReturnUrl = computed(() => {
    const candidate = this.returnUrl();
    return candidate.startsWith('/') && !candidate.startsWith('//') ? candidate : '/';
  });

  constructor() {
    this.authStore.clearError();
    effect(() => {
      if (this.authStore.multiFactorChallengeRequired()) {
        this.multiFactorRevealed.set(true);
        untracked(() => this.requireMultiFactorCode());
      }
    });
  }

  protected async submitSignIn(): Promise<void> {
    clearServerValidationErrors(this.signInForm);
    if (this.signInForm.invalid) {
      this.signInForm.markAllAsTouched();
      return;
    }

    const formValue = this.signInForm.getRawValue();
    const trimmedCode = formValue.multiFactorCode.trim();
    const succeeded = await this.authStore.signIn({
      email: formValue.email,
      password: formValue.password,
      multiFactorCode: trimmedCode.length > 0 ? trimmedCode : null,
      rememberMe: formValue.rememberMe,
    });

    if (succeeded) {
      await this.router.navigateByUrl(this.safeReturnUrl());
      return;
    }

    const signInError = this.authStore.error();
    if (signInError?.hasValidationErrors) {
      applyServerValidationErrors(this.signInForm, signInError);
    }
  }

  protected isInvalid(controlName: SignInControlName): boolean {
    const control = this.signInForm.controls[controlName];
    return control.invalid && control.touched;
  }

  protected errorTextFor(controlName: SignInControlName): string {
    const controlErrors = this.signInForm.controls[controlName].errors;
    if (!controlErrors) {
      return '';
    }
    const serverMessage = controlErrors[ServerValidationErrorKey];
    if (typeof serverMessage === 'string') {
      return serverMessage;
    }
    const firstErrorKey = Object.keys(controlErrors)[0];
    return firstErrorKey ? (SignInErrorMessages[controlName][firstErrorKey] ?? 'This value is invalid.') : '';
  }

  private requireMultiFactorCode(): void {
    const codeControl = this.signInForm.controls.multiFactorCode;
    codeControl.addValidators(Validators.required);
    codeControl.updateValueAndValidity();
  }
}
