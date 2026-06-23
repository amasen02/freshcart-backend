export interface NotificationDto {
  readonly id: string;
  readonly type: string;
  readonly title: string;
  readonly message: string;
  readonly orderId: string | null;
  readonly createdOnUtc: string;
  readonly isRead: boolean;
}

export interface UnreadCountResponse {
  readonly unreadCount: number;
}

const OrderStatusNotificationTypePrefix = 'Order';

export function isOrderStatusNotification(notification: NotificationDto): boolean {
  return notification.type.startsWith(OrderStatusNotificationTypePrefix);
}
