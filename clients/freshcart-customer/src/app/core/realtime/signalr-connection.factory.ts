import { Injectable } from '@angular/core';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

import { RealtimeConnection } from './realtime-connection';

const ReconnectDelaysMs: readonly number[] = [0, 2000, 5000, 10000, 30000];

@Injectable({ providedIn: 'root' })
export class SignalrConnectionFactory {
  create(hubPath: string): RealtimeConnection {
    const hubConnection = new HubConnectionBuilder()
      // The gateway forwards the Identity session cookie on the WebSocket upgrade, so no
      // accessTokenFactory is wired: bearer tokens never reach JavaScript.
      .withUrl(hubPath, { withCredentials: true })
      .withAutomaticReconnect([...ReconnectDelaysMs])
      .configureLogging(LogLevel.Warning)
      .build();

    return new RealtimeConnection(hubConnection);
  }
}
