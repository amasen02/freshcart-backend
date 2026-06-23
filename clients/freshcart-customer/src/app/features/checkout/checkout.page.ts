import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { BasketStore } from '../../core/basket/basket.store';
import { ApiError } from '../../core/http/api-error.model';
import { NotificationToastService } from '../../core/notifications/toast.service';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { CheckoutAddress, CountryChoices, PaymentMethodChoices } from './checkout.models';
import { CheckoutService } from './checkout.service';

const MaximumLineLength = 200;
const MaximumCityLength = 100;
const MaximumPostalCodeLength = 20;

@Component({
  selector: 'fc-checkout-page',
  imports: [ReactiveFormsModule, RouterLink, MoneyPipe],
  templateUrl: './checkout.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CheckoutPage {
  protected readonly basketStore = inject(BasketStore);
  private readonly checkoutService = inject(CheckoutService);
  private readonly toastService = inject(NotificationToastService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly paymentMethods = PaymentMethodChoices;
  protected readonly countries = CountryChoices;

  protected readonly isSubmitting = signal(false);
  protected readonly submissionError = signal<ApiError | null>(null);

  protected readonly requiresShipping = computed(() => this.basketStore.requiresShipping());

  protected readonly checkoutForm = this.formBuilder.group({
    paymentMethod: this.formBuilder.control(PaymentMethodChoices[0]?.value ?? '', [Validators.required]),
    sameAsBilling: this.formBuilder.control(true),
    billingAddress: this.buildAddressGroup(),
    shippingAddress: this.buildAddressGroup(),
  });

  constructor() {
    if (this.basketStore.status() === 'initial') {
      void this.basketStore.load();
    }

    effect(() => {
      const requiresShipping = this.requiresShipping();
      const sameAsBilling = this.checkoutForm.controls.sameAsBilling.value;
      untracked(() => this.applyShippingRequirement(requiresShipping, sameAsBilling));
    });

    this.checkoutForm.controls.sameAsBilling.valueChanges.subscribe((sameAsBilling) =>
      this.applyShippingRequirement(this.requiresShipping(), sameAsBilling),
    );
  }

  protected get shippingVisible(): boolean {
    return this.requiresShipping() && !this.checkoutForm.controls.sameAsBilling.value;
  }

  protected isAddressFieldInvalid(group: 'billingAddress' | 'shippingAddress', field: string): boolean {
    const control = this.checkoutForm.controls[group].get(field);
    return control !== null && control.invalid && control.touched;
  }

  protected async submitCheckout(): Promise<void> {
    if (this.checkoutForm.invalid || this.basketStore.isEmpty() || this.isSubmitting()) {
      this.checkoutForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.submissionError.set(null);
    const formValue = this.checkoutForm.getRawValue();
    const billingAddress = toAddress(formValue.billingAddress);
    const shippingAddress =
      this.requiresShipping() && !formValue.sameAsBilling ? toAddress(formValue.shippingAddress) : null;

    try {
      const result = await firstValueFrom(
        this.checkoutService.startCheckout({
          paymentMethod: formValue.paymentMethod,
          billingAddress,
          shippingAddress,
        }),
      );
      this.basketStore.clearAfterCheckout();
      this.toastService.showSuccess('Order placed. We are processing it now.');
      await this.router.navigate(['/orders', result.orderId, 'confirmation']);
    } catch (error: unknown) {
      this.submissionError.set(ApiError.fromUnknown(error));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private buildAddressGroup(): AddressFormGroup {
    return this.formBuilder.group({
      line1: this.formBuilder.control('', [Validators.required, Validators.maxLength(MaximumLineLength)]),
      line2: this.formBuilder.control('', [Validators.maxLength(MaximumLineLength)]),
      city: this.formBuilder.control('', [Validators.required, Validators.maxLength(MaximumCityLength)]),
      postalCode: this.formBuilder.control('', [Validators.required, Validators.maxLength(MaximumPostalCodeLength)]),
      countryCode: this.formBuilder.control(CountryChoices[0]?.code ?? '', [Validators.required]),
    });
  }

  private applyShippingRequirement(requiresShipping: boolean, sameAsBilling: boolean): void {
    const shippingGroup = this.checkoutForm.controls.shippingAddress;
    const shippingRequired = requiresShipping && !sameAsBilling;
    for (const fieldName of ['line1', 'city', 'postalCode', 'countryCode']) {
      const control = shippingGroup.get(fieldName);
      if (!control) {
        continue;
      }
      if (shippingRequired) {
        control.addValidators(Validators.required);
      } else {
        control.removeValidators(Validators.required);
      }
      control.updateValueAndValidity({ emitEvent: false });
    }
  }
}

type AddressFormGroup = FormGroup<{
  line1: FormControl<string>;
  line2: FormControl<string>;
  city: FormControl<string>;
  postalCode: FormControl<string>;
  countryCode: FormControl<string>;
}>;

interface AddressFormValue {
  readonly line1: string;
  readonly line2: string;
  readonly city: string;
  readonly postalCode: string;
  readonly countryCode: string;
}

function toAddress(value: AddressFormValue): CheckoutAddress {
  const line2 = value.line2.trim();
  return {
    line1: value.line1.trim(),
    line2: line2.length > 0 ? line2 : null,
    city: value.city.trim(),
    postalCode: value.postalCode.trim(),
    countryCode: value.countryCode,
  };
}
