import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NgbToast } from '@ng-bootstrap/ng-bootstrap';

import { ToastKind } from '../../core/notifications/toast.model';
import { NotificationToastService } from '../../core/notifications/toast.service';

const ToastKindClasses: Readonly<Record<ToastKind, string>> = {
  success: 'text-bg-success',
  danger: 'text-bg-danger',
  info: 'text-bg-primary',
};

@Component({
  selector: 'fc-toast-host',
  imports: [NgbToast],
  templateUrl: './toast-host.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastHostComponent {
  protected readonly toastService = inject(NotificationToastService);

  protected kindClass(kind: ToastKind): string {
    return ToastKindClasses[kind];
  }
}
