import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { BasketStore } from '../../core/basket/basket.store';
import { BasketDto, BasketItem } from '../../core/basket/basket.model';
import { ApiRoutes } from '../../core/config/api-routes';
import { CheckoutPage } from './checkout.page';

const physicalLine: BasketItem = {
  productId: '11111111-1111-4111-8111-111111111111',
  productSku: 'GROCERY-1',
  productName: 'Fresh bread',
  primaryCategory: 'Bakery',
  unitPrice: 2.5,
  discountedUnitPrice: 2.5,
  lineTotal: 2.5,
  quantity: 1,
  imageUrl: null,
  isDigital: false,
};

const digitalLine: BasketItem = {
  ...physicalLine,
  productId: '22222222-2222-4222-8222-222222222222',
  productSku: 'EBOOK-1',
  productName: 'Recipe e-book',
  isDigital: true,
};

function basketWith(items: readonly BasketItem[]): BasketDto {
  return {
    items,
    currencyCode: 'USD',
    subtotal: items.reduce((total, line) => total + line.lineTotal, 0),
    discountTotal: 0,
    taxTotal: 0,
    grandTotal: items.reduce((total, line) => total + line.lineTotal, 0),
    appliedCoupon: null,
  };
}

describe('CheckoutPage', () => {
  let httpTesting: HttpTestingController;
  let basketStore: InstanceType<typeof BasketStore>;
  let fixture: ComponentFixture<CheckoutPage>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    basketStore = TestBed.inject(BasketStore);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  async function loadBasket(items: readonly BasketItem[]): Promise<void> {
    const loading = basketStore.load();
    httpTesting.expectOne(ApiRoutes.basket.root).flush(basketWith(items));
    await loading;
  }

  function createPage(): void {
    fixture = TestBed.createComponent(CheckoutPage);
    fixture.detectChanges();
  }

  it('requiresShippingWhenAtLeastOneLineIsPhysical', async () => {
    await loadBasket([physicalLine, digitalLine]);
    createPage();

    expect(fixture.componentInstance['requiresShipping']()).toBeTrue();
  });

  it('doesNotRequireShippingWhenEveryLineIsDigital', async () => {
    await loadBasket([digitalLine]);
    createPage();

    expect(fixture.componentInstance['requiresShipping']()).toBeFalse();
  });

  it('keepsTheShippingGroupOptionalUntilTheCustomerOptsOutOfSameAsBilling', async () => {
    await loadBasket([physicalLine]);
    createPage();

    const form = fixture.componentInstance['checkoutForm'];

    form.controls.billingAddress.patchValue({
      line1: '1 High Street',
      city: 'London',
      postalCode: 'E1 6AN',
      countryCode: 'GB',
    });
    expect(form.controls.billingAddress.valid).toBeTrue();
    expect(form.valid).toBeTrue();
  });

  it('requiresAShippingAddressOnceSameAsBillingIsUnchecked', async () => {
    await loadBasket([physicalLine]);
    createPage();

    const form = fixture.componentInstance['checkoutForm'];
    form.controls.billingAddress.patchValue({
      line1: '1 High Street',
      city: 'London',
      postalCode: 'E1 6AN',
      countryCode: 'GB',
    });
    form.controls.sameAsBilling.setValue(false);

    expect(form.controls.shippingAddress.valid).toBeFalse();
    expect(form.valid).toBeFalse();

    form.controls.shippingAddress.patchValue({
      line1: '2 Market Square',
      city: 'Leeds',
      postalCode: 'LS1 1AA',
      countryCode: 'GB',
    });
    expect(form.valid).toBeTrue();
  });
});
