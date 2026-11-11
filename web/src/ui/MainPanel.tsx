import { useComputed } from '@preact/signals';
import type { GoalProgress } from '../engine/types';
import { goals } from '../engine/engine';
import { duration, formatAmount, formatMoney, symbol } from '../engine/format';
import { code, t } from '../i18n/L';
import { homeCopy } from '../i18n/homeCopy';
import { demo, dialog, copyKey, goalIndex, loadNotice, mini, now, privacy, settings, snapshot } from '../state/store';
import { FlipNumber } from './FlipNumber';

const MASK = '••••••';
const LOCALE_TAGS: Record<string, string> = {
  'zh-Hans': 'zh-CN', 'zh-Hant': 'zh-TW', en: 'en-US', ja: 'ja-JP', es: 'es-ES', ko: 'ko-KR',
};

function home(key: string, ...values: unknown[]): string {
  return homeCopy(settings.value, key, ...values);
}

function openCopy(key: string): void {
  dialog.value = 'copy';
  copyKey.value = key;
}

function dateKeyText(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

export function MainPanel() {
  const s = snapshot.value;
  const data = settings.value;
  const list: GoalProgress[] = useComputed(() => goals(snapshot.value.Month, settings.value)).value;
  const index = goalChoice(list);
  const goal = list.length === 0 ? null : list[index];
  const masked = privacy.value;
  const dateText = new Intl.DateTimeFormat(LOCALE_TAGS[code] ?? 'en-US', { month: '2-digit', day: '2-digit', weekday: 'short' }).format(now.value);

  return (
    <main class="panel" data-mini={mini.value}>
      {!mini.value && (
        <header class="panel-intro">
          <div class="panel-kicker-row">
            <span class="copyable kicker" onDblClick={() => openCopy('main.kicker')}>{home('main.kicker')}</span>
            <span class="kicker panel-date">{dateText}</span>
          </div>
          <h1 class="headline copyable" onDblClick={() => openCopy('main.headline')}>{home('main.headline')}</h1>
          <p class="panel-subtitle copyable" onDblClick={() => openCopy('main.subtitle')}>
            {loadNotice.value !== '' ? t('error.load') : !data.Configured ? home('main.setupHint') : home('main.subtitle')}
          </p>
        </header>
      )}

      <section class="card hero">
        <div class="card-head">
          <span class="muted small copyable" onDblClick={() => openCopy('main.today')}>{home('main.today')}</span>
          <span class="badge">{demo.value ? home('main.demoBadge') : t(s.StatusKey)}</span>
        </div>
        <FlipNumber text={masked ? MASK : formatAmount(s.Today, data)} settings={data} animate={!masked} />
        <div class="hero-foot muted small">
          {masked ? home('main.hidden') : home('main.rate', symbol(data.Currency) + s.Rate.toFixed(4))}
        </div>
      </section>

      {!mini.value && (
        <>
          <section class="card">
            <div class="card-head">
              <div>
                <div class="muted small">{home(s.CountdownKey)}</div>
                <div class="muted tiny schedule">{data.Start} — {data.End}{data.LunchEnabled ? home('main.breakSuffix') : ''}</div>
              </div>
              <div class="countdown">{duration(s.CountdownSeconds)}</div>
            </div>
            <div class="bar"><div class="bar-fill" style={{ width: `${Math.min(100, s.Progress * 100)}%` }} /></div>
            <div class="card-foot">
              <span class="muted tiny">{home('main.hours', s.Worked / 3600, s.Total / 3600)}</span>
              <span class="tiny accent">{(s.Progress * 100).toFixed(1)}%</span>
            </div>
          </section>

          <div class="pair">
            <section class="card">
              <div class="muted tiny">{home('main.hourly')}</div>
              <div class="stat">{masked ? '••••' : formatMoney(s.Hourly, data)}</div>
            </section>
            <section class="card" title={t('income.period', dateKeyText(s.PeriodStart), dateKeyText(s.PeriodEnd))}>
              <div class="muted tiny">{home('main.month')}</div>
              <div class="stat">{masked ? '••••' : formatMoney(s.Month, data)}</div>
            </section>
          </div>

          <section class="card alt">
            <div class="card-head">
              <span class="muted tiny">{goal == null ? home('goal.heading') : home('goal.index', index + 1, list.length)}</span>
              <span class="goal-nav">
                <span class="accent stat-small">{masked || goal == null ? '—' : `${Math.round(goal.Fraction * 100)}%`}</span>
                <button class="icon" disabled={list.length < 2} onClick={() => (goalIndex.value = (index - 1 + list.length) % list.length)} aria-label={t('goal.previous')}>‹</button>
                <button class="icon" disabled={list.length < 2} onClick={() => (goalIndex.value = (index + 1) % list.length)} aria-label={t('goal.next')}>›</button>
              </span>
            </div>
            <div class="goal-row">
              <span class="goal-name">{goal == null ? home('goal.emptyTitle') : goal.Goal.Name}</span>
              <span class="accent">{masked || goal == null ? '＋' : formatMoney(goal.Goal.Amount, data)}</span>
            </div>
            <div class="bar"><div class="bar-fill" style={{ width: `${masked || goal == null ? 0 : Math.min(100, goal.Fraction * 100)}%` }} /></div>
            <div class="muted tiny">
              {masked
                ? home('goal.hidden')
                : goal == null
                  ? home('goal.emptyHint')
                  : goal.Fraction >= 1
                    ? (index === list.length - 1 ? home('goal.allDone') : home('goal.done'))
                    : home('goal.remaining', s.Hourly > 0 ? goal.RemainingToReach / s.Hourly : 0)}
            </div>
          </section>

          <p class="footnote copyable" onDblClick={() => openCopy('main.footnote')}>
            {data.Basis === 'actual' ? home('main.actualFootnote', s.MonthDays) : home('main.fixedFootnote')}
          </p>
        </>
      )}

      <footer class="actions">
        {data.ExpensesEnabled && (
          <button class="toggle" onClick={() => (dialog.value = 'expenses')}>{home('main.expenses')}</button>
        )}
        <button class="toggle" data-active={privacy.value} onClick={() => (privacy.value = !privacy.value)}>{home('mode.privacy')}</button>
        <button class="toggle" data-active={mini.value} onClick={() => (mini.value = !mini.value)}>{home('mode.mini')}</button>
        <button class="toggle" data-active={demo.value} onClick={() => (demo.value = !demo.value)}>{demo.value ? home('main.demoActive') : home('main.demo')}</button>
        <button class="primary" onClick={() => (dialog.value = 'settings')}>{data.Configured ? home('settings.open') : home('main.firstUse')}</button>
      </footer>
    </main>
  );
}

function goalChoice(list: GoalProgress[]): number {
  if (list.length === 0) return 0;
  if (goalIndex.value >= 0) return Math.min(goalIndex.value, list.length - 1);
  const next = list.findIndex((g) => g.Fraction < 1);
  return next < 0 ? list.length - 1 : next;
}
