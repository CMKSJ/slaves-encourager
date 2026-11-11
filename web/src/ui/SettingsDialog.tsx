import { useEffect, useRef, useState } from 'preact/hooks';
import type { Settings } from '../engine/types';
import { ValidationError } from '../engine/types';
import { calculate } from '../engine/engine';
import { confirmIncome, copySettings, cycleDay, dateKey, validate } from '../engine/settings';
import { formatAmount } from '../engine/format';
import { languages, t, use } from '../i18n/L';
import { applySettings, settings as liveSettings } from '../state/store';
import { FlipNumber } from './FlipNumber';
import { RewardListEditor } from './RewardListEditor';
import { ScheduleCalendar } from './ScheduleCalendar';

const TABS = ['tab.appearance', 'tab.salary', 'tab.calendar', 'tab.goals', 'expense.title'] as const;

// 设置对话框：五页签 + 实时预览 + 期初收入确认流 / Five tabs with live preview and the opening-income confirmation flow.
export function SettingsDialog({ initialTab = 0, onClose }: { initialTab?: number; onClose: () => void }) {
  const original = liveSettings.value;
  const [draft, setDraft] = useState<Settings>(() => {
    const copy = copySettings(original);
    copy.Goals ??= null;
    return copy;
  });
  const [tab, setTab] = useState(!original.Configured ? 1 : initialTab);
  const [error, setError] = useState('');
  const [adjustIncome, setAdjustIncome] = useState(false);
  const [incomePrompt, setIncomePrompt] = useState<Settings | null>(null);
  const [tick, setTick] = useState(0);

  useEffect(() => {
    const timer = window.setInterval(() => setTick((value) => value + 1), 1000);
    return () => window.clearInterval(timer);
  }, []);

  const touch = () => setDraft({ ...draft });

  const save = () => {
    try {
      const candidate = copySettings(draft);
      validate(candidate);
      const needsIncome = adjustIncome || !original.Configured || cycleDay(original) !== cycleDay(candidate) ||
        (!original.ExpensesEnabled && candidate.ExpensesEnabled);
      if (cycleDay(original) !== cycleDay(candidate)) {
        candidate.OpeningAt = null;
        candidate.OpeningIncome = null;
      }
      if (needsIncome) {
        setIncomePrompt(candidate);
        return;
      }
      applySettings(candidate);
      onClose();
    } catch (exception) {
      if (exception instanceof ValidationError) setError(t(exception.key));
      else if (exception instanceof Error) setError(exception.message);
    }
  };

  const close = () => {
    use(original.Language);
    onClose();
  };

  return (
    <div class="overlay" role="dialog" aria-modal="true">
      <div class="dialog settings-dialog">
        <div class="dialog-head">
          <div>
            <h2>{t('settings.headline')}</h2>
            <p class="muted tiny">{t('settings.previewHint')}</p>
          </div>
          <label class="language-box">
            <span class="muted tiny">{t('language.label')}</span>
            <select
              value={draft.Language ?? 'auto'}
              onChange={(event) => {
                draft.Language = event.currentTarget.value;
                use(draft.Language);
                touch();
              }}
            >
              <option value="auto">{t('language.auto')}</option>
              {languages.map((language) => <option key={language} value={language}>{languageName(language)}</option>)}
            </select>
          </label>
        </div>

        <div class="settings-preview">
          <FlipNumber text={formatAmount(1234.5678 + tick * 0.037, draft)} settings={draft} animate />
        </div>

        <div class="tabs" role="tablist">
          {TABS.map((key, index) => (
            (key !== 'expense.title' || draft.ExpensesEnabled) && (
              <button key={key} role="tab" class="tab" data-active={tab === index} onClick={() => setTab(index)}>{t(key)}</button>
            )
          ))}
        </div>

        <div class="dialog-body">
          {tab === 0 && <AppearanceTab draft={draft} touch={touch} />}
          {tab === 1 && <SalaryTab draft={draft} touch={touch} onAdjust={() => setAdjustIncome(true)} onOpenExpenses={() => setTab(4)} onOpenCalendar={() => setTab(2)} />}
          {tab === 2 && <ScheduleCalendar draft={draft} onChanged={touch} />}
          {tab === 3 && (
            <div>
              <RewardListEditor title={t('goal.listTitle')} items={goalList(draft)} prefix="goal" onChanged={touch} />
              <p class="muted tiny">{t('goal.allocationHelp')}</p>
              <h3 class="small">{t('help.title')}</h3>
              <p class="tiny">{t('help.shortcuts')}</p>
              <p class="tiny">{t('help.demo')}</p>
              <p class="muted tiny">{t('help.local')}</p>
            </div>
          )}
          {tab === 4 && (
            <div>
              <RewardListEditor title={t('expense.listTitle')} items={expenseList(draft)} prefix="expense" onChanged={touch} />
              <p class="muted tiny">{t('expense.editorHelp')}</p>
            </div>
          )}
        </div>

        {error !== '' && <p class="dialog-error">{error}</p>}
        <div class="dialog-actions">
          <button class="toggle" onClick={close}>{t('action.cancel')}</button>
          <button class="primary" onClick={save}>{t('action.save')}</button>
        </div>
      </div>
      {incomePrompt !== null && (
        <IncomeDialog
          draft={incomePrompt}
          onCancel={() => setIncomePrompt(null)}
          onConfirm={(amount) => {
            const candidate = incomePrompt;
            confirmIncome(candidate, new Date(), amount);
            applySettings(candidate);
            setIncomePrompt(null);
            onClose();
          }}
        />
      )}
    </div>
  );
}

