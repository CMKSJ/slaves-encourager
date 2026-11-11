import type { ExpenseProgress, GoalProgress, Interval, Settings, Snapshot } from './types';
import { ValidationError } from './types';
import { cycleDay, dateKey, expenseItems, goalItems, parseDates, parseDays } from './settings';

// 计算核心逐行对照 Engine.cs；金额用 number，比较用毫秒精度。
// The calculation core mirrors Engine.cs; money uses number, comparisons use millisecond precision.

export function goals(income: number, settings: Settings): GoalProgress[] {
  const cycleIncome = Math.max(0, income);
  let available = rewardIncome(income, settings);
  let before = 0;
  const result: GoalProgress[] = [];
  for (const goal of goalItems(settings)) {
    const allocated = Math.max(0, Math.min(goal.Amount, available - before));
    before += goal.Amount;
    result.push({
      Goal: goal,
      Allocated: allocated,
      Fraction: allocated / goal.Amount,
      RemainingToReach: Math.max(0, before + expenseTotal(settings) - cycleIncome),
    });
  }
  return result;
}

// 同一份周期收入先覆盖必要开支，再进入奖励 / Cover expenses before rewards using each cycle's income once.
export function expenseTotal(settings: Settings): number {
  return settings.ExpensesEnabled ? expenseItems(settings).reduce((sum, item) => sum + item.Amount, 0) : 0;
}

export function rewardIncome(income: number, settings: Settings): number {
  return Math.max(0, income - expenseTotal(settings));
}

export function expenseCoverage(income: number, settings: Settings): ExpenseProgress[] {
  const result: ExpenseProgress[] = [];
  if (!settings.ExpensesEnabled) return result;
  let available = Math.max(0, income);
  for (const expense of expenseItems(settings)) {
    const covered = Math.min(available, expense.Amount);
    available -= covered;
    const remaining = expense.Amount - covered;
    result.push({ Item: expense, Covered: covered, Remaining: remaining, RemainingFraction: remaining / expense.Amount });
  }
  return result;
}

// "HH:mm" 解析为当日分钟数 / Parse "HH:mm" into minutes-of-day.
export function clock(value: string): number {
  const match = /^([01]\d|2[0-3]):([0-5]\d)$/.exec(value ?? '');
  if (!match) throw new ValidationError('validation.time');
  return Number(match[1]) * 60 + Number(match[2]);
}

export function isWorkday(date: Date, settings: Settings): boolean {
  const key = dateKey(date);
  if (parseDates(settings.WorkDates).has(key)) return true;
  if (parseDates(settings.RestDates).has(key)) return false;
  return parseDays(settings.Weekdays ?? '').has(date.getDay());
}

