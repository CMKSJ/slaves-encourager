import { describe, expect, it } from 'vitest';
import { calculate, dayPay, expenseCoverage, expenseTotal, goals, isWorkday, payPeriod, rawShift, rewardIncome } from '../engine';
import { confirmIncome, copySettings, cycleDay, dateKey, defaultSettings, loadSettings, saveSettings, validate } from '../settings';
import { CalendarOverrides } from '../calendar';
import { ValidationError, type RewardGoal, type Settings } from '../types';
import { duration, formatAmount, moneyValue, symbol } from '../format';

// 黄金断言对照 tests/EngineTests.cs 逐条映射 / Golden assertions mirror tests/EngineTests.cs case by case.
const near = (actual: number, expected: number) => expect(actual).toBeCloseTo(expected, 5);
const at = (y: number, m: number, d: number, h = 0, mi = 0, s = 0) => new Date(y, m - 1, d, h, mi, s);
const rejects = (action: () => unknown) => expect(action).toThrow(ValidationError);

function base(): Settings {
  const s = defaultSettings();
  s.MonthlySalary = 21750;
  s.Basis = 'fixed';
  s.Start = '09:00';
  s.End = '18:00';
  s.LunchStart = '12:00';
  s.LunchEnd = '13:00';
  return s;
}

describe('shift earnings', () => {
  it('earns zero before and at shift start', () => {
    const s = base();
    validate(s);
    near(calculate(at(2026, 11, 4, 8, 59, 59), s).Today, 0);
    near(calculate(at(2026, 11, 4, 9, 0, 0), s).Today, 0);
  });
  it('accrues one hour at fixed daily pay', () => {
    near(calculate(at(2026, 11, 4, 10, 0, 0), base()).Today, 125);
    near(calculate(at(2026, 11, 4, 12, 0, 0), base()).Today, 375);
    near(calculate(at(2026, 11, 4, 12, 59, 59), base()).Today, 375);
  });
  it('reports lunch status', () => {
    expect(calculate(at(2026, 11, 4, 12, 30, 0), base()).StatusKey).toBe('status.break');
  });
  it('caps the daily total and stops after shift', () => {
    near(calculate(at(2026, 11, 4, 18, 0, 0), base()).Today, 1000);
    near(calculate(at(2026, 11, 4, 23, 59, 59), base()).Today, 1000);
    near(calculate(at(2026, 11, 7, 11, 0, 0), base()).Today, 0);
    near(calculate(at(2026, 11, 4, 15, 0, 0), base()).Today, 625);
  });
  it('counts the countdown including lunch and to the next day', () => {
    expect(calculate(at(2026, 11, 4, 9, 0, 0), base()).CountdownSeconds / 3600).toBe(9);
    expect(calculate(at(2026, 11, 4, 18, 0, 0), base()).CountdownSeconds / 3600).toBe(15);
  });
});

describe('pay cycle', () => {
  it('reconciles actual-day months and clears on rollover', () => {
    const s = base();
    s.Basis = 'actual';
    near(calculate(at(2026, 11, 30, 23, 59, 59), s).Month, 21750);
    near(calculate(at(2026, 12, 1, 8, 0, 0), s).Month, 0);
  });
  it('honors manual rest and makeup overrides', () => {
    const s = base();
    s.Basis = 'actual';
    s.RestDates = '2026-11-04';
    s.WorkDates = '2026-11-07';
    validate(s);
    expect(isWorkday(at(2026, 11, 4), s)).toBe(false);
    expect(isWorkday(at(2026, 11, 7), s)).toBe(true);
    near(calculate(at(2026, 11, 30, 23, 59, 59), s).Month, 21750);
    s.WorkDates = '2026-11-04';
    rejects(() => validate(s));
  });
  it('bounds cycles with clamping, leap years and year crossings', () => {
    const cycle = base();
    cycle.SettlementDay = 15;
    expect(dateKey(payPeriod(at(2026, 11, 14), cycle).Start)).toBe('2026-10-15');
    expect(dateKey(payPeriod(at(2026, 11, 14), cycle).End)).toBe('2026-11-15');
    expect(dateKey(payPeriod(at(2026, 11, 15), cycle).Start)).toBe('2026-11-15');
    expect(dateKey(payPeriod(at(2026, 11, 15), cycle).End)).toBe('2026-12-15');
    cycle.SettlementDay = 31;
    expect(dateKey(payPeriod(at(2026, 2, 28), cycle).Start)).toBe('2026-02-28');
    expect(dateKey(payPeriod(at(2026, 2, 28), cycle).End)).toBe('2026-03-31');
    expect(dateKey(payPeriod(at(2028, 2, 29), cycle).Start)).toBe('2028-02-29');
    expect(dateKey(payPeriod(at(2026, 1, 1), cycle).Start)).toBe('2025-12-31');
    cycle.SettlementDay = 0;
    rejects(() => validate(cycle));
    cycle.SettlementDay = 32;
    rejects(() => validate(cycle));
  });
  it('reconciles a complete non-calendar cycle and resets at settlement', () => {
    const cycle = base();
    cycle.SettlementDay = 15;
    cycle.Basis = 'actual';
    cycle.LunchEnd = '13:00';
    near(calculate(at(2026, 11, 14, 23, 59, 59), cycle).Month, 21750);
    near(calculate(at(2026, 11, 15, 0, 0, 0), cycle).Month, 0);
  });
  it('keeps overnight shifts inside their starting cycle', () => {
    const nightCycle = base();
    nightCycle.SettlementDay = 15;
    nightCycle.Start = '22:00';
    nightCycle.End = '06:00';
    nightCycle.LunchEnabled = false;
    nightCycle.Basis = 'fixed';
    nightCycle.Weekdays = '0,1,2,3,4,5,6';
    near(calculate(at(2026, 11, 15, 1, 0, 0), nightCycle).Month, 0);
    near(calculate(at(2026, 11, 15, 23, 0, 0), nightCycle).Month, 125);
  });
});

