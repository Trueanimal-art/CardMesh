const fs = require("fs");
const path = require("path");
const zlib = require("zlib");

const outDir = path.join(__dirname, "..", "Assets", "Resources", "UI", "RoyalEvent");
fs.mkdirSync(outDir, { recursive: true });

const crcTable = new Uint32Array(256);
for (let n = 0; n < 256; n += 1) {
  let c = n;
  for (let k = 0; k < 8; k += 1) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  crcTable[n] = c >>> 0;
}

function crc32(buf) {
  let c = 0xffffffff;
  for (const b of buf) c = crcTable[(c ^ b) & 0xff] ^ (c >>> 8);
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

  fs.writeFileSync(path.join(outDir, file), Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk("IHDR", ihdr),
    chunk("IDAT", zlib.deflateSync(raw, { level: 9 })),
    chunk("IEND", Buffer.alloc(0)),
  ]));
}

function rgba(hex, alpha = 255) {
  const n = parseInt(hex.replace("#", ""), 16);
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

function blend(pixels, i, src) {
  const sa = src[3] / 255;
  if (sa <= 0) return;
  const da = pixels[i + 3] / 255;
  const oa = sa + da * (1 - sa);
  if (oa <= 0) return;
  pixels[i] = Math.round((src[0] * sa + pixels[i] * da * (1 - sa)) / oa);
  pixels[i + 1] = Math.round((src[1] * sa + pixels[i + 1] * da * (1 - sa)) / oa);
  pixels[i + 2] = Math.round((src[2] * sa + pixels[i + 2] * da * (1 - sa)) / oa);
  pixels[i + 3] = Math.round(oa * 255);
}

function sdr(px, py, x, y, w, h, r) {
  const qx = Math.abs(px - x - w / 2) - (w / 2 - r);
  const qy = Math.abs(py - y - h / 2) - (h / 2 - r);
  return Math.hypot(Math.max(qx, 0), Math.max(qy, 0)) + Math.min(Math.max(qx, qy), 0) - r;
}

function rect(p, w, h, x, y, rw, rh, r, color, stroke, strokeW = 0) {
  for (let py = Math.floor(y - 24); py < Math.ceil(y + rh + 24); py += 1) {
    if (py < 0 || py >= h) continue;
    for (let px = Math.floor(x - 24); px < Math.ceil(x + rw + 24); px += 1) {
      if (px < 0 || px >= w) continue;
      const d = sdr(px + 0.5, py + 0.5, x, y, rw, rh, r);
      const i = (py * w + px) * 4;
      if (d <= 0) {
        const aa = Math.max(0, Math.min(1, 0.9 - d));
        blend(p, i, [color[0], color[1], color[2], Math.round(color[3] * aa)]);
        if (stroke && -d < strokeW) blend(p, i, stroke);
      }
    }
  }
}

function gradientRect(p, w, h, x, y, rw, rh, r, top, mid, bottom, stroke, strokeW = 0) {
  for (let py = Math.floor(y - 30); py < Math.ceil(y + rh + 30); py += 1) {
    if (py < 0 || py >= h) continue;
    for (let px = Math.floor(x - 30); px < Math.ceil(x + rw + 30); px += 1) {
      if (px < 0 || px >= w) continue;
      const d = sdr(px + 0.5, py + 0.5, x, y, rw, rh, r);
      const i = (py * w + px) * 4;
      if (d <= 0) {
        const t = Math.max(0, Math.min(1, (py - y) / rh));
        const c = t < 0.48 ? mix(top, mid, t / 0.48) : mix(mid, bottom, (t - 0.48) / 0.52);
        blend(p, i, c);
        if (py - y < rh * 0.12) blend(p, i, [255, 244, 175, Math.round((1 - (py - y) / (rh * 0.12)) * 28)]);
        if (stroke && -d < strokeW) blend(p, i, stroke);
      } else if (stroke) {
        const glow = Math.exp(-(d * d) / 240) * 26;
        blend(p, i, [255, 198, 42, Math.round(glow)]);
      }
    }
  }
}

function circle(p, w, h, cx, cy, r, color) {
  for (let y = Math.floor(cy - r - 8); y <= Math.ceil(cy + r + 8); y += 1) {
    if (y < 0 || y >= h) continue;
    for (let x = Math.floor(cx - r - 8); x <= Math.ceil(cx + r + 8); x += 1) {
      if (x < 0 || x >= w) continue;
      const d = Math.hypot(x + 0.5 - cx, y + 0.5 - cy);
      const a = Math.max(0, Math.min(1, r + 1 - d));
      if (a > 0) blend(p, (y * w + x) * 4, [color[0], color[1], color[2], Math.round(color[3] * a)]);
    }
  }
}

function ellipse(p, w, h, cx, cy, rx, ry, color) {
  for (let y = Math.floor(cy - ry - 8); y <= Math.ceil(cy + ry + 8); y += 1) {
    if (y < 0 || y >= h) continue;
    for (let x = Math.floor(cx - rx - 8); x <= Math.ceil(cx + rx + 8); x += 1) {
      if (x < 0 || x >= w) continue;
      const dx = (x + 0.5 - cx) / rx;
      const dy = (y + 0.5 - cy) / ry;
      const d = Math.sqrt(dx * dx + dy * dy);
      const a = Math.max(0, Math.min(1, 1.02 - d));
      if (a > 0) blend(p, (y * w + x) * 4, [color[0], color[1], color[2], Math.round(color[3] * a)]);
    }
  }
}

function line(p, w, h, x1, y1, x2, y2, thick, color) {
  const dx = x2 - x1;
  const dy = y2 - y1;
  const len = dx * dx + dy * dy;
  for (let y = Math.floor(Math.min(y1, y2) - thick - 2); y <= Math.ceil(Math.max(y1, y2) + thick + 2); y += 1) {
    if (y < 0 || y >= h) continue;
    for (let x = Math.floor(Math.min(x1, x2) - thick - 2); x <= Math.ceil(Math.max(x1, x2) + thick + 2); x += 1) {
      if (x < 0 || x >= w) continue;
      const t = len === 0 ? 0 : Math.max(0, Math.min(1, ((x + 0.5 - x1) * dx + (y + 0.5 - y1) * dy) / len));
      const px = x1 + dx * t;
      const py = y1 + dy * t;
      const d = Math.hypot(x + 0.5 - px, y + 0.5 - py);
      const a = Math.max(0, Math.min(1, thick / 2 + 0.8 - d));
      if (a > 0) blend(p, (y * w + x) * 4, [color[0], color[1], color[2], Math.round(color[3] * a)]);
    }
  }
}

function poly(p, w, h, pts, color) {
  const minY = Math.floor(Math.min(...pts.map(([, y]) => y)));
  const maxY = Math.ceil(Math.max(...pts.map(([, y]) => y)));
  for (let y = minY; y <= maxY; y += 1) {
    if (y < 0 || y >= h) continue;
    const xs = [];
    for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
      const [xi, yi] = pts[i];
      const [xj, yj] = pts[j];
      if ((yi > y) !== (yj > y)) xs.push(((xj - xi) * (y - yi)) / (yj - yi) + xi);
    }
    xs.sort((a, b) => a - b);
    for (let k = 0; k < xs.length; k += 2) {
      for (let x = Math.floor(xs[k]); x <= Math.ceil(xs[k + 1]); x += 1) {
        if (x >= 0 && x < w) blend(p, (y * w + x) * 4, color);
      }
    }
  }
}

