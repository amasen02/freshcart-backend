import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiRoutes } from '../config/api-routes';
import { PaginatedResult } from '../../features/catalog/catalog.models';
import { NotificationDto, UnreadCountResponse } from './notification.model';

const RecentNotificationsPageSize = 20;

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly httpClient = inject(HttpClient);

  listRecent(): Observable<readonly NotificationDto[]> {
    const params = new HttpParams().set('pageSize', RecentNotificationsPageSize);
    return this.httpClient
      .get<PaginatedResult<NotificationDto>>(ApiRoutes.notifications.root, { params })
      .pipe(map((page) => page.items));
  }

  getUnreadCount(): Observable<UnreadCountResponse> {
    return this.httpClient.get<UnreadCountResponse>(ApiRoutes.notifications.unreadCount);
  }

  markAsRead(notificationId: string): Observable<void> {
    return this.httpClient.put<void>(ApiRoutes.notifications.markAsRead(notificationId), {});
  }
}
