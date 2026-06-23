export type DeliveryStatus = 'Scheduled' | 'OutForDelivery' | 'Completed' | 'Failed';

export interface DeliveryTracking {
  readonly deliveryId: string;
  readonly orderId: string;
  readonly status: DeliveryStatus;
  readonly slotStartUtc: string;
  readonly slotEndUtc: string;
  readonly driverDisplayName: string | null;
  readonly completedOnUtc: string | null;
}