function diamond(p, w, h, cx, cy, size, color, glow = 0) {
  if (glow > 0) circle(p, w, h, cx, cy, size * 1.45, [255, 210, 55, glow]);
  poly(p, w, h, [[cx, cy - size], [cx + size, cy], [cx, cy + size], [cx - size, cy]], color);
}

function card(p, w, h, cx, cy, cw, ch, rot, active = false) {
  const x = cx - cw / 2;
  const y = cy - ch / 2;
  if (active) ellipse(p, w, h, cx, cy, cw * 0.78, ch * 0.52, rgba("#AFFF39", 38));
  gradientRect(p, w, h, x - 10, y - 10, cw + 20, ch + 20, 22, rgba("#FFEBA0"), rgba("#D1932E"), rgba("#6F3A08"), rgba(active ? "#FFECA0" : "#A96F1E"), 6);
  gradientRect(p, w, h, x + 8, y + 8, cw - 16, ch - 16, 15, rgba("#0E6033"), rgba("#05331D"), rgba("#01140A"), rgba("#B77E24"), 3);
  rect(p, w, h, x + 22, y + 24, cw - 44, ch - 48, 10, rgba("#062B19", 68), rgba("#2FED84", 58), 2);
  diamond(p, w, h, cx, y + 38, 16, rgba("#1BE778", 220), 18);
  diamond(p, w, h, cx, y + ch - 34, 12, rgba("#B97915", 220), 10);
  line(p, w, h, x + 28, y + 22, x + 72, y + 22, 3, rgba("#FFF3A1", 140));
  line(p, w, h, x + cw - 28, y + ch - 22, x + cw - 72, y + ch - 22, 3, rgba("#5D2E05", 140));
  if (rot !== 0) {
    // The surrounding glow masks the simple non-rotated inner construction well enough for the generated hero stack.
  }
}

