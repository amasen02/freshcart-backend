import { computed, effect, inject, signal, untracked } from '@angular/core';
import { patchState, signalStore, withComputed, withHooks, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { AuthStore } from '../auth/auth.store';
import { ApiRoutes } from '../config/api-routes';
import { NotificationToastService } from '../notifications/toast.service';
import { RealtimeConnectionState } from './connection-state';
import { isOrderStatusNotification, NotificationDto } from './notification.model';
import { NotificationHubServerEvents } from './notification-hub.events';
import { NotificationsApiService } from './notifications-api.service';
import { RealtimeConnection } from './realtime-connection';
import { SignalrConnectionFactory } from './signalr-connection.factory';

type ServerCallback = (...payload: readonly unknown[]) => void;

interface NotificationsState {
  readonly items: readonly NotificationDto[];
  readonly unreadCount: number;
  readonly connectionState: RealtimeConnectionState;
  readonly dashboardTick: number;
}

const initialNotificationsState: NotificationsState = {
  items: [],
  unreadCount: 0,
  connectionState: 'disconnected',
  dashboardTick: 0,
};

export const NotificationsStore = signalStore(
  { providedIn: 'root' },
  withState(initialNotificationsState),
  withComputed(({ connectionState }) => ({
    isConnected: computed(() => connectionState() === 'connected'),
    isReconnecting: computed(() => connectionState() === 'reconnecting'),
  })),
  withMethods((store) => {
    const notificationsApi = inject(NotificationsApiService);
    const connectionFactory = inject(SignalrConnectionFactory);
    const toastService = inject(NotificationToastService);

    const activeConnection = signal<RealtimeConnection | null>(null);
    let notificationReceivedHandler: ServerCallback | null = null;
    let salesDashboardUpdatedHandler: ServerCallback | null = null;
    let lifecycle: Promise<void> = Promise.resolve();

    const prependNotification = (notification: NotificationDto): void => {
      patchState(store, (current) => ({
        items: [notification, ...current.items],
        unreadCount: notification.isRead ? current.unreadCount : current.unreadCount + 1,
      }));
      if (!notification.isRead && isOrderStatusNotification(notification)) {
        toastService.showInfo(notification.title);
      }
    };

    const registerHandlers = (connection: RealtimeConnection): void => {
      notificationReceivedHandler = (payload) => prependNotification(payload as NotificationDto);
      salesDashboardUpdatedHandler = () =>
        patchState(store, (current) => ({ dashboardTick: current.dashboardTick + 1 }));
      connection.on(NotificationHubServerEvents.notificationReceived, notificationReceivedHandler);
      connection.on(NotificationHubServerEvents.salesDashboardUpdated, salesDashboardUpdatedHandler);
    };

    const deregisterHandlers = (connection: RealtimeConnection): void => {
      if (notificationReceivedHandler) {
        connection.off(NotificationHubServerEvents.notificationReceived, notificationReceivedHandler);
        notificationReceivedHandler = null;
      }
      if (salesDashboardUpdatedHandler) {
        connection.off(NotificationHubServerEvents.salesDashboardUpdated, salesDashboardUpdatedHandler);
        salesDashboardUpdatedHandler = null;
      }
    };

    const loadInitialStateAsync = async (): Promise<void> => {
      const [recent, unread] = await Promise.all([
        firstValueFrom(notificationsApi.listRecent()),
        firstValueFrom(notificationsApi.getUnreadCount()),
      ]);
      patchState(store, { items: recent, unreadCount: unread.unreadCount });
    };

    const startAsync = async (): Promise<void> => {
      if (activeConnection()) {
        return;
      }
      const connection = connectionFactory.create(ApiRoutes.hubs.notifications);
      registerHandlers(connection);
      activeConnection.set(connection);
      try {
        await loadInitialStateAsync();
        await connection.start();
      } catch {
        // A failed channel must not crash the shell: the bell shows the disconnected indicator
        // and the order-confirmation poll remains the fallback path.
      }
    };

    const stopAsync = async (): Promise<void> => {
      const connection = activeConnection();
      if (!connection) {
        return;
      }
      activeConnection.set(null);
      deregisterHandlers(connection);
      try {
        await connection.stop();
      } finally {
        patchState(store, initialNotificationsState);
      }
    };

    const enqueueLifecycle = (transition: () => Promise<void>): Promise<void> => {
      lifecycle = lifecycle.then(transition);
      return lifecycle;
    };

    return {
      activeConnectionState: computed<RealtimeConnectionState>(
        () => activeConnection()?.state() ?? 'disconnected',
      ),

      start(): Promise<void> {
        return enqueueLifecycle(startAsync);
      },

      stop(): Promise<void> {
        return enqueueLifecycle(stopAsync);
      },

      async markAsRead(notificationId: string): Promise<void> {
        const target = store.items().find((item) => item.id === notificationId);
        if (!target || target.isRead) {
          return;
        }
        await firstValueFrom(notificationsApi.markAsRead(notificationId));
        patchState(store, (current) => ({
          items: current.items.map((item) => (item.id === notificationId ? { ...item, isRead: true } : item)),
          unreadCount: Math.max(0, current.unreadCount - 1),
        }));
      },
    };
  }),
  withHooks({
    onInit(store) {
      const authStore = inject(AuthStore);

      effect(() => {
        const state = store.activeConnectionState();
        untracked(() => patchState(store, { connectionState: state }));
      });

      effect(() => {
        const isAuthenticated = authStore.isAuthenticated();
        untracked(() => {
          if (isAuthenticated) {
            void store.start();
          } else {
            void store.stop();
          }
        });
      });
    },
    onDestroy(store) {
      void store.stop();
    },
  }),
);
