import { useRef } from 'preact/hooks';
import type { RewardGoal } from '../engine/types';
import { t } from '../i18n/L';

// 目标与开支共用的有序列表编辑器，上限 20 条 / Ordered list editor shared by goals and expenses, capped at 20.
// 输入用 onInput 逐键提交（Preact 的 onChange 是原生 change，失焦才触发）；
// 金额保留原始草稿文本，避免 "3." 这类中间态被 Number() 规格化打断。
// Inputs commit per keystroke via onInput (Preact's onChange is the native blur-time event);
// amounts keep their raw draft so intermediate states like "3." survive Number() round-trips.
export function RewardListEditor({ title, items, prefix, onChanged }: {
  title: string;
  items: RewardGoal[];
  prefix: string;
  onChanged: () => void;
}) {
  const rawAmounts = useRef(new Map<number, string>());
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
            onInput={(event) => { item.Name = event.currentTarget.value; onChanged(); }}
          />
          <input
            class="list-amount"
            value={rawAmounts.current.get(index) ?? String(item.Amount)}
            inputMode="decimal"
            aria-label={t('goal.amount')}
            onInput={(event) => {
              const raw = event.currentTarget.value;
              rawAmounts.current.set(index, raw);
              const parsed = Number(raw);
              if (raw.trim() !== '' && Number.isFinite(parsed)) item.Amount = parsed;
              onChanged();
            }}
            onBlur={() => rawAmounts.current.delete(index)}
          />
          <span class="list-tools">
            <button class="icon" aria-label="up" onClick={() => move(index, -1)} disabled={index === 0}>↑</button>
            <button class="icon" aria-label="down" onClick={() => move(index, 1)} disabled={index === items.length - 1}>↓</button>
            <button class="icon" title={t(prefix === 'expense' ? 'expense.removeTip' : 'goal.removeTip')} onClick={() => { items.splice(index, 1); rawAmounts.current.delete(index); onChanged(); }}>×</button>
          </span>
        </div>
      ))}
    </div>
  );
}
