import { ValidationError, type RewardGoal, type Settings } from './types';
import { clock, lunchInterval, rawShift } from './shift';
import { validateHomeText } from '../i18n/homeCopy';
import { t } from '../i18n/L';

// 默认值对照 Engine.cs Settings 字段初始化 / Defaults mirror the Engine.cs field initializers.
export function defaultSettings(): Settings {
  return {
    MonthlySalary: 3000,
    Start: '09:00',
    End: '18:00',
    LunchStart: '12:00',
    LunchEnd: '13:30',
    LunchEnabled: true,
    Weekdays: '1,2,3,4,5',
    RestDates: '',
    WorkDates: '',
    Basis: 'actual',
    Decimals: 3,
    Grouping: true,
    Currency: 'CNY',
    Pattern: '{symbol}{amount}',
    Animation: 'flip',
    Font: 'DIN',
    Theme: 'midnight',
    Speed: 420,
    GoalName: t('goal.default'),
    GoalAmount: 3000,
    Goals: null,
    Topmost: false,
    Configured: false,
    Language: 'auto',
    HomeText: null,
    ExpensesEnabled: false,
    Expenses: null,
    SettlementDay: null,
    OpeningAt: null,
    OpeningIncome: null,
  };
}

export function parseDays(value: string): Set<number> {
  const result = new Set<number>();
  for (const part of (value ?? '').split(',')) {
    if (part.trim() === '') continue;
    const day = Number(part.trim());
    if (!Number.isInteger(day) || day < 0 || day > 6) throw new ValidationError('validation.day');
    result.add(day);
  }
  return result;
}

export function parseDates(value: string | null): Set<string> {
  const result = new Set<string>();
  for (const token of (value ?? '').split(/[,，;\r\n ]/)) {
    if (token.trim() === '') continue;
    if (!/^\d{4}-\d{2}-\d{2}$/.test(token.trim())) throw new ValidationError('validation.date');
    result.add(token.trim());
  }
  return result;
}

// 日期键统一用本地 yyyy-MM-dd 字符串 / Dates key on local yyyy-MM-dd strings.
export function dateKey(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

export function cycleDay(settings: Settings): number {
  return settings.SettlementDay ?? 1;
}

function tryOpening(value: string | null): boolean {
  return value !== null && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}$/.test(value);
}

export function confirmIncome(settings: Settings, now: Date, amount: number): void {
  const pad = (n: number, w = 2) => String(n).padStart(w, '0');
  settings.OpeningAt =
    `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}` +
    `T${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}.${pad(now.getMilliseconds(), 3)}0000`;
  settings.OpeningIncome = amount;
}

export function expenseItems(settings: Settings): RewardGoal[] {
  return settings.Expenses ?? [];
}

export function goalItems(settings: Settings): RewardGoal[] {
  return settings.Goals ?? [{ Name: settings.GoalName, Amount: settings.GoalAmount }];
}

