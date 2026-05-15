import * as esbuild from 'esbuild';

const outDir = '../wwwroot/Scripts';
const shared = {
  entryPoints: ['entry.js'],
  bundle: true,
  format: 'iife',
  platform: 'browser',
  target: ['es2020'],
};

await esbuild.build({ ...shared, outfile: `${outDir}/codemirror6-bundle.js`,     minify: false });
await esbuild.build({ ...shared, outfile: `${outDir}/codemirror6-bundle.min.js`, minify: true  });

console.log('CM6 bundle written to wwwroot/Scripts/codemirror6-bundle.js and .min.js');
