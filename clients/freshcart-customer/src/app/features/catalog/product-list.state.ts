import { computed, inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { pipe, switchMap, tap } from 'rxjs';
import { catchError, of } from 'rxjs';

import { ApiError } from '../../core/http/api-error.model';
import { CatalogApiService } from './catalog-api.service';
import { PaginatedResult, ProductQuery, ProductSortToken, ProductSummary, totalPageCount } from './catalog.models';

export const DefaultPageSize = 12;

export interface ProductFilters {
  readonly term: string | null;
  readonly categoryId: string | null;
  readonly brandId: string | null;
  readonly maxPrice: number | null;
  readonly isDigital: boolean | null;
  readonly sort: ProductSortToken;
}

const InitialFilters: ProductFilters = {
  term: null,
  categoryId: null,
  brandId: null,
  maxPrice: null,
  isDigital: null,
  sort: 'newest',
};

type ListStatus = 'idle' | 'loading' | 'ready' | 'error';

interface ProductListStateShape {
  filters: ProductFilters;
  pageNumber: number;
  items: readonly ProductSummary[];
  totalItemCount: number;
  status: ListStatus;
  error: ApiError | null;
}

const initialState: ProductListStateShape = {
  filters: InitialFilters,
  pageNumber: 1,
  items: [],
  totalItemCount: 0,
  status: 'idle',
  error: null,
};

export const ProductListState = signalStore(
  withState(initialState),
  withComputed(({ filters, pageNumber, totalItemCount, items, status }) => ({
    pageSize: computed(() => DefaultPageSize),
    totalPages: computed(() => totalPageCount({ totalItemCount: totalItemCount(), pageSize: DefaultPageSize })),
    isEmptyResult: computed(() => status() === 'ready' && items().length === 0),
    activeQuery: computed<ProductQuery>(() => ({
      ...filters(),
      pageNumber: pageNumber(),
      pageSize: DefaultPageSize,
    })),
  })),
  withMethods((store) => {
    const catalogApi = inject(CatalogApiService);

    // switchMap drops the in-flight request when a newer query arrives, so a slow earlier
    // response can never overwrite the results of a more recent search.
    const runQuery = rxMethod<ProductQuery>(
      pipe(
        tap(() => patchState(store, { status: 'loading', error: null })),
        switchMap((query) =>
          catalogApi.listProducts(query).pipe(
            tap((page: PaginatedResult<ProductSummary>) =>
              patchState(store, {
                items: page.items,
                totalItemCount: page.totalItemCount,
                status: 'ready',
              }),
            ),
            catchError((error: unknown) => {
              patchState(store, { status: 'error', error: ApiError.fromUnknown(error) });
              return of(null);
            }),
          ),
        ),
      ),
    );

    const reload = (): void => {
      runQuery(store.activeQuery());
    };

    return {
      initialize(initialTerm: string | null): void {
        patchState(store, {
          filters: { ...InitialFilters, term: initialTerm },
          pageNumber: 1,
        });
        reload();
      },

      setSearchTerm(term: string): void {
        const normalized = term.trim();
        patchState(store, {
          filters: { ...store.filters(), term: normalized.length > 0 ? normalized : null },
          pageNumber: 1,
        });
        reload();
      },

      updateFilters(partial: Partial<ProductFilters>): void {
        patchState(store, {
          filters: { ...store.filters(), ...partial },
          pageNumber: 1,
        });
        reload();
      },

      goToPage(pageNumber: number): void {
        if (pageNumber === store.pageNumber()) {
          return;
        }
        patchState(store, { pageNumber });
        reload();
      },

      clearError(): void {
        patchState(store, { error: null });
      },
    };
  }),
);
