import { useState } from 'preact/hooks';
import { isWorkday } from '../engine/engine';
import { CalendarOverrides, type DayOverride } from '../engine/calendar';
import { dateKey } from '../engine/settings';
import type { Settings } from '../engine/types';
import { code, t } from '../i18n/L';

const LOCALE_TAGS: Record<string, string> = { 'zh-Hans': 'zh-CN', 'zh-Hant': 'zh-TW', en: 'en-US', ja: 'ja-JP', es: 'es-ES', ko: 'ko-KR' };
const WEEKDAY_KEYS = ['weekday.sun', 'weekday.mon', 'weekday.tue', 'weekday.wed', 'weekday.thu', 'weekday.fri', 'weekday.sat'];

// 月视图排班日历：休息/补班/恢复三模式，同模式再点一次取消。
// Monthly schedule calendar with rest/makeup/restore modes; the same mode toggles off.
export function ScheduleCalendar({ draft, onChanged }: { draft: Settings; onChanged: () => void }) {
  const [view, setView] = useState(() => new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  const [mode, setMode] = useState<DayOverride>('rest');
  const overrides = new CalendarOverrides(draft.RestDates, draft.WorkDates);

  // 固定六周网格，从周一开始 / A fixed six-week grid starting on Monday.
  const gridStart = new Date(view.getFullYear(), view.getMonth(), 1 - ((new Date(view.getFullYear(), view.getMonth(), 1).getDay() + 6) % 7));
  const shown: Date[] = [];
  for (let i = 0; i < 42; i++) shown.push(new Date(gridStart.getFullYear(), gridStart.getMonth(), gridStart.getDate() + i));

  const modes: { key: DayOverride; label: string; hint: string }[] = [
    { key: 'rest', label: t('calendar.rest'), hint: t('calendar.restHint') },
    { key: 'work', label: t('calendar.work'), hint: t('calendar.workHint') },
    { key: 'schedule', label: t('calendar.restore'), hint: t('calendar.restoreHint') },
  ];

  return (
    <div class="calendar">
      <div class="calendar-head">
        <button class="icon" title={t('calendar.previous')} onClick={() => setView(new Date(view.getFullYear(), view.getMonth() - 1, 1))}>‹</button>
        <strong>{new Intl.DateTimeFormat(LOCALE_TAGS[code] ?? 'en-US', { year: 'numeric', month: 'long' }).format(view)}</strong>
        <button class="icon" title={t('calendar.next')} onClick={() => setView(new Date(view.getFullYear(), view.getMonth() + 1, 1))}>›</button>
        <button class="toggle" title={t('calendar.currentTip')} onClick={() => setView(new Date(new Date().getFullYear(), new Date().getMonth(), 1))}>{t('calendar.current')}</button>
      </div>
      <div class="calendar-modes">
        {modes.map((entry) => (
          <button key={entry.key} class="toggle" data-active={mode === entry.key} onClick={() => setMode(entry.key)}>{entry.label}</button>
        ))}
      </div>
      <div class="calendar-grid">
        {[1, 2, 3, 4, 5, 6, 0].map((day) => <span key={day} class="calendar-weekday muted tiny">{t(WEEKDAY_KEYS[day]).slice(0, 2)}</span>)}
        {shown.map((date) => {
          const key = dateKey(date);
          const rest = overrides.Rest.has(key);
          const work = overrides.Work.has(key);
          return (
            <button
              key={key}
              class="calendar-day"
              data-out={date.getMonth() !== view.getMonth()}
              data-rest={rest}
              data-work={work}
              data-scheduled={isWorkday(date, draft)}
              title={rest ? t('calendar.restMark') : work ? t('calendar.workMark') : ''}
              onClick={() => {
                overrides.Click(date, mode);
                draft.RestDates = CalendarOverrides.serialize(overrides.Rest);
                draft.WorkDates = CalendarOverrides.serialize(overrides.Work);
                onChanged();
              }}
            >
              {date.getDate()}
            </button>
          );
        })}
      </div>
      <p class="muted tiny">{modes.find((entry) => entry.key === mode)!.hint}</p>
    </div>
  );
}
