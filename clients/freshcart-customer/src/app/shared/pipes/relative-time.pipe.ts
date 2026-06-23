import { Pipe, PipeTransform, inject } from '@angular/core';

import { Clock } from '../../core/config/clock';

const MillisecondsPerSecond = 1000;
const SecondsPerMinute = 60;
const MinutesPerHour = 60;
const HoursPerDay = 24;
const DaysPerWeek = 7;
const JustNowThresholdSeconds = 45;

const RelativeTimeFormatter = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' });

@Pipe({ name: 'fcRelativeTime' })
export class RelativeTimePipe implements PipeTransform {
  private readonly clock = inject(Clock);

  transform(value: string | null | undefined): string {
    if (!value) {
      return '';
    }
    const instant = Date.parse(value);
    if (Number.isNaN(instant)) {
      return '';
    }

    const elapsedSeconds = Math.round((this.clock.now() - instant) / MillisecondsPerSecond);
    if (elapsedSeconds < JustNowThresholdSeconds) {
      return 'just now';
    }

    const elapsedMinutes = Math.round(elapsedSeconds / SecondsPerMinute);
    if (Math.abs(elapsedMinutes) < MinutesPerHour) {
      return RelativeTimeFormatter.format(-elapsedMinutes, 'minute');
    }

    const elapsedHours = Math.round(elapsedMinutes / MinutesPerHour);
    if (Math.abs(elapsedHours) < HoursPerDay) {
      return RelativeTimeFormatter.format(-elapsedHours, 'hour');
    }

    const elapsedDays = Math.round(elapsedHours / HoursPerDay);
    if (Math.abs(elapsedDays) < DaysPerWeek) {
      return RelativeTimeFormatter.format(-elapsedDays, 'day');
    }

    const elapsedWeeks = Math.round(elapsedDays / DaysPerWeek);
    return RelativeTimeFormatter.format(-elapsedWeeks, 'week');
  }
}
