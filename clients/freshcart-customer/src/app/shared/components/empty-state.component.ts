import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'fc-empty-state',
  template: `
    <div class="text-center py-5">
      <i [class]="iconClasses()" aria-hidden="true"></i>
      <h2 class="h5 mt-3">{{ heading() }}</h2>
      @if (message().length > 0) {
        <p class="text-body-secondary">{{ message() }}</p>
      }
      <ng-content />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyStateComponent {
  readonly heading = input.required<string>();
  readonly message = input('');
  readonly icon = input('inbox');

  protected readonly iconClasses = computed(() => `bi bi-${this.icon()} display-4 text-secondary`);
}