function goalList(draft: Settings) {
  draft.Goals ??= draft.GoalName !== '' || draft.GoalAmount !== 0 ? [{ Name: draft.GoalName, Amount: draft.GoalAmount }] : [];
  return draft.Goals;
}

function expenseList(draft: Settings) {
  draft.Expenses ??= [];
  return draft.Expenses;
}

function languageName(code: string): string {
  const NAMES: Record<string, string> = { 'zh-Hans': '简体中文', 'zh-Hant': '繁體中文', en: 'English', ja: '日本語', es: 'Español', ko: '한국어' };
  return NAMES[code] ?? code;
}

function AppearanceTab({ draft, touch }: { draft: Settings; touch: () => void }) {
  return (
    <div class="form-grid">
      <Field label={t('appearance.animation')}>
        <select value={draft.Animation} onChange={(e) => { draft.Animation = e.currentTarget.value; touch(); }}>
          <option value="flip">{t('animation.flip')}</option>
          <option value="slide">{t('animation.slide')}</option>
          <option value="none">{t('animation.none')}</option>
        </select>
      </Field>
      <Field label={t('appearance.theme')}>
        <select value={draft.Theme} onChange={(e) => { draft.Theme = e.currentTarget.value; touch(); }}>
          <option value="midnight">{t('theme.mist')}</option>
          <option value="forest">{t('theme.dusk')}</option>
          <option value="paper">{t('theme.paper')}</option>
        </select>
      </Field>
      <Field label={t('appearance.font')}>
        <select value={draft.Font} onChange={(e) => { draft.Font = e.currentTarget.value; touch(); }}>
          <option value="DIN">{t('font.din')}</option>
          <option value="Mono">{t('font.mono')}</option>
          <option value="Classic">{t('font.classic')}</option>
        </select>
      </Field>
      <Field label={t('appearance.decimals')}>
        <select value={draft.Decimals} onChange={(e) => { draft.Decimals = Number(e.currentTarget.value); touch(); }}>
          {[0, 1, 2, 3, 4].map((value) => <option key={value} value={value}>{t(`precision.${value}`)}</option>)}
        </select>
      </Field>
      <Field label={t('appearance.currency')}>
        <select value={draft.Currency} onChange={(e) => { draft.Currency = e.currentTarget.value; touch(); }}>
          {['CNY', 'USD', 'EUR', 'JPY'].map((value) => <option key={value} value={value}>{t(`currency.${value.toLowerCase()}`)}</option>)}
        </select>
      </Field>
      <Field label={t('appearance.speed')}>
        <select value={draft.Speed} onChange={(e) => { draft.Speed = Number(e.currentTarget.value); touch(); }}>
          <option value="220">{t('speed.fast')}</option>
          <option value="420">{t('speed.normal')}</option>
          <option value="650">{t('speed.slow')}</option>
        </select>
      </Field>
      <label class="check">
        <input type="checkbox" checked={draft.Grouping} onChange={(e) => { draft.Grouping = e.currentTarget.checked; touch(); }} />
        {t('appearance.grouping')}
      </label>
      <Field label={t('appearance.layout')}>
        <select
          value={['{symbol}{amount}', '{amount} {code}', '{amount}'].includes(draft.Pattern) ? draft.Pattern : 'custom'}
          onChange={(e) => {
            const value = e.currentTarget.value;
            if (value !== 'custom') draft.Pattern = value;
            else if (!['{symbol}{amount}', '{amount} {code}', '{amount}'].includes(draft.Pattern)) draft.Pattern = draft.Pattern;
            touch();
          }}
        >
          <option value="{symbol}{amount}">{t('pattern.prefix')}</option>
          <option value="{amount} {code}">{t('pattern.code')}</option>
          <option value="{amount}">{t('pattern.number')}</option>
          <option value="custom">{t('pattern.custom')}</option>
        </select>
      </Field>
      {(['{symbol}{amount}', '{amount} {code}', '{amount}'].includes(draft.Pattern) ? '' : draft.Pattern) !== '' && (
        <Field label={t('pattern.label')}>
          <input value={draft.Pattern} maxLength={48} onInput={(e) => { draft.Pattern = e.currentTarget.value; touch(); }} />
        </Field>
      )}
      <p class="muted tiny">{t('currency.help')}</p>
    </div>
  );
}

