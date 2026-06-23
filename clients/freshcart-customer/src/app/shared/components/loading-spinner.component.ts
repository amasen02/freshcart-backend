import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'fc-loading-spinner',
  template: `
    <div class="d-flex justify-content-center py-5">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">{{ label() }}</span>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadingSpinnerComponent {
  readonly label = input('Loading');
}
