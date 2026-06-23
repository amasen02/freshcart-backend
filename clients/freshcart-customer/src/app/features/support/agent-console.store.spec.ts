import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ApiRoutes } from '../../core/config/api-routes';
import { TypingIndicatorDebounceMs } from '../../core/config/timing.tokens';
import { FakeHubConnection } from '../../core/realtime/fake-hub-connection';
import { RealtimeConnection } from '../../core/realtime/realtime-connection';
import { SignalrConnectionFactory } from '../../core/realtime/signalr-connection.factory';
import { AgentConsoleStore } from './agent-console.store';
import { SupportHubClientMethods } from './support-hub.events';
import { ChatSessionDto } from './support.models';

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));
const TypingDebounceMs = 400;

function activeSession(overrides: Partial<ChatSessionDto> = {}): ChatSessionDto {
  return {
    sessionId: 'session-1',
    topic: 'Where is my order',
    customerId: 'customer-1',
    customerDisplayName: 'Sam Shopper',
    agentId: 'agent-1',
    agentDisplayName: 'Agent Avery',
    status: 'Active',
    startedOnUtc: '2026-06-18T10:00:00Z',
    ...overrides,
  };
}

describe('AgentConsoleStore', () => {
  let fakeHub: FakeHubConnection;
  let store: InstanceType<typeof AgentConsoleStore>;
  let httpTesting: HttpTestingController;

  function configure(): void {
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
        { provide: TypingIndicatorDebounceMs, useValue: TypingDebounceMs },
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AgentConsoleStore);
  }

  async function startWith(sessions: readonly ChatSessionDto[]): Promise<void> {
    const starting = store.start();
    await settle();
    httpTesting.expectOne(ApiRoutes.support.activeSessions).flush(sessions);
    await starting;
  }

  const typingSends = () =>
    fakeHub.sent.filter((entry) => entry.methodName === SupportHubClientMethods.setTyping);

  it('reportTypingSendsTrueOnceThenTrailingFalseAfterTheDebounceWindow', async () => {
    configure();
    const session = activeSession();
    await startWith([session]);

    jasmine.clock().install();
    jasmine.clock().mockDate(new Date(0));
    try {
      store.reportTyping();
      store.reportTyping();

      expect(typingSends().length).toBe(1);
      expect(typingSends()[0]?.args).toEqual([session.sessionId, true]);

      jasmine.clock().tick(TypingDebounceMs + 1);

      expect(typingSends().length).toBe(2);
      expect(typingSends()[1]?.args).toEqual([session.sessionId, false]);
    } finally {
      jasmine.clock().uninstall();
    }
    httpTesting.verify();
  });

  it('switchingSessionWhileTypingClearsThePreviousSessionAndAnnouncesTheNewOne', async () => {
    configure();
    const first = activeSession({ sessionId: 'session-1' });
    const second = activeSession({ sessionId: 'session-2' });
    await startWith([first, second]);

    store.selectSession(first.sessionId);
    await settle();
    httpTesting.expectOne(ApiRoutes.support.sessionMessages(first.sessionId)).flush({ items: [] });

    store.reportTyping();
    expect(typingSends().length).toBe(1);
    expect(typingSends()[0]?.args).toEqual([first.sessionId, true]);

    store.selectSession(second.sessionId);
    await settle();
    httpTesting.expectOne(ApiRoutes.support.sessionMessages(second.sessionId)).flush({ items: [] });

    store.reportTyping();
    expect(typingSends().length).toBe(3);
    expect(typingSends()[1]?.args).toEqual([first.sessionId, false]);
    expect(typingSends()[2]?.args).toEqual([second.sessionId, true]);

    httpTesting.verify();
  });
});
