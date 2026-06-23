import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { ChatThreadComponent } from './chat-thread.component';
import { AgentConsoleStore } from './agent-console.store';
import { ChatSessionDto } from './support.models';

@Component({
  selector: 'fc-agent-console-page',
  imports: [ChatThreadComponent],
  templateUrl: './agent-console.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgentConsolePage {
  protected readonly store = inject(AgentConsoleStore);

  protected readonly counterpartTyping = computed(() => {
    const selectedId = this.store.selectedSessionId();
    return selectedId !== null && this.store.typingSessionId() === selectedId;
  });

  constructor() {
    void this.store.start();
  }

  protected select(session: ChatSessionDto): void {
    this.store.selectSession(session.sessionId);
  }

  protected sendMessage(text: string): void {
    void this.store.sendMessage(text);
  }

  protected reportTyping(): void {
    this.store.reportTyping();
  }
}
