import { beforeEach, describe, expect, it } from 'vitest';
import { code, keys, languages, resolve, t, use } from '../L';
import { homeCopy, homeCopyKeys, raw, reset, resetLanguage, set } from '../homeCopy';
import { ValidationError } from '../../engine/types';
import { defaultSettings, validate } from '../../engine/settings';
import { calculate } from '../../engine/engine';

describe('language resolution', () => {
  it('maps regional variants and falls back to English', () => {
    expect(resolve('zh-HK')).toBe('zh-Hant');
    expect(resolve('zh-CN')).toBe('zh-Hans');
    expect(resolve('es-MX')).toBe('es');
    expect(resolve('ja-JP')).toBe('ja');
    expect(resolve('ko-KR')).toBe('ko');
    expect(resolve('fr-FR')).toBe('en');
  });
});

describe('catalog completeness', () => {
  beforeEach(() => use('zh-Hans'));

  it('keeps every translation non-empty in all six languages', () => {
    expect(languages.length).toBe(6);
    for (const language of languages) {
      use(language);
      expect(code).toBe(language);
      expect(keys().length).toBeGreaterThanOrEqual(180);
      for (const key of keys()) expect(t(key).trim().length).toBeGreaterThan(0);
      expect(t('main.hours', 1.5, 8.0).length).toBeGreaterThan(10);
      expect(t('goal.index', 2, 3)).toContain('2');
      validate(defaultSettings());
    }
  });
  it('reports localized validation error keys', () => {
    for (const language of languages) {
      use(language);
      const invalid = defaultSettings();
      invalid.MonthlySalary = 0;
      try {
        validate(invalid);
        throw new Error('expected salary rejection');
      } catch (error) {
        expect(error).toBeInstanceOf(ValidationError);
        expect((error as ValidationError).key).toBe('validation.salary');
        expect((error as ValidationError).key).toBeTruthy();
      }
    }
  });
});

describe('home copy overrides', () => {
  beforeEach(() => use('zh-Hans'));

  it('stores overrides per language and survives persistence', () => {
    const copy = defaultSettings();
    set(copy, 'main.headline', '今天也在为自己赚钱。');
    set(copy, 'mode.privacy', '保护金额');
    set(copy, 'main.hours', '努力 {0:F2} / {1:F2} 小时');
    expect(homeCopy(copy, 'main.headline')).toBe('今天也在为自己赚钱。');
    expect(homeCopy(copy, 'mode.privacy')).toBe('保护金额');
    expect(homeCopy(copy, 'main.hours', 2.5, 8.0)).toContain('2.50');
  });
  it('isolates drafts and languages', () => {
    const copy = defaultSettings();
    set(copy, 'main.headline', '今天也在为自己赚钱。');
    const draft = structuredClone(copy);
    set(draft, 'main.headline', '临时修改');
    expect(homeCopy(copy, 'main.headline')).not.toBe('临时修改');
    use('en');
    expect(raw(copy, 'main.headline')).toBe(t('main.headline'));
    set(copy, 'main.headline', 'Earn your next adventure.');
    resetLanguage(copy);
    use('zh-Hans');
    expect(raw(copy, 'main.headline')).toBe('今天也在为自己赚钱。');
    reset(copy, 'main.headline');
    expect(raw(copy, 'main.headline')).toBe(t('main.headline'));
  });
  it('rejects mismatched placeholders and overlong text', () => {
    const copy = defaultSettings();
    expect(() => set(copy, 'main.hours', '努力 {0:F2}')).toThrow(ValidationError);
    expect(() => set(copy, 'main.hours', '{0:F2} {1:F2} {2}')).toThrow(ValidationError);
    expect(() => set(copy, 'main.rate', '{{0}}')).toThrow(ValidationError);
    expect(() => set(copy, 'main.headline', 'a'.repeat(301))).toThrow(ValidationError);
  });
  it('allows hiding optional copy and never changes earnings', () => {
    const copy = defaultSettings();
    set(copy, 'main.subtitle', '');
    expect(raw(copy, 'main.subtitle')).toBe('');
    expect(defaultSettings().HomeText).toBeNull();
    expect(raw(defaultSettings(), 'main.headline')).toBe(t('main.headline'));
    const now = new Date(2026, 10, 4, 10, 0, 0);
    expect(calculate(now, copy).Today).toBeCloseTo(calculate(now, defaultSettings()).Today, 10);
    expect(homeCopyKeys().length).toBeGreaterThan(10);
  });
});
