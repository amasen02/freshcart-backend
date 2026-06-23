import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { Clock } from '../../core/config/clock';
import { RelativeTimePipe } from './relative-time.pipe';

const FixedNow = Date.parse('2026-06-18T12:00:00Z');

function createPipe(): RelativeTimePipe {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      RelativeTimePipe,
      { provide: Clock, useValue: { now: () => FixedNow } },
    ],
  });
  return TestBed.inject(RelativeTimePipe);
}

describe('RelativeTimePipe', () => {
  it('rendersJustNowForVeryRecentInstants', () => {
    const pipe = createPipe();
    expect(pipe.transform('2026-06-18T11:59:30Z')).toBe('just now');
  });

  it('rendersMinutesForInstantsWithinTheHour', () => {
    const pipe = createPipe();
    expect(pipe.transform('2026-06-18T11:45:00Z')).toBe('15 minutes ago');
  });

  it('rendersHoursForInstantsWithinTheDay', () => {
    const pipe = createPipe();
    expect(pipe.transform('2026-06-18T09:00:00Z')).toBe('3 hours ago');
  });

  it('rendersDaysForInstantsWithinTheWeek', () => {
    const pipe = createPipe();
    expect(pipe.transform('2026-06-15T12:00:00Z')).toBe('3 days ago');
  });

  it('returnsEmptyStringForMissingOrUnparseableValues', () => {
    const pipe = createPipe();
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform('not-a-date')).toBe('');
  });
});
