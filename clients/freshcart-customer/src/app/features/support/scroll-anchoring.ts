export interface ScrollViewportMetrics {
  readonly scrollTop: number;
  readonly scrollHeight: number;
  readonly clientHeight: number;
}

const NearBottomThresholdPx = 48;

export function isScrolledNearBottom(metrics: ScrollViewportMetrics): boolean {
  const distanceFromBottom = metrics.scrollHeight - metrics.scrollTop - metrics.clientHeight;
  return distanceFromBottom <= NearBottomThresholdPx;
}
