import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import { Brand, CategoryNode, PaginatedResult, ProductDetails, ProductQuery, ProductSummary } from './catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogApiService {
  private readonly httpClient = inject(HttpClient);

  listProducts(query: ProductQuery): Observable<PaginatedResult<ProductSummary>> {
    const term = query.term?.trim() ?? '';
    if (term.length > 0) {
      return this.httpClient.get<PaginatedResult<ProductSummary>>(ApiRoutes.catalog.productSearch, {
        params: this.buildSearchParams(term, query),
      });
    }
    return this.httpClient.get<PaginatedResult<ProductSummary>>(ApiRoutes.catalog.products, {
      params: this.buildListingParams(query),
    });
  }

  getProductBySlug(slug: string): Observable<ProductDetails> {
    return this.httpClient.get<ProductDetails>(ApiRoutes.catalog.productBySlug(slug));
  }

  getCategoryTree(): Observable<readonly CategoryNode[]> {
    return this.httpClient.get<readonly CategoryNode[]>(ApiRoutes.catalog.categories);
  }

  getBrands(): Observable<readonly Brand[]> {
    return this.httpClient.get<readonly Brand[]>(ApiRoutes.catalog.brands);
  }

  private buildListingParams(query: ProductQuery): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize)
      .set('sort', query.sort);
    if (query.categoryId) {
      params = params.set('categoryId', query.categoryId);
    }
    if (query.brandId) {
      params = params.set('brandId', query.brandId);
    }
    if (query.maxPrice !== null) {
      params = params.set('maxPrice', query.maxPrice);
    }
    if (query.isDigital !== null) {
      params = params.set('isDigital', query.isDigital);
    }
    return params;
  }

  private buildSearchParams(term: string, query: ProductQuery): HttpParams {
    return new HttpParams()
      .set('term', term)
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);
  }
}
