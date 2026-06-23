import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterRenderEffect,
  computed,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { RelativeTimePipe } from '../../shared/pipes/relative-time.pipe';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';
import { isScrolledNearBottom } from './scroll-anchoring';
import { ChatMessageDto, ChatSenderRole } from './support.models';

@Component({
  selector: 'fc-chat-thread',
  imports: [RelativeTimePipe, AutofocusDirective],
  templateUrl: './chat-thread.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatThreadComponent {
  readonly messages = input.required<readonly ChatMessageDto[]>();
  readonly viewerRole = input.required<ChatSenderRole>();
  readonly counterpartName = input<string>('');
  readonly counterpartTyping = input(false);
  readonly sending = input(false);
  readonly composerDisabled = input(false);

  readonly messageSubmitted = output<string>();
  readonly typingDetected = output<void>();

  private readonly scrollContainer = viewChild<ElementRef<HTMLElement>>('scrollContainer');
  protected readonly draft = signal('');
  protected readonly canSend = computed(() => this.draft().trim().length > 0 && !this.composerDisabled());

  constructor() {
    afterRenderEffect(() => {
      // Reading the message count makes the effect re-run on every new message; scroll only when the
      // viewer is already at the foot of the thread so a manual scroll-back is never yanked away.
      const messageCount = this.messages().length;
      const element = this.scrollContainer()?.nativeElement;
      if (messageCount > 0 && element && isScrolledNearBottom(element)) {
        element.scrollTop = element.scrollHeight;
      }
    });
  }

  protected isOwnMessage(message: ChatMessageDto): boolean {
    return message.senderRole === this.viewerRole();
  }

  protected onDraftInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.draft.set(textarea.value);
    this.typingDetected.emit();
  }

  protected onEnter(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    if (keyboardEvent.shiftKey) {
      return;
    }
    keyboardEvent.preventDefault();
    this.submit();
  }

  protected submit(): void {
    if (!this.canSend()) {
      return;
    }
    this.messageSubmitted.emit(this.draft().trim());
    this.draft.set('');
  }
}
