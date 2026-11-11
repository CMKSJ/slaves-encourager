import type { Settings } from '../engine/types';

// 翻页数字：数字位翻页/滑动，符号与分隔符静态显示。
// Flip digits: numeric tiles flip or slide; symbols and separators stay static.
export function FlipNumber({ text, settings, animate }: { text: string; settings: Settings; animate: boolean }) {
  const mode = settings.Animation;
  const duration = Math.min(700, Math.max(160, settings.Speed));
  const fontClass = settings.Font === 'Mono' ? 'flip-mono' : settings.Font === 'Classic' ? 'flip-classic' : 'flip-din';
  return (
    <div class={`flip-number ${fontClass}`} aria-label={text}>
      {[...text].map((character, index) => (
        <FlipTile
          key={`${index}-${character}`}
          character={character}
          mode={mode}
          duration={duration}
          animate={animate && /[0-9]/.test(character)}
        />
      ))}
    </div>
  );
}

function FlipTile({ character, mode, duration, animate }: { character: string; mode: string; duration: number; animate: boolean }) {
  const numeric = /[0-9]/.test(character);
  const style = animate && mode !== 'none'
    ? mode === 'slide'
      ? `flip-slide ${duration}ms`
      : `flip-in ${duration}ms`
    : undefined;
  if (!numeric) return <span class="flip-static">{character}</span>;
  return (
    <span class="flip-tile">
      <span key={character} class={style}>{character}</span>
    </span>
  );
}
