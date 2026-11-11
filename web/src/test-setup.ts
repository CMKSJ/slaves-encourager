// Node 26 未提供 --localstorage-file 时全局 localStorage 缺失；测试环境使用内存实现。
// Node 26 without --localstorage-file leaves globals without localStorage; tests use an in-memory implementation.
class MemoryStorage implements Storage {
  private data = new Map<string, string>();
  get length(): number { return this.data.size; }
  clear(): void { this.data.clear(); }
  getItem(key: string): string | null { return this.data.has(key) ? this.data.get(key)! : null; }
  key(index: number): string | null { return [...this.data.keys()][index] ?? null; }
  removeItem(key: string): void { this.data.delete(key); }
  setItem(key: string, value: string): void { this.data.set(key, String(value)); }
}

const storage = new MemoryStorage();
const existing = (globalThis as { localStorage?: Storage }).localStorage;
if (existing == null) {
  Object.defineProperty(globalThis, 'localStorage', { value: storage, configurable: true });
  const scope = (globalThis as { window?: { localStorage?: Storage } }).window;
  if (scope != null) Object.defineProperty(scope, 'localStorage', { value: storage, configurable: true });
}
