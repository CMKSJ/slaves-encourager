import { describe, expect, it } from 'vitest';
import { render } from 'preact';
import { AppearanceTab } from '../SettingsDialog';
import { defaultSettings, validate } from '../../engine/settings';
import { use } from '../../i18n/L';

// 选择"自定义"后必须能编辑模板，切回预设时同步取值。
// Choosing "custom" must expose the template editor; picking a preset updates the value.
describe('custom money template', () => {
  it('enters custom mode, edits and validates the template', () => {
    use('zh-Hans');
    const host = document.createElement('div');
    document.body.appendChild(host);
    const draft = defaultSettings();
    const draw = () => render(<AppearanceTab draft={draft} touch={() => {}} />, host);
    draw();

    // 布局下拉是唯一含 custom 选项的 select / The layout select is the one with a custom option
    const select = [...host.querySelectorAll<HTMLSelectElement>('select')]
      .find((entry) => [...entry.options].some((item) => item.value === 'custom'))!;
    const option = (value: string) => [...select.options].find((entry) => entry.value === value)!;
    // 初始为预设，无自定义输入框 / Starts on a preset with no custom field
    expect(select.value).toBe('{symbol}{amount}');
    expect(host.querySelectorAll('input[type="text"], input:not([type])').length).toBe(0);

    // 选自定义：输入框出现并展示全部占位符 / Pick custom: the editor appears showing every token
    select.value = 'custom';
    select.dispatchEvent(new Event('change', { bubbles: true }));
    draw();
    expect(option('custom').selected).toBe(true);
    const field = host.querySelector<HTMLInputElement>('input:not([type])')!;
    expect(field.value).toBe('{symbol}{amount} {code}');
    expect(draft.Pattern).toBe('{symbol}{amount} {code}');

    // 编辑为自定义模板 / Edit into a custom template
    field.value = '{amount} 元';
    field.dispatchEvent(new Event('input', { bubbles: true }));
    draw();
    expect(field.value).toBe('{amount} 元');
    expect(draft.Pattern).toBe('{amount} 元');
    expect(() => validate(draft)).not.toThrow();

    // 切回预设 / Switch back to a preset
    select.value = '{amount} {code}';
    select.dispatchEvent(new Event('change', { bubbles: true }));
    draw();
    expect(draft.Pattern).toBe('{amount} {code}');
    expect(host.querySelector('input:not([type])')).toBeNull();

    // 已有自定义文本时重开设置直接进入自定义模式且不覆盖 / Reopening with custom text keeps it intact
    draft.Pattern = '{amount} 元';
    host.remove();
    const reopen = document.createElement('div');
    document.body.appendChild(reopen);
    render(<AppearanceTab draft={draft} touch={() => {}} />, reopen);
    const kept = reopen.querySelector<HTMLInputElement>('input:not([type])')!;
    expect(kept.value).toBe('{amount} 元');
    reopen.remove();
  });
});