// 校验规则逐条对照 Settings.Validate / Rules mirror Settings.Validate case by case.
export function validate(settings: Settings): void {
  validateHomeText(settings.HomeText);
  if (cycleDay(settings) < 1 || cycleDay(settings) > 31) throw new ValidationError('validation.settlement');
  const openingPaired = (settings.OpeningIncome !== null) === !!(settings.OpeningAt && settings.OpeningAt !== '');
  if (!openingPaired ||
    (settings.OpeningIncome !== null && (settings.OpeningIncome < 0 || settings.OpeningIncome > 100000000 || !tryOpening(settings.OpeningAt))))
    throw new ValidationError('validation.opening');
  if (settings.MonthlySalary <= 0 || settings.MonthlySalary > 100000000) throw new ValidationError('validation.salary');
  const start = clock(settings.Start), end = clock(settings.End);
  if (start === end) throw new ValidationError('validation.shift');
  if (settings.LunchEnabled) {
    const a = clock(settings.LunchStart), b = clock(settings.LunchEnd);
    if (a === b) throw new ValidationError('validation.breakEqual');
    const probe = new Date(2026, 0, 5);
    const shift = rawShift(probe, settings);
    const lunch = lunchInterval(probe, settings);
    if (lunch.Start < shift.Start || lunch.End > shift.End || lunch.End.getTime() - lunch.Start.getTime() >= shift.End.getTime() - shift.Start.getTime())
      throw new ValidationError('validation.breakBounds');
  }
  if (parseDays(settings.Weekdays ?? '').size === 0) throw new ValidationError('validation.weekdays');
  const rest = parseDates(settings.RestDates), work = parseDates(settings.WorkDates);
  for (const key of rest) if (work.has(key)) throw new ValidationError('validation.dateConflict');
  if (settings.Basis !== 'actual' && settings.Basis !== 'fixed') throw new ValidationError('validation.basis');
  if (settings.Decimals < 0 || settings.Decimals > 4) throw new ValidationError('validation.decimals');
  if (!['CNY', 'USD', 'EUR', 'JPY'].includes(settings.Currency)) throw new ValidationError('validation.currency');
  if (!['flip', 'slide', 'none'].includes(settings.Animation)) throw new ValidationError('validation.animation');
  if (!['DIN', 'Mono', 'Classic'].includes(settings.Font)) throw new ValidationError('validation.font');
  if (!['midnight', 'forest', 'paper'].includes(settings.Theme)) throw new ValidationError('validation.theme');
  if (settings.Speed < 160 || settings.Speed > 700) throw new ValidationError('validation.speed');
  if (goalItems(settings).length > 20) throw new ValidationError('validation.goalCount');
  for (const goal of goalItems(settings)) {
    if (goal == null || goal.Name.trim() === '' || goal.Name.length > 28) throw new ValidationError('validation.goalName');
    if (goal.Amount <= 0 || goal.Amount > 100000000) throw new ValidationError('validation.goalAmount');
  }
  if (expenseItems(settings).length > 20) throw new ValidationError('validation.expenseCount');
  for (const expense of expenseItems(settings)) {
    if (expense == null || expense.Name.trim() === '' || expense.Name.length > 28) throw new ValidationError('validation.expenseName');
    if (expense.Amount <= 0 || expense.Amount > 100000000) throw new ValidationError('validation.expenseAmount');
  }
  const pattern = settings.Pattern;
  if (pattern === '' || pattern == null || pattern.length > 48 || !pattern.includes('{amount}') || (pattern.match(/\{/g) ?? []).length > 3)
    throw new ValidationError('validation.pattern');
  if (pattern.indexOf('{amount}') !== pattern.lastIndexOf('{amount}')) throw new ValidationError('validation.patternOnce');
  const rest2 = pattern.replaceAll('{amount}', '').replaceAll('{symbol}', '').replaceAll('{code}', '');
  if (/[{}\r\n\t]/.test(rest2)) throw new ValidationError('validation.patternTokens');
}

export function copySettings(source: Settings): Settings {
  return fromJson(JSON.parse(JSON.stringify(toJson(source))));
}

// 持久化：localStorage + .bak 回退，镜像桌面原子写与备份语义。
// Persistence: localStorage with a .bak fallback mirroring the desktop atomic save.
const STORAGE_KEY = 'slaves-encourager.settings';

// 序列化只保留已知字段，未知成员被忽略（与桌面反序列化一致）。
// Serialization keeps known fields only; unknown members are ignored like the desktop deserializer.
export function toJson(settings: Settings): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const key of Object.keys(defaultSettings())) {
    // @ts-expect-error 索引访问由已知键列表约束 / Index access is bounded by the known key list.
    result[key] = settings[key];
  }
  return result;
}

export function fromJson(raw: unknown): Settings {
  const result = defaultSettings();
  if (raw == null || typeof raw !== 'object') return result;
  for (const key of Object.keys(result)) {
    // @ts-expect-error 同上 / Same as above.
    if (raw[key] !== undefined) result[key] = raw[key];
  }
  return result;
}

export function saveSettings(settings: Settings, storage: Storage = localStorage): void {
  validate(settings);
  const text = JSON.stringify(toJson(settings));
  const current = storage.getItem(STORAGE_KEY);
  if (current !== null) storage.setItem(STORAGE_KEY + '.bak', current);
  storage.setItem(STORAGE_KEY, text);
}

export function loadSettings(storage: Storage = localStorage): { settings: Settings; notice: boolean } {
  for (const key of [STORAGE_KEY, STORAGE_KEY + '.bak']) {
    const text = storage.getItem(key);
    if (text === null) continue;
    try {
      const settings = fromJson(JSON.parse(text));
      validate(settings);
      return { settings, notice: false };
    } catch {
      // 主键损坏时尝试备份；两者都坏则回退默认 / Try the backup, then fall back to defaults.
    }
  }
  return { settings: defaultSettings(), notice: storage.getItem(STORAGE_KEY) !== null };
}
