import { defineConfig, type Plugin } from 'vite';
import preact from '@preact/preset-vite';
import { VitePWA } from 'vite-plugin-pwa';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// 目录清单单一来源：构建时直接读取 ../locales/catalog.tsv，不复制副本。
// Single source for the catalog: read ../locales/catalog.tsv at build time; never copy it.
const LANGUAGES = ['zh-Hans', 'zh-Hant', 'en', 'ja', 'es', 'ko'] as const;

function catalogPlugin(): Plugin {
  const virtualId = '\0catalog';
  return {
    name: 'slaves-catalog',
    resolveId(source) {
      if (source === 'virtual:catalog') return virtualId;
      return null;
    },
    load(id) {
      if (id !== virtualId) return null;
      const file = fileURLToPath(new URL('../locales/catalog.tsv', import.meta.url));
      this.addWatchFile(file);
      const rows: Record<string, string[]> = {};
      const lines = readFileSync(file, 'utf-8').split('\n');
      lines.forEach((line, index) => {
        if (index === 0 || line.trim() === '') return;
        const columns = line.split('\t');
        if (columns.length !== 7) throw new Error(`Invalid catalog row ${index + 1}`);
        for (const language of LANGUAGES) {
          if (!columns[LANGUAGES.indexOf(language) + 1].trim()) throw new Error(`Empty translation: ${columns[0]}`);
        }
        rows[columns[0]] = columns.slice(1);
      });
      return `export const languages = ${JSON.stringify(LANGUAGES)};\nexport default ${JSON.stringify(rows)};`;
    },
  };
}

export default defineConfig({
  plugins: [
    preact(),
    catalogPlugin(),
    VitePWA({
      manifest: {
        name: '牛马鼓励器',
        short_name: '牛马鼓励器',
        description: '把工作时间变成看得见的小奖励',
        theme_color: '#F1F4FA',
        background_color: '#F1F4FA',
        display: 'standalone',
        icons: [
          { src: 'icons/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png' },
          { src: 'icons/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: { globPatterns: ['**/*.{js,css,html,png,svg,woff2}'] },
    }),
  ],
  test: {
    environment: 'happy-dom',
    include: ['src/**/*.test.{ts,tsx}'],
    setupFiles: ['src/test-setup.ts'],
  },
} as never);
