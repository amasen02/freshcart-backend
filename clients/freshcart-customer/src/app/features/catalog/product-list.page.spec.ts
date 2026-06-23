import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { SearchDebounceMs } from '../../core/config/timing.tokens';
import { ApiRoutes } from '../../core/config/api-routes';
import { PaginatedResult, ProductSummary } from './catalog.models';
import { ProductListPage } from './product-list.page';

const TestDebounceMs = 10;

function emptyPage(): PaginatedResult<ProductSummary> {
  return { pageNumber: 1, pageSize: 12, totalItemCount: 0, items: [] };
}

function waitLongerThanDebounce(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, TestDebounceMs * 3));
}

describe('ProductListPage', () => {
  let httpTesting: HttpTestingController;
  let fixture: ComponentFixture<ProductListPage>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SearchDebounceMs, useValue: TestDebounceMs },
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ProductListPage);
  });

  function settleInitialRequests(): void {
    fixture.detectChanges();
    httpTesting.match((candidate) => candidate.url === ApiRoutes.catalog.products).forEach((r) => r.flush(emptyPage()));
    httpTesting.match((candidate) => candidate.url === ApiRoutes.catalog.categories).forEach((r) => r.flush([]));
  }

  function searchInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('#product-search') as HTMLInputElement;
  }

  function typeSearch(value: string): void {
    const input = searchInput();
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function searchRequests() {
    return httpTesting.match((candidate) => candidate.url === ApiRoutes.catalog.productSearch);
  }

  it('collapsesRapidKeystrokesIntoOneDebouncedSearchRequest', async () => {
    settleInitialRequests();

    for (const value of ['a', 'ap', 'app', 'appl', 'apple']) {
      typeSearch(value);
    }

    await waitLongerThanDebounce();

    const requests = searchRequests();
    expect(requests.length).toBe(1);
    expect(requests[0]?.request.params.get('term')).toBe('apple');
    requests.forEach((request) => request.flush(emptyPage()));
  });

  it('ignoresAKeystrokeThatDoesNotChangeTheTerm', async () => {
    settleInitialRequests();

    typeSearch('milk');
    await waitLongerThanDebounce();
    searchRequests().forEach((request) => request.flush(emptyPage()));

    typeSearch('milk');
    await waitLongerThanDebounce();

    expect(searchRequests().length).toBe(0);
  });
});
