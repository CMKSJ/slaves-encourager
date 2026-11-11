import { useEffect, useState } from 'preact/hooks';
import type { Settings } from '../engine/types';
import { ValidationError } from '../engine/types';
import { expenseCoverage, expenseTotal, rewardIncome } from '../engine/engine';
import { formatMoney } from '../engine/format';
import { t } from '../i18n/L';
import { homeCopy as homeText, homeCopyKeys, reset as resetCopy, resetLanguage, set as setCopy } from '../i18n/homeCopy';
import { privacy, settings, snapshot } from '../state/store';

// 开支覆盖面板：红条随收入缩短 / Expense coverage panel with shrinking red bars.
export function ExpensesDialog({ onClose, onEdit }: { onClose: () => void; onEdit: () => void }) {
  const [, force] = useState(0);
  useEffect(() => {
    const timer = window.setInterval(() => force((value) => value + 1), 1000);
    return () => window.clearInterval(timer);
  }, []);
  const data = settings.value;
  const income = snapshot.value.Month;
  const coverage = expenseCoverage(income, data);
  const total = expenseTotal(data);
  return (
    <div class="overlay" role="dialog" aria-modal="true">
      <div class="dialog">
        <h2>{t('expense.title')}</h2>
        <p class="muted tiny">{t('expense.explanation')}</p>
        <p class="small">
          {privacy.value ? t('expense.hidden') : t('expense.summary', formatMoney(coverage.reduce((sum, item) => sum + item.Remaining, 0), data), formatMoney(total, data))}
        </p>
        <p class="small muted">{t('expense.rewardBalance')}  {privacy.value ? '••••' : formatMoney(rewardIncome(income, data), data)}</p>
        {coverage.map((item) => (
          <div key={item.Item.Name} class="card alt expense-item">
            <div class="card-head">
              <span>{item.Item.Name}</span>
              <span class="muted small">{privacy.value ? t('expense.hidden') : t('expense.remainingValue', formatMoney(item.Remaining, data))}</span>
            </div>
            <div class="bar"><div class="bar-fill red" style={{ width: `${privacy.value ? 0 : item.RemainingFraction * 100}%` }} /></div>
          </div>
        ))}
        <div class="dialog-actions">
          <button class="toggle" onClick={onEdit}>{t('expense.edit')}</button>
          <button class="toggle" onClick={onClose}>{t('action.cancel')}</button>
        </div>
      </div>
    </div>
  );
}

// 主页文案编辑：逐键改写、单项恢复、恢复当前语言 / Home copy editor with per-key reset.
export function CopyEditor({ initialKey, onClose }: { initialKey: string | null; onClose: () => void }) {
  const [draft] = useState<Settings>(() => structuredClone(settings.value));
  const [error, setError] = useState('');
  const [selected, setSelected] = useState(initialKey);
  const [force, forceUpdate] = useState(0);
  const keys = homeCopyKeys();

  const save = () => {
    settings.value = draft;
    onClose();
  };

  return (
    <div class="overlay" role="dialog" aria-modal="true">
      <div class="dialog copy-dialog">
        <h2>{t('copy.edit')}</h2>
        <p class="muted tiny">{t('copy.help')}</p>
        <div class="copy-list">
          {keys.map((key) => (
            <div key={key} class="copy-row" data-selected={selected === key}>
              <button class="copy-key tiny" onClick={() => setSelected(key)}>{key}</button>
              <input
                value={homeText(draft, key)}
                maxLength={300}
                onFocus={() => setSelected(key)}
                onInput={(event) => {
                  try {
                    setCopy(draft, key, event.currentTarget.value);
                    setError('');
                  } catch (exception) {
                    setError(exception instanceof ValidationError ? t(exception.key) : String(exception));
                  }
                  forceUpdate(force + 1);
                }}
              />
              <button class="icon" title={t('copy.reset')} onClick={() => { resetCopy(draft, key); forceUpdate(force + 1); }}>↺</button>
            </div>
          ))}
        </div>
        {error !== '' && <p class="dialog-error">{error}</p>}
        <div class="dialog-actions">
          <button class="toggle" onClick={() => { resetLanguage(draft); forceUpdate(force + 1); }}>{t('copy.resetAll')}</button>
          <button class="toggle" onClick={onClose}>{t('action.cancel')}</button>
          <button class="primary" onClick={save}>{t('action.save')}</button>
        </div>
      </div>
    </div>
  );
}
