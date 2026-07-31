const fs = require("fs");
const path = require("path");
const sharp = require("sharp");

const outDir = path.resolve("Artifacts/OneTapGarageSplash");
fs.mkdirSync(outDir, { recursive: true });

const C = {
  deep: "#020A07",
  greenBlack: "#03110C",
  emerald: "#0B5A3A",
  emeraldCore: "#123D30",
  gold: "#E8BB63",
  goldMuted: "#C9A55D",
  goldDark: "#9B6B30",
  highlight: "#FFE0A0",
  ivory: "#F3EBDD",
};

function defs(prefix) {
  return `
    <defs>
      <linearGradient id="${prefix}-bg" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="${C.deep}"/>
        <stop offset="0.52" stop-color="#06251A"/>
        <stop offset="1" stop-color="${C.deep}"/>
      </linearGradient>
      <radialGradient id="${prefix}-ambient">
        <stop offset="0" stop-color="${C.emeraldCore}" stop-opacity=".82"/>
        <stop offset=".52" stop-color="${C.emerald}" stop-opacity=".25"/>
        <stop offset="1" stop-color="${C.emerald}" stop-opacity="0"/>
      </radialGradient>
      <radialGradient id="${prefix}-core">
        <stop offset="0" stop-color="${C.gold}" stop-opacity=".12"/>
        <stop offset="1" stop-color="${C.gold}" stop-opacity="0"/>
      </radialGradient>
      <radialGradient id="${prefix}-vignette">
        <stop offset=".44" stop-color="${C.deep}" stop-opacity="0"/>
        <stop offset="1" stop-color="#010503" stop-opacity=".86"/>
      </radialGradient>
      <linearGradient id="${prefix}-mark" x1="42" y1="32" x2="218" y2="226" gradientUnits="userSpaceOnUse">
        <stop stop-color="${C.highlight}"/>
        <stop offset=".42" stop-color="${C.gold}"/>
        <stop offset="1" stop-color="${C.goldDark}"/>
      </linearGradient>
      <filter id="${prefix}-blur">
        <feGaussianBlur stdDeviation="42"/>
      </filter>
      <filter id="${prefix}-soft">
        <feGaussianBlur stdDeviation="18"/>
      </filter>
    </defs>`;
}

function logoMark(prefix, size, x, y, glow = true) {
  const scale = size / 256;
  return `
    <g id="Logo Mark" transform="translate(${x} ${y}) scale(${scale})">
      ${glow ? `<circle cx="128" cy="128" r="92" fill="${C.emerald}" opacity=".22" filter="url(#${prefix}-soft)"/>` : ""}
      <path id="Circuit Facet" d="M164 60L142 38Q128 24 114 38L38 114Q24 128 38 142L114 218Q128 232 142 218L218 142Q232 128 218 114L192 88"
        fill="none" stroke="url(#${prefix}-mark)" stroke-width="18" stroke-linecap="round" stroke-linejoin="round"/>
      <circle id="Tap Point Outer" cx="178" cy="74" r="12" fill="${C.highlight}"/>
      <circle id="Tap Point Core" cx="178" cy="74" r="5" fill="${C.gold}"/>
    </g>`;
}

function logoText(width, x, y, scale = 1) {
  const oneSize = 76 * scale;
  const garageSize = 28 * scale;
  return `
    <g id="Logo Text" text-anchor="middle">
      <text id="ONE TAP" x="${x + width / 2}" y="${y + oneSize}" fill="${C.ivory}"
        font-family="Inter, Arial, sans-serif" font-size="${oneSize}" font-weight="600"
        letter-spacing="${oneSize * 0.08}">ONE TAP</text>
      <text id="GARAGE" x="${x + width / 2}" y="${y + oneSize + 52 * scale}" fill="${C.goldMuted}"
        font-family="Inter, Arial, sans-serif" font-size="${garageSize}" font-weight="500"
        letter-spacing="${garageSize * 0.34}">GARAGE</text>
    </g>`;
}

function particles(w, h) {
  const pts = [
    [.18, .42, 2, .24], [.29, .68, 1.5, .17], [.39, .24, 1.8, .21],
    [.61, .29, 1.4, .18], [.72, .63, 2, .21], [.81, .39, 1.2, .16],
    [.46, .74, 1.1, .14], [.57, .18, 1.3, .16],
  ];
  return `<g id="Particles" fill="${C.gold}">${pts.map(
    ([px, py, r, o]) => `<circle cx="${w * px}" cy="${h * py}" r="${r}" opacity="${o}"/>`
  ).join("")}</g>`;
}

