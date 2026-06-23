import { Pipe, PipeTransform } from '@angular/core';

const DefaultLocaleCacheKey = 'default';

@Pipe({ name: 'fcMoney' })
export class MoneyPipe implements PipeTransform {
  private static readonly formatterCache = new Map<string, Intl.NumberFormat>();

  transform(value: number | null | undefined, currencyCode: string, locale?: string): string {
    if (value === null || value === undefined || Number.isNaN(value)) {
      return '';
    }
    return MoneyPipe.resolveFormatter(currencyCode, locale).format(value);
  }

  private static resolveFormatter(currencyCode: string, locale?: string): Intl.NumberFormat {
    const cacheKey = `${locale ?? DefaultLocaleCacheKey}:${currencyCode}`;
    let formatter = MoneyPipe.formatterCache.get(cacheKey);
    if (!formatter) {
      formatter = new Intl.NumberFormat(locale, { style: 'currency', currency: currencyCode });
      MoneyPipe.formatterCache.set(cacheKey, formatter);
    }
    return formatter;
  }
}
