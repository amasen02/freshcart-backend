import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, combineLatest, firstValueFrom, map, of, switchMap } from 'rxjs';

import { AuthStore } from '../../core/auth/auth.store';
import { BasketStore } from '../../core/basket/basket.store';
import { ApiError } from '../../core/http/api-error.model';
import { NotificationToastService } from '../../core/notifications/toast.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { RatingStarsComponent } from '../../shared/components/rating-stars.component';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { ProductDetails } from './catalog.models';
import { CatalogApiService } from './catalog-api.service';
import { ReviewsApiService } from './reviews-api.service';
import { ProductReviewsResult, buildStarDistribution } from './reviews.models';

const MinimumQuantity = 1;
const MaximumQuantity = 99;
const ReviewsPageSize = 10;
const MinimumTitleLength = 5;
const MaximumTitleLength = 120;
const MinimumBodyLength = 10;
const MaximumBodyLength = 4000;
const RatingChoices: readonly number[] = [5, 4, 3, 2, 1];

interface ProductResourceState {
  readonly product: ProductDetails | null;
  readonly error: ApiError | null;
}

@Component({
  selector: 'fc-product-detail-page',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    DecimalPipe,
    MoneyPipe,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    RatingStarsComponent,
  ],
  templateUrl: './product-detail.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductDetailPage {
  protected readonly authStore = inject(AuthStore);
  private readonly basketStore = inject(BasketStore);
  private readonly catalogApi = inject(CatalogApiService);
  private readonly reviewsApi = inject(ReviewsApiService);
  private readonly toastService = inject(NotificationToastService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly slug = input.required<string>();

  protected readonly ratingChoices = RatingChoices;
  protected readonly minimumQuantity = MinimumQuantity;
  protected readonly maximumQuantity = MaximumQuantity;

  protected readonly quantity = signal(MinimumQuantity);
  protected readonly isAddingToBasket = signal(false);
  protected readonly isSubmittingReview = signal(false);
  protected readonly reviewFormVisible = signal(false);

  private readonly reviewsRefreshToken = signal(0);

  private readonly productResource = toSignal(
    toObservable(this.slug).pipe(
      switchMap((slug) =>
        this.catalogApi.getProductBySlug(slug).pipe(
          map((product): ProductResourceState => ({ product, error: null })),
          catchError((error: unknown) => of<ProductResourceState>({ product: null, error: ApiError.fromUnknown(error) })),
        ),
      ),
    ),
    { initialValue: { product: null, error: null } satisfies ProductResourceState },
  );

  protected readonly product = computed<ProductDetails | null>(() => this.productResource().product);
  protected readonly loadError = computed<ApiError | null>(() => this.productResource().error);
  protected readonly isLoading = computed(() => this.product() === null && this.loadError() === null);

  private readonly productSku = computed(() => this.product()?.sku ?? null);

  private readonly reviewsResource = toSignal(
    combineLatest([toObservable(this.productSku), toObservable(this.reviewsRefreshToken)]).pipe(
      switchMap(([sku]) =>
        sku === null
          ? of<ProductReviewsResult | null>(null)
          : this.reviewsApi
              .getProductReviews(sku, 1, ReviewsPageSize)
              .pipe(catchError(() => of<ProductReviewsResult | null>(null))),
      ),
    ),
    { initialValue: null },
  );

  protected readonly reviews = computed<ProductReviewsResult | null>(() => this.reviewsResource());
  protected readonly distribution = computed(() => {
    const summary = this.reviews()?.summary;
    return summary ? buildStarDistribution(summary) : [];
  });

  protected readonly reviewForm = this.formBuilder.group({
    rating: this.formBuilder.control(5, [Validators.required]),
    title: this.formBuilder.control('', [
      Validators.required,
      Validators.minLength(MinimumTitleLength),
      Validators.maxLength(MaximumTitleLength),
    ]),
    body: this.formBuilder.control('', [
      Validators.required,
      Validators.minLength(MinimumBodyLength),
      Validators.maxLength(MaximumBodyLength),
    ]),
  });

  protected adjustQuantity(delta: number): void {
    this.quantity.update((current) => clampQuantity(current + delta));
  }

  protected onQuantityInput(rawValue: string): void {
    const parsed = Number.parseInt(rawValue, 10);
    this.quantity.set(Number.isNaN(parsed) ? MinimumQuantity : clampQuantity(parsed));
  }

  protected async addToBasket(): Promise<void> {
    const product = this.product();
    if (!product || this.isAddingToBasket()) {
      return;
    }
    this.isAddingToBasket.set(true);
    try {
      await this.basketStore.addItem(product.id, this.quantity());
      this.toastService.showSuccess(`${product.name} added to your basket.`);
    } finally {
      this.isAddingToBasket.set(false);
    }
  }

  protected showReviewForm(): void {
    this.reviewFormVisible.set(true);
  }

  protected async submitReview(): Promise<void> {
    const product = this.product();
    if (!product || this.reviewForm.invalid || this.isSubmittingReview()) {
      this.reviewForm.markAllAsTouched();
      return;
    }
    this.isSubmittingReview.set(true);
    const formValue = this.reviewForm.getRawValue();
    try {
      await firstValueFrom(
        this.reviewsApi.createReview({
          productSku: product.sku,
          rating: formValue.rating,
          title: formValue.title.trim(),
          body: formValue.body.trim(),
        }),
      );
      this.reviewForm.reset({ rating: 5, title: '', body: '' });
      this.reviewFormVisible.set(false);
      this.reviewsRefreshToken.update((token) => token + 1);
      this.toastService.showSuccess('Thank you. Your review is awaiting moderation.');
    } catch (error: unknown) {
      this.toastService.showDanger(ApiError.fromUnknown(error).detail);
    } finally {
      this.isSubmittingReview.set(false);
    }
  }

  protected isReviewFieldInvalid(controlName: 'title' | 'body'): boolean {
    const control = this.reviewForm.controls[controlName];
    return control.invalid && control.touched;
  }
}

function clampQuantity(value: number): number {
  return Math.min(MaximumQuantity, Math.max(MinimumQuantity, value));
}
