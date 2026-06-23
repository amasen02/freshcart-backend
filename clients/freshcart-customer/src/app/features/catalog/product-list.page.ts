import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgbPagination } from '@ng-bootstrap/ng-bootstrap';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { BasketStore } from '../../core/basket/basket.store';
import { SearchDebounceMs } from '../../core/config/timing.tokens';
import { NotificationToastService } from '../../core/notifications/toast.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { CategoryNode, ProductSortToken } from './catalog.models';
import { CatalogApiService } from './catalog-api.service';
import { ProductListState } from './product-list.state';

const AddedToBasketQuantity = 1;

interface SortChoice {
  readonly token: ProductSortToken;
  readonly label: string;
}

const SortChoices: readonly SortChoice[] = [
  { token: 'newest', label: 'Newest' },
  { token: 'name', label: 'Name' },
  { token: 'price-asc', label: 'Price: low to high' },
  { token: 'price-desc', label: 'Price: high to low' },
];

@Component({
  selector: 'fc-product-list-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NgbPagination,
    MoneyPipe,
    LoadingSpinnerComponent,
    EmptyStateComponent,
  ],
  providers: [ProductListState],
  templateUrl: './product-list.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductListPage {
  protected readonly state = inject(ProductListState);
  private readonly basketStore = inject(BasketStore);
  private readonly catalogApi = inject(CatalogApiService);
  private readonly toastService = inject(NotificationToastService);
  private readonly searchDebounceMs = inject(SearchDebounceMs);

  readonly term = input<string | undefined>();

  private static readonly noCategories: readonly CategoryNode[] = [];

  protected readonly sortChoices = SortChoices;
  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly categories = toSignal(this.catalogApi.getCategoryTree(), {
    initialValue: ProductListPage.noCategories,
  });
  protected readonly flattenedCategories = computed(() => flattenCategories(this.categories()));

  private readonly pendingProductIds = signal<ReadonlySet<string>>(new Set<string>());

  constructor() {
    const initialTerm = this.term() ?? '';
    this.searchControl.setValue(initialTerm, { emitEvent: false });
    this.state.initialize(initialTerm.length > 0 ? initialTerm : null);

    this.searchControl.valueChanges
      .pipe(debounceTime(this.searchDebounceMs), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((value) => this.state.setSearchTerm(value));

    effect(() => {
      const incomingTerm = this.term() ?? '';
      untracked(() => {
        if (incomingTerm !== this.searchControl.value) {
          this.searchControl.setValue(incomingTerm, { emitEvent: false });
          this.state.setSearchTerm(incomingTerm);
        }
      });
    });
  }

  protected onSortChange(token: ProductSortToken): void {
    this.state.updateFilters({ sort: token });
  }

  protected onCategoryChange(rawCategoryId: string): void {
    this.state.updateFilters({ categoryId: rawCategoryId.length > 0 ? rawCategoryId : null });
  }

  protected onPageChange(pageNumber: number): void {
    this.state.goToPage(pageNumber);
  }

  protected isAdding(productId: string): boolean {
    return this.pendingProductIds().has(productId);
  }

  protected async addToBasket(productId: string, productName: string): Promise<void> {
    if (this.isAdding(productId)) {
      return;
    }
    this.markPending(productId, true);
    try {
      await this.basketStore.addItem(productId, AddedToBasketQuantity);
      this.toastService.showSuccess(`${productName} added to your basket.`);
    } finally {
      this.markPending(productId, false);
    }
  }

  private markPending(productId: string, isPending: boolean): void {
    this.pendingProductIds.update((current) => {
      const next = new Set(current);
      if (isPending) {
        next.add(productId);
      } else {
        next.delete(productId);
      }
      return next;
    });
  }
}

interface CategoryOption {
  readonly id: string;
  readonly label: string;
}

function flattenCategories(nodes: readonly CategoryNode[], depth = 0): readonly CategoryOption[] {
  return nodes.flatMap((node) => [
    { id: node.id, label: `${'  '.repeat(depth)}${node.name}` },
    ...flattenCategories(node.children, depth + 1),
  ]);
}
