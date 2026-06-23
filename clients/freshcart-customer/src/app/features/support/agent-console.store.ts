import { DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withState } from '@ngrx/signals';
import { Subject, debounceTime, firstValueFrom } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import { TypingIndicatorDebounceMs } from '../../core/config/timing.tokens';
import { ApiError } from '../../core/http/api-error.model';
import { NotificationToastService } from '../../core/notifications/toast.service';
import { RealtimeConnectionState } from '../../core/realtime/connection-state';
import { RealtimeConnection } from '../../core/realtime/realtime-connection';
import { SignalrConnectionFactory } from '../../core/realtime/signalr-connection.factory';
import { SupportApiService } from './support-api.service';
import { SupportHubClientMethods, SupportHubServerEvents } from './support-hub.events';
import { ChatMessageDto, ChatSessionDto } from './support.models';

type ServerCallback = (...payload: readonly unknown[]) => void;

interface AgentConsoleState {
  readonly sessions: readonly ChatSessionDto[];
  readonly messagesBySession: Readonly<Record<string, readonly ChatMessageDto[]>>;
  readonly selectedSessionId: string | null;
  readonly typingSessionId: string | null;
  readonly connectionState: RealtimeConnectionState;
  readonly error: ApiError | null;
}

const initialAgentConsoleState: AgentConsoleState = {
  sessions: [],
  messagesBySession: {},
  selectedSessionId: null,
  typingSessionId: null,
  connectionState: 'disconnected',
  error: null,
};

function upsertSession(
  sessions: readonly ChatSessionDto[],
  incoming: ChatSessionDto,
): readonly ChatSessionDto[] {
  const withoutIncoming = sessions.filter((session) => session.sessionId !== incoming.sessionId);
  return [incoming, ...withoutIncoming];
}