function crown(p, w, h, cx, cy, scale = 1) {
  const shadow = rgba("#5D2A00", 135);
  const gold = rgba("#FFE45A");
  const bright = rgba("#FFF6A7");
  const amber = rgba("#F08A08");
  ellipse(p, w, h, cx, cy + 50 * scale, 132 * scale, 34 * scale, rgba("#000000", 70));
  circle(p, w, h, cx, cy, 132 * scale, rgba("#FFE72E", 24));
  const peaks = [
    [-118, 44, -91, -40, -54, 38],
    [-70, 35, -39, -82, -14, 38],
    [-30, 36, 0, -124, 30, 36],
    [14, 38, 39, -82, 70, 35],
    [54, 38, 91, -40, 118, 44],
  ];
  for (const pts of peaks) {
    poly(p, w, h, [[cx + pts[0] * scale, cy + pts[1] * scale], [cx + pts[2] * scale, cy + pts[3] * scale], [cx + pts[4] * scale, cy + pts[5] * scale]], shadow);
  }
  for (const pts of peaks) {
    poly(p, w, h, [[cx + pts[0] * scale, cy + (pts[1] - 8) * scale], [cx + pts[2] * scale, cy + (pts[3] - 8) * scale], [cx + pts[4] * scale, cy + (pts[5] - 8) * scale]], gold);
  }
  for (const [px, py] of [[-91, -48], [-39, -90], [0, -132], [39, -90], [91, -48]]) {
    circle(p, w, h, cx + px * scale, cy + py * scale, 12 * scale, rgba("#FFF7BE"));
    circle(p, w, h, cx + px * scale - 3 * scale, cy + py * scale - 3 * scale, 5 * scale, rgba("#FFFFFF", 170));
  }
  gradientRect(p, w, h, cx - 132 * scale, cy + 22 * scale, 264 * scale, 66 * scale, 17 * scale, rgba("#FFC83D"), amber, rgba("#B84A00"), rgba("#FFF1A3"), 4 * scale);
  rect(p, w, h, cx - 116 * scale, cy + 34 * scale, 232 * scale, 18 * scale, 8 * scale, rgba("#FFF5A8", 74), null, 0);
  for (const [jx, jy, js] of [[-64, 58, 17], [0, 60, 24], [64, 58, 17]]) {
    diamond(p, w, h, cx + jx * scale, cy + jy * scale, js * scale, rgba("#08DB68"), 18 * scale);
    diamond(p, w, h, cx + jx * scale - 3 * scale, cy + (jy - 4) * scale, js * 0.42 * scale, rgba("#B8FFD4", 180), 0);
  }
  line(p, w, h, cx - 90 * scale, cy + 84 * scale, cx + 90 * scale, cy + 84 * scale, 4 * scale, rgba("#6B2F00", 110));
}

function drawPanel() {
  const w = 1500, h = 1080, p = Buffer.alloc(w * h * 4);
  gradientRect(p, w, h, 28, 30, 1444, 1020, 32, rgba("#062F1D", 250), rgba("#02180F", 252), rgba("#010805", 255), rgba("#C98D28"), 8);
  rect(p, w, h, 54, 58, 1392, 968, 20, rgba("#031C11", 130), rgba("#815614", 150), 2);
  for (let i = 0; i < 260; i += 1) {
    const x = 90 + ((i * 197) % 1320);
    const y = 92 + ((i * 137) % 900);
    circle(p, w, h, x, y, (i % 3) + 1.2, rgba(i % 2 ? "#D8A943" : "#18A65A", i % 2 ? 72 : 46));
  }
  line(p, w, h, 138, 136, 580, 136, 4, rgba("#AA7620", 160));
  line(p, w, h, 920, 136, 1362, 136, 4, rgba("#AA7620", 160));
  crown(p, w, h, 750, 60, 0.42);
  for (const [x, y] of [[54, 58], [1446, 58], [54, 1026], [1446, 1026]]) {
    diamond(p, w, h, x, y, 24, rgba("#16C75F"), 20);
    diamond(p, w, h, x, y, 42, rgba("#D69324", 48), 0);
  }
  writePng("royal_event_panel.png", w, h, p);
}

