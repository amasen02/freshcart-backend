import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { CategoryNode, ProductSummary } from '../catalog/catalog.models';
import { CatalogApiService } from '../catalog/catalog-api.service';

const FeaturedProductCount = 8;

@Component({
  selector: 'fc-home-page',
  imports: [RouterLink, MoneyPipe, LoadingSpinnerComponent],
  templateUrl: './home.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  private readonly catalogApi = inject(CatalogApiService);

  protected readonly featuredProducts = toSignal<readonly ProductSummary[] | null>(
    this.catalogApi
      .listProducts({
        term: null,
        categoryId: null,
        brandId: null,
        maxPrice: null,
        isDigital: null,
        sort: 'newest',
        pageNumber: 1,
        pageSize: FeaturedProductCount,
      })
      .pipe(
        map((page) => page.items),
        catchError(() => of<readonly ProductSummary[]>([])),
      ),
    { initialValue: null },
  );

  private static readonly noCategories: readonly CategoryNode[] = [];

  protected readonly categories = toSignal(
    this.catalogApi.getCategoryTree().pipe(catchError(() => of<readonly CategoryNode[]>([]))),
    { initialValue: HomePage.noCategories },
  );
}
