import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgbModal, NgbNav, NgbNavContent, NgbNavItem, NgbNavLinkButton, NgbNavOutlet } from '@ng-bootstrap/ng-bootstrap';
import { firstValueFrom } from 'rxjs';

import { AuthStore } from '../../core/auth/auth.store';
import { ApiError } from '../../core/http/api-error.model';
import { NotificationToastService } from '../../core/notifications/toast.service';
import { AccountApiService } from './account-api.service';
import { EnrollMultiFactorResponse } from './account.models';
import { RecoveryCodesComponent } from './recovery-codes.component';

const VerificationCodeLength = 6;

type SecurityStage = 'idle' | 'enrolling' | 'verifying' | 'disabling';

@Component({
  selector: 'fc-account-page',
  imports: [
    ReactiveFormsModule,
    NgbNav,
    NgbNavItem,
    NgbNavLinkButton,
    NgbNavContent,
    NgbNavOutlet,
  ],
  templateUrl: './account.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountPage {
  protected readonly authStore = inject(AuthStore);
  private readonly accountApi = inject(AccountApiService);
  private readonly toastService = inject(NotificationToastService);
  private readonly modalService = inject(NgbModal);

  protected readonly activeTab = signal<string>('profile');
  protected readonly stage = signal<SecurityStage>('idle');
  protected readonly enrollment = signal<EnrollMultiFactorResponse | null>(null);
  protected readonly securityError = signal<ApiError | null>(null);

  protected readonly multiFactorEnabled = computed(() => this.authStore.user()?.multiFactorEnabled ?? false);
  protected readonly isBusy = computed(() => this.stage() !== 'idle');

  protected readonly verificationCodeControl = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(VerificationCodeLength), Validators.maxLength(VerificationCodeLength)],
  });

  protected readonly disableCodeControl = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(VerificationCodeLength), Validators.maxLength(VerificationCodeLength)],
  });

  protected async beginEnrollment(): Promise<void> {
    this.securityError.set(null);
    this.stage.set('enrolling');
    try {
      const response = await firstValueFrom(this.accountApi.enrollMultiFactor());
      this.enrollment.set(response);
    } catch (error: unknown) {
      this.securityError.set(ApiError.fromUnknown(error));
    } finally {
      this.stage.set('idle');
    }
  }

  protected async confirmEnrollment(): Promise<void> {
    if (this.verificationCodeControl.invalid || this.isBusy()) {
      this.verificationCodeControl.markAsTouched();
      return;
    }
    this.securityError.set(null);
    this.stage.set('verifying');
    try {
      const response = await firstValueFrom(
        this.accountApi.verifyMultiFactor(this.verificationCodeControl.value.trim()),
      );
      this.enrollment.set(null);
      this.verificationCodeControl.reset();
      await this.authStore.refreshCurrentUser();
      this.toastService.showSuccess('Two-factor authentication is now enabled.');
      this.showRecoveryCodes(response.recoveryCodes);
    } catch (error: unknown) {
      this.securityError.set(ApiError.fromUnknown(error));
    } finally {
      this.stage.set('idle');
    }
  }

  protected async disableMultiFactor(): Promise<void> {
    if (this.disableCodeControl.invalid || this.isBusy()) {
      this.disableCodeControl.markAsTouched();
      return;
    }
    this.securityError.set(null);
    this.stage.set('disabling');
    try {
      await firstValueFrom(this.accountApi.disableMultiFactor(this.disableCodeControl.value.trim()));
      this.disableCodeControl.reset();
      await this.authStore.refreshCurrentUser();
      this.toastService.showInfo('Two-factor authentication has been disabled.');
    } catch (error: unknown) {
      this.securityError.set(ApiError.fromUnknown(error));
    } finally {
      this.stage.set('idle');
    }
  }

  protected cancelEnrollment(): void {
    this.enrollment.set(null);
    this.verificationCodeControl.reset();
    this.securityError.set(null);
  }

  private showRecoveryCodes(recoveryCodes: readonly string[]): void {
    const modalRef = this.modalService.open(RecoveryCodesComponent, { centered: true, backdrop: 'static' });
    const instance: RecoveryCodesComponent = modalRef.componentInstance;
    instance.recoveryCodes.set(recoveryCodes);
  }
}
