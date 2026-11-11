import { describe, expect, it } from 'vitest';
import { render } from 'preact';
import { SalaryTab } from '../SettingsDialog';
import { defaultSettings } from '../../engine/settings';
import { use } from '../../i18n/L';

// 月薪输入必须能删空再重输，重渲染不得回写旧值。
// The salary input must allow deleting everything and retyping; rerenders must not restore the old value.
describe('SalaryTab amount input', () => {
  it('deletes 3000 down to empty and accepts a new number', () => {
    use('zh-Hans');
    const host = document.createElement('div');
    document.body.appendChild(host);
    const draft = defaultSettings();
    const noop = () => {};
    const draw = () => render(<SalaryTab draft={draft} touch={noop} onAdjust={noop} onOpenExpenses={noop} onOpenCalendar={noop} />, host);
    draw();
    const input = host.querySelector<HTMLInputElement>('input')!;
    expect(input.value).toBe('3000');
    for (const text of ['300', '30', '3', '']) {
      input.value = text;
      input.dispatchEvent(new Event('input', { bubbles: true }));
      draw(); // 每键后父组件重渲染 / The parent rerenders after each keystroke
      expect(input.value).toBe(text);
    }
    for (const text of ['5', '50', '5000']) {
      input.value = text;
      input.dispatchEvent(new Event('input', { bubbles: true }));
      draw();
      expect(input.value).toBe(text);
    }
    expect(draft.MonthlySalary).toBe(5000);
    host.remove();
  });
});