function drawHero() {
  const w = 900, h = 430, p = Buffer.alloc(w * h * 4);
  for (let i = 0; i < 42; i += 1) {
    const a = (Math.PI * 2 * i) / 42;
    line(p, w, h, 450, 214, 450 + Math.cos(a) * 390, 214 + Math.sin(a) * 160, 3, rgba("#D7B646", i % 2 ? 28 : 18));
  }
  ellipse(p, w, h, 450, 210, 270, 130, rgba("#8DFF35", 32));
  card(p, w, h, 270, 226, 176, 250, -15);
  card(p, w, h, 630, 226, 176, 250, 15);
  card(p, w, h, 450, 216, 218, 292, 0, true);
  crown(p, w, h, 450, 206, 0.96);
  for (const [x, y] of [[330, 96], [570, 94], [312, 288], [588, 286]]) {
    diamond(p, w, h, x, y, 12, rgba("#25D874", 160), 14);
  }
  gradientRect(p, w, h, 314, 326, 272, 72, 35, rgba("#124225"), rgba("#061F12"), rgba("#020C06"), rgba("#C58A24"), 4);
  writePng("royal_event_hero.png", w, h, p);
}

function drawReward(active) {
  const w = 250, h = 320, p = Buffer.alloc(w * h * 4);
  if (active) ellipse(p, w, h, 125, 162, 122, 148, rgba("#FFE43E", 42));
  gradientRect(p, w, h, 24, 28, 202, 268, 22, rgba("#F5D980"), rgba("#A86B19"), rgba("#3B1B04"), rgba(active ? "#E8C65A" : "#8F601C"), active ? 6 : 4);
  gradientRect(p, w, h, 40, 48, 170, 226, 14, rgba("#0A4126"), rgba("#041E12"), rgba("#010905"), rgba("#2BBE66"), active ? 2 : 1);
  rect(p, w, h, 54, 64, 142, 194, 11, rgba("#02170D", 118), rgba("#22A956", active ? 55 : 28), 2);
  diamond(p, w, h, 125, 30, 28, rgba(active ? "#EAD06B" : "#B47A24"), 16);
  diamond(p, w, h, 125, 30, 12, rgba("#092A18", 240), 0);
  diamond(p, w, h, 125, 288, 20, rgba("#19DA69", active ? 255 : 210), 28);
  line(p, w, h, 58, 74, 88, 74, 3, rgba("#FFF3A1", 120));
  line(p, w, h, 192, 252, 160, 252, 3, rgba("#5D2E05", 120));
  writePng(active ? "royal_reward_card_active.png" : "royal_reward_card.png", w, h, p);
}

function drawTooltip() {
  const w = 380, h = 140, p = Buffer.alloc(w * h * 4);
  gradientRect(p, w, h, 14, 14, 352, 94, 18, rgba("#FFF5D0"), rgba("#F4D99B"), rgba("#CF8C27"), rgba("#FFE88A"), 4);
  poly(p, w, h, [[177, 106], [203, 106], [190, 136]], rgba("#B77B26"));
  poly(p, w, h, [[182, 106], [198, 106], [190, 124]], rgba("#FFF1C4"));
  writePng("royal_tooltip.png", w, h, p);
}

function drawProgress(file, fill) {
  const w = 1100, h = 70, p = Buffer.alloc(w * h * 4);
  if (fill) {
    gradientRect(p, w, h, 28, 24, 1044, 22, 11, rgba("#78EC42"), rgba("#18B745"), rgba("#056B29"), rgba("#A9E56C"), 2);
  } else {
    rect(p, w, h, 18, 18, 1064, 34, 17, rgba("#2B1704", 245), rgba("#B6761E", 230), 5);
    rect(p, w, h, 28, 26, 1044, 18, 9, rgba("#160B02", 235), rgba("#80500E", 180), 2);
    line(p, w, h, 52, 31, 1048, 31, 2, rgba("#D2A13C", 88));
  }
  writePng(file, w, h, p);
}

