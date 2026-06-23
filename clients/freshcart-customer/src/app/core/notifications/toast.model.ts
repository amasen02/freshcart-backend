export type ToastKind = 'success' | 'danger' | 'info';

export interface ToastMessage {
  readonly id: number;
  readonly kind: ToastKind;
  readonly text: string;
  readonly autohideMs: number;
}
