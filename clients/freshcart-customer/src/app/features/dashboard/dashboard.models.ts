export type KpiUnit = 'Currency' | 'Count' | 'Percentage' | 'Minutes' | 'None';
export type KpiTrend = 'Up' | 'Down' | 'Flat';

export interface KpiMetric {
  readonly code: string;
  readonly displayName: string;
  readonly currentValue: number;
  readonly previousValue: number | null;
  readonly unit: KpiUnit;
  readonly description: string | null;
  readonly deltaPercentage: number | null;
  readonly trend: KpiTrend;
}

export interface ReportingPeriod {
  readonly fromUtc: string;
  readonly toUtcExclusive: string;
}

export interface SalesSnapshot {
  readonly day: string;
  readonly orderCount: number;
  readonly uniqueCustomerCount: number;
  readonly grossRevenue: number;
  readonly discountTotal: number;
  readonly refundTotal: number;
  readonly taxTotal: number;
  readonly shippingTotal: number;
  readonly netRevenue: number;
}

export interface SalesOverview {
  readonly currentPeriod: ReportingPeriod;
  readonly previousPeriod: ReportingPeriod;
  readonly current: SalesSnapshot;
  readonly previous: SalesSnapshot;
  readonly tiles: readonly KpiMetric[];
}

export interface SalesTimeSeries {
  readonly period: ReportingPeriod;
  readonly bucket: string;
  readonly points: readonly SalesSnapshot[];
}

export interface RevenueByCategoryRow {
  readonly categoryName: string;
  readonly orderCount: number;
  readonly netRevenue: number;
}

export interface RevenueByPaymentMethodRow {
  readonly paymentMethod: string;
  readonly transactionCount: number;
  readonly netRevenue: number;
}

export interface RevenueBreakdown {
  readonly period: ReportingPeriod;
  readonly byCategory: readonly RevenueByCategoryRow[];
  readonly byPaymentMethod: readonly RevenueByPaymentMethodRow[];
}

export interface TopEntityRanking {
  readonly rank: number;
  readonly entityId: string;
  readonly displayName: string;
  readonly metricValue: number;
  readonly secondaryCount: number;
  readonly thumbnail: string | null;
}

export interface TopProducts {
  readonly period: ReportingPeriod;
  readonly mode: string;
  readonly rows: readonly TopEntityRanking[];
}

export interface DashboardData {
  readonly overview: SalesOverview;
  readonly timeSeries: SalesTimeSeries;
  readonly breakdown: RevenueBreakdown;
  readonly topProducts: TopProducts;
}
