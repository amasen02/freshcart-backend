import { Injectable, inject } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';

import { ConfirmDialogComponent } from './confirm-dialog.component';

export interface ConfirmDialogOptions {
  readonly title: string;
  readonly message: string;
  readonly confirmLabel?: string;
  readonly cancelLabel?: string;
  readonly destructive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly modalService = inject(NgbModal);

  async confirm(options: ConfirmDialogOptions): Promise<boolean> {
    const modalRef = this.modalService.open(ConfirmDialogComponent, { centered: true });
    const dialogInstance: ConfirmDialogComponent = modalRef.componentInstance;
    dialogInstance.title = options.title;
    dialogInstance.message = options.message;
    dialogInstance.confirmLabel = options.confirmLabel ?? 'Confirm';
    dialogInstance.cancelLabel = options.cancelLabel ?? 'Cancel';
    dialogInstance.destructive = options.destructive ?? false;

    try {
      return (await modalRef.result) === true;
    } catch {
      // NgbModal rejects the result promise when the dialog is dismissed; that is a "no".
      return false;
    }
  }
}
