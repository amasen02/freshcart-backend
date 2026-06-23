import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { OrderPollIntervalMs, OrderPollMaxAttempts } from '../../core/config/timing.tokens';
import { ApiRoutes } from '../../core/config/api-routes';
import { OrderDetail, OrderStatus } from '../orders/orders.models';
import { OrderConfirmationPage } from './order-confirmation.page';

const TestPollIntervalMs = 40;
const TestMaxAttempts = 4;
const PollAppearanceTimeoutMs = 2000;
const OrderId = '33333333-3333-4333-8333-333333333333';

function orderWith(status: OrderStatus): OrderDetail {
  return {
    orderId: OrderId,
    customerId: 'customer-1',
    status,
    customerEmail: 'shopper@example.com',
    customerDisplayName: 'Shopper',
    paymentMethod: 'CreditCard',
    subtotal: 10,
    discountTotal: 0,
    taxTotal: 0,
    shippingTotal: 0,
    grandTotal: 10,
    currencyCode: 'USD',
    billingAddress: { line1: '1 High Street', line2: null, city: 'London', postalCode: 'E1 6AN', countryCode: 'GB' },
    shippingAddress: null,
    failureReason: null,
    submittedOnUtc: '2026-06-18T09:00:00Z',
    confirmedOnUtc: null,
    cancelledOnUtc: null,
    lines: [],
  };
}

const yieldMacrotask = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));

describe('OrderConfirmationPage', () => {
  let httpTesting: HttpTestingController;
  let fixture: ComponentFixture<OrderConfirmationPage>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: OrderPollIntervalMs, useValue: TestPollIntervalMs },
        { provide: OrderPollMaxAttempts, useValue: TestMaxAttempts },
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(OrderConfirmationPage);
    fixture.componentRef.setInput('orderId', OrderId);
    fixture.detectChanges();
  });

  function livePolls(): TestRequest[] {
    return httpTesting.match((candidate) => candidate.url === ApiRoutes.orders.byId(OrderId)).filter((r) => !r.cancelled);
  }

  async function awaitNextPoll(): Promise<TestRequest> {
    const deadline = Date.now() + PollAppearanceTimeoutMs;
    for (;;) {
      const live = livePolls();
      if (live.length > 0) {
        return live[live.length - 1] as TestRequest;
      }
      if (Date.now() > deadline) {
        throw new Error('Expected a poll request but none was issued within the timeout.');
      }
      await yieldMacrotask();
    }
  }

  async function flushNextPoll(status: OrderStatus): Promise<void> {
    const request = await awaitNextPoll();
    request.flush(orderWith(status));
  }

  const waitPastSeveralIntervals = (): Promise<void> =>
    new Promise((resolve) => setTimeout(resolve, TestPollIntervalMs * 6));

  async function pollHasStopped(): Promise<boolean> {
    await waitPastSeveralIntervals();
    return livePolls().length === 0;
  }

  it('stopsPollingOnceTheOrderReachesATerminalStatus', async () => {
    await flushNextPoll('Submitted');
    await flushNextPoll('StockReserved');
    await flushNextPoll('Confirmed');

    expect(await pollHasStopped()).toBeTrue();
    expect(fixture.componentInstance['status']()).toBe('Confirmed');
  });

  it('stopsPollingAfterTheAttemptCapEvenWhenNeverTerminal', async () => {
    for (let attempt = 0; attempt < TestMaxAttempts; attempt += 1) {
      await flushNextPoll('Submitted');
    }

    expect(await pollHasStopped()).toBeTrue();
    expect(fixture.componentInstance['status']()).toBe('Submitted');
  });

  it('cancelsTheTimerOnDestroySoNoZombiePollSurvives', async () => {
    await flushNextPoll('Submitted');

    fixture.destroy();

    expect(await pollHasStopped()).toBeTrue();
  });
});
