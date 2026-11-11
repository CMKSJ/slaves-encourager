import type { Settings } from './types';

// MidpointRounding.AwayFromZero 的显式实现；不用 toFixed（受二进制表示干扰）。
// Explicit AwayFromZero rounding; toFixed is affected by binary representation.
export function roundAwayFromZero(value: number, decimals: number): number {
  const factor = Math.pow(10, decimals);
  return (Math.sign(value) * Math.round(Math.abs(value) * factor)) / factor;
}

// C# "N"/"F" + InvariantCulture 的等价格式化 / Equivalent of C# "N"/"F" with InvariantCulture.
export function fixed(value: number, decimals: number, grouping: boolean): string {
  const rounded = roundAwayFromZero(value, decimals);
  return new Intl.NumberFormat('en-US', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
    useGrouping: grouping,
  }).format(Math.abs(rounded) === 0 ? 0 : rounded);
}

// String.Format 的最小实现，目录只使用 {0} {1} {0:F2} {0:F1}。
// Minimal String.Format: the catalog only uses {0} {1} {0:F2} {0:F1}.
export function format(template: string, values: unknown[]): string {
  return template.replace(/\{(\d+)(?::(F)(\d))?\}/g, (_, index: string, spec?: string, digits?: string) => {
    const value = values[Number(index)];
    if (spec === 'F') return fixed(Number(value), Number(digits), false);
    return String(value);
  });
}

export function symbol(currency: string): string {
  return currency === 'USD' ? '$' : currency === 'EUR' ? '€' : '¥';
}

export function amount(value: number, settings: Settings): string {
  return fixed(value, settings.Decimals, settings.Grouping);
}

export function moneyValue(value: number): string {
  return fixed(value, 2, true);
}

export function formatMoney(value: number, settings: Settings): string {
  return symbol(settings.Currency) + moneyValue(value);
}

export function formatAmount(value: number, settings: Settings): string {
  return settings.Pattern
    .replaceAll('{amount}', amount(value, settings))
    .replaceAll('{symbol}', symbol(settings.Currency))
    .replaceAll('{code}', settings.Currency);
}

// 秒数向上取整的 HH:mm:ss，跨天不回绕 / Ceiled HH:mm:ss that does not wrap at 24 hours.
export function duration(seconds: number): string {
  const total = Math.ceil(Math.max(0, seconds));
  const h = Math.floor(total / 3600);
  const m = Math.floor(total / 60) % 60;
  const s = total % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}
