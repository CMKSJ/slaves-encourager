// 可注入时钟：测试与演示模式使用固定起点 / Injectable clock: tests and demo mode use a fixed start.
export type Clock = () => Date;

export const systemClock: Clock = () => new Date();

// 与桌面版一致的演示起点 / The demo start point matches the desktop build.
export const DEMO_START = new Date(2026, 10, 4, 14, 32, 18);

export function demoClock(): { clock: Clock; elapsed: () => number } {
  const startedAt = performance.now();
  return {
    clock: () => new Date(DEMO_START.getTime() + (performance.now() - startedAt)),
    elapsed: () => (performance.now() - startedAt) / 1000,
  };
}
