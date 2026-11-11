import { beforeEach, describe, expect, it } from 'vitest';
import { render } from 'preact';
import { App } from '../../app';
import { initialize, settings } from '../../state/store';
import { defaultSettings } from '../../engine/settings';
import { t, use } from '../../i18n/L';

// 界面冒烟：固定设置渲染主面板 / UI smoke: render the main panel with fixed settings.
// 每次挂载到独立节点，避免清空 preact 容器缓存 / Mount to a fresh host each time; never wipe the container.
function mount(): HTMLElement {
  const host = document.createElement('div');
  document.body.appendChild(host);
  render(<App />, host);
  return host;
}

describe('app smoke', () => {
  beforeEach(() => {
    localStorage.clear();
    use('zh-Hans');
    settings.value = defaultSettings();
  });

  it('renders the main panel with configured settings', () => {
    const data = defaultSettings();
    data.MonthlySalary = 21750;
    data.Configured = true;
    settings.value = data;
    const host = mount();
    const text = host.textContent ?? '';
    expect(text).toContain('¥');
    expect(text).toContain(t('main.today'));
    expect(text.length).toBeGreaterThan(50);
    host.remove();
  });

  it('shows the first-use setup hint for unconfigured settings', () => {
    settings.value = defaultSettings();
    const host = mount();
    expect(host.textContent).toContain(t('main.setupHint'));
    host.remove();
  });

  it('renders unconfigured settings from an empty store', () => {
    localStorage.clear();
    initialize();
    expect(settings.value.Configured).toBe(false);
  });
});
