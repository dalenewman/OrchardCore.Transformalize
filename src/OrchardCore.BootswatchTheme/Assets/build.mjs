import { mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import * as sass from 'sass';
import rtlcss from 'rtlcss';

const source = fileURLToPath(new URL('codex.scss', import.meta.url));
const output = new URL('../wwwroot/styles/codex/', import.meta.url);
await mkdir(output, { recursive: true });

for (const [suffix, style] of [['', 'expanded'], ['.min', 'compressed']]) {
  const result = sass.compile(source, {
    loadPaths: [fileURLToPath(new URL('node_modules/', import.meta.url))],
    style,
    charset: false,
    // Bootstrap 5 uses the legacy Sass API internally. Keep our own warnings visible.
    quietDeps: true,
    silenceDeprecations: ['import'],
    sourceMap: false
  });
  const banner = '@charset "UTF-8";\n/*! Codex — Bootstrap 5.3.8. Generated from Assets/codex.scss; run npm ci && npm run build in Assets. */\n';
  await writeFile(new URL(`bootstrap${suffix}.css`, output), banner + result.css + '\n');
  await writeFile(new URL(`bootstrap.rtl${suffix}.css`, output), banner + rtlcss.process(result.css) + '\n');
}
console.log('Built Codex: light/dark, LTR/RTL, debug/minified.');
