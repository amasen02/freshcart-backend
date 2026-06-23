import { DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withState } from '@ngrx/signals';
import { Subject, debounceTime, firstValueFrom } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import { RealtimeConnectionState } from '../../core/realtime/connection-state';
import { RealtimeConnection } from '../../core/realtime/realtime-connection';
import { SignalrConnectionFactory } from '../../core/realtime/signalr-connection.factory';
import { TypingIndicatorDebounceMs } from '../../core/config/timing.tokens';
import { ApiError } from '../../core/http/api-error.model';
import { NotificationToastService } from '../../core/notifications/toast.service';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog.service';
import { SupportApiService } from './support-api.service';
import { SupportHubClientMethods, SupportHubServerEvents } from './support-hub.events';
import { ChatMessageDto, ChatSessionDto } from './support.models';

type ServerCallback = (...payload: readonly unknown[]) => void;

interface SupportChatState {
  readonly session: ChatSessionDto | null;
  readonly messages: readonly ChatMessageDto[];
  readonly queuePosition: number | null;
  readonly agentTyping: boolean;
  readonly widgetOpen: boolean;
  readonly isSending: boolean;
  readonly connectionState: RealtimeConnectionState;
  readonly error: ApiError | null;
}

const initialSupportChatState: SupportChatState = {
  session: null,
  messages: [],
  queuePosition: null,
  agentTyping: false,
  widgetOpen: false,
  isSending: false,
  connectionState: 'disconnected',
  error: null,
};

const EndChatConfirmation = {
  title: 'End chat',
  message: 'End this conversation with support?',
  confirmLabel: 'End chat',
  cancelLabel: 'Keep chatting',
} as const;

