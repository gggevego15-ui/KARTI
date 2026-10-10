const fs = require('node:fs');
const path = require('node:path');

const out = path.join(__dirname, '..', 'build');
fs.mkdirSync(out, { recursive: true });

function writeBmp(file, width, height, pixelAt) {
  const rowBytes = Math.ceil(width * 3 / 4) * 4;
  const pixelBytes = rowBytes * height;
  const header = Buffer.alloc(54);
  header.write('BM', 0);
  header.writeUInt32LE(54 + pixelBytes, 2);
  header.writeUInt32LE(54, 10);
  header.writeUInt32LE(40, 14);
  header.writeInt32LE(width, 18);
  header.writeInt32LE(height, 22);
  header.writeUInt16LE(1, 26);
  header.writeUInt16LE(24, 28);
  header.writeUInt32LE(pixelBytes, 34);
  header.writeInt32LE(2835, 38);
  header.writeInt32LE(2835, 42);
  const pixels = Buffer.alloc(pixelBytes);
  for (let y = 0; y < height; y++) {
    const bmpY = height - 1 - y;
    for (let x = 0; x < width; x++) {
      const [r, g, b] = pixelAt(x, y, width, height);
      const offset = bmpY * rowBytes + x * 3;
      pixels[offset] = b;
      pixels[offset + 1] = g;
      pixels[offset + 2] = r;
    }
  }
  fs.writeFileSync(file, Buffer.concat([header, pixels]));
}

function artPixel(x, y, w, h) {
  const t = y / h;
  let r = 5 + 6 * (1 - t), g = 19 + 18 * (1 - t), b = 34 + 25 * (1 - t);
  const glow = (cx, cy, radius, color, strength) => {
    const d = Math.hypot((x - cx) / radius, (y - cy) / radius);
    const a = Math.max(0, 1 - d) * strength;
    r += (color[0] - r) * a;
    g += (color[1] - g) * a;
    b += (color[2] - b) * a;
  };
  glow(w * .16, h * .12, w * .95, [30, 143, 143], .48);
  glow(w * .98, h * .78, w * .8, [92, 83, 172], .43);
  glow(w * .25, h * .57, w * .34, [71, 225, 190], .12);

  const scale = Math.min(w / 164, h / 314);
  const bx = (w - 76 * scale) / 2, by = 36 * scale, bs = 76 * scale;
  const xx = (x - bx) / bs, yy = (y - by) / bs;
  if (xx >= 0 && xx <= 1 && yy >= 0 && yy <= 1) {
    const rounded = xx > .08 && xx < .92 && yy > .02 && yy < .98;
    const mark = (xx > .25 && xx < .39 && yy > .2 && yy < .8) ||
      (xx > .36 && xx < .68 && Math.abs(yy - (1.02 - xx)) < .09) ||
      (xx > .36 && xx < .73 && Math.abs(yy - (xx - .02)) < .09);
    if (rounded && mark) {
      const glass = .75 + .25 * Math.sin((xx + yy) * 18);
      r = 145 * glass; g = 255 * glass; b = 225 * glass;
    } else if (rounded && Math.min(xx, 1 - xx, yy, 1 - yy) < .025) {
      r = 208; g = 255; b = 246;
    }
  }
  if (y % 47 < 1) { r += 4; g += 7; b += 8; }
  return [Math.min(255, r) | 0, Math.min(255, g) | 0, Math.min(255, b) | 0];
}

writeBmp(path.join(out, 'installer-sidebar.bmp'), 164, 314, artPixel);
writeBmp(path.join(out, 'installer-header.bmp'), 150, 57, artPixel);
console.log('Generated branded KARTI Liquid Glass installer artwork.');
