import type { Interval, Settings } from './types';
import { ValidationError } from './types';

// 班次时间换算，无其他依赖 / Shift time helpers with no other dependencies.

// "HH:mm" 解析为当日分钟数 / Parse "HH:mm" into minutes-of-day.
export function clock(value: string): number {
  const match = /^([01]\d|2[0-3]):([0-5]\d)$/.exec(value ?? '');
  if (!match) throw new ValidationError('validation.time');
  return Number(match[1]) * 60 + Number(match[2]);
}

export function atMinutes(date: Date, minutes: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 0, minutes);
}

export function rawShift(date: Date, settings: Settings): Interval {
  const start = atMinutes(date, clock(settings.Start));
  const end = atMinutes(date, clock(settings.End));
  if (end.getTime() <= start.getTime()) end.setDate(end.getDate() + 1);
  return { Start: start, End: end };
}

export function lunchInterval(date: Date, settings: Settings): Interval {
  const shift = rawShift(date, settings);
  const start = atMinutes(date, clock(settings.LunchStart));
  if (start.getTime() < shift.Start.getTime()) start.setDate(start.getDate() + 1);
  const end = atMinutes(start, clock(settings.LunchEnd));
  if (end.getTime() <= start.getTime()) end.setDate(end.getDate() + 1);
  return { Start: start, End: end };
}

export function segments(date: Date, settings: Settings): Interval[] {
  const shift = rawShift(date, settings);
  if (!settings.LunchEnabled) return [shift];
  const lunch = lunchInterval(date, settings);
  return [{ Start: shift.Start, End: lunch.Start }, { Start: lunch.End, End: shift.End }];
}

export function secondsOf(interval: Interval): number {
  return (interval.End.getTime() - interval.Start.getTime()) / 1000;
}

export function elapsed(now: Date, parts: Interval[]): number {
  return parts.reduce(
    (sum, s) => sum + Math.max(0, (Math.min(now.getTime(), s.End.getTime()) - s.Start.getTime()) / 1000),
    0,
  );
}
