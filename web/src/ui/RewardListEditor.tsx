import type { RewardGoal } from '../engine/types';
import { t } from '../i18n/L';

// 目标与开支共用的有序列表编辑器，上限 20 条 / Ordered list editor shared by goals and expenses, capped at 20.
export function RewardListEditor({ title, items, prefix, onChanged }: {
  title: string;
  items: RewardGoal[];
  prefix: string;
  onChanged: () => void;
}) {
  const add = () => {
    if (items.length >= 20) return;
    items.push({ Name: t(prefix === 'expense' ? 'expense.new' : 'goal.new'), Amount: 1000 });
    onChanged();
  };
  const move = (index: number, delta: number) => {
    const target = index + delta;
    if (target < 0 || target >= items.length) return;
    [items[index], items[target]] = [items[target], items[index]];
    onChanged();
  };
  return (
    <div class="list-editor">
      <div class="card-head">
        <strong class="small">{title}</strong>
        <button class="toggle" onClick={add} disabled={items.length >= 20}>{t(prefix === 'expense' ? 'expense.add' : 'goal.add')}</button>
      </div>
      {items.map((item, index) => (
        <div key={index} class="list-row">
          <input
            value={item.Name}
            maxLength={28}
            aria-label={t('goal.name')}
            onChange={(event) => { item.Name = event.currentTarget.value; onChanged(); }}
          />
          <input
            class="list-amount"
            value={String(item.Amount)}
            inputMode="decimal"
            aria-label={t('goal.amount')}
            onChange={(event) => { item.Amount = Number(event.currentTarget.value); onChanged(); }}
          />
          <span class="list-tools">
            <button class="icon" aria-label="up" onClick={() => move(index, -1)} disabled={index === 0}>↑</button>
            <button class="icon" aria-label="down" onClick={() => move(index, 1)} disabled={index === items.length - 1}>↓</button>
            <button class="icon" title={t(prefix === 'expense' ? 'expense.removeTip' : 'goal.removeTip')} onClick={() => { items.splice(index, 1); onChanged(); }}>×</button>
          </span>
        </div>
      ))}
    </div>
  );
}
