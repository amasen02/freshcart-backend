import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthStore } from '../auth/auth.store';
import { CurrentUser } from '../auth/current-user.model';
import { NotificationToastService } from '../notifications/toast.service';
import { ApiRoutes } from '../config/api-routes';
import { PaginatedResult } from '../../features/catalog/catalog.models';
import { FakeHubConnection } from './fake-hub-connection';
import { NotificationDto } from './notification.model';
import { NotificationHubServerEvents } from './notification-hub.events';
import { NotificationsStore } from './notifications.store';
import { RealtimeConnection } from './realtime-connection';
import { SignalrConnectionFactory } from './signalr-connection.factory';

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));
const noContent = { status: 204, statusText: 'No Content' } as const;
const RecentNotificationsPageSize = 20;

function recentPage(items: readonly NotificationDto[]): PaginatedResult<NotificationDto> {
  return {
    pageNumber: 1,
    pageSize: RecentNotificationsPageSize,
    totalItemCount: items.length,
    items,
  };
}

const customer: CurrentUser = {
  userId: 'user-1',
  email: 'shopper@freshcart.test',
  displayName: 'Sam Shopper',
  roles: ['Customer'],
  multiFactorEnabled: false,
};

function notification(overrides: Partial<NotificationDto> = {}): NotificationDto {
  return {
    id: 'n-1',
    type: 'OrderConfirmed',
    title: 'Order confirmed',
    message: 'Your order is on its way.',
    orderId: 'order-1',
    createdOnUtc: '2026-06-18T10:00:00Z',
    isRead: false,
    ...overrides,
  };
}

describe('NotificationsStore', () => {
  let fakeHub: FakeHubConnection;
  let httpTesting: HttpTestingController;
  let store: InstanceType<typeof NotificationsStore>;
  let authStore: InstanceType<typeof AuthStore>;
  let toastService: NotificationToastService;

  beforeEach(() => {
    fakeHub = new FakeHubConnection();
    const factory: Pick<SignalrConnectionFactory, 'create'> = {
      create: () => new RealtimeConnection(fakeHub.asHubConnection()),
    };

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SignalrConnectionFactory, useValue: factory },
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    authStore = TestBed.inject(AuthStore);
    store = TestBed.inject(NotificationsStore);
    toastService = TestBed.inject(NotificationToastService);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  async function authenticate(): Promise<void> {
    const initialization = authStore.initialize();
    httpTesting.expectOne(ApiRoutes.account.me).flush(customer);
    await initialization;
  }

  async function startViaAuth(recent: readonly NotificationDto[], unreadCount: number): Promise<void> {
    await authenticate();
    // The auth-watching effect enqueues start(); flush effects then let the async chain issue loads.
    TestBed.tick();
    await settle();
    httpTesting.expectOne((request) => request.url === ApiRoutes.notifications.root).flush(recentPage(recent));
    httpTesting.expectOne(ApiRoutes.notifications.unreadCount).flush({ unreadCount });
    await settle();
    TestBed.tick();
  }

  it('authenticatingStartsTheConnectionAndHydratesRecentAndUnread', async () => {
    await startViaAuth([notification()], 1);

    expect(store.items().length).toBe(1);
    expect(store.unreadCount()).toBe(1);
    expect(fakeHub.startCount).toBe(1);
    expect(store.connectionState()).toBe('connected');
    expect(fakeHub.handlerCount(NotificationHubServerEvents.notificationReceived)).toBe(1);
    expect(fakeHub.handlerCount(NotificationHubServerEvents.salesDashboardUpdated)).toBe(1);
  });

  it('notificationReceivedPrependsTheItemBumpsUnreadAndRaisesAToastForOrderStatusTypes', async () => {
    await startViaAuth([], 0);

    fakeHub.emit(NotificationHubServerEvents.notificationReceived, notification({ id: 'n-2' }));

    expect(store.items()[0]?.id).toBe('n-2');
    expect(store.unreadCount()).toBe(1);
    expect(toastService.toasts().length).toBe(1);
    expect(toastService.toasts()[0]?.kind).toBe('info');
  });

  it('reflectsTheReconnectingStateRaisedByTheHubLifecycle', async () => {
    await startViaAuth([], 0);

    fakeHub.simulateReconnecting();
    TestBed.tick();

    expect(store.connectionState()).toBe('reconnecting');
    expect(store.isReconnecting()).toBeTrue();

    fakeHub.simulateReconnected();
    TestBed.tick();

    expect(store.connectionState()).toBe('connected');
  });

  it('notificationReceivedDoesNotToastForNonOrderTypes', async () => {
    await startViaAuth([], 0);

    fakeHub.emit(NotificationHubServerEvents.notificationReceived, notification({ id: 'n-3', type: 'Promotion' }));

    expect(store.unreadCount()).toBe(1);
    expect(toastService.toasts().length).toBe(0);
  });

  it('salesDashboardUpdatedIncrementsTheDashboardTick', async () => {
    await startViaAuth([], 0);
    const before = store.dashboardTick();

    fakeHub.emit(NotificationHubServerEvents.salesDashboardUpdated);

    expect(store.dashboardTick()).toBe(before + 1);
  });

  it('markAsReadPutsThenFlipsTheItemAndDecrementsUnreadOptimistically', async () => {
    await startViaAuth([notification({ id: 'n-4', isRead: false })], 1);

    const marking = store.markAsRead('n-4');
    httpTesting.expectOne(ApiRoutes.notifications.markAsRead('n-4')).flush(null, noContent);
    await marking;

    expect(store.items()[0]?.isRead).toBeTrue();
    expect(store.unreadCount()).toBe(0);
  });

  it('markAsReadIsANoOpForAlreadyReadNotificationsAndIssuesNoRequest', async () => {
    await startViaAuth([notification({ id: 'n-5', isRead: true })], 0);

    await store.markAsRead('n-5');

    expect(store.unreadCount()).toBe(0);
  });

  it('signingOutStopsTheConnectionDeregistersEveryHandlerAndClearsState', async () => {
    await startViaAuth([notification()], 1);

    patchAnonymous();
    TestBed.tick();
    await settle();
    TestBed.tick();

    expect(fakeHub.stopCount).toBe(1);
    expect(fakeHub.handlerCount(NotificationHubServerEvents.notificationReceived)).toBe(0);
    expect(fakeHub.handlerCount(NotificationHubServerEvents.salesDashboardUpdated)).toBe(0);
    expect(store.items()).toEqual([]);
    expect(store.unreadCount()).toBe(0);
    expect(store.connectionState()).toBe('disconnected');
  });

  function patchAnonymous(): void {
    const signOut = authStore.signOut();
    httpTesting.expectOne(ApiRoutes.auth.signOut).flush(null, noContent);
    void signOut;
  }
});
