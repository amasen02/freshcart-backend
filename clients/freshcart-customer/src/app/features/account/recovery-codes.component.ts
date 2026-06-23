import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'fc-recovery-codes',
  template: `
    <div class="modal-header">
      <h2 class="modal-title h5">Save your recovery codes</h2>
      <button type="button" class="btn-close" aria-label="Close" (click)="activeModal.close()"></button>
    </div>
    <div class="modal-body">
      <p class="text-body-secondary">
        Store these codes somewhere safe. Each one can be used once if you lose access to your authenticator.
        They will not be shown again.
      </p>
      <ul class="list-group">
        @for (code of recoveryCodes(); track code) {
          <li class="list-group-item font-monospace">{{ code }}</li>
        }
      </ul>
    </div>
    <div class="modal-footer">
      <button type="button" class="btn btn-outline-secondary" (click)="copyCodes()">
        <i class="bi bi-clipboard me-1" aria-hidden="true"></i>{{ copyLabel() }}
      </button>
      <button type="button" class="btn btn-primary" (click)="activeModal.close()">Done</button>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecoveryCodesComponent {
  protected readonly activeModal = inject(NgbActiveModal);

  readonly recoveryCodes = signal<readonly string[]>([]);
  protected readonly copyLabel = signal('Copy codes');

  protected async copyCodes(): Promise<void> {
    await navigator.clipboard.writeText(this.recoveryCodes().join('\n'));
    this.copyLabel.set('Copied');
  }
}
