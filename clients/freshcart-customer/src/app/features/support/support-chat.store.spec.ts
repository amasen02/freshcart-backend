import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { TypingIndicatorDebounceMs } from '../../core/config/timing.tokens';
import { FakeHubConnection } from '../../core/realtime/fake-hub-connection';
import { RealtimeConnection } from '../../core/realtime/realtime-connection';
import { SignalrConnectionFactory } from '../../core/realtime/signalr-connection.factory';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog.service';
import { ApiRoutes } from '../../core/config/api-routes';
import { SupportChatStore } from './support-chat.store';
import { SupportHubClientMethods, SupportHubServerEvents } from './support-hub.events';
import { ChatMessageDto, ChatSessionDto } from './support.models';

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));
const TypingDebounceMs = 400;

const queuedSession: ChatSessionDto = {
  sessionId: 'session-1',
  topic: 'Where is my order',
  customerId: 'customer-1',
  customerDisplayName: 'Sam Shopper',
  agentId: null,
  agentDisplayName: null,
  status: 'Queued',
  startedOnUtc: '2026-06-18T10:00:00Z',
};

const assignedSession: ChatSessionDto = {
  ...queuedSession,
  agentId: 'agent-1',
  agentDisplayName: 'Agent Avery',
  status: 'Active',
};

function message(overrides: Partial<ChatMessageDto> = {}): ChatMessageDto {
  return {
    messageId: 'm-1',
    sessionId: 'session-1',
    senderId: 'agent-1',
    senderDisplayName: 'Agent Avery',
    senderRole: 'Agent',
    text: 'Hello, how can I help?',
    sentOnUtc: '2026-06-18T10:01:00Z',
    ...overrides,
  };
}

describe('SupportChatStore', () => {
  let fakeHub: FakeHubConnection;
  let confirmResult: boolean;
  let store: InstanceType<typeof SupportChatStore>;
  let httpTesting: HttpTestingController;

  function configure(): void {
    fakeHub = new FakeHubConnection();
    confirmResult = true;
    const factory: Pick<SignalrConnectionFactory, 'create'> = {
      create: () => new RealtimeConnection(fakeHub.asHubConnection()),
    };
    const confirmDialog: Pick<ConfirmDialogService, 'confirm'> = {
      confirm: () => Promise.resolve(confirmResult),
    };

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SignalrConnectionFactory, useValue: factory },
        { provide: ConfirmDialogService, useValue: confirmDialog },
        { provide: TypingIndicatorDebounceMs, useValue: TypingDebounceMs },
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SupportChatStore);
  }

  async function openWidget(): Promise<void> {
    const opening = store.openWidget();
    await settle();
    httpTesting.expectOne(ApiRoutes.support.activeSessions).flush([]);
    await opening;
  }

  it('openWidgetStartsTheConnectionRestoresNoSessionWhenNoneIsActive', async () => {
    configure();
    await openWidget();

    expect(store.widgetOpen()).toBeTrue();
    expect(fakeHub.startCount).toBe(1);
    expect(store.hasSession()).toBeFalse();
    httpTesting.verify();
  });

  it('requestChatInvokesTheHubAndStoresTheQueuedSession', async () => {
    configure();
    await openWidget();
    fakeHub.resolveInvokeWith(SupportHubClientMethods.requestChat, queuedSession);

    await store.requestChat('Where is my order');

    expect(store.session()?.status).toBe('Queued');
    expect(store.isQueued()).toBeTrue();
    httpTesting.verify();
  });

  it('chatAssignedTransitionsTheQueuedSessionToActiveAndClearsTheQueuePosition', async () => {
    configure();
    await openWidget();
    fakeHub.resolveInvokeWith(SupportHubClientMethods.requestChat, queuedSession);
    await store.requestChat('Where is my order');

    fakeHub.emit(SupportHubServerEvents.queuePositionChanged, queuedSession.sessionId, 3);
    expect(store.queuePosition()).toBe(3);

    fakeHub.emit(SupportHubServerEvents.chatAssigned, assignedSession);

    expect(store.isActive()).toBeTrue();
    expect(store.queuePosition()).toBeNull();
    httpTesting.verify();
  });

  it('messageReceivedAppendsWithoutDuplicatingTheSameMessageId', async () => {
    configure();
    await openWidget();
    fakeHub.resolveInvokeWith(SupportHubClientMethods.requestChat, assignedSession);
    await store.requestChat('Where is my order');

    fakeHub.emit(SupportHubServerEvents.messageReceived, message());
    fakeHub.emit(SupportHubServerEvents.messageReceived, message());

    expect(store.messages().length).toBe(1);
    httpTesting.verify();
  });

  it('reportTypingSendsTrueOnceThenTrailingFalseAfterTheDebounceWindow', async () => {
    configure();
    await openWidget();
    fakeHub.resolveInvokeWith(SupportHubClientMethods.requestChat, assignedSession);
    await store.requestChat('Where is my order');

    const typingSends = () => fakeHub.sent.filter((entry) => entry.methodName === SupportHubClientMethods.setTyping);

    jasmine.clock().install();
    jasmine.clock().mockDate(new Date(0));
    try {
      store.reportTyping();
      store.reportTyping();

      expect(typingSends().length).toBe(1);
      expect(typingSends()[0]?.args).toEqual([assignedSession.sessionId, true]);

      jasmine.clock().tick(TypingDebounceMs + 1);

      expect(typingSends().length).toBe(2);
      expect(typingSends()[1]?.args).toEqual([assignedSession.sessionId, false]);
    } finally {
      jasmine.clock().uninstall();
    }
    httpTesting.verify();
  });

  it('endChatSendsTheEndCommandWhenConfirmed', async () => {
    configure();
    await openWidget();
    fakeHub.resolveInvokeWith(SupportHubClientMethods.requestChat, assignedSession);
    await store.requestChat('Where is my order');

    await store.endChat();

    expect(fakeHub.sent.some((entry) => entry.methodName === SupportHubClientMethods.endChat)).toBeTrue();
    httpTesting.verify();
  });

  it('endChatDoesNothingWhenTheConfirmationIsDeclined', async () => {
    configure();
    await openWidget();
    fakeHub.resolveInvokeWith(SupportHubClientMethods.requestChat, assignedSession);
    await store.requestChat('Where is my order');
    confirmResult = false;

    await store.endChat();

    expect(fakeHub.sent.some((entry) => entry.methodName === SupportHubClientMethods.endChat)).toBeFalse();
    httpTesting.verify();
  });

  it('stopDeregistersEveryHandlerItRegisteredLeavingNoListenerLeak', async () => {
    configure();
    await openWidget();
    const registeredEvents = Object.values(SupportHubServerEvents);
    for (const eventName of registeredEvents) {
      expect(fakeHub.handlerCount(eventName)).toBe(1);
    }

    await store.stop();

    for (const eventName of registeredEvents) {
      expect(fakeHub.handlerCount(eventName)).toBe(0);
    }
    expect(fakeHub.stopCount).toBe(1);
    httpTesting.verify();
  });
});
