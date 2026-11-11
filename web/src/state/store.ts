import { computed, signal } from '@preact/signals';
import type { Settings, Snapshot } from '../engine/types';
import { calculate } from '../engine/engine';
import { defaultSettings, loadSettings, saveSettings } from '../engine/settings';
import { use } from '../i18n/L';
import type { Clock } from './clock';

// 全局状态：设置、界面标志与派生快照 / Global state: settings, UI flags and the derived snapshot.
export const settings = signal<Settings>(defaultSettings());
export const privacy = signal(false);
export const mini = signal(false);
export const demo = signal(false);
export const goalIndex = signal(-1);
export const loadNotice = signal('');
export const clock = signal<Clock>(() => new Date());
export const now = signal(new Date());
export const dialog = signal<null | 'settings' | 'expenses' | 'copy'>(null);
export const copyKey = signal<string | null>(null);
export const settingsTab = signal(0);

export const snapshot = computed<Snapshot>(() => calculate(now.value, settings.value));

export function initialize(): void {
  const loaded = loadSettings();
  settings.value = loaded.settings;
  loadNotice.value = loaded.notice ? 'notice' : '';
  use(loaded.settings.Language);
  if (!loaded.settings.Configured) demo.value = true;
}

export function applySettings(candidate: Settings): void {
  candidate.Configured = true;
  saveSettings(candidate);
  settings.value = candidate;
  use(candidate.Language);
  demo.value = false;
  loadNotice.value = '';
  goalIndex.value = -1;
}

// 每秒重算；隐藏标签页暂停，恢复即按当前时间重算（重算式设计天然容错）。
// Recompute every second; hidden tabs pause and resume by wall clock (stateless recalculation tolerates both).
export function startTicker(): () => void {
  const tick = () => {
    if (document.visibilityState === 'visible') now.value = clock.value();
  };
  tick();
  const timer = window.setInterval(tick, 1000);
  const visible = () => tick();
  document.addEventListener('visibilitychange', visible);
  return () => {
    window.clearInterval(timer);
    document.removeEventListener('visibilitychange', visible);
  };
}
