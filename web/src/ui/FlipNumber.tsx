import { useRef } from 'preact/hooks';
import type { Settings } from '../engine/types';

const DIGIT = /[0-9]/;

// 翻页数字：数字位翻页/滑动，符号与分隔符静态显示。
// Flip digits: numeric tiles flip or slide; symbols and separators stay static.
export function FlipNumber({ text, settings, animate }: { text: string; settings: Settings; animate: boolean }) {
  const mode = settings.Animation;
  const duration = Math.min(700, Math.max(160, settings.Speed));
  const fontClass = settings.Font === 'Mono' ? 'flip-mono' : settings.Font === 'Classic' ? 'flip-classic' : 'flip-din';
  return (
    <div class={`flip-number ${fontClass}`} aria-label={text}>
      {[...text].map((character, index) =>
        DIGIT.test(character) ? (
          <FlipTile key={index} character={character} mode={mode} duration={duration} animate={animate} />
        ) : (
          <span key={index} class="flip-static">{character}</span>
        ),
      )}
    </div>
  );
}

// 每格记住上一位数字：变化时旧数字翻出、新数字翻入 / Tiles remember the previous digit: the old one flips out while the new one flips in.
function FlipTile({ character, mode, duration, animate }: { character: string; mode: string; duration: number; animate: boolean }) {
  const previous = useRef<string | null>(null);
  const last = previous.current;
  previous.current = character;
  const changed = animate && mode !== 'none' && last !== null && last !== character;
  const enter = changed
    ? { animation: `${mode === 'slide' ? 'slide-in' : 'flip-in'} ${duration}ms ease-out` }
    : undefined;
  const leave = changed
    ? { animation: `${mode === 'slide' ? 'slide-out' : 'flip-out'} ${duration}ms ease-out forwards` }
    : undefined;
  return (
    <span class="flip-tile">
      {changed && last !== null && <span key={last} class="flip-old" style={leave}>{last}</span>}
      <span key={character} style={enter}>{character}</span>
    </span>
  );
}