export const SupportChatStore = signalStore(
  { providedIn: 'root' },
  withState(initialSupportChatState),
  withComputed(({ session, queuePosition }) => ({
    isQueued: computed(() => session()?.status === 'Queued'),
    isActive: computed(() => session()?.status === 'Active'),
    hasSession: computed(() => session() !== null),
    queuePositionLabel: computed(() => {
      const position = queuePosition();
      return position === null ? '' : `You are number ${position} in the queue.`;
    }),
  })),
  withMethods((store) => {
    const connectionFactory = inject(SignalrConnectionFactory);
    const supportApi = inject(SupportApiService);
    const confirmDialog = inject(ConfirmDialogService);
    const toastService = inject(NotificationToastService);
    const typingDebounceMs = inject(TypingIndicatorDebounceMs);
    const destroyRef = inject(DestroyRef);

    const activeConnection = signal<RealtimeConnection | null>(null);
    const registeredHandlers = new Map<string, ServerCallback>();
    const typingSignals = new Subject<boolean>();
    let typingActive = false;
    let lifecycle: Promise<void> = Promise.resolve();

    typingSignals.pipe(debounceTime(typingDebounceMs), takeUntilDestroyed(destroyRef)).subscribe(() => {
      const session = store.session();
      if (session && typingActive) {
        typingActive = false;
        void activeConnection()?.send(SupportHubClientMethods.setTyping, session.sessionId, false);
      }
    });

    const replaceMessage = (incoming: ChatMessageDto): void => {
      patchState(store, (current) =>
        current.messages.some((message) => message.messageId === incoming.messageId)
          ? current
          : { messages: [...current.messages, incoming] },
      );
    };

    const registerHandlers = (connection: RealtimeConnection): void => {
      const handlers: Readonly<Record<string, ServerCallback>> = {
        [SupportHubServerEvents.chatAssigned]: (payload) =>
          patchState(store, { session: payload as ChatSessionDto, queuePosition: null }),
        [SupportHubServerEvents.messageReceived]: (payload) => replaceMessage(payload as ChatMessageDto),
        [SupportHubServerEvents.participantTyping]: (_sessionId, _displayName, isTyping) =>
          patchState(store, { agentTyping: isTyping === true }),
        [SupportHubServerEvents.chatEnded]: () =>
          patchState(store, (current) => ({
            session: current.session ? { ...current.session, status: 'Ended' as const } : null,
            agentTyping: false,
            queuePosition: null,
          })),
        [SupportHubServerEvents.queuePositionChanged]: (_sessionId, position) =>
          patchState(store, { queuePosition: typeof position === 'number' ? position : null }),
      };
      for (const [eventName, handler] of Object.entries(handlers)) {
        connection.on(eventName, handler);
        registeredHandlers.set(eventName, handler);
      }
    };

    const deregisterHandlers = (connection: RealtimeConnection): void => {
      for (const [eventName, handler] of registeredHandlers) {
        connection.off(eventName, handler);
      }
      registeredHandlers.clear();
    };

    const ensureConnectionAsync = async (): Promise<RealtimeConnection | null> => {
      const existing = activeConnection();
      if (existing) {
        return existing;
      }
      const connection = connectionFactory.create(ApiRoutes.hubs.support);
      registerHandlers(connection);
      activeConnection.set(connection);
      try {
        await connection.start();
        return connection;
      } catch (error: unknown) {
        patchState(store, { error: ApiError.fromUnknown(error) });
        return connection;
      }
    };

    const restoreActiveSessionAsync = async (): Promise<void> => {
      try {
        const sessions = await firstValueFrom(supportApi.listActiveSessions());
        const session = sessions[0];
        if (!session) {
          return;
        }
        const history = await firstValueFrom(supportApi.listSessionMessages(session.sessionId));
        patchState(store, { session, messages: history });
      } catch (error: unknown) {
        patchState(store, { error: ApiError.fromUnknown(error) });
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
        patchState(store, initialSupportChatState);
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

      async openWidget(): Promise<void> {
        patchState(store, { widgetOpen: true });
        await enqueueLifecycle(async () => {
          const connection = await ensureConnectionAsync();
          if (connection && store.session() === null) {
            await restoreActiveSessionAsync();
          }
        });
      },

      closeWidget(): void {
        patchState(store, { widgetOpen: false });
      },

      async requestChat(topic: string): Promise<void> {
        const connection = activeConnection();
        if (!connection || store.hasSession()) {
          return;
        }
        try {
          const session = await connection.invoke<ChatSessionDto>(SupportHubClientMethods.requestChat, topic);
          patchState(store, { session, messages: [], error: null });
        } catch (error: unknown) {
          patchState(store, { error: ApiError.fromUnknown(error) });
        }
      },

      async sendMessage(text: string): Promise<void> {
        const session = store.session();
        const connection = activeConnection();
        const trimmed = text.trim();
        if (!session || !connection || trimmed.length === 0 || store.isSending()) {
          return;
        }
        patchState(store, { isSending: true });
        try {
          await connection.send(SupportHubClientMethods.sendMessage, session.sessionId, trimmed);
        } catch (error: unknown) {
          patchState(store, { error: ApiError.fromUnknown(error) });
        } finally {
          patchState(store, { isSending: false });
        }
      },

      reportTyping(): void {
        const session = store.session();
        const connection = activeConnection();
        if (!session || !connection) {
          return;
        }
        if (!typingActive) {
          typingActive = true;
          void connection.send(SupportHubClientMethods.setTyping, session.sessionId, true);
        }
        typingSignals.next(false);
      },

      async endChat(): Promise<void> {
        const session = store.session();
        const connection = activeConnection();
        if (!session || !connection) {
          return;
        }
        const confirmed = await confirmDialog.confirm({ ...EndChatConfirmation, destructive: true });
        if (!confirmed) {
          return;
        }
        try {
          await connection.send(SupportHubClientMethods.endChat, session.sessionId);
        } catch (error: unknown) {
          toastService.showDanger(ApiError.fromUnknown(error).detail);
        }
      },

      stop(): Promise<void> {
        return enqueueLifecycle(stopAsync);
      },
    };
  }),
  withHooks({
    onInit(store) {
      effect(() => {
        const state = store.activeConnectionState();
        untracked(() => patchState(store, { connectionState: state }));
      });
    },
    onDestroy(store) {
      void store.stop();
    },
  }),
);
