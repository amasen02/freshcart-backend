import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ApiRoutes } from '../../core/config/api-routes';
import { PaginatedResult, ProductSummary } from './catalog.models';
import { ProductListState } from './product-list.state';

function emptyPage(): PaginatedResult<ProductSummary> {
  return { pageNumber: 1, pageSize: 12, totalItemCount: 0, items: [] };
}

function pageWith(productId: string): PaginatedResult<ProductSummary> {
  return {
    pageNumber: 1,
    pageSize: 12,
    totalItemCount: 1,
    items: [
      {
        id: productId,
        name: `Product ${productId}`,
        slug: `product-${productId}`,
        sku: `SKU-${productId}`,
        basePrice: 9.99,
        currencyCode: 'USD',
        primaryImageUrl: null,
        categoryId: 'category-1',
        brandId: 'brand-1',
        isDigital: true,
        isActive: true,
      },
    ],
  };
}

describe('ProductListState', () => {
  let httpTesting: HttpTestingController;
  let state: InstanceType<typeof ProductListState>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        ProductListState,
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    state = TestBed.inject(ProductListState);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('initializeIssuesAListingRequestAndPublishesTheResults', () => {
    state.initialize(null);

    const request = httpTesting.expectOne((candidate) => candidate.url === ApiRoutes.catalog.products);
    request.flush(pageWith('alpha'));

    expect(state.status()).toBe('ready');
    expect(state.items().length).toBe(1);
    expect(state.totalItemCount()).toBe(1);
  });

  it('routesAnInitialTermThroughTheSearchEndpoint', () => {
    state.initialize('apples');

    const request = httpTesting.expectOne((candidate) => candidate.url === ApiRoutes.catalog.productSearch);
    expect(request.request.params.get('term')).toBe('apples');
    request.flush(emptyPage());

    expect(state.status()).toBe('ready');
  });

  it('changingAFilterResetsThePageToOne', () => {
    state.initialize(null);
    httpTesting.expectOne((candidate) => candidate.url === ApiRoutes.catalog.products).flush(pageWith('alpha'));

    state.goToPage(3);
    httpTesting.expectOne((candidate) => candidate.params.get('pageNumber') === '3').flush(pageWith('beta'));
    expect(state.pageNumber()).toBe(3);

    state.updateFilters({ sort: 'price-asc' });
    const request = httpTesting.expectOne((candidate) => candidate.url === ApiRoutes.catalog.products);
    expect(request.request.params.get('pageNumber')).toBe('1');
    request.flush(pageWith('gamma'));

    expect(state.pageNumber()).toBe(1);
  });

  it('switchMapCancelsTheStaleRequestSoOnlyTheLatestQueryRenders', () => {
    state.initialize(null);
    const first = httpTesting.expectOne((candidate) => candidate.url === ApiRoutes.catalog.products);

    state.updateFilters({ categoryId: 'category-9' });
    const second = httpTesting.expectOne((candidate) => candidate.params.get('categoryId') === 'category-9');

    expect(first.cancelled).toBeTrue();
    second.flush(pageWith('latest'));

    expect(state.items().length).toBe(1);
    expect(state.items()[0]?.id).toBe('latest');
  });

  it('capturesTheApiErrorWhenTheListingRequestFails', () => {
    state.initialize(null);

    httpTesting
      .expectOne((candidate) => candidate.url === ApiRoutes.catalog.products)
      .flush({ title: 'Server error.' }, { status: 500, statusText: 'Internal Server Error' });

    expect(state.status()).toBe('error');
    expect(state.error()?.status).toBe(500);
  });
});
