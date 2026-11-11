import catalog, { languages } from 'virtual:catalog';
import { format } from '../engine/format';

// 六语言解析与格式化对照 Localization.cs 的 L / Resolution and formatting mirror the L class.
const maps = new Map<string, Map<string, string>>(
  languages.map((language) => [language, new Map<string, string>()]),
);
for (const [key, columns] of Object.entries(catalog)) {
  languages.forEach((language, index) => maps.get(language)!.set(key, columns[index]));
}

export { languages };

const LOCALE_NAMES: Record<string, string> = {
  'zh-Hans': 'zh-CN',
  'zh-Hant': 'zh-TW',
  en: 'en-US',
  ja: 'ja-JP',
  es: 'es-ES',
  ko: 'ko-KR',
};

export function localeOf(code: string): string {
  return LOCALE_NAMES[code] ?? 'en-US';
}

export function systemLocale(): string {
  const candidates = typeof navigator !== 'undefined' ? [...(navigator.languages ?? []), navigator.language] : [];
  return candidates.find((value) => value && value !== 'auto') ?? 'en';
}

export function resolve(choice: string | null | undefined): string {
  const value = (!choice || choice === 'auto' ? systemLocale() : choice).toLowerCase();
  if (value.startsWith('zh'))
    return value.includes('hant') || value.includes('tw') || value.includes('hk') || value.includes('mo') ? 'zh-Hant' : 'zh-Hans';
  for (const language of ['ja', 'es', 'ko', 'en']) if (value.startsWith(language)) return language;
  return 'en';
}

export let code = resolve('auto');

export function use(choice: string | null | undefined): void {
  code = resolve(choice);
}

export function t(key: string, ...values: unknown[]): string {
  const entry = maps.get(code)!.get(key);
  if (entry === undefined) throw new Error(`Unknown translation key: ${key}`);
  return values.length === 0 ? entry : format(entry, values);
}

export function keys(): string[] {
  return [...maps.get('en')!.keys()].sort();
}

// 数字格式化使用当前语言区域（与桌面 CultureInfo 一致）/ Numbers format with the active UI locale.
export function localeNumber(value: number, decimals: number, grouping = true): string {
  return new Intl.NumberFormat(localeOf(code), {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
    useGrouping: grouping,
  }).format(value);
}
