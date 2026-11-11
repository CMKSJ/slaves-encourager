import { useEffect } from 'preact/hooks';
import { clock, copyKey, demo, dialog, mini, privacy, settings, settingsTab } from './state/store';
import { demoClock, systemClock } from './state/clock';
import { MainPanel } from './ui/MainPanel';
import { SettingsDialog } from './ui/SettingsDialog';
import { CopyEditor, ExpensesDialog } from './ui/dialogs';
import { code, t, use } from './i18n/L';

// 根组件：主题/语言/演示时钟与对话框路由 / Root: theme, language, demo clock and dialog routing.
export function App() {
  useEffect(() => {
    use(settings.value.Language);
  }, [settings.value.Language]);

  useEffect(() => {
    document.documentElement.dataset.theme = settings.value.Theme;
    document.title = t('app.name');
  }, [settings.value.Theme, code]);

  // 演示模式使用固定起点时钟 / Demo mode runs on the fixed-start clock.
  useEffect(() => {
    clock.value = demo.value ? demoClock().clock : systemClock;
  }, [demo.value]);

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      const command = event.ctrlKey || event.metaKey;
      if (command && event.key.toLowerCase() === 'm') { mini.value = !mini.value; event.preventDefault(); }
      if (command && event.shiftKey && event.key.toLowerCase() === 'p') { privacy.value = !privacy.value; event.preventDefault(); }
      if (command && event.key.toLowerCase() === 'e') { dialog.value = 'copy'; copyKey.value = null; event.preventDefault(); }
      if (event.key === 'Escape' && mini.value && dialog.value === null) { mini.value = false; event.preventDefault(); }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  return (
    <>
      <MainPanel />
      {dialog.value === 'settings' && <SettingsDialog initialTab={settingsTab.value} onClose={() => { settingsTab.value = 0; dialog.value = null; }} />}
      {dialog.value === 'expenses' && (
        <ExpensesDialog
          onClose={() => (dialog.value = null)}
          onEdit={() => { settingsTab.value = 4; dialog.value = 'settings'; }}
        />
      )}
      {dialog.value === 'copy' && <CopyEditor initialKey={copyKey.value} onClose={() => (dialog.value = null)} />}
    </>
  );
}
