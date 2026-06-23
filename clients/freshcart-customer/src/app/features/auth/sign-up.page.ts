import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NgbAlert } from '@ng-bootstrap/ng-bootstrap';

import { AuthStore } from '../../core/auth/auth.store';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';
import {
  ServerValidationErrorKey,
  applyServerValidationErrors,
  clearServerValidationErrors,
} from '../../shared/forms/server-validation';

// Mirrors the Identity service password policy: 12+ characters with digit, lower, upper and symbol.
const MinimumPasswordLength = 12;
const PasswordComplexityPattern = /^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^\da-zA-Z]).+$/;
const MaximumDisplayNameLength = 100;

type SignUpControlName = 'displayName' | 'email' | 'password' | 'confirmPassword';

const SignUpErrorMessages: Readonly<Record<SignUpControlName, Readonly<Record<string, string>>>> = {
  displayName: {
    required: 'Display name is required.',
    maxlength: `Display name cannot exceed ${MaximumDisplayNameLength} characters.`,
  },
  email: {
    required: 'Email is required.',
    email: 'Enter a valid email address.',
  },
  password: {
    required: 'Password is required.',
    minlength: `Password must be at least ${MinimumPasswordLength} characters.`,
    pattern: 'Password must contain a digit, a lowercase letter, an uppercase letter and a symbol.',
  },
  confirmPassword: {
    required: 'Confirm your password.',
    passwordsMismatch: 'Passwords do not match.',
  },
};

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirmPassword = group.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordsMismatch: true };
}

@Component({
  selector: 'fc-sign-up-page',
  imports: [ReactiveFormsModule, RouterLink, NgbAlert, AutofocusDirective],
  templateUrl: './sign-up.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignUpPage {
  protected readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly returnUrl = input('/');

  protected readonly signUpForm = this.formBuilder.group(
    {
      displayName: this.formBuilder.control('', [
        Validators.required,
        Validators.maxLength(MaximumDisplayNameLength),
      ]),
      email: this.formBuilder.control('', [Validators.required, Validators.email]),
      password: this.formBuilder.control('', [
        Validators.required,
        Validators.minLength(MinimumPasswordLength),
        Validators.pattern(PasswordComplexityPattern),
      ]),
      confirmPassword: this.formBuilder.control('', [Validators.required]),
      marketingConsent: this.formBuilder.control(false),
    },
    { validators: [passwordsMatchValidator] },
  );

  protected readonly isAuthenticating = computed(() => this.authStore.status() === 'authenticating');

  // Only same-origin paths are honored so a crafted link cannot redirect users off-site after sign-up.
  private readonly safeReturnUrl = computed(() => {
    const candidate = this.returnUrl();
    return candidate.startsWith('/') && !candidate.startsWith('//') ? candidate : '/';
  });

  constructor() {
    this.authStore.clearError();
  }

  protected async submitSignUp(): Promise<void> {
    clearServerValidationErrors(this.signUpForm);
    if (this.signUpForm.invalid) {
      this.signUpForm.markAllAsTouched();
      return;
    }

    const formValue = this.signUpForm.getRawValue();
    const succeeded = await this.authStore.signUp({
      email: formValue.email,
      password: formValue.password,
      displayName: formValue.displayName.trim(),
      marketingConsent: formValue.marketingConsent,
    });

    if (succeeded) {
      await this.router.navigateByUrl(this.safeReturnUrl());
      return;
    }

    const signUpError = this.authStore.error();
    if (signUpError?.hasValidationErrors) {
      applyServerValidationErrors(this.signUpForm, signUpError);
    }
  }

  protected isInvalid(controlName: SignUpControlName): boolean {
    const control = this.signUpForm.controls[controlName];
    if (controlName === 'confirmPassword' && this.signUpForm.hasError('passwordsMismatch') && control.touched) {
      return true;
    }
    return control.invalid && control.touched;
  }

  protected errorTextFor(controlName: SignUpControlName): string {
    const control = this.signUpForm.controls[controlName];
    if (controlName === 'confirmPassword' && !control.errors && this.signUpForm.hasError('passwordsMismatch')) {
      return SignUpErrorMessages.confirmPassword['passwordsMismatch'] ?? '';
    }
    const controlErrors = control.errors;
    if (!controlErrors) {
      return '';
    }
    const serverMessage = controlErrors[ServerValidationErrorKey];
    if (typeof serverMessage === 'string') {
      return serverMessage;
    }
    const firstErrorKey = Object.keys(controlErrors)[0];
    return firstErrorKey ? (SignUpErrorMessages[controlName][firstErrorKey] ?? 'This value is invalid.') : '';
  }
}