export const AgentConsoleStore = signalStore(
  { providedIn: 'root' },
  withState(initialAgentConsoleState),
  withComputed(({ sessions, selectedSessionId, messagesBySession, connectionState }) => ({
    activeSessions: computed(() => sessions().filter((session) => session.status !== 'Ended')),
    queueLength: computed(() => sessions().filter((session) => session.status === 'Queued').length),
    selectedSession: computed<ChatSessionDto | null>(
      () => sessions().find((session) => session.sessionId === selectedSessionId()) ?? null,
    ),
    selectedMessages: computed<readonly ChatMessageDto[]>(() => {
      const sessionId = selectedSessionId();
      return sessionId === null ? [] : messagesBySession()[sessionId] ?? [];
    }),
    isConnected: computed(() => connectionState() === 'connected'),
  })),
  withMethods((store) => {
    const connectionFactory = inject(SignalrConnectionFactory);
    const supportApi = inject(SupportApiService);
    const toastService = inject(NotificationToastService);
    const typingDebounceMs = inject(TypingIndicatorDebounceMs);
    const destroyRef = inject(DestroyRef);

    const activeConnection = signal<RealtimeConnection | null>(null);
    const registeredHandlers = new Map<string, ServerCallback>();
    const typingSignals = new Subject<string>();

    // Holds the session a leading setTyping(true) was sent for, or null when no indicator is live.
    // Tracking the id rather than a bare flag lets the agent switch sessions: the new session gets
    // its own true and the previous session is cleared, so no indicator is left stuck on.
    let typingActiveSessionId: string | null = null;
    let lifecycle: Promise<void> = Promise.resolve();

    const clearTyping = (sessionId: string): void => {
      if (typingActiveSessionId === sessionId) {
        typingActiveSessionId = null;
        void activeConnection()?.send(SupportHubClientMethods.setTyping, sessionId, false);
      }
    };

    typingSignals.pipe(debounceTime(typingDebounceMs), takeUntilDestroyed(destroyRef)).subscribe(clearTyping);

    const appendMessage = (message: ChatMessageDto): void => {
      patchState(store, (current) => {
        const existing = current.messagesBySession[message.sessionId] ?? [];
        if (existing.some((candidate) => candidate.messageId === message.messageId)) {
          return current;
        }
        return {
          messagesBySession: { ...current.messagesBySession, [message.sessionId]: [...existing, message] },
        };
      });
    };

    const registerHandlers = (connection: RealtimeConnection): void => {
      const handlers: Readonly<Record<string, ServerCallback>> = {
        [SupportHubServerEvents.chatAssigned]: (payload) =>
          patchState(store, (current) => ({
            sessions: upsertSession(current.sessions, payload as ChatSessionDto),
            selectedSessionId: current.selectedSessionId ?? (payload as ChatSessionDto).sessionId,
          })),
        [SupportHubServerEvents.messageReceived]: (payload) => appendMessage(payload as ChatMessageDto),
        [SupportHubServerEvents.participantTyping]: (sessionId, _displayName, isTyping) =>
          patchState(store, {
            typingSessionId: isTyping === true && typeof sessionId === 'string' ? sessionId : null,
          }),
        [SupportHubServerEvents.chatEnded]: (sessionId) =>
          patchState(store, (current) => ({
            sessions: current.sessions.map((session) =>
              session.sessionId === sessionId ? { ...session, status: 'Ended' as const } : session,
            ),
            typingSessionId: current.typingSessionId === sessionId ? null : current.typingSessionId,
          })),
        [SupportHubServerEvents.queuePositionChanged]: () => undefined,
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

    const loadActiveSessionsAsync = async (): Promise<void> => {
      try {
        const sessions = await firstValueFrom(supportApi.listActiveSessions());
        patchState(store, (current) => ({
          sessions,
          selectedSessionId: current.selectedSessionId ?? sessions[0]?.sessionId ?? null,
        }));
      } catch (error: unknown) {
        patchState(store, { error: ApiError.fromUnknown(error) });
      }
    };

    const loadSessionHistoryAsync = async (sessionId: string): Promise<void> => {
      if (store.messagesBySession()[sessionId]) {
        return;
      }
      try {
        const history = await firstValueFrom(supportApi.listSessionMessages(sessionId));
        patchState(store, (current) => ({
          messagesBySession: { ...current.messagesBySession, [sessionId]: history },
        }));
      } catch (error: unknown) {
        patchState(store, { error: ApiError.fromUnknown(error) });
      }
    };

    const startAsync = async (): Promise<void> => {
      if (activeConnection()) {
        return;
      }
      const connection = connectionFactory.create(ApiRoutes.hubs.support);
      registerHandlers(connection);
      activeConnection.set(connection);
      try {
        await connection.start();
        await loadActiveSessionsAsync();
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
        patchState(store, initialAgentConsoleState);
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

      selectSession(sessionId: string): void {
        patchState(store, { selectedSessionId: sessionId });
        void loadSessionHistoryAsync(sessionId);
      },

      async sendMessage(text: string): Promise<void> {
        const sessionId = store.selectedSessionId();
        const connection = activeConnection();
        const trimmed = text.trim();
        if (sessionId === null || !connection || trimmed.length === 0) {
          return;
        }
        try {
          await connection.send(SupportHubClientMethods.sendMessage, sessionId, trimmed);
        } catch (error: unknown) {
          toastService.showDanger(ApiError.fromUnknown(error).detail);
        }
      },

      reportTyping(): void {
        const sessionId = store.selectedSessionId();
        const connection = activeConnection();
        if (sessionId === null || !connection) {
          return;
        }
        if (typingActiveSessionId !== null && typingActiveSessionId !== sessionId) {
          clearTyping(typingActiveSessionId);
        }
        if (typingActiveSessionId === null) {
          typingActiveSessionId = sessionId;
          void connection.send(SupportHubClientMethods.setTyping, sessionId, true);
        }
        typingSignals.next(sessionId);
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