describe('overnight shifts', () => {
  const night = (): Settings => {
    const s = base();
    s.Start = '22:00';
    s.End = '06:00';
    s.LunchStart = '02:00';
    s.LunchEnd = '03:00';
    validate(s);
    return s;
  };
  it('attributes Friday night work to Friday', () => {
    const snapshot = calculate(at(2026, 11, 7, 1, 0, 0), night());
    expect(dateKey(snapshot.WorkDate)).toBe('2026-11-06');
    near(snapshot.Today, (1000 * 3) / 7);
  });
  it('excludes the overnight lunch and ends at the exact boundary', () => {
    near(calculate(at(2026, 11, 7, 2, 30, 0), night()).Today, (1000 * 4) / 7);
    expect(calculate(at(2026, 11, 7, 6, 0, 0), night()).Today).toBe(0);
  });
  it('spreads a no-lunch schedule across all hours', () => {
    const s = base();
    s.LunchEnabled = false;
    near(calculate(at(2026, 11, 4, 12, 30, 0), s).Today, (1000 * 3.5) / 9);
  });
});

describe('formatting', () => {
  it('rounds midpoints away from zero with grouping', () => {
    const s = base();
    s.Decimals = 2;
    s.Grouping = true;
    expect(formatAmount(1234.565, s)).toBe('¥1,234.57');
  });
  it('carries digits at the grouping boundary', () => {
    const s = base();
    s.Decimals = 0;
    expect(formatAmount(999.9, s)).toBe('¥1,000');
  });
  it('supports custom suffixes and four decimals', () => {
    const s = base();
    s.Decimals = 4;
    s.Grouping = false;
    s.Pattern = '{amount} 元';
    expect(formatAmount(1234.56789, s)).toBe('1234.5679 元');
  });
  it('renders the currency template', () => {
    const s = base();
    s.Decimals = 4;
    s.Grouping = false;
    s.Currency = 'USD';
    s.Pattern = '{symbol}{amount} {code}';
    expect(formatAmount(10, s)).toBe('$10.0000 USD');
    expect(symbol('EUR')).toBe('€');
    expect(moneyValue(1234.5)).toBe('1,234.50');
  });
  it('formats durations that cross 24 hours and round up', () => {
    expect(duration(90061)).toBe('25:01:01');
    expect(duration(0.1)).toBe('00:00:01');
  });
});

describe('validation', () => {
  it('rejects broken patterns, precision, times and workweeks', () => {
    const s = base();
    s.Pattern = '{amount}{amount}';
    rejects(() => validate(s));
    s.Pattern = '{unknown}{amount}';
    rejects(() => validate(s));
    s.Pattern = '{amount}';
    s.Decimals = 7;
    rejects(() => validate(s));
    s.Decimals = 2;
    s.Start = '25:00';
    rejects(() => validate(s));
    s.Start = '09:00';
    s.LunchStart = '08:00';
    rejects(() => validate(s));
    s.LunchStart = '09:00';
    s.LunchEnd = '18:00';
    rejects(() => validate(s));
    s.LunchStart = '12:00';
    s.LunchEnd = '13:00';
    s.Weekdays = '';
    rejects(() => validate(s));
  });
});

