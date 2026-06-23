import { PaginatedResult } from './catalog.models';

export interface ProductReview {
  readonly id: string;
  readonly productSku: string;
  readonly customerDisplayName: string;
  readonly rating: number;
  readonly title: string;
  readonly body: string;
  readonly isVerifiedPurchase: boolean;
  readonly createdOnUtc: string;
}

export interface RatingSummary {
  readonly average: number;
  readonly count: number;
  readonly fiveStarCount: number;
  readonly fourStarCount: number;
  readonly threeStarCount: number;
  readonly twoStarCount: number;
  readonly oneStarCount: number;
}

export interface ProductReviewsResult extends PaginatedResult<ProductReview> {
  readonly summary: RatingSummary;
}

export interface CreateReviewRequest {
  readonly productSku: string;
  readonly rating: number;
  readonly title: string;
  readonly body: string;
}

export interface StarDistributionBar {
  readonly star: number;
  readonly count: number;
  readonly percentage: number;
}

export function buildStarDistribution(summary: RatingSummary): readonly StarDistributionBar[] {
  const counts: readonly { star: number; count: number }[] = [
    { star: 5, count: summary.fiveStarCount },
    { star: 4, count: summary.fourStarCount },
    { star: 3, count: summary.threeStarCount },
    { star: 2, count: summary.twoStarCount },
    { star: 1, count: summary.oneStarCount },
  ];
  return counts.map((entry) => ({
    star: entry.star,
    count: entry.count,
    percentage: summary.count === 0 ? 0 : Math.round((entry.count / summary.count) * 100),
  }));
}
