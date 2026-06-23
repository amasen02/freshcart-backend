import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { Clock } from '../../core/config/clock';
import { NotificationDto } from '../../core/realtime/notification.model';
import { NotificationsStore } from '../../core/realtime/notifications.store';
import { NotificationBellComponent } from './notification-bell.component';

const FixedNow = Date.parse('2026-06-18T12:00:00Z');

function notification(overrides: Partial<NotificationDto> = {}): NotificationDto {
  return {
    id: 'n-1',
    type: 'OrderConfirmed',
    title: 'Order confirmed',
    message: 'Your order is on its way.',
    orderId: 'order-1',
    createdOnUtc: '2026-06-18T11:30:00Z',
    isRead: false,
    ...overrides,
  };
}

class NotificationsStoreStub {
  readonly items = signal<readonly NotificationDto[]>([]);
  readonly unreadCount = signal(0);
  readonly isReconnecting = signal(false);
  readonly markAsRead = jasmine.createSpy('markAsRead').and.resolveTo(undefined);
}

describe('NotificationBellComponent', () => {
  let fixture: ComponentFixture<NotificationBellComponent>;
  let storeStub: NotificationsStoreStub;
  let router: Router;

  beforeEach(() => {
    storeStub = new NotificationsStoreStub();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: NotificationsStore, useValue: storeStub },
        { provide: Clock, useValue: { now: () => FixedNow } },
      ],
    });
    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(NotificationBellComponent);
  });

  function nativeText(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it('hidesTheUnreadBadgeWhenThereAreNoUnreadNotifications', async () => {
    storeStub.unreadCount.set(0);
    await fixture.whenStable();
    fixture.detectChanges();

    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');
    expect(badge).toBeNull();
  });

  it('rendersTheUnreadCountInTheBadgeWhenUnreadNotificationsExist', async () => {
    storeStub.unreadCount.set(3);
    await fixture.whenStable();
    fixture.detectChanges();

    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');
    expect(badge?.textContent).toContain('3');
  });

  it('rendersAnEmptyStateWhenThereAreNoNotifications', async () => {
    await fixture.whenStable();
    fixture.detectChanges();

    expect(nativeText()).toContain('no notifications yet');
  });

  it('marksReadAndNavigatesToTheOrderWhenANotificationWithAnOrderIdIsOpened', async () => {
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);
    storeStub.items.set([notification({ id: 'n-9', orderId: 'order-42' })]);
    storeStub.unreadCount.set(1);
    await fixture.whenStable();
    fixture.detectChanges();

    await fixture.componentInstance['openNotification'](notification({ id: 'n-9', orderId: 'order-42' }));

    expect(storeStub.markAsRead).toHaveBeenCalledWith('n-9');
    expect(navigateSpy).toHaveBeenCalledWith(['/orders', 'order-42']);
  });
});
