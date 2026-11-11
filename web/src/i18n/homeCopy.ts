import type { Settings } from '../engine/types';
import { ValidationError } from '../engine/types';
import { code, keys, languages, t } from './L';
import { format } from '../engine/format';

// 主页文案按语言独立保存；不覆盖全局翻译 / Store home copy per language without changing global translations.
const EXTRA_KEYS = ['app.name', 'settings.open', 'goal.heading', 'goal.index', 'goal.emptyTitle', 'goal.emptyHint',
  'goal.hidden', 'goal.allDone', 'goal.done', 'goal.remaining', 'goal.estimateHelp', 'goal.previous', 'goal.next'];

export function homeCopyKeys(): string[] {
  return keys().filter((key) => key.startsWith('main.') || key.startsWith('mode.') || key.startsWith('status.') || EXTRA_KEYS.includes(key));
}

export function raw(settings: Settings, key: string): string {
  const entry = settings.HomeText?.[`${code}/${key}`];
  return entry !== undefined ? entry : t(key);
}

export function homeCopy(settings: Settings, key: string, ...values: unknown[]): string {
  const template = raw(settings, key);
  return values.length === 0 ? template : format(template, values);
}

export function set(settings: Settings, key: string, value: string): void {
  checkCopy(key, value);
  settings.HomeText ??= {};
  if (value === t(key)) delete settings.HomeText[`${code}/${key}`];
  else settings.HomeText[`${code}/${key}`] = value;
  if (Object.keys(settings.HomeText).length === 0) settings.HomeText = null;
}

export function reset(settings: Settings, key: string): void {
  if (settings.HomeText) delete settings.HomeText[`${code}/${key}`];
}

export function resetLanguage(settings: Settings): void {
  if (!settings.HomeText) return;
  for (const entry of Object.keys(settings.HomeText)) {
    if (entry.startsWith(`${code}/`)) delete settings.HomeText[entry];
  }
  if (Object.keys(settings.HomeText).length === 0) settings.HomeText = null;
}

function tokens(value: string): string[] {
  return (value.match(/\{\d+(?::[^{}]+)?\}/g) ?? []).sort();
}

// 占位符必须逐个一致，去掉占位符后不得残留花括号 / Placeholders must match exactly; no stray braces may remain.
function checkCopy(key: string, value: string): void {
  if (!homeCopyKeys().includes(key) || value == null || value.length > 300) throw new ValidationError('copy.invalid');
  const stripped = value.replace(/\{\d+(?::[^{}]+)?\}/g, '');
  if (JSON.stringify(tokens(value)) !== JSON.stringify(tokens(t(key))) || /[{}]/.test(stripped))
    throw new ValidationError('copy.tokens');
}

// 兼容 settings.validate 的入口 / Entry point used by settings.validate.
export function validateHomeText(values: Record<string, string> | null): void {
  if (!values) return;
  const allowed = homeCopyKeys();
  if (Object.keys(values).length > allowed.length * languages.length) throw new ValidationError('copy.invalid');
  for (const [entry, value] of Object.entries(values)) {
    const split = entry.indexOf('/');
    if (split <= 0 || !languages.includes(entry.slice(0, split))) throw new ValidationError('copy.invalid');
    checkCopy(entry.slice(split + 1), value);
  }
}