// 月薪输入保留原始草稿文本，避免 "3." 中间态被 Number() 规格化打断 / Keep the raw salary draft so intermediate states like "3." survive.
function SalaryTab({ draft, touch, onAdjust, onOpenExpenses, onOpenCalendar }: {
  draft: Settings;
  touch: () => void;
  onAdjust: () => void;
  onOpenExpenses: () => void;
  onOpenCalendar: () => void;
}) {
  const rawSalary = useRef('');
  return (
    <div class="form-grid">
      <Field label={t('salary.amount')}>
        <input
          value={rawSalary.current !== '' ? rawSalary.current : String(draft.MonthlySalary)}
          inputMode="decimal"
          onInput={(e) => {
            const raw = e.currentTarget.value;
            rawSalary.current = raw === String(draft.MonthlySalary) ? '' : raw;
            const parsed = Number(raw);
            if (raw.trim() !== '' && Number.isFinite(parsed)) draft.MonthlySalary = parsed;
            touch();
          }}
          onBlur={() => { rawSalary.current = ''; touch(); }}
        />
      </Field>
      <Field label={t('salary.basis')}>
        <select value={draft.Basis} onChange={(e) => { draft.Basis = e.currentTarget.value; touch(); }}>
          <option value="actual">{t('basis.actual')}</option>
          <option value="fixed">{t('basis.fixed')}</option>
        </select>
      </Field>
      <Field label={t('salary.settlement')}>
        <select value={cycleDay(draft)} onChange={(e) => { draft.SettlementDay = Number(e.currentTarget.value); touch(); }}>
          {Array.from({ length: 31 }, (_, index) => index + 1).map((day) => <option key={day} value={day}>{day}</option>)}
        </select>
      </Field>
      <p class="muted tiny">{t('salary.cycleHelp')}</p>
      <button class="toggle" onClick={() => { onAdjust(); }}>{t('income.adjust')}</button>
      <p class="muted tiny">{t('income.adjustHint')}</p>
      <label class="check">
        <input
          type="checkbox"
          checked={draft.ExpensesEnabled}
          onChange={(e) => {
            draft.ExpensesEnabled = e.currentTarget.checked;
            if (draft.ExpensesEnabled) draft.Expenses ??= [];
            touch();
          }}
        />
        {t('expense.enable')}
      </label>
      {draft.ExpensesEnabled && <button class="toggle" onClick={onOpenExpenses}>{t('expense.edit')}</button>}
      <div class="pair">
        <Field label={t('salary.start')}>
          <input value={draft.Start} maxLength={5} onInput={(e) => { draft.Start = e.currentTarget.value; touch(); }} />
        </Field>
        <Field label={t('salary.end')}>
          <input value={draft.End} maxLength={5} onInput={(e) => { draft.End = e.currentTarget.value; touch(); }} />
        </Field>
      </div>
      <label class="check">
        <input type="checkbox" checked={draft.LunchEnabled} onChange={(e) => { draft.LunchEnabled = e.currentTarget.checked; touch(); }} />
        {t('salary.breakEnabled')}
      </label>
      {draft.LunchEnabled && (
        <div class="pair">
          <Field label={t('salary.breakStart')}>
            <input value={draft.LunchStart} maxLength={5} onInput={(e) => { draft.LunchStart = e.currentTarget.value; touch(); }} />
          </Field>
          <Field label={t('salary.breakEnd')}>
            <input value={draft.LunchEnd} maxLength={5} onInput={(e) => { draft.LunchEnd = e.currentTarget.value; touch(); }} />
          </Field>
        </div>
      )}
      <Field label={t('salary.weekdays')}>
        <div class="weekday-row">
          {[1, 2, 3, 4, 5, 6, 0].map((day) => (
            <label key={day} class="check weekday">
              <input
                type="checkbox"
                checked={weekdaySet(draft).has(day)}
                onChange={(e) => {
                  const days = weekdaySet(draft);
                  if (e.currentTarget.checked) days.add(day);
                  else days.delete(day);
                  draft.Weekdays = [...days].sort().join(',');
                  touch();
                }}
              />
              {t(`weekday.${['sun', 'mon', 'tue', 'wed', 'thu', 'fri', 'sat'][day]}`).slice(0, 2)}
            </label>
          ))}
        </div>
      </Field>
      <button class="toggle" onClick={onOpenCalendar}>{t('calendar.open')}</button>
      <p class="muted tiny">{t('salary.help')}</p>
    </div>
  );
}