function atMinutes(date: Date, minutes: number): Date {
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

function secondsOf(interval: Interval): number {
  return (interval.End.getTime() - interval.Start.getTime()) / 1000;
}

function elapsed(now: Date, parts: Interval[]): number {
  return parts.reduce(
    (sum, s) => sum + Math.max(0, (Math.min(now.getTime(), s.End.getTime()) - s.Start.getTime()) / 1000),
    0,
  );
}

function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

function daysInMonth(year: number, month: number): number {
  return new Date(year, month + 1, 0).getDate();
}

export function monthDays(date: Date, settings: Settings): number {
  const period = payPeriod(date, settings);
  let count = 0;
  for (let day = period.Start; day < period.End; day = addDays(day, 1)) if (isWorkday(day, settings)) count++;
  return count;
}

function settlement(month: Date, day: number): Date {
  // 手动钳制到月末，避免 Date 构造器向次月溢出 / Clamp manually; the Date constructor overflows into next month.
  return new Date(month.getFullYear(), month.getMonth(), Math.min(day, daysInMonth(month.getFullYear(), month.getMonth())));
}

export function payPeriod(date: Date, settings: Settings): Interval {
  let month = new Date(date.getFullYear(), date.getMonth(), 1);
  if (dateKey(date) < dateKey(settlement(month, cycleDay(settings)))) month = new Date(month.getFullYear(), month.getMonth() - 1, 1);
  return { Start: settlement(month, cycleDay(settings)), End: settlement(new Date(month.getFullYear(), month.getMonth() + 1, 1), cycleDay(settings)) };
}

// 夜班按开班日归属周期；重置发生在结算日零点 / Night shifts belong to their start date; cycles reset at midnight.
function periodIncome(now: Date, settings: Settings): number {
  const period = payPeriod(now, settings);
  const daily = dayPay(now, settings);
  let income = 0;
  for (let day = period.Start; dateKey(day) <= dateKey(now); day = addDays(day, 1)) {
    if (!isWorkday(day, settings)) continue;
    const parts = segments(day, settings);
    income += daily * (elapsed(now, parts) / parts.reduce((sum, s) => sum + secondsOf(s), 0));
  }
  return income;
}

export function dayPay(date: Date, settings: Settings): number {
  const count = monthDays(date, settings);
  return settings.MonthlySalary / (settings.Basis === 'fixed' ? 21.75 : Math.max(1, count));
}

function openingDate(settings: Settings): Date | null {
  if (!settings.OpeningAt) return null;
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})\.(\d{3})/.exec(settings.OpeningAt);
  if (!match) return null;
  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]), Number(match[4]), Number(match[5]), Number(match[6]), Number(match[7]));
}

export function calculate(now: Date, settings: Settings): Snapshot {
  // 跨午夜班次归属开班日 / Overnight shifts belong to their starting date.
  let workDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const previous = rawShift(addDays(workDate, -1), settings);
  if (isWorkday(addDays(workDate, -1), settings) && now.getTime() < previous.End.getTime()) workDate = addDays(workDate, -1);
  const parts = segments(workDate, settings);
  const shift = rawShift(workDate, settings);
  const workingDay = isWorkday(workDate, settings);
  const total = parts.reduce((sum, s) => sum + secondsOf(s), 0);
  const worked = workingDay ? elapsed(now, parts) : 0;
  const daily = dayPay(workDate, settings);
  const rate = daily / total;
  const nowMs = now.getTime();
  const statusKey = !workingDay
    ? 'status.rest'
    : nowMs < shift.Start.getTime()
      ? 'status.before'
      : nowMs >= shift.End.getTime()
        ? 'status.after'
        : parts.some((s) => nowMs >= s.Start.getTime() && nowMs < s.End.getTime())
          ? 'status.earning'
          : 'status.break';
  let countdownKey = 'main.countdownOn';
  let countdown = 0;
  if (workingDay && nowMs >= shift.Start.getTime() && nowMs < shift.End.getTime()) {
    countdownKey = 'main.countdownOff';
    countdown = (shift.End.getTime() - nowMs) / 1000;
  } else {
    let next = workDate;
    for (let i = 0; i < 370; i++, next = addDays(next, 1)) {
      const start = rawShift(next, settings).Start;
      if (start.getTime() > nowMs && isWorkday(next, settings)) {
        countdown = (start.getTime() - nowMs) / 1000;
        break;
      }
    }
  }
  const period = payPeriod(now, settings);
  let month = periodIncome(now, settings);
  const anchor = openingDate(settings);
  if (settings.OpeningIncome !== null && anchor !== null && now.getTime() >= anchor.getTime() &&
    anchor.getTime() >= period.Start.getTime() && anchor.getTime() < period.End.getTime())
    month = settings.OpeningIncome + Math.max(0, month - periodIncome(anchor, settings));
  return {
    WorkDate: workDate,
    Total: total,
    Worked: worked,
    Progress: worked / total,
    Daily: daily,
    Rate: rate,
    Hourly: rate * 3600,
    Today: rate * worked,
    MonthDays: monthDays(now, settings),
    StatusKey: statusKey,
    CountdownKey: countdownKey,
    CountdownSeconds: countdown,
    PeriodStart: period.Start,
    PeriodEnd: period.End,
    Month: month,
  };
}
