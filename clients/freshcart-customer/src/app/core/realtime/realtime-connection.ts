import { Signal, WritableSignal, signal } from '@angular/core';
import { HubConnection } from '@microsoft/signalr';

import { RealtimeConnectionState } from './connection-state';

type ServerCallback = (...payload: readonly unknown[]) => void;

export class RealtimeConnection {
  private readonly connectionState: WritableSignal<RealtimeConnectionState> = signal('disconnected');

  readonly state: Signal<RealtimeConnectionState> = this.connectionState.asReadonly();

  constructor(private readonly hubConnection: HubConnection) {
    this.hubConnection.onreconnecting(() => this.connectionState.set('reconnecting'));
    this.hubConnection.onreconnected(() => this.connectionState.set('connected'));
    this.hubConnection.onclose(() => this.connectionState.set('disconnected'));
  }

  async start(): Promise<void> {
    if (this.connectionState() === 'connected' || this.connectionState() === 'connecting') {
      return;
    }
    this.connectionState.set('connecting');
    try {
      await this.hubConnection.start();
      this.connectionState.set('connected');
    } catch (error: unknown) {
      this.connectionState.set('disconnected');
      throw error;
    }
  }

  async stop(): Promise<void> {
    await this.hubConnection.stop();
    this.connectionState.set('disconnected');
  }

  on(methodName: string, handler: ServerCallback): void {
    this.hubConnection.on(methodName, handler);
  }

  off(methodName: string, handler: ServerCallback): void {
    this.hubConnection.off(methodName, handler);
  }

  invoke<TResult>(methodName: string, ...args: readonly unknown[]): Promise<TResult> {
    return this.hubConnection.invoke<TResult>(methodName, ...args);
  }

  send(methodName: string, ...args: readonly unknown[]): Promise<void> {
    return this.hubConnection.send(methodName, ...args);
  }
}
