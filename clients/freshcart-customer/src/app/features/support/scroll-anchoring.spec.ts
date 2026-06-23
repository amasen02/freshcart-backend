import { isScrolledNearBottom } from './scroll-anchoring';

describe('isScrolledNearBottom', () => {
  it('isTrueWhenTheViewportSitsExactlyAtTheBottom', () => {
    expect(isScrolledNearBottom({ scrollTop: 600, scrollHeight: 1000, clientHeight: 400 })).toBeTrue();
  });

  it('isTrueWhenWithinTheAnchoringThresholdOfTheBottom', () => {
    expect(isScrolledNearBottom({ scrollTop: 560, scrollHeight: 1000, clientHeight: 400 })).toBeTrue();
  });

  it('isFalseWhenTheUserHasScrolledBackUpBeyondTheThreshold', () => {
    expect(isScrolledNearBottom({ scrollTop: 200, scrollHeight: 1000, clientHeight: 400 })).toBeFalse();
  });

  it('isTrueWhenContentFitsWithoutScrolling', () => {
    expect(isScrolledNearBottom({ scrollTop: 0, scrollHeight: 300, clientHeight: 400 })).toBeTrue();
  });
});