function splashSvg(w, h, orientation) {
  const prefix = `otg-${orientation}`;
  const markSize = orientation === "landscape" ? 220 : 256;
  const markY = orientation === "landscape" ? 250 : orientation === "portrait" ? 570 : 650;
  const textY = orientation === "landscape" ? 508 : orientation === "portrait" ? 865 : 952;
  const markX = (w - markSize) / 2;
  const textWidth = 500;
  const textX = (w - textWidth) / 2;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${defs(prefix)}
    <g id="Background">
      <rect width="${w}" height="${h}" fill="url(#${prefix}-bg)"/>
    </g>
    <g id="Ambient Light">
      <ellipse cx="${w / 2}" cy="${h * .44}" rx="${w * .33}" ry="${h * .33}" fill="url(#${prefix}-ambient)"/>
      <ellipse cx="${w / 2}" cy="${h * .42}" rx="${w * .14}" ry="${h * .13}" fill="url(#${prefix}-core)"/>
    </g>
    ${particles(w, h)}
    <g id="Vignette"><rect width="${w}" height="${h}" fill="url(#${prefix}-vignette)"/></g>
    ${logoMark(prefix, markSize, markX, markY, true)}
    ${logoText(textWidth, textX, textY, 1)}
  </svg>`;
}

function transparentMarkSvg() {
  const w = 512, h = 512, prefix = "mark-export";
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${defs(prefix)}
    ${logoMark(prefix, 420, 46, 46, false)}
  </svg>`;
}

function transparentTextSvg() {
  const w = 1024, h = 256;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${logoText(760, 132, 28, 1.5)}
  </svg>`;
}

function transparentLockupSvg() {
  const w = 1024, h = 1024, prefix = "lockup-export";
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${defs(prefix)}
    ${logoMark(prefix, 420, 302, 145, false)}
    ${logoText(700, 162, 635, 1.4)}
  </svg>`;
}

