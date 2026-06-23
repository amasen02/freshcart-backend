export interface PaginatedResult<TItem> {
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalItemCount: number;
  readonly items: readonly TItem[];
}

export interface ProductSummary {
  readonly id: string;
  readonly name: string;
  readonly slug: string;
  readonly sku: string;
  readonly basePrice: number;
  readonly currencyCode: string;
  readonly primaryImageUrl: string | null;
  readonly categoryId: string;
  readonly brandId: string;
  readonly isDigital: boolean;
  readonly isActive: boolean;
}

export interface ProductImage {
  readonly url: string;
  readonly altText: string;
  readonly isPrimary: boolean;
}

export interface ProductAttribute {
  readonly name: string;
  readonly value: string;
}

export interface ProductDetails {
  readonly id: string;
  readonly name: string;
  readonly slug: string;
  readonly description: string | null;
  readonly sku: string;
  readonly price: number;
  readonly currencyCode: string;
  readonly categoryId: string;
  readonly primaryCategory: string;
  readonly brandId: string;
  readonly brandName: string;
  readonly isActive: boolean;
  readonly isDigital: boolean;
  readonly imageUrl: string | null;
  readonly images: readonly ProductImage[];
  readonly attributes: readonly ProductAttribute[];
  readonly createdOnUtc: string;
  readonly updatedOnUtc: string;
}

export interface CategoryNode {
  readonly id: string;
  readonly name: string;
  readonly slug: string;
  readonly description: string | null;
  readonly parentCategoryId: string | null;
  readonly sortOrder: number;
  readonly children: readonly CategoryNode[];
}

export interface Brand {
  readonly id: string;
  readonly name: string;
  readonly slug: string;
  readonly logoUrl: string | null;
  readonly isActive: boolean;
}

export type ProductSortToken = 'name' | 'price-asc' | 'price-desc' | 'newest';

export interface ProductQuery {
  readonly term: string | null;
  readonly categoryId: string | null;
  readonly brandId: string | null;
  readonly maxPrice: number | null;
  readonly isDigital: boolean | null;
  readonly sort: ProductSortToken;
  readonly pageNumber: number;
  readonly pageSize: number;
}

export function totalPageCount(result: Pick<PaginatedResult<unknown>, 'totalItemCount' | 'pageSize'>): number {
  return result.pageSize <= 0 ? 0 : Math.ceil(result.totalItemCount / result.pageSize);
}
