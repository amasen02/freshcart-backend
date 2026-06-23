import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

import { AuthStore } from '../../core/auth/auth.store';
import { ChatThreadComponent } from './chat-thread.component';
import { SupportChatStore } from './support-chat.store';

const DefaultChatTopic = 'General enquiry';

@Component({
  selector: 'fc-support-chat-widget',
  imports: [ReactiveFormsModule, ChatThreadComponent],
  templateUrl: './support-chat-widget.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupportChatWidgetComponent {
  protected readonly store = inject(SupportChatStore);
  protected readonly authStore = inject(AuthStore);
  protected readonly topicControl = new FormControl<string>(DefaultChatTopic, { nonNullable: true });

  protected toggle(): void {
    if (this.store.widgetOpen()) {
      this.store.closeWidget();
      return;
    }
    void this.store.openWidget();
  }

  protected startChat(): void {
    const topic = this.topicControl.value.trim();
    void this.store.requestChat(topic.length > 0 ? topic : DefaultChatTopic);
  }

  protected sendMessage(text: string): void {
    void this.store.sendMessage(text);
  }

  protected reportTyping(): void {
    this.store.reportTyping();
  }

  protected endChat(): void {
    void this.store.endChat();
  }
}
