import path from "node:path";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const dependencyRoot = process.env.CARDACTION_NODE_MODULES;
if (!dependencyRoot) {
  throw new Error("CARDACTION_NODE_MODULES must point to the bundled node_modules.");
}
const sharp = require(path.join(dependencyRoot, "sharp"));

const [inputPath, outputPath] = process.argv.slice(2);
if (!inputPath || !outputPath) {
  throw new Error(
    "Usage: process_mobile_action_plate.mjs <source.png> <output.png>",
  );
}

const { data, info } = await sharp(inputPath)
  .ensureAlpha()
  .raw()
  .toBuffer({ resolveWithObject: true });
const output = Buffer.from(data);
const visited = new Uint8Array(info.width * info.height);
const queue = new Int32Array(info.width * info.height);
let head = 0;
let tail = 0;

function isBackground(pixelIndex) {
  const offset = pixelIndex * 4;
  const red = output[offset];
  const green = output[offset + 1];
  const blue = output[offset + 2];
  const maximum = Math.max(red, green, blue);
  const minimum = Math.min(red, green, blue);
  return minimum >= 236 && maximum - minimum <= 7;
}

function enqueue(pixelIndex) {
  if (visited[pixelIndex] || !isBackground(pixelIndex)) {
    return;
  }
  visited[pixelIndex] = 1;
  queue[tail++] = pixelIndex;
}

for (let x = 0; x < info.width; x += 1) {
  enqueue(x);
  enqueue((info.height - 1) * info.width + x);
}
for (let y = 0; y < info.height; y += 1) {
  enqueue(y * info.width);
  enqueue(y * info.width + info.width - 1);
}

while (head < tail) {
  const pixelIndex = queue[head++];
  output[pixelIndex * 4 + 3] = 0;
  const x = pixelIndex % info.width;
  const y = Math.floor(pixelIndex / info.width);
  if (x > 0) enqueue(pixelIndex - 1);
  if (x + 1 < info.width) enqueue(pixelIndex + 1);
  if (y > 0) enqueue(pixelIndex - info.width);
  if (y + 1 < info.height) enqueue(pixelIndex + info.width);
}

const transparentPng = await sharp(output, {
  raw: {
    width: info.width,
    height: info.height,
    channels: 4,
  },
})
  .png()
  .toBuffer();

const trimmed = await sharp(transparentPng)
  .trim({ background: { r: 0, g: 0, b: 0, alpha: 0 }, threshold: 6 })
  .png()
  .toBuffer();

await sharp(trimmed)
  .resize(600, 280, {
    fit: "contain",
    background: { r: 0, g: 0, b: 0, alpha: 0 },
    kernel: sharp.kernel.lanczos3,
  })
  .png({ compressionLevel: 9 })
  .toFile(outputPath);