describe('calendar overrides', () => {
  it('applies exclusive clicks and toggles off', () => {
    const dates = new CalendarOverrides('2026-11-06', '2026-11-07');
    dates.Click(at(2026, 11, 6), 'work');
    expect(dates.Rest.has('2026-11-06')).toBe(false);
    expect(dates.Work.has('2026-11-06')).toBe(true);
    dates.Click(at(2026, 11, 6), 'work');
    expect(dates.Work.has('2026-11-06')).toBe(false);
    dates.Click(at(2026, 11, 7), 'schedule');
    expect(dates.Rest.size).toBe(0);
    expect(dates.Work.size).toBe(0);
  });
  it('serializes months in order and feeds payroll', () => {
    const dates = new CalendarOverrides('', '');
    dates.Click(at(2026, 12, 5), 'rest');
    dates.Click(at(2026, 11, 6), 'rest');
    expect(CalendarOverrides.serialize(dates.Rest)).toBe('2026-11-06,2026-12-05');
    const adjusted = base();
    adjusted.RestDates = CalendarOverrides.serialize(dates.Rest);
    adjusted.WorkDates = '2026-11-07';
    validate(adjusted);
    expect(isWorkday(at(2026, 11, 6), adjusted)).toBe(false);
    expect(isWorkday(at(2026, 11, 7), adjusted)).toBe(true);
  });
});

describe('goal allocation', () => {
  const multiple = (): Settings => {
    const s = defaultSettings();
    s.Goals = [goal('咖啡', 150), goal('旅行', 3000), goal('耳机', 1600)];
    validate(s);
    return s;
  };
  const goal = (name: string, amount: number): RewardGoal => ({ Name: name, Amount: amount });

  it('allocates income in order without double counting', () => {
    const allocation = goals(3000, multiple());
    near(allocation[0].Allocated, 150);
    near(allocation[1].Allocated, 2850);
    near(allocation[2].Allocated, 0);
    near(allocation.reduce((sum, g) => sum + g.Allocated, 0), 3000);
    near(allocation[1].RemainingToReach, 150);
    near(goals(0, multiple()).reduce((sum, g) => sum + g.Allocated, 0), 0);
  });
  it('isolates copies and rejects invalid lists', () => {
    const original = multiple();
    const cloned = copySettings(original);
    cloned.Goals![0].Name = '已编辑';
    expect(original.Goals![0].Name).toBe('咖啡');
    const bad = defaultSettings();
    bad.Goals = [goal('无效', 0)];
    rejects(() => validate(bad));
    bad.Goals = Array.from({ length: 21 }, () => goal('目标', 10));
    rejects(() => validate(bad));
  });
  it('keeps an empty list empty', () => {
    const empty = defaultSettings();
    empty.Goals = [];
    validate(empty);
    expect(goals(3000, empty)).toHaveLength(0);
  });
});

describe('expense coverage', () => {
  const costs = (): Settings => {
    const s = defaultSettings();
    s.ExpensesEnabled = true;
    s.Expenses = [{ Name: '租金', Amount: 3000 }, { Name: '生活费', Amount: 2000 }];
    s.Goals = [{ Name: '旅行', Amount: 1000 }];
    validate(s);
    return s;
  };
  it('covers expenses in order before rewards', () => {
    const coverage = expenseCoverage(3500, costs());
    near(coverage[0].Remaining, 0);
    near(coverage[1].Covered, 500);
    expect(coverage[1].RemainingFraction).toBe(0.75);
    expect(expenseCoverage(0, costs())[0].RemainingFraction).toBe(1);
  });
  it('never double counts between expenses and goals', () => {
    const s = costs();
    near(goals(3500, s)[0].Allocated, 0);
    near(goals(3500, s)[0].RemainingToReach, 2500);
    near(goals(5500, s)[0].Allocated, 500);
    near(expenseCoverage(5500, s).reduce((sum, e) => sum + e.Covered, 0) + goals(5500, s).reduce((sum, g) => sum + g.Allocated, 0), 5500);
    expect(expenseCoverage(6000, s).every((e) => e.RemainingFraction === 0)).toBe(true);
    near(expenseCoverage(-1, s).reduce((sum, e) => sum + e.Covered, 0), 0);
  });
  it('excludes disabled expenses but keeps the list', () => {
    const s = costs();
    s.ExpensesEnabled = false;
    expect(expenseCoverage(3500, s)).toHaveLength(0);
    expect(expenseTotal(s)).toBe(0);
    near(goals(500, s)[0].Allocated, 500);
    expect(s.Expenses!.length).toBe(2);
    expect(defaultSettings().ExpensesEnabled).toBe(false);
    near(rewardIncome(500, s), 500);
  });
  it('rejects zero amounts and overlong lists', () => {
    const s = costs();
    s.Expenses = [...s.Expenses!, { Name: 'invalid', Amount: 0 }];
    rejects(() => validate(s));
    s.Expenses = Array.from({ length: 21 }, () => ({ Name: 'cost', Amount: 1 }));
    rejects(() => validate(s));
  });
  it('resets coverage at settlement', () => {
    const cycle = costs();
    cycle.SettlementDay = 15;
    cycle.Basis = 'actual';
    expect(expenseCoverage(calculate(at(2026, 11, 15), cycle).Month, cycle)[0].RemainingFraction).toBe(1);
    expect(goals(calculate(at(2026, 11, 15), cycle).Month, cycle)[0].Allocated).toBe(0);
  });
});

