import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import { PaginatedResult } from '../catalog/catalog.models';
import { ChatMessageDto, ChatSessionDto } from './support.models';

@Injectable({ providedIn: 'root' })
export class SupportApiService {
  private readonly httpClient = inject(HttpClient);

  listActiveSessions(): Observable<readonly ChatSessionDto[]> {
    return this.httpClient.get<readonly ChatSessionDto[]>(ApiRoutes.support.activeSessions);
  }

  listSessionMessages(sessionId: string): Observable<readonly ChatMessageDto[]> {
    return this.httpClient
      .get<PaginatedResult<ChatMessageDto>>(ApiRoutes.support.sessionMessages(sessionId))
      .pipe(map((page) => page.items));
  }
}
