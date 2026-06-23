import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const MaximumRating = 5;
const StarPositions: readonly number[] = Array.from({ length: MaximumRating }, (_, index) => index + 1);

type StarFill = 'full' | 'half' | 'empty';

@Component({
  selector: 'fc-rating-stars',
  template: `
    <span class="d-inline-flex align-items-center" [attr.aria-label]="ariaLabel()">
      @for (star of stars(); track star.position) {
        <i class="bi text-warning" [class.bi-star-fill]="star.fill === 'full'"
          [class.bi-star-half]="star.fill === 'half'"
          [class.bi-star]="star.fill === 'empty'" aria-hidden="true"></i>
      }
      @if (showCount() && reviewCount() > 0) {
        <small class="text-body-secondary ms-1">({{ reviewCount() }})</small>
      }
    </span>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RatingStarsComponent {
  readonly rating = input.required<number>();
  readonly reviewCount = input(0);
  readonly showCount = input(true);

  protected readonly stars = computed<readonly { position: number; fill: StarFill }[]>(() => {
    const value = this.rating();
    return StarPositions.map((position) => ({ position, fill: resolveFill(value, position) }));
  });

  protected readonly ariaLabel = computed(() => `Rated ${this.rating().toFixed(1)} out of ${MaximumRating}`);
}

function resolveFill(rating: number, position: number): StarFill {
  if (rating >= position) {
    return 'full';
  }
  return rating >= position - 0.5 ? 'half' : 'empty';
}
