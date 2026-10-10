const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const out = path.join(root, 'www');
fs.rmSync(out, { recursive: true, force: true });
fs.mkdirSync(out, { recursive: true });
for (const file of ['index.html', 'manifest.webmanifest', 'sw.js', 'icon.svg']) {
  fs.copyFileSync(path.join(root, file), path.join(out, file));
}
console.log('Prepared KARTI web assets in www/');
