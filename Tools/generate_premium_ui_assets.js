const fs = require("fs");
const path = require("path");
const zlib = require("zlib");

const outDir = path.join(__dirname, "..", "Assets", "Resources", "UI", "Premium");
fs.mkdirSync(outDir, { recursive: true });

const crcTable = new Uint32Array(256);
for (let n = 0; n < 256; n += 1) {
  let c = n;
  for (let k = 0; k < 8; k += 1) {
    c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  }
  crcTable[n] = c >>> 0;
}

function crc32(buf) {
  let c = 0xffffffff;
  for (const b of buf) {
    c = crcTable[(c ^ b) & 0xff] ^ (c >>> 8);
  }
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const typeBuf = Buffer.from(type, "ascii");
  const len = Buffer.alloc(4);
  const crc = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  crc.writeUInt32BE(crc32(Buffer.concat([typeBuf, data])));
  return Buffer.concat([len, typeBuf, data, crc]);
}

function writePng(file, width, height, pixels) {
  const stride = width * 4;
  const raw = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y += 1) {
    raw[y * (stride + 1)] = 0;
    pixels.copy(raw, y * (stride + 1) + 1, y * stride, (y + 1) * stride);
  }

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;

  const png = Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk("IHDR", ihdr),
    chunk("IDAT", zlib.deflateSync(raw, { level: 9 })),
    chunk("IEND", Buffer.alloc(0)),
  ]);
  fs.writeFileSync(path.join(outDir, file), png);
}

function hex(value, alpha = 255) {
  const n = parseInt(value.replace("#", ""), 16);
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255, alpha];
}

function mix(a, b, t) {
  return [
    Math.round(a[0] * (1 - t) + b[0] * t),
    Math.round(a[1] * (1 - t) + b[1] * t),
    Math.round(a[2] * (1 - t) + b[2] * t),
    Math.round(a[3] * (1 - t) + b[3] * t),
  ];
}

function blend(dst, index, src) {
  const sa = src[3] / 255;
  if (sa <= 0) return;
  const da = dst[index + 3] / 255;
  const oa = sa + da * (1 - sa);
  if (oa <= 0) return;
  dst[index] = Math.round((src[0] * sa + dst[index] * da * (1 - sa)) / oa);
  dst[index + 1] = Math.round((src[1] * sa + dst[index + 1] * da * (1 - sa)) / oa);
  dst[index + 2] = Math.round((src[2] * sa + dst[index + 2] * da * (1 - sa)) / oa);
  dst[index + 3] = Math.round(oa * 255);
}

function signedDistanceRoundedRect(px, py, x, y, width, height, radius) {
  const cx = x + width / 2;
  const cy = y + height / 2;
  const qx = Math.abs(px - cx) - (width / 2 - radius);
  const qy = Math.abs(py - cy) - (height / 2 - radius);
  const ox = Math.max(qx, 0);
  const oy = Math.max(qy, 0);
  return Math.hypot(ox, oy) + Math.min(Math.max(qx, qy), 0) - radius;
}

function drawRoundedGradient(width, height, options) {
  const pixels = Buffer.alloc(width * height * 4);
  const top = hex(options.top);
  const mid = hex(options.mid || options.top);
  const bottom = hex(options.bottom || options.top);
  const stroke = hex(options.stroke || options.top, options.strokeA ?? 255);
  const glow = options.glow ? hex(options.glow.hex, Math.round(options.glow.alpha * 255)) : null;

  for (let py = 0; py < height; py += 1) {
    for (let px = 0; px < width; px += 1) {
      const index = (py * width + px) * 4;
      const distance = signedDistanceRoundedRect(px + 0.5, py + 0.5, options.x, options.y, options.w, options.h, options.r);

      if (glow && distance > 0) {
        const alpha = Math.max(0, Math.exp(-(distance * distance) / (2 * options.glow.sigma * options.glow.sigma)) * glow[3]);
        blend(pixels, index, [glow[0], glow[1], glow[2], Math.round(alpha)]);
      }

      if (distance <= 0) {
        const t = Math.max(0, Math.min(1, (py - options.y) / options.h));
        const fill = t < 0.48 ? mix(top, mid, t / 0.48) : mix(mid, bottom, (t - 0.48) / 0.52);
        blend(pixels, index, fill);

        if (options.highlight !== false && py - options.y < 10) {
          blend(pixels, index, [255, 244, 160, Math.round((1 - (py - options.y) / 10) * 70)]);
        }

        if (-distance < (options.strokeW || 3)) {
          blend(pixels, index, stroke);
        }
      }
    }
  }

  writePng(options.file, width, height, pixels);
}

function drawVignette() {
  const width = 512;
  const height = 512;
  const pixels = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const index = (y * width + x) * 4;
      const dx = (x - width / 2) / (width / 2);
      const dy = (y - height * 0.48) / (height / 2);
      const distance = Math.sqrt(dx * dx + dy * dy);
      const edge = Math.max(0, Math.min(1, (distance - 0.42) / 0.58));
      const highlight = Math.max(0, 1 - distance / 0.42);
      blend(pixels, index, [246, 182, 41, Math.round(20 * highlight)]);
      blend(pixels, index, [0, 12, 6, Math.round(48 * edge)]);
    }
  }
  writePng("vignette_soft.png", width, height, pixels);
}