function designBoardSvg() {
  const w = 2400, h = 1400, prefix = "board";
  const conceptNames = [
    ["01", "CIRCUIT FACET PULSE", "SELECTED", "9.4"],
    ["02", "OTG MONOBLOCK", "LETTERFORM", "7.6"],
    ["03", "TAP HALO", "TOO GENERIC", "7.8"],
    ["04", "IGNITION CUT", "LESS CARDMESH", "7.7"],
    ["05", "IDEA APERTURE", "TOO COMPLEX", "7.9"],
  ];
  const cards = conceptNames.map((d, i) => {
    const x = 70 + i * 455;
    return `<g id="Concept ${d[0]}">
      <rect x="${x}" y="380" width="410" height="420" rx="28" fill="${i === 0 ? "#0B2C20" : "#071B14"}"
        stroke="${i === 0 ? C.gold : "#1D3C31"}" stroke-width="${i === 0 ? 3 : 1}"/>
      ${i === 0
        ? logoMark(prefix, 128, x + 141, 420, false)
        : `<g transform="translate(${x + 141} 420)"><rect x="24" y="24" width="80" height="80" rx="${i === 1 ? 22 : 40}" fill="none" stroke="${C.goldMuted}" stroke-width="8"/><circle cx="${48 + i * 7}" cy="${64 - i * 3}" r="8" fill="${C.gold}"/><path d="M48 88L88 40" stroke="${C.gold}" stroke-width="8" stroke-linecap="round"/></g>`}
      <text x="${x + 34}" y="610" fill="${C.ivory}" font-family="Inter, Arial, sans-serif" font-size="24" font-weight="600" letter-spacing="1.5">${d[1]}</text>
      <text x="${x + 34}" y="665" fill="${i === 0 ? C.highlight : "#91A69C"}" font-family="Inter, Arial, sans-serif" font-size="16" font-weight="500" letter-spacing="3">${d[2]}</text>
      <text x="${x + 34}" y="748" fill="${i === 0 ? C.gold : "#6C8378"}" font-family="Inter, Arial, sans-serif" font-size="52" font-weight="600">${d[3]}</text>
    </g>`;
  }).join("");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${defs(prefix)}
    <rect width="${w}" height="${h}" rx="36" fill="${C.deep}"/>
    <g id="References">
      <text x="70" y="88" fill="${C.ivory}" font-family="Inter, Arial, sans-serif" font-size="42" font-weight="600" letter-spacing="4">ONE TAP GARAGE — SPLASH SYSTEM</text>
      <text x="70" y="142" fill="#B8C8C0" font-family="Inter, Arial, sans-serif" font-size="22">Night prologue to CardMesh: shared emerald–gold temperature, far less brightness and ornament.</text>
      ${[
        ["Deep", C.deep], ["Emerald", C.emerald], ["Champagne", C.gold],
        ["Ivory", C.ivory], ["Muted gold", C.goldMuted],
      ].map(([n, c], i) => `<g id="Color ${n}"><rect x="${70 + i * 250}" y="205" width="210" height="105" rx="18" fill="${c}"/><text x="${88 + i * 250}" y="340" fill="#9EB1A7" font-family="Inter, Arial, sans-serif" font-size="16">${n} · ${c}</text></g>`).join("")}
    </g>
    <g id="Concepts">
      <text x="70" y="370" fill="${C.ivory}" font-family="Inter, Arial, sans-serif" font-size="24" font-weight="600" letter-spacing="3">CONCEPTS — INTERNAL SELECTION</text>
      ${cards}
    </g>
    <g id="Motion Notes">
      <rect x="70" y="860" width="1450" height="455" rx="28" fill="#061B15"/>
      <text x="110" y="920" fill="${C.ivory}" font-family="Inter, Arial, sans-serif" font-size="28" font-weight="600" letter-spacing="2">MOTION NOTES · 1.05 s</text>
      <text x="110" y="975" fill="#D6DED9" font-family="Inter, Arial, sans-serif" font-size="20">
        <tspan x="110" dy="0">0.00  Background visible immediately</tspan>
        <tspan x="110" dy="38">0.04–0.48  Mark alpha 0→1, scale 96→100%, Ease Out Cubic</tspan>
        <tspan x="110" dy="38">0.20–0.58  Text alpha 0→1, Y +10→0, Ease Out Quad</tspan>
        <tspan x="110" dy="38">0.30–0.70  Optional single masked highlight sweep</tspan>
        <tspan x="110" dy="38">0.78–1.05  CanvasGroup alpha 1→0, Ease InOut Quad</tspan>
        <tspan x="110" dy="52">No custom shaders. Hold static if loading runs long; never loop.</tspan>
      </text>
    </g>
    <g id="Export">
      <rect x="1570" y="860" width="760" height="455" rx="28" fill="#061B15"/>
      <text x="1610" y="920" fill="${C.ivory}" font-family="Inter, Arial, sans-serif" font-size="28" font-weight="600" letter-spacing="2">EXPORT / UNITY</text>
      <text x="1610" y="975" fill="#D6DED9" font-family="Inter, Arial, sans-serif" font-size="19">
        <tspan x="1610" dy="0">1920×1080 · opaque</tspan>
        <tspan x="1610" dy="34">1080×1920 · opaque</tspan>
        <tspan x="1610" dy="34">2048×2048 · opaque master</tspan>
        <tspan x="1610" dy="46">512×512 mark · transparent</tspan>
        <tspan x="1610" dy="34">1024×256 text · transparent</tspan>
        <tspan x="1610" dy="34">1024×1024 lockup · transparent</tspan>
        <tspan x="1610" dy="46">Safe area: 12% minimum</tspan>
      </text>
    </g>
  </svg>`;
}

const assets = [
  ["OTG_Splash_L_1920x1080", splashSvg(1920, 1080, "landscape"), true],
  ["OTG_Splash_P_1080x1920", splashSvg(1080, 1920, "portrait"), true],
  ["OTG_Splash_S_2048x2048", splashSvg(2048, 2048, "square"), true],
  ["OTG_LogoMark_512x512", transparentMarkSvg(), false],
  ["OTG_LogoText_1024x256", transparentTextSvg(), false],
  ["OTG_LogoLockup_1024x1024", transparentLockupSvg(), false],
];

async function main() {
  for (const [name, svg, opaque] of assets) {
    const svgPath = path.join(outDir, `${name}.svg`);
    const pngPath = path.join(outDir, `${name}.png`);
    fs.writeFileSync(svgPath, svg);
    let pipeline = sharp(Buffer.from(svg), { density: 72 });
    if (opaque) pipeline = pipeline.removeAlpha();
    await pipeline.png({ compressionLevel: 9, adaptiveFiltering: true }).toFile(pngPath);
  }

  const manifest = {
    concept: "Circuit Facet Pulse",
    fonts: ["Inter Semi Bold", "Inter Medium"],
    colors: C,
    assets: assets.map(([name, , opaque]) => ({
      name,
      svg: `${name}.svg`,
      png: `${name}.png`,
      alpha: opaque ? "opaque RGB" : "transparent RGBA",
    })),
  };
  fs.writeFileSync(path.join(outDir, "manifest.json"), JSON.stringify(manifest, null, 2));

  const board = designBoardSvg();
  fs.writeFileSync(path.join(outDir, "OTG_Design_Board.svg"), board);
  await sharp(Buffer.from(board), { density: 72 }).png({ compressionLevel: 9 }).toFile(path.join(outDir, "OTG_Design_Board.png"));

  const importDir = path.join(outDir, "FigmaImports");
  fs.mkdirSync(importDir, { recursive: true });
  const importNames = [
    "OTG_Design_Board",
    "OTG_Splash_P_1080x1920",
    "OTG_Splash_S_2048x2048",
    "OTG_LogoMark_512x512",
    "OTG_LogoText_1024x256",
    "OTG_LogoLockup_1024x1024",
  ];
  for (const name of importNames) {
    const source = name === "OTG_Design_Board" ? board : assets.find(([n]) => n === name)[1];
    fs.writeFileSync(path.join(importDir, `${name}.svg`), source);
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
