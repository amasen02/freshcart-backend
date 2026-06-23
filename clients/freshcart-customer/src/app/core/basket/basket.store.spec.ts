import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ApiRoutes } from '../config/api-routes';
import { BasketDto } from './basket.model';
import { BasketStore } from './basket.store';

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));
const noContent = { status: 204, statusText: 'No Content' } as const;

describe('BasketStore', () => {
  const pricedBasket: BasketDto = {
    items: [
      {
        productId: 'a1e2c3d4-0000-4000-8000-000000000001',
        productSku: 'FRUIT-APPLE-1KG',
        productName: 'Crisp Apples 1kg',
        primaryCategory: 'Fruit',
        unitPrice: 4.99,
        discountedUnitPrice: 4.49,
        lineTotal: 8.98,
        quantity: 2,
        imageUrl: null,
        isDigital: false,
      },
      {
        productId: 'a1e2c3d4-0000-4000-8000-000000000002',
        productSku: 'GIFT-CARD-25',
        productName: 'Gift Card 25',
        primaryCategory: 'Gifts',
        unitPrice: 25,
        discountedUnitPrice: 25,
        lineTotal: 25,
        quantity: 1,
        imageUrl: null,
        isDigital: true,
      },
    ],
    currencyCode: 'USD',
    subtotal: 33.98,
    discountTotal: 1,
    taxTotal: 2.72,
    grandTotal: 35.7,
    appliedCoupon: 'FRESH10',
  };

  let httpTesting: HttpTestingController;
  let store: InstanceType<typeof BasketStore>;

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
    store = TestBed.inject(BasketStore);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  async function loadBasket(basket: BasketDto): Promise<void> {
    const loading = store.load();
    httpTesting.expectOne(ApiRoutes.basket.root).flush(basket);
    await loading;
  }

  async function flushMutationReload(basket: BasketDto): Promise<void> {
    await settle();
    httpTesting.expectOne(ApiRoutes.basket.root).flush(basket);
  }

  it('loadPopulatesItemsTotalsAndCouponFromTheServer', async () => {
    const loading = store.load();
    expect(store.status()).toBe('loading');

    httpTesting.expectOne(ApiRoutes.basket.root).flush(pricedBasket);
    await loading;

    expect(store.status()).toBe('ready');
    expect(store.items().length).toBe(2);
    expect(store.totals()).toEqual({ subtotal: 33.98, discountTotal: 1, taxTotal: 2.72, grandTotal: 35.7 });
    expect(store.appliedCoupon()).toBe('FRESH10');
    expect(store.itemCount()).toBe(3);
    expect(store.isEmpty()).toBeFalse();
    expect(store.requiresShipping()).toBeTrue();
  });

  it('requiresShippingIsFalseWhenEveryLineIsDigital', async () => {
    const digitalLine = pricedBasket.items[1];
    if (!digitalLine) {
      fail('test fixture must contain a digital line');
      return;
    }
    await loadBasket({ ...pricedBasket, items: [digitalLine] });

    expect(store.requiresShipping()).toBeFalse();
  });

  it('loadCapturesTheApiErrorWhenTheServerFails', async () => {
    const loading = store.load();

    httpTesting
      .expectOne(ApiRoutes.basket.root)
      .flush({ title: 'An unexpected error occurred.' }, { status: 500, statusText: 'Internal Server Error' });
    await loading;

    expect(store.status()).toBe('error');
    expect(store.error()?.status).toBe(500);
  });

  it('addItemPostsTheLineThenReloadsTheBasket', async () => {
    const adding = store.addItem('a1e2c3d4-0000-4000-8000-000000000001', 2);
    await settle();

    const postRequest = httpTesting.expectOne(ApiRoutes.basket.items);
    expect(postRequest.request.method).toBe('POST');
    expect(postRequest.request.body).toEqual({
      productId: 'a1e2c3d4-0000-4000-8000-000000000001',
      quantity: 2,
    });
    postRequest.flush(null, noContent);
    await flushMutationReload(pricedBasket);
    await adding;

    expect(store.status()).toBe('ready');
    expect(store.itemCount()).toBe(3);
  });

  it('addItemSurfacesTheServerRejectionWithoutReloading', async () => {
    const adding = store.addItem('a1e2c3d4-0000-4000-8000-000000000099', 1);
    await settle();

    httpTesting
      .expectOne(ApiRoutes.basket.items)
      .flush(
        { title: 'Bad request.', detail: 'Product is no longer available.' },
        { status: 400, statusText: 'Bad Request' },
      );
    await adding;

    expect(store.status()).toBe('error');
    expect(store.error()?.detail).toBe('Product is no longer available.');
  });

  it('updateQuantityPutsTheNewQuantityForTheProductLine', async () => {
    const updating = store.updateQuantity('a1e2c3d4-0000-4000-8000-000000000001', 5);
    await settle();

    const putRequest = httpTesting.expectOne(
      ApiRoutes.basket.item('a1e2c3d4-0000-4000-8000-000000000001'),
    );
    expect(putRequest.request.method).toBe('PUT');
    expect(putRequest.request.body).toEqual({ quantity: 5 });
    putRequest.flush(null, noContent);
    await flushMutationReload(pricedBasket);
    await updating;

    expect(store.status()).toBe('ready');
  });

  it('removeItemDeletesTheLineThenReloadsTheBasket', async () => {
    const removing = store.removeItem('a1e2c3d4-0000-4000-8000-000000000002');
    await settle();

    const deleteRequest = httpTesting.expectOne(
      ApiRoutes.basket.item('a1e2c3d4-0000-4000-8000-000000000002'),
    );
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null, noContent);

    const physicalOnly: BasketDto = {
      ...pricedBasket,
      items: pricedBasket.items.filter((line) => !line.isDigital),
    };
    await flushMutationReload(physicalOnly);
    await removing;

    expect(store.itemCount()).toBe(2);
  });

  it('applyCouponPostsTheCodeAndRefreshesTotals', async () => {
    const applying = store.applyCoupon('FRESH10');
    await settle();

    const couponRequest = httpTesting.expectOne(ApiRoutes.basket.coupon);
    expect(couponRequest.request.method).toBe('POST');
    expect(couponRequest.request.body).toEqual({ code: 'FRESH10' });
    couponRequest.flush(null, noContent);
    await flushMutationReload(pricedBasket);
    await applying;

    expect(store.appliedCoupon()).toBe('FRESH10');
    expect(store.totals().discountTotal).toBe(1);
  });

  it('applyCouponSurfacesTheValidatorRejection', async () => {
    const applying = store.applyCoupon('EXPIRED');
    await settle();

    httpTesting
      .expectOne(ApiRoutes.basket.coupon)
      .flush({ title: 'Bad request.', detail: 'Coupon has expired.' }, { status: 400, statusText: 'Bad Request' });
    await applying;

    expect(store.status()).toBe('error');
    expect(store.error()?.detail).toBe('Coupon has expired.');
  });

  it('removeCouponDeletesTheCouponThenReloads', async () => {
    const removing = store.removeCoupon();
    await settle();

    const deleteRequest = httpTesting.expectOne(ApiRoutes.basket.coupon);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null, noContent);
    await flushMutationReload({ ...pricedBasket, appliedCoupon: null, discountTotal: 0 });
    await removing;

    expect(store.appliedCoupon()).toBeNull();
  });

  it('mutationsAreSerializedSoConcurrentCallsCannotInterleave', async () => {
    const firstAdd = store.addItem('a1e2c3d4-0000-4000-8000-000000000001', 1);
    const secondAdd = store.addItem('a1e2c3d4-0000-4000-8000-000000000002', 1);
    await settle();

    const pendingPosts = httpTesting.match(ApiRoutes.basket.items);
    expect(pendingPosts.length).toBe(1);
    pendingPosts.forEach((request) => request.flush(null, noContent));
    await flushMutationReload(pricedBasket);
    await firstAdd;

    httpTesting.expectOne(ApiRoutes.basket.items).flush(null, noContent);
    await flushMutationReload(pricedBasket);
    await secondAdd;

    expect(store.status()).toBe('ready');
  });

  it('clearAfterCheckoutEmptiesTheBasketWithoutAServerCall', async () => {
    await loadBasket(pricedBasket);

    store.clearAfterCheckout();

    expect(store.items()).toEqual([]);
    expect(store.itemCount()).toBe(0);
    expect(store.totals().grandTotal).toBe(0);
    expect(store.appliedCoupon()).toBeNull();
    expect(store.status()).toBe('ready');
  });
});