describe('confirmed opening income', () => {
  it('replaces the estimate and continues accruing', () => {
    const anchored = base();
    anchored.Basis = 'fixed';
    const confirmedAt = at(2026, 11, 4, 10, 0, 0);
    confirmIncome(anchored, confirmedAt, 40);
    validate(anchored);
    near(calculate(confirmedAt, anchored).Month, 40);
    near(calculate(at(2026, 11, 4, 11, 0, 0), anchored).Month, 165);
    near(calculate(at(2026, 11, 5, 10, 0, 0), anchored).Month, 1040);
  });
  it('supports zero starts and expires next cycle', () => {
    const anchored = base();
    const confirmedAt = at(2026, 11, 4, 10, 0, 0);
    confirmIncome(anchored, confirmedAt, 0);
    near(calculate(confirmedAt, anchored).Month, 0);
    near(calculate(at(2026, 12, 1, 10, 0, 0), anchored).Month, 125);
  });
  it('rejects malformed anchors', () => {
    const anchored = base();
    confirmIncome(anchored, at(2026, 11, 4, 10, 0, 0), 40);
    anchored.OpeningIncome = -1;
    rejects(() => validate(anchored));
    anchored.OpeningIncome = 0;
    anchored.OpeningAt = 'broken';
    rejects(() => validate(anchored));
    anchored.OpeningAt = null;
    rejects(() => validate(anchored));
  });
});

describe('settings persistence', () => {
  it('round-trips settings and keeps a backup', () => {
    localStorage.clear();
    const saved = defaultSettings();
    saved.Configured = true;
    saved.GoalName = '周末出发';
    saveSettings(saved);
    const first = loadSettings();
    expect(first.settings.GoalName).toBe('周末出发');
    expect(first.settings.Configured).toBe(true);
    expect(first.notice).toBe(false);
    saved.MonthlySalary = 20000;
    saveSettings(saved);
    expect(localStorage.getItem('slaves-encourager.settings.bak')).toBeTruthy();
    expect(loadSettings().settings.MonthlySalary).toBe(20000);
  });
  it('falls back to defaults on corrupt data without destroying it', () => {
    localStorage.clear();
    localStorage.setItem('slaves-encourager.settings', '{broken');
    const result = loadSettings();
    expect(result.settings.Configured).toBe(false);
    expect(result.notice).toBe(true);
    expect(localStorage.getItem('slaves-encourager.settings')).toBe('{broken');
  });
  it('ignores unknown fields such as legacy stock flags', () => {
    localStorage.clear();
    localStorage.setItem('slaves-encourager.settings', JSON.stringify({
      MonthlySalary: 9000, Start: '09:00', End: '18:00', LunchStart: '12:00', LunchEnd: '13:30',
      StocksEnabled: true, LegacyField: 'x',
    }));
    const { settings } = loadSettings();
    expect(settings.MonthlySalary).toBe(9000);
    expect('StocksEnabled' in settings).toBe(false);
    expect('LegacyField' in settings).toBe(false);
  });
  it('accepts old JSON without optional fields', () => {
    localStorage.clear();
    const stored = JSON.parse(JSON.stringify(defaultSettings()));
    delete stored.ExpensesEnabled;
    delete stored.Language;
    delete stored.SettlementDay;
    localStorage.setItem('slaves-encourager.settings', JSON.stringify(stored));
    const { settings, notice } = loadSettings();
    expect(cycleDay(settings)).toBe(1);
    expect(settings.ExpensesEnabled).toBe(false);
    expect(settings.OpeningIncome).toBeNull();
    expect(notice).toBe(false);
  });
});

describe('misc engine surface', () => {
  it('exposes raw shift bounds and daily pay', () => {
    const s = base();
    const shift = rawShift(at(2026, 11, 4), s);
    expect(shift.Start.getHours()).toBe(9);
    expect(shift.End.getHours()).toBe(18);
    near(dayPay(at(2026, 11, 4), s), 1000);
  });
});