function drawCoin() {
  const w = 128, h = 128, p = Buffer.alloc(w * h * 4);
  circle(p, w, h, 64, 64, 45, rgba("#8A4A00", 190));
  circle(p, w, h, 60, 58, 43, rgba("#FFC422"));
  circle(p, w, h, 54, 48, 18, rgba("#FFF097", 145));
  poly(p, w, h, [[64, 37], [78, 58], [88, 47], [82, 80], [46, 80], [40, 47], [50, 58]], rgba("#C87A00", 235));
  writePng("royal_icon_coin.png", w, h, p);
}

function drawButton() {
  const w = 160, h = 160, p = Buffer.alloc(w * h * 4);
  circle(p, w, h, 82, 86, 72, rgba("#3B2000", 120));
  circle(p, w, h, 80, 80, 72, rgba("#FFE23A"));
  circle(p, w, h, 80, 80, 61, rgba("#B86104"));
  circle(p, w, h, 80, 80, 53, rgba("#0B6336"));
  circle(p, w, h, 80, 80, 45, rgba("#063A22"));
  circle(p, w, h, 66, 58, 24, rgba("#FFFFFF", 54));
  crown(p, w, h, 80, 86, 0.45);
  circle(p, w, h, 80, 80, 76, rgba("#FFE83F", 18));
  writePng("royal_event_button.png", w, h, p);
}

function drawBadge() {
  const w = 82, h = 52, p = Buffer.alloc(w * h * 4);
  gradientRect(p, w, h, 6, 8, 70, 34, 17, rgba("#FF604D"), rgba("#C91112"), rgba("#740405"), rgba("#FFF0B4"), 3);
  rect(p, w, h, 13, 12, 56, 10, 5, rgba("#FFFFFF", 54), null, 0);
  writePng("royal_badge_red.png", w, h, p);
}

function drawMarker() {
  const w = 72, h = 72, p = Buffer.alloc(w * h * 4);
  diamond(p, w, h, 36, 36, 25, rgba("#C98B24"), 16);
  diamond(p, w, h, 36, 36, 17, rgba("#0B6E35"), 0);
  diamond(p, w, h, 36, 36, 8, rgba("#25F17A"), 0);
  writePng("royal_progress_marker.png", w, h, p);
}

function meta(guid, border, max = 2048) {
  const [l, b, r, t] = border;
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
  spriteBorder: {x: ${l}, y: ${b}, z: ${r}, w: ${t}}
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: ${max}
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 70
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

drawPanel();
drawHero();
drawReward(false);
drawReward(true);
drawTooltip();
drawProgress("royal_progress_back.png", false);
drawProgress("royal_progress_fill.png", true);
drawCoin();
drawButton();
drawBadge();
drawMarker();

const metas = {
  "royal_event_panel.png": ["28b3a54da2d84f5495fb92b619f34e0a", [64, 64, 64, 64], 2048],
  "royal_event_hero.png": ["785a76cf25da4e45a1a67a68ce18b1ad", [0, 0, 0, 0], 1024],
  "royal_reward_card.png": ["3a12e8a1fced41d7a6a31c206b9a5279", [42, 48, 42, 42], 512],
  "royal_reward_card_active.png": ["0a3a29d5fc504ac29f4e5cd19d1e1265", [42, 48, 42, 42], 512],
  "royal_tooltip.png": ["af7ec869fa1c49e4b8c74b43e7493d99", [34, 34, 34, 34], 512],
  "royal_progress_back.png": ["848cd7f65c2449188464f8a7478f930e", [46, 24, 46, 24], 2048],
  "royal_progress_fill.png": ["f46cd297e66a4fa9ba1f703d694b1821", [46, 24, 46, 24], 2048],
  "royal_icon_coin.png": ["5a10836b4d4e4d4ead4eea9e32d0c215", [0, 0, 0, 0], 256],
  "royal_event_button.png": ["6c10cb603ba24d7e82c7e3ae90bc95d7", [0, 0, 0, 0], 256],
  "royal_badge_red.png": ["7d22ed5cf0cd41e3849a4db250cb0a1a", [22, 18, 22, 18], 256],
  "royal_progress_marker.png": ["514c69ec366b420b9e2313d378da5c98", [0, 0, 0, 0], 256],
};

for (const [file, [guid, border, max]] of Object.entries(metas)) {
  fs.writeFileSync(path.join(outDir, `${file}.meta`), meta(guid, border, max));
}

console.log(`Generated ${Object.keys(metas).length} RoyalEvent sprites in ${outDir}`);
