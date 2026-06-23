import { Injectable, signal } from '@angular/core';

import { ToastKind, ToastMessage } from './toast.model';

const DefaultAutohideMs = 5000;

@Injectable({ providedIn: 'root' })
export class NotificationToastService {
  private nextToastId = 1;
  private readonly toastQueue = signal<readonly ToastMessage[]>([]);

  readonly toasts = this.toastQueue.asReadonly();

  showSuccess(text: string): void {
    this.enqueue('success', text);
  }

  showDanger(text: string): void {
    this.enqueue('danger', text);
  }

  showInfo(text: string): void {
    this.enqueue('info', text);
  }

  dismiss(toastId: number): void {
    this.toastQueue.update((queue) => queue.filter((toast) => toast.id !== toastId));
  }

  private enqueue(kind: ToastKind, text: string): void {
    const toast: ToastMessage = {
      id: this.nextToastId,
      kind,
      text,
      autohideMs: DefaultAutohideMs,
    };
    this.nextToastId += 1;
    this.toastQueue.update((queue) => [...queue, toast]);
  }
}
