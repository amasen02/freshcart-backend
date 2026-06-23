import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import { CreateReviewRequest, ProductReviewsResult } from './reviews.models';

@Injectable({ providedIn: 'root' })
export class ReviewsApiService {
  private readonly httpClient = inject(HttpClient);

  getProductReviews(productSku: string, pageNumber: number, pageSize: number): Observable<ProductReviewsResult> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.httpClient.get<ProductReviewsResult>(ApiRoutes.reviews.byProductSku(productSku), { params });
  }

  createReview(request: CreateReviewRequest): Observable<void> {
    return this.httpClient.post<void>(ApiRoutes.reviews.root, request);
  }
}
