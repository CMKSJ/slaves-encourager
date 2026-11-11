import { dateKey } from './settings';

// 排班日历互斥点击语义对照 CalendarOverrides / Exclusive-click semantics mirror CalendarOverrides.
export type DayOverride = 'schedule' | 'rest' | 'work';

export class CalendarOverrides {
  readonly Rest = new Set<string>();
  readonly Work = new Set<string>();

  constructor(rest: string, work: string) {
    for (const key of splitDates(rest)) this.Rest.add(key);
    for (const key of splitDates(work)) this.Work.add(key);
  }

  Click(date: Date, mode: DayOverride): void {
    const key = dateKey(date);
    const selected = mode === 'rest'
      ? this.Rest.has(key)
      : mode === 'work' && this.Work.has(key);
    // 单日互斥，再点一次恢复排班 / Overrides are exclusive; clicking the same mode again restores the schedule.
    this.Rest.delete(key);
    this.Work.delete(key);
    if (!selected && mode === 'rest') this.Rest.add(key);
    if (!selected && mode === 'work') this.Work.add(key);
  }

  static serialize(dates: Iterable<string>): string {
    return [...dates].sort().join(',');
  }
}

function splitDates(value: string): string[] {
  return (value ?? '').split(/[,，;\r\n ]/).map((token) => token.trim()).filter((token) => token !== '');
}
