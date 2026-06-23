import { HttpClient } from '@angular/common/http';
import { computed, effect, inject, untracked } from '@angular/core';
import { patchState, signalStore, withComputed, withHooks, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { AuthStore } from '../auth/auth.store';
import { ApiRoutes } from '../config/api-routes';
import { ApiError } from '../http/api-error.model';
import { BasketDto, BasketItem, BasketStatus, BasketTotals } from './basket.model';

const DefaultCurrencyCode = 'USD';

const EmptyTotals: BasketTotals = {
  subtotal: 0,
  discountTotal: 0,
  taxTotal: 0,
  grandTotal: 0,
};

interface BasketState {
  items: readonly BasketItem[];
  totals: BasketTotals;
  currencyCode: string;
  appliedCoupon: string | null;
  status: BasketStatus;
  error: ApiError | null;
}

const initialBasketState: BasketState = {
  items: [],
  totals: EmptyTotals,
  currencyCode: DefaultCurrencyCode,
  appliedCoupon: null,
  status: 'initial',
  error: null,
};

export const BasketStore = signalStore(
  { providedIn: 'root' },
  withState(initialBasketState),
  withComputed(({ items }) => ({
    itemCount: computed(() => items().reduce((runningCount, line) => runningCount + line.quantity, 0)),
    isEmpty: computed(() => items().length === 0),
    requiresShipping: computed(() => items().some((line) => !line.isDigital)),
  })),
  withMethods((store) => {
    const httpClient = inject(HttpClient);

    // Mutations are chained so two rapid clicks can never interleave their request/reload pairs.
    let mutationChain: Promise<void> = Promise.resolve();

    const fetchBasketAsync = (): Promise<BasketDto> =>
      firstValueFrom(httpClient.get<BasketDto>(ApiRoutes.basket.root));

    const applyBasket = (basket: BasketDto): void => {
      patchState(store, {
        items: basket.items,
        totals: {
          subtotal: basket.subtotal,
          discountTotal: basket.discountTotal,
          taxTotal: basket.taxTotal,
          grandTotal: basket.grandTotal,
        },
        currencyCode: basket.currencyCode,
        appliedCoupon: basket.appliedCoupon,
        status: 'ready',
        error: null,
      });
    };

    const performMutationAsync = async (sendMutationRequest: () => Promise<unknown>): Promise<void> => {
      patchState(store, { status: 'mutating', error: null });
      try {
        await sendMutationRequest();
        applyBasket(await fetchBasketAsync());
      } catch (error: unknown) {
        patchState(store, { status: 'error', error: ApiError.fromUnknown(error) });
      }
    };

    const enqueueMutation = (sendMutationRequest: () => Promise<unknown>): Promise<void> => {
      mutationChain = mutationChain.then(() => performMutationAsync(sendMutationRequest));
      return mutationChain;
    };

    return {
      async load(): Promise<void> {
        patchState(store, { status: 'loading', error: null });
        try {
          applyBasket(await fetchBasketAsync());
        } catch (error: unknown) {
          patchState(store, { status: 'error', error: ApiError.fromUnknown(error) });
        }
      },

      addItem(productId: string, quantity: number): Promise<void> {
        return enqueueMutation(() =>
          firstValueFrom(
            httpClient.post(ApiRoutes.basket.items, { productId, quantity }, { observe: 'response' }),
          ),
        );
      },

      updateQuantity(productId: string, quantity: number): Promise<void> {
        return enqueueMutation(() =>
          firstValueFrom(
            httpClient.put(ApiRoutes.basket.item(productId), { quantity }, { observe: 'response' }),
          ),
        );
      },

      removeItem(productId: string): Promise<void> {
        return enqueueMutation(() =>
          firstValueFrom(httpClient.delete(ApiRoutes.basket.item(productId), { observe: 'response' })),
        );
      },

      applyCoupon(code: string): Promise<void> {
        return enqueueMutation(() =>
          firstValueFrom(httpClient.post(ApiRoutes.basket.coupon, { code }, { observe: 'response' })),
        );
      },

      removeCoupon(): Promise<void> {
        return enqueueMutation(() =>
          firstValueFrom(httpClient.delete(ApiRoutes.basket.coupon, { observe: 'response' })),
        );
      },

      clearAfterCheckout(): void {
        patchState(store, { ...initialBasketState, status: 'ready' });
      },

      reset(): void {
        patchState(store, initialBasketState);
      },
    };
  }),
  withHooks({
    onInit(store) {
      const authStore = inject(AuthStore);
      effect(() => {
        const isAuthenticated = authStore.isAuthenticated();
        const authStatus = authStore.status();
        untracked(() => {
          if (isAuthenticated) {
            void store.load();
          } else if (authStatus === 'anonymous') {
            store.reset();
          }
        });
      });
    },
  }),
);
