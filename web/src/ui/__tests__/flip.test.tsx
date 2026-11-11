import { describe, expect, it } from 'vitest';
import { render } from 'preact';
import { FlipNumber } from '../FlipNumber';
import { defaultSettings } from '../../engine/settings';

// 数字变化时必须带有时长的进入/退出动画 / Digit changes must animate with an explicit duration.
function mount(text: string, mode: string, host: HTMLElement) {
  const settings = defaultSettings();
  settings.Animation = mode;
  settings.Speed = 420;
  render(<FlipNumber text={text} settings={settings} animate />, host);
}

describe('FlipNumber animations', () => {
  it('plays flip with a real duration when a digit changes', () => {
    const host = document.createElement('div');
    document.body.appendChild(host);
    mount('¥1.00', 'flip', host);
    expect(host.querySelector('.flip-old')).toBeNull();
    mount('¥1.01', 'flip', host);
    const old = host.querySelector('.flip-old');
    expect(old?.textContent).toBe('0');
    expect(old?.getAttribute('style')).toContain('flip-out 420ms');
    const entering = [...host.querySelectorAll('.flip-tile > span:not(.flip-old)')];
    expect(entering.at(-1)?.getAttribute('style')).toContain('flip-in 420ms');
    host.remove();
  });

  it('slides with a real duration and stays silent in none mode', () => {
    const host = document.createElement('div');
    document.body.appendChild(host);
    mount('¥1.00', 'slide', host);
    mount('¥1.01', 'slide', host);
    expect(host.querySelector('.flip-old')?.getAttribute('style')).toContain('slide-out 420ms');
    const entering = [...host.querySelectorAll('.flip-tile > span:not(.flip-old)')];
    expect(entering.at(-1)?.getAttribute('style')).toContain('slide-in 420ms');
    host.remove();

    const still = document.createElement('div');
    document.body.appendChild(still);
    mount('¥1.00', 'none', still);
    mount('¥1.01', 'none', still);
    expect(still.querySelector('.flip-old')).toBeNull();
    expect([...still.querySelectorAll('.flip-tile > span')].every((node) => node.getAttribute('style') === null)).toBe(true);
    still.remove();
  });
});
