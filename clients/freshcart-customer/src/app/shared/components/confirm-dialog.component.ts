import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'fc-confirm-dialog',
  template: `
    <div class="modal-header">
      <h1 class="modal-title fs-5">{{ title }}</h1>
      <button type="button" class="btn-close" aria-label="Close" (click)="activeModal.dismiss()"></button>
    </div>
    <div class="modal-body">
      <p class="mb-0">{{ message }}</p>
    </div>
    <div class="modal-footer">
      <button type="button" class="btn btn-outline-secondary" (click)="activeModal.dismiss()">
        {{ cancelLabel }}
      </button>
      <button
        type="button"
        [class]="destructive ? 'btn btn-danger' : 'btn btn-primary'"
        (click)="activeModal.close(true)">
        {{ confirmLabel }}
      </button>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialogComponent {
  protected readonly activeModal = inject(NgbActiveModal);

  // Plain properties because NgbModal assigns onto componentInstance after creation.
  title = 'Confirm';
  message = '';
  confirmLabel = 'Confirm';
  cancelLabel = 'Cancel';
  destructive = false;
}
