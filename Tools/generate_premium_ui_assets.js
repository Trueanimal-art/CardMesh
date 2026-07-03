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

function drawLayeredActionButton() {
  const width = 560;
  const height = 150;
  const pixels = Buffer.alloc(width * height * 4);
  const clear = [0, 0, 0, 0];
  const shadow = [46, 18, 0, 128];
  const goldDark = [112, 61, 10, 255];
  const goldMid = [189, 130, 33, 255];
  const goldLight = [242, 196, 64, 255];
  const greenTop = [20, 92, 48, 255];
  const greenBottom = [3, 51, 33, 255];
  const greenGloss = [89, 168, 99, 255];

  for (let py = 0; py < height; py += 1) {
    for (let px = 0; px < width; px += 1) {
      const index = (py * width + px) * 4;
      const outer = signedDistanceRoundedRect(px + 0.5, py + 0.5, 10, 10, width - 20, height - 20, 54);
      if (outer > 0) {
        const softShadow = Math.max(0, Math.min(1, 1 - outer / 8));
        const source = softShadow > 0 ? [shadow[0], shadow[1], shadow[2], Math.round(shadow[3] * softShadow)] : clear;
        pixels[index] = source[0];
        pixels[index + 1] = source[1];
        pixels[index + 2] = source[2];
        pixels[index + 3] = source[3];
        continue;
      }

      const inner = signedDistanceRoundedRect(px + 0.5, py + 0.5, 18, 18, width - 36, height - 36, 46);
      const vertical = Math.max(0, Math.min(1, py / (height - 1)));
      const center = Math.max(0, Math.min(1, 1 - Math.abs(px - width * 0.5) / (width * 0.5)));
      let pixel;

      if (inner <= 0) {
        const enamelT = Math.max(0, Math.min(1, (py - 34) / 82));
        pixel = mix(greenTop, greenBottom, enamelT);

        const topGloss = Math.max(0, Math.min(1, 1 - Math.abs(py - 40) / 11))
          * Math.max(0, Math.min(1, 1 - Math.abs(px - width * 0.5) / 250));
        pixel = mix(pixel, greenGloss, topGloss * 0.08);
      } else {
        const bevel = Math.max(0, Math.min(1, Math.abs(outer) / 22));
        pixel = mix(goldLight, mix(goldMid, goldDark, vertical), bevel);
        if (inner < 5) {
          pixel = mix(pixel, greenTop, Math.max(0, Math.min(1, 1 - inner / 5)) * 0.12);
        }
      }

      pixels[index] = pixel[0];
      pixels[index + 1] = pixel[1];
      pixels[index + 2] = pixel[2];
      pixels[index + 3] = pixel[3];
    }
  }

  writePng("button_gameplay_action_gold.png", width, height, pixels);
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

function drawSoftCircle(pixels, width, height, cx, cy, radius, color) {
  for (let y = Math.floor(cy - radius - 3); y <= Math.ceil(cy + radius + 3); y += 1) {
    if (y < 0 || y >= height) continue;
    for (let x = Math.floor(cx - radius - 3); x <= Math.ceil(cx + radius + 3); x += 1) {
      if (x < 0 || x >= width) continue;
      const distance = Math.hypot(x + 0.5 - cx, y + 0.5 - cy);
      const alpha = Math.max(0, Math.min(1, radius + 0.8 - distance));
      if (alpha <= 0) continue;
      const index = (y * width + x) * 4;
      blend(pixels, index, [color[0], color[1], color[2], Math.round(color[3] * alpha)]);
    }
  }
}

function drawSoftLine(pixels, width, height, x1, y1, x2, y2, thickness, color) {
  const minX = Math.floor(Math.min(x1, x2) - thickness - 2);
  const maxX = Math.ceil(Math.max(x1, x2) + thickness + 2);
  const minY = Math.floor(Math.min(y1, y2) - thickness - 2);
  const maxY = Math.ceil(Math.max(y1, y2) + thickness + 2);
  const dx = x2 - x1;
  const dy = y2 - y1;
  const lengthSq = dx * dx + dy * dy;

  for (let y = minY; y <= maxY; y += 1) {
    if (y < 0 || y >= height) continue;
    for (let x = minX; x <= maxX; x += 1) {
      if (x < 0 || x >= width) continue;
      const t = lengthSq <= 0 ? 0 : Math.max(0, Math.min(1, (((x + 0.5 - x1) * dx) + ((y + 0.5 - y1) * dy)) / lengthSq));
      const px = x1 + dx * t;
      const py = y1 + dy * t;
      const distance = Math.hypot(x + 0.5 - px, y + 0.5 - py);
      const alpha = Math.max(0, Math.min(1, thickness / 2 + 0.9 - distance));
      if (alpha <= 0) continue;
      const index = (y * width + x) * 4;
      blend(pixels, index, [color[0], color[1], color[2], Math.round(color[3] * alpha)]);
    }
  }
}

function drawSolidRoundedRect(pixels, width, height, x, y, w, h, radius, color) {
  for (let py = Math.floor(y - 2); py <= Math.ceil(y + h + 2); py += 1) {
    if (py < 0 || py >= height) continue;
    for (let px = Math.floor(x - 2); px <= Math.ceil(x + w + 2); px += 1) {
      if (px < 0 || px >= width) continue;
      const distance = signedDistanceRoundedRect(px + 0.5, py + 0.5, x, y, w, h, radius);
      const alpha = Math.max(0, Math.min(1, 0.8 - distance));
      if (alpha <= 0) continue;
      const index = (py * width + px) * 4;
      blend(pixels, index, [color[0], color[1], color[2], Math.round(color[3] * alpha)]);
    }
  }
}

function drawIcon(file, painter) {
  const width = 128;
  const height = 128;
  const pixels = Buffer.alloc(width * height * 4);
  painter(pixels, width, height);
  writePng(file, width, height, pixels);
}

drawIcon("icon_hint_bulb.png", (pixels, width, height) => {
  const cream = [255, 248, 212, 255];
  const gold = [255, 214, 79, 245];
  const amber = [177, 101, 9, 235];
  drawSoftCircle(pixels, width, height, 64, 48, 27, [255, 222, 88, 80]);
  drawSoftCircle(pixels, width, height, 64, 45, 21, gold);
  drawSoftCircle(pixels, width, height, 58, 38, 9, cream);
  drawSoftLine(pixels, width, height, 52, 50, 64, 65, 6, amber);
  drawSoftLine(pixels, width, height, 76, 50, 64, 65, 6, amber);
  drawSolidRoundedRect(pixels, width, height, 50, 68, 28, 15, 5, cream);
  drawSoftLine(pixels, width, height, 54, 88, 74, 88, 6, amber);
  drawSoftLine(pixels, width, height, 57, 96, 71, 96, 5, amber);
});

drawIcon("icon_undo_gold.png", (pixels, width, height) => {
  const cream = [255, 247, 203, 255];
  const amber = [255, 203, 67, 250];
  const shadow = [113, 61, 5, 185];
  drawSoftLine(pixels, width, height, 82, 36, 46, 36, 13, shadow);
  drawSoftLine(pixels, width, height, 46, 36, 32, 53, 13, shadow);
  drawSoftLine(pixels, width, height, 46, 36, 32, 19, 13, shadow);
  for (let i = 0; i < 30; i += 1) {
    const a = Math.PI * (0.07 + i / 35);
    const x1 = 82 - Math.cos(a) * 34;
    const y1 = 68 - Math.sin(a) * 31;
    const x2 = 82 - Math.cos(a + 0.06) * 34;
    const y2 = 68 - Math.sin(a + 0.06) * 31;
    drawSoftLine(pixels, width, height, x1, y1, x2, y2, 13, shadow);
  }
  drawSoftLine(pixels, width, height, 84, 40, 48, 40, 10, amber);
  drawSoftLine(pixels, width, height, 48, 40, 34, 54, 10, amber);
  drawSoftLine(pixels, width, height, 48, 40, 34, 26, 10, amber);
  for (let i = 0; i < 30; i += 1) {
    const a = Math.PI * (0.1 + i / 36);
    const x1 = 82 - Math.cos(a) * 32;
    const y1 = 68 - Math.sin(a) * 28;
    const x2 = 82 - Math.cos(a + 0.06) * 32;
    const y2 = 68 - Math.sin(a + 0.06) * 28;
    drawSoftLine(pixels, width, height, x1, y1, x2, y2, 10, amber);
  }
  drawSoftLine(pixels, width, height, 78, 42, 50, 42, 4, cream);
});

drawIcon("icon_booster_rocket.png", (pixels, width, height) => {
  const cream = [255, 248, 218, 255];
  const gold = [255, 203, 67, 255];
  const orange = [238, 102, 18, 245];
  const emerald = [45, 214, 112, 240];
  drawSoftCircle(pixels, width, height, 43, 88, 16, [255, 129, 21, 110]);
  drawSoftLine(pixels, width, height, 51, 74, 32, 102, 11, orange);
  drawSoftLine(pixels, width, height, 55, 69, 72, 87, 15, [119, 65, 6, 165]);
  drawSoftLine(pixels, width, height, 50, 72, 86, 36, 22, cream);
  drawSoftLine(pixels, width, height, 55, 77, 91, 41, 12, gold);
  drawSoftCircle(pixels, width, height, 87, 36, 15, gold);
  drawSoftCircle(pixels, width, height, 86, 37, 7, cream);
  drawSoftLine(pixels, width, height, 48, 78, 34, 82, 13, emerald);
  drawSoftLine(pixels, width, height, 62, 93, 59, 108, 13, emerald);
});

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

drawLayeredActionButton();

drawRoundedGradient(320, 96, {
  file: "hud_counter_gold.png",
  x: 10,
  y: 16,
  w: 300,
  h: 64,
  r: 26,
  top: "#F6D888",
  mid: "#DB9D3D",
  bottom: "#A9631A",
  stroke: "#F4D57A",
  strokeA: 236,
  strokeW: 3,
});

drawRoundedGradient(232, 140, {
  file: "hud_panel_cream_gold.png",
  x: 12,
  y: 18,
  w: 208,
  h: 108,
  r: 24,
  top: "#FFF0BE",
  mid: "#E9C36F",
  bottom: "#B57420",
  stroke: "#F2D477",
  strokeA: 238,
  strokeW: 3,
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
  "button_gameplay_action_gold.png": ["87713c2de3634b9f85447dd85c61cb68", [84, 60, 84, 60]],
  "hud_counter_gold.png": ["bb876ab7b37e4a979f0f0e4f180dfa9f", [42, 32, 42, 32]],
  "hud_panel_cream_gold.png": ["c4e2160de73d44fa8ce87b74e7416ec4", [44, 44, 44, 44]],
  "button_secondary_emerald.png": ["e5d9e1bb56db4c70a286b7fd7194923d", [56, 36, 56, 36]],
  "panel_emerald.png": ["ad4f5b0d6b5b4aa8978bc6ed27a95ca4", [38, 38, 38, 38]],
  "panel_cream_gold.png": ["eabfd50e8f2c4f0fab5adc522a8948b6", [48, 48, 48, 48]],
  "icon_circle_gold.png": ["5af8beb775ab4640a75da851c33c5efe", [48, 48, 48, 48]],
  "icon_circle_emerald.png": ["dd80721141f947ff9979ef41f23ad1c2", [48, 48, 48, 48]],
  "icon_hint_bulb.png": ["1564c9a86ad04e338518c8a1ad20e9f1", [0, 0, 0, 0]],
  "icon_undo_gold.png": ["71b1d163a4f94bc8b6bbd7bc40a99aa7", [0, 0, 0, 0]],
  "icon_booster_rocket.png": ["e87519b73d3d474995dd19fce1b2b254", [0, 0, 0, 0]],
  "category_slot_emerald.png": ["f3cd8df706414221b9b5da79a5c32441", [24, 24, 24, 24]],
  "category_label_gold.png": ["a96bcf1833c84786a97434a26608c01e", [32, 20, 32, 20]],
  "vignette_soft.png": ["0b756c739f314f04b1c01547e1ca34ea", [0, 0, 0, 0]],
};

for (const [file, [guid, border]] of Object.entries(metas)) {
  fs.writeFileSync(path.join(outDir, `${file}.meta`), meta(guid, border));
}

console.log(`Generated ${Object.keys(metas).length} premium UI sprites in ${outDir}`);
