import { MoneyPipe } from './money.pipe';

// Intl emits non-breaking and narrow non-breaking spaces that differ across ICU versions.
const normalizeSpaces = (formatted: string): string => formatted.replace(/[\u00A0\u202F]/g, ' ');

describe('MoneyPipe', () => {
  const pipe = new MoneyPipe();

  it('formatsUsDollarsWithSymbolAndGrouping', () => {
    expect(pipe.transform(1234.5, 'USD', 'en-US')).toBe('$1,234.50');
  });

  it('formatsEurosWithGermanGroupingAndTrailingSymbol', () => {
    expect(normalizeSpaces(pipe.transform(1234.5, 'EUR', 'de-DE'))).toBe('1.234,50 €');
  });

  it('formatsBritishPoundsForTheUkLocale', () => {
    expect(pipe.transform(99.99, 'GBP', 'en-GB')).toBe('£99.99');
  });

  it('returnsAnEmptyStringForNullValues', () => {
    expect(pipe.transform(null, 'USD', 'en-US')).toBe('');
  });

  it('returnsAnEmptyStringForUndefinedValues', () => {
    expect(pipe.transform(undefined, 'USD', 'en-US')).toBe('');
  });

  it('returnsAnEmptyStringForNotANumberValues', () => {
    expect(pipe.transform(Number.NaN, 'USD', 'en-US')).toBe('');
  });

  it('producesIdenticalOutputOnRepeatedCallsThroughTheFormatterCache', () => {
    const firstResult = pipe.transform(42.42, 'USD', 'en-US');
    const secondResult = pipe.transform(42.42, 'USD', 'en-US');
    expect(secondResult).toBe(firstResult);
  });
});
