import { describe, expect, it } from 'vitest';
import { render } from 'preact';
import { RewardListEditor } from '../RewardListEditor';
import { use } from '../../i18n/L';

// 逐键输入后强制重渲染（模拟设置页每秒预览 tick），输入不得被重置。
// Type per keystroke, then force a rerender (mimicking the per-second preview tick); inputs must keep their values.
describe('RewardListEditor typing survives rerenders', () => {
  it('keeps the name while the parent rerenders every second', () => {
    use('zh-Hans');
    const host = document.createElement('div');
    document.body.appendChild(host);
    const items = [{ Name: '咖啡', Amount: 150 }];
    const draw = () => render(<RewardListEditor title="list" items={items} prefix="goal" onChanged={() => {}} />, host);
    draw();
    const name = host.querySelector<HTMLInputElement>('.list-row input')!;
    let typed = '';
    for (const character of '旅行基金') {
      typed += character; // 模拟全选后逐字重输 / Retype from scratch, one keystroke at a time
      name.value = typed;
      name.dispatchEvent(new Event('input', { bubbles: true }));
      draw(); // 每键后父组件重渲染 / The parent rerenders after each keystroke
    }
    expect(items[0].Name).toBe('旅行基金');
    expect(host.querySelector<HTMLInputElement>('.list-row input')!.value).toBe('旅行基金');
    host.remove();
  });

  it('allows intermediate decimal states such as "3." during typing', () => {
    use('zh-Hans');
    const host = document.createElement('div');
    document.body.appendChild(host);
    const items = [{ Name: 'goal', Amount: 3000 }];
    const draw = () => render(<RewardListEditor title="list" items={items} prefix="goal" onChanged={() => {}} />, host);
    draw();
    const amount = host.querySelector<HTMLInputElement>('.list-row .list-amount')!;
    for (const text of ['3', '3.', '3.5']) {
      amount.value = text;
      amount.dispatchEvent(new Event('input', { bubbles: true }));
      draw();
      expect(amount.value).toBe(text);
    }
    expect(items[0].Amount).toBe(3.5);
    host.remove();
  });
});