function weekdaySet(draft: Settings): Set<number> {
  const result = new Set<number>();
  for (const part of (draft.Weekdays ?? '').split(',')) {
    const day = Number(part);
    if (Number.isInteger(day) && day >= 0 && day <= 6) result.add(day);
  }
  return result;
}

function Field({ label, children }: { label: string; children: preact.ComponentChildren }) {
  return (
    <label class="field">
      <span class="small">{label}</span>
      {children}
    </label>
  );
}

// 期初收入确认：预估可改、可从 0 开始 / Opening-income confirmation with editable estimate.
function IncomeDialog({ draft, onCancel, onConfirm }: {
  draft: Settings;
  onCancel: () => void;
  onConfirm: (amount: number) => void;
}) {
  const period = calculate(new Date(), draft);
  const estimate = period.Month;
  const [value, setValue] = useState(estimate.toFixed(2));
  const [error, setError] = useState('');
  const confirm = () => {
    const amount = Number(value);
    if (!Number.isFinite(amount) || amount < 0 || amount > 100000000) {
      setError(t('validation.opening'));
      return;
    }
    onConfirm(amount);
  };
  return (
    <div class="overlay nested" role="dialog" aria-modal="true">
      <div class="dialog">
        <h2>{t('income.title')}</h2>
        <p class="muted small">{t('income.period', dateKey(period.PeriodStart), dateKey(dateAfter(period.PeriodEnd)))}</p>
        <p class="small">{t('income.estimate', formatAmount(estimate, draft))}</p>
        <p class="muted tiny">{t('income.help')}</p>
        <label class="field">
          <span class="small">{t('income.amount', draft.Currency)}</span>
          <input value={value} inputMode="decimal" maxLength={16} onInput={(event) => setValue(event.currentTarget.value)} />
        </label>
        {error !== '' && <p class="dialog-error">{error}</p>}
        <div class="dialog-actions">
          <button class="toggle" onClick={onCancel}>{t('action.cancel')}</button>
          <button class="toggle" onClick={() => setValue('0')}>{t('income.zero')}</button>
          <button class="primary" onClick={confirm}>{t('income.confirm')}</button>
        </div>
      </div>
    </div>
  );
}

function dateAfter(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() - 1);
}