drawRoundedGradient(384, 128, {
  file: "button_primary_gold.png",
  x: 18,
  y: 20,
  w: 348,
  h: 88,
  r: 44,
  top: "#FFF2A8",
  mid: "#F8C547",
  bottom: "#7A3F08",
  stroke: "#FFE88A",
  strokeW: 4,
  glow: { hex: "#F6B629", alpha: 0.42, sigma: 16 },
});

drawRoundedGradient(320, 104, {
  file: "button_secondary_emerald.png",
  x: 14,
  y: 16,
  w: 292,
  h: 72,
  r: 36,
  top: "#1D834A",
  mid: "#0E4F2C",
  bottom: "#062B1B",
  stroke: "#F6B629",
  strokeW: 3,
  glow: { hex: "#31E77A", alpha: 0.26, sigma: 14 },
});

drawRoundedGradient(320, 128, {
  file: "panel_emerald.png",
  x: 14,
  y: 14,
  w: 292,
  h: 100,
  r: 24,
  top: "#16713C",
  mid: "#0E4F2C",
  bottom: "#062B1B",
  stroke: "#F6B629",
  strokeW: 3,
  glow: { hex: "#31E77A", alpha: 0.18, sigma: 16 },
});

drawRoundedGradient(420, 260, {
  file: "panel_cream_gold.png",
  x: 18,
  y: 18,
  w: 384,
  h: 224,
  r: 28,
  top: "#FFF8DA",
  mid: "#F7E8BE",
  bottom: "#B77A1A",
  stroke: "#FFE88A",
  strokeW: 4,
  glow: { hex: "#F6B629", alpha: 0.22, sigma: 18 },
});

drawRoundedGradient(128, 128, {
  file: "icon_circle_gold.png",
  x: 18,
  y: 18,
  w: 92,
  h: 92,
  r: 46,
  top: "#FFE88A",
  mid: "#F6B629",
  bottom: "#A95709",
  stroke: "#FFF2A8",
  strokeW: 3,
  glow: { hex: "#F6B629", alpha: 0.35, sigma: 14 },
});

drawRoundedGradient(128, 128, {
  file: "icon_circle_emerald.png",
  x: 18,
  y: 18,
  w: 92,
  h: 92,
  r: 46,
  top: "#1D834A",
  mid: "#0E4F2C",
  bottom: "#062B1B",
  stroke: "#FFE88A",
  strokeW: 4,
  glow: { hex: "#31E77A", alpha: 0.3, sigma: 14 },
});

drawRoundedGradient(180, 230, {
  file: "category_slot_emerald.png",
  x: 18,
  y: 20,
  w: 144,
  h: 190,
  r: 14,
  top: "#0E4F2C",
  mid: "#06361F",
  bottom: "#02180F",
  stroke: "#BDEACB",
  strokeA: 150,
  strokeW: 2,
  glow: { hex: "#31E77A", alpha: 0.18, sigma: 14 },
  highlight: false,
});

drawRoundedGradient(190, 58, {
  file: "category_label_gold.png",
  x: 12,
  y: 10,
  w: 166,
  h: 38,
  r: 18,
  top: "#FFE88A",
  mid: "#F6B629",
  bottom: "#B86A12",
  stroke: "#FFF2A8",
  strokeW: 2,
  glow: { hex: "#F6B629", alpha: 0.18, sigma: 10 },
});

drawVignette();

function meta(guid, border) {
  const [left, bottom, right, top] = border;
  return `fileFormatVersion: 2
guid: ${guid}
TextureImporter:
  serializedVersion: 12
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  textureType: 8
  spriteMode: 1
  spritePixelsToUnits: 100
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteMeshType: 1
  spritePivot: {x: 0.5, y: 0.5}
  spriteBorder: {x: ${left}, y: ${bottom}, z: ${right}, w: ${top}}
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
}

const metas = {
  "button_primary_gold.png": ["0d84d6184e06421986d0a1f0c381cf64", [68, 44, 68, 44]],
  "button_secondary_emerald.png": ["e5d9e1bb56db4c70a286b7fd7194923d", [56, 36, 56, 36]],
  "panel_emerald.png": ["ad4f5b0d6b5b4aa8978bc6ed27a95ca4", [38, 38, 38, 38]],
  "panel_cream_gold.png": ["eabfd50e8f2c4f0fab5adc522a8948b6", [48, 48, 48, 48]],
  "icon_circle_gold.png": ["5af8beb775ab4640a75da851c33c5efe", [48, 48, 48, 48]],
  "icon_circle_emerald.png": ["dd80721141f947ff9979ef41f23ad1c2", [48, 48, 48, 48]],
  "category_slot_emerald.png": ["f3cd8df706414221b9b5da79a5c32441", [24, 24, 24, 24]],
  "category_label_gold.png": ["a96bcf1833c84786a97434a26608c01e", [32, 20, 32, 20]],
  "vignette_soft.png": ["0b756c739f314f04b1c01547e1ca34ea", [0, 0, 0, 0]],
};

for (const [file, [guid, border]] of Object.entries(metas)) {
  fs.writeFileSync(path.join(outDir, `${file}.meta`), meta(guid, border));
}

console.log(`Generated ${Object.keys(metas).length} premium UI sprites in ${outDir}`);
