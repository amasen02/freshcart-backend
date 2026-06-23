import { HubConnection } from '@microsoft/signalr';

type ServerCallback = (...payload: readonly unknown[]) => void;

export interface InvokeResolution {
  readonly methodName: string;
  readonly result: unknown;
}

export class FakeHubConnection {
  readonly handlers = new Map<string, Set<ServerCallback>>();
  readonly sent: { methodName: string; args: readonly unknown[] }[] = [];
  readonly invoked: { methodName: string; args: readonly unknown[] }[] = [];
  startCount = 0;
  stopCount = 0;
  private invokeResults = new Map<string, unknown>();
  private startRejection: unknown = null;
  private reconnectingCallback: (() => void) | null = null;
  private reconnectedCallback: (() => void) | null = null;
  private closeCallback: (() => void) | null = null;

  failNextStartWith(error: unknown): void {
    this.startRejection = error;
  }

  resolveInvokeWith(methodName: string, result: unknown): void {
    this.invokeResults.set(methodName, result);
  }

  emit(methodName: string, ...payload: readonly unknown[]): void {
    for (const handler of this.handlers.get(methodName) ?? []) {
      handler(...payload);
    }
  }

  handlerCount(methodName: string): number {
    return this.handlers.get(methodName)?.size ?? 0;
  }

  on(methodName: string, handler: ServerCallback): void {
    const set = this.handlers.get(methodName) ?? new Set<ServerCallback>();
    set.add(handler);
    this.handlers.set(methodName, set);
  }

  off(methodName: string, handler: ServerCallback): void {
    this.handlers.get(methodName)?.delete(handler);
  }

  start(): Promise<void> {
    this.startCount += 1;
    if (this.startRejection !== null) {
      const rejection = this.startRejection;
      this.startRejection = null;
      return Promise.reject(rejection);
    }
    return Promise.resolve();
  }

  stop(): Promise<void> {
    this.stopCount += 1;
    return Promise.resolve();
  }

  send(methodName: string, ...args: readonly unknown[]): Promise<void> {
    this.sent.push({ methodName, args });
    return Promise.resolve();
  }

  invoke<TResult>(methodName: string, ...args: readonly unknown[]): Promise<TResult> {
    this.invoked.push({ methodName, args });
    return Promise.resolve(this.invokeResults.get(methodName) as TResult);
  }

  simulateReconnecting(): void {
    this.reconnectingCallback?.();
  }

  simulateReconnected(): void {
    this.reconnectedCallback?.();
  }

  simulateClose(): void {
    this.closeCallback?.();
  }

  onreconnecting(callback: () => void): void {
    this.reconnectingCallback = callback;
  }

  onreconnected(callback: () => void): void {
    this.reconnectedCallback = callback;
  }

  onclose(callback: () => void): void {
    this.closeCallback = callback;
  }

  asHubConnection(): HubConnection {
    // Test double: only the members RealtimeConnection touches are implemented, so the structural
    // gap to the full HubConnection surface is deliberate and confined to specs.
    return this as unknown as HubConnection;
  }
}
