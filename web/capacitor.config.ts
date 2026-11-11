import type { CapacitorConfig } from '@capacitor/cli';

// 安卓壳配置：复用 web/dist 构建产物 / Android shell config reusing the web/dist build.
const config: CapacitorConfig = {
  appId: 'io.github.thisemailrectrashcpu.slavesencourager',
  appName: '牛马鼓励器',
  webDir: 'dist',
  backgroundColor: '#F1F4FA',
};

export default config;
