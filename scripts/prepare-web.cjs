const fs = require('node:fs');
const path = require('node:path');
const packageJson = require(path.join(__dirname, '..', 'package.json'));
const root = path.resolve(__dirname, '..');
const out = path.join(root, 'www');
fs.rmSync(out, { recursive: true, force: true });
fs.mkdirSync(out, { recursive: true });
for (const file of ['index.html', 'manifest.webmanifest', 'sw.js', 'icon.svg']) {
  let contents = fs.readFileSync(path.join(root, file), 'utf8');
  if (file === 'index.html') contents = contents.replace(/(<meta name="karti-version" content=")[^"]+("\s*>)/, '$1' + packageJson.version + '$2');
  fs.writeFileSync(path.join(out, file), contents);
}
console.log('Prepared KARTI web assets in www/');
