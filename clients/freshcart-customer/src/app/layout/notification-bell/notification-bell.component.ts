import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NgbDropdown, NgbDropdownMenu, NgbDropdownToggle } from '@ng-bootstrap/ng-bootstrap';

import { NotificationDto } from '../../core/realtime/notification.model';
import { NotificationsStore } from '../../core/realtime/notifications.store';
import { RelativeTimePipe } from '../../shared/pipes/relative-time.pipe';

const MaximumBellItems = 10;

@Component({
  selector: 'fc-notification-bell',
  imports: [NgbDropdown, NgbDropdownToggle, NgbDropdownMenu, RelativeTimePipe],
  templateUrl: './notification-bell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationBellComponent {
  private readonly notificationsStore = inject(NotificationsStore);
  private readonly router = inject(Router);

  protected readonly unreadCount = this.notificationsStore.unreadCount;
  protected readonly isReconnecting = this.notificationsStore.isReconnecting;
  protected readonly latestNotifications = computed<readonly NotificationDto[]>(() =>
    this.notificationsStore.items().slice(0, MaximumBellItems),
  );

  protected async openNotification(notification: NotificationDto): Promise<void> {
    await this.notificationsStore.markAsRead(notification.id);
    if (notification.orderId) {
      await this.router.navigate(['/orders', notification.orderId]);
    }
  }
}
