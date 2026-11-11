// 从 ../assets/app.png 生成 PWA/安卓图标，产物提交入库保证构建确定性。
// Generate PWA/Android icons from ../assets/app.png; committed output keeps builds deterministic.
import sharp from 'sharp';
import { mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const source = fileURLToPath(new URL('../../assets/app.png', import.meta.url));
const output = fileURLToPath(new URL('../public/icons', import.meta.url));
await mkdir(output, { recursive: true });

for (const size of [192, 512]) {
  await sharp(source).resize(size, size).png().toFile(`${output}/icon-${size}.png`);
}
await sharp(source).resize(512, 512, { fit: 'contain', background: '#F1F4FA' }).png().toFile(`${output}/icon-maskable-512.png`);
console.log(`icons written to ${output}`);
