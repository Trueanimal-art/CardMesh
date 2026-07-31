import fs from "node:fs";
import path from "node:path";
import sharp from "/Users/vnm/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp/lib/index.js";

const ROOT = "/Users/vnm/Documents/CardMesh";
const SOURCE_MENU = path.join(ROOT, "Artifacts/MainMenuAmbient_GameView.png");
const CURRENT = path.join(ROOT, "Artifacts/OneTapGarageSplash/PrototypeRound5/Prototype_B_frames");
const OUT = path.join(ROOT, "Artifacts/OneTapGarageSplash/RefinedB");
const REFINED_FRAMES = path.join(OUT, "Refined_B_frames");
const CURRENT_FRAMES = path.join(OUT, "Current_B_1080_frames");
const KEYFRAMES = path.join(OUT, "Keyframes");
const W = 1920;
const H = 1080;
const FPS = 30;
const DURATION = 2.05;
const COUNT = Math.ceil(DURATION * FPS);

for (const dir of [OUT, REFINED_FRAMES, CURRENT_FRAMES, KEYFRAMES]) fs.mkdirSync(dir, { recursive: true });

const clamp = (v, a = 0, b = 1) => Math.max(a, Math.min(b, v));
const ramp = (t, a, b) => clamp((t - a) / (b - a));
const cubicOut = (x) => 1 - Math.pow(1 - clamp(x), 3);
const cubicInOut = (x) => {
  x = clamp(x);
  return x < .5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
};
const smooth = (t, a, b) => cubicOut(ramp(t, a, b));
const fadeOut = (t, a, b) => 1 - cubicOut(ramp(t, a, b));

async function prepareMenu() {
  const meta = await sharp(SOURCE_MENU).metadata();
  const fullWidth = Math.round(H * meta.width / meta.height);
  const left = Math.round((W - fullWidth) / 2);
  const backdrop = await sharp(SOURCE_MENU)
    .resize(W, H, { fit: "cover" })
    .blur(26)
    .modulate({ brightness: .67, saturation: .92 })
    .toBuffer();
  const clean = await sharp(SOURCE_MENU)
    .resize(fullWidth, H, { fit: "fill" })
    .sharpen({ sigma: .45 })
    .toBuffer();
  const output = path.join(OUT, "CardMesh_Current_Clean_1920x1080.png");
  await sharp(backdrop)
    .composite([
      { input: clean, left, top: 0 },
      {
        input: Buffer.from(`<svg width="${W}" height="${H}">
          <defs><linearGradient id="edge"><stop stop-color="#04120d" stop-opacity=".28"/><stop offset=".5" stop-color="#04120d" stop-opacity="0"/><stop offset="1" stop-color="#04120d" stop-opacity=".28"/></linearGradient></defs>
          <rect width="${W}" height="${H}" fill="url(#edge)"/>
        </svg>`)
      }
    ])
    .png()
    .toFile(output);
  return output;
}

const menuFile = await prepareMenu();
const menu64 = fs.readFileSync(menuFile).toString("base64");

function letterMarkup(fill, opacity, stroke = "none", strokeWidth = 0, extra = "") {
  const letters = [
    ["O", 610], ["N", 750], ["E", 890],
    ["T", 1080], ["A", 1220], ["P", 1360]
  ];
  return letters.map(([char, x]) =>
    `<text x="${x}" y="555" text-anchor="middle" fill="${fill}" opacity="${opacity}" stroke="${stroke}" stroke-width="${strokeWidth}" ${extra}>${char}</text>`
  ).join("");
}

function refinedSvg(t) {
  const atmosphere = smooth(t, 0, .18);
  const dotIn = smooth(t, .18, .30);
  const tapMove = cubicInOut(ramp(t, .28, .42));
  const dotOut = fadeOut(t, .49, .63);
  const dotOpacity = dotIn * dotOut;
  const dotX = 960 + Math.sin(t * 7) * .8;
  const dotY = 420 + 34 * tapMove;
  const contact = Math.max(0, 1 - Math.abs((t - .42) / .18));
  const wave = cubicOut(ramp(t, .36, .62)) * fadeOut(t, .53, .72);
  const outline = smooth(t, .43, .62) * fadeOut(t, 1.25, 1.54);
  const titleFill = smooth(t, .49, .88) * fadeOut(t, 1.25, 1.56);
  const revealWidth = 455 * cubicOut(ramp(t, .49, .86));
  const garage = smooth(t, .68, 1.02) * fadeOut(t, 1.28, 1.56);
  const fogDrift = Math.sin(t * .72) * 34;
  const depthPulse = contact * .24;
  const passP = cubicInOut(ramp(t, 1.26, 1.82));
  const passX = -220 + (W + 440) * passP;
  const passVisible = t >= 1.23 ? Math.min(smooth(t, 1.23, 1.31), fadeOut(t, 1.82, 2.04)) : 0;
  const menuReady = t >= 1.24 ? 1 : 0;
  const xTop = passX - 54;
  const xA = passX - 16;
  const xB = passX + 12;
  const xBottom = passX + 66;
  const curve = `M ${xTop} -80 C ${passX - 78} 185, ${xA + 28} 350, ${xA} 510 C ${xB - 30} 690, ${xBottom + 20} 835, ${xBottom} 1160`;
  const revealPath = `M 0 0 L ${xTop} 0 C ${passX - 78} 185, ${xA + 28} 350, ${xA} 510 C ${xB - 30} 690, ${xBottom + 20} 835, ${xBottom} 1080 L 0 1080 Z`;
  const frontL = 960 - revealWidth;
  const frontR = 960 + revealWidth;

  return `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}">
    <defs>
      <radialGradient id="base" cx="50%" cy="44%" r="82%">
        <stop offset="0" stop-color="#123a2a"/>
        <stop offset=".30" stop-color="#0a251b"/>
        <stop offset=".64" stop-color="#04130e"/>
        <stop offset="1" stop-color="#010503"/>
      </radialGradient>
      <radialGradient id="emerald" cx="50%" cy="50%" r="50%">
        <stop offset="0" stop-color="#2e8b61" stop-opacity="${.30 + depthPulse}"/>
        <stop offset=".33" stop-color="#176143" stop-opacity=".15"/>
        <stop offset="1" stop-color="#0c3828" stop-opacity="0"/>
      </radialGradient>
      <radialGradient id="tapGlow">
        <stop stop-color="#ffecb6" stop-opacity=".82"/>
        <stop offset=".20" stop-color="#e1ae59" stop-opacity=".46"/>
        <stop offset="1" stop-color="#b87522" stop-opacity="0"/>
      </radialGradient>
      <linearGradient id="goldText" x1="0" y1="0" x2="0" y2="1">
        <stop stop-color="#f5f1e8"/><stop offset=".55" stop-color="#eee9df"/><stop offset="1" stop-color="#d9d2c5"/>
      </linearGradient>
      <linearGradient id="garageGold" x1="0" y1="0" x2="1" y2="0">
        <stop stop-color="#9f7137"/><stop offset=".5" stop-color="#d1a359"/><stop offset="1" stop-color="#9f7137"/>
      </linearGradient>
      <linearGradient id="passGold" x1="0" y1="0" x2="0" y2="1">
        <stop stop-color="#77b993" stop-opacity=".08"/>
        <stop offset=".26" stop-color="#d7aa59" stop-opacity=".42"/>
        <stop offset=".50" stop-color="#fff0bd" stop-opacity=".98"/>
        <stop offset=".74" stop-color="#d7aa59" stop-opacity=".38"/>
        <stop offset="1" stop-color="#77b993" stop-opacity=".06"/>
      </linearGradient>
      <clipPath id="titleReveal"><rect x="${960 - revealWidth}" y="355" width="${revealWidth * 2}" height="260"/></clipPath>
      <clipPath id="menuReveal"><path d="${revealPath}"/></clipPath>
      <filter id="blur6"><feGaussianBlur stdDeviation="6"/></filter>
      <filter id="blur16"><feGaussianBlur stdDeviation="16"/></filter>
      <filter id="blur34"><feGaussianBlur stdDeviation="34"/></filter>
      <filter id="blur60"><feGaussianBlur stdDeviation="60"/></filter>
      <filter id="texture">
        <feTurbulence type="fractalNoise" baseFrequency=".004 .008" numOctaves="2" seed="12"/>
        <feColorMatrix values="0 0 0 0 0.08  0 0 0 0 0.24  0 0 0 0 0.17  0 0 0 .15 0"/>
      </filter>
    </defs>
    <rect width="${W}" height="${H}" fill="#010503"/>
    <g opacity="${atmosphere}">
      <rect width="${W}" height="${H}" fill="url(#base)"/>
      <ellipse cx="${960 + fogDrift}" cy="485" rx="710" ry="445" fill="url(#emerald)" filter="url(#blur34)"/>
      <ellipse cx="${630 - fogDrift * .4}" cy="400" rx="390" ry="185" fill="#1b4f39" opacity=".065" filter="url(#blur60)"/>
      <ellipse cx="${1325 + fogDrift * .25}" cy="650" rx="420" ry="210" fill="#267153" opacity=".045" filter="url(#blur60)"/>
      <rect width="${W}" height="${H}" filter="url(#texture)" opacity=".10"/>
      <path d="M 170 780 C 540 620, 1370 760, 1760 560" fill="none" stroke="#4f8b6c" stroke-width="2" opacity=".025" filter="url(#blur6)"/>
      <circle cx="${310 + t * 5}" cy="260" r="1.4" fill="#d6b373" opacity=".16"/>
      <circle cx="${1530 - t * 4}" cy="742" r="1.1" fill="#e0bf7c" opacity=".13"/>
      <circle cx="${1398 + t * 2}" cy="228" r=".8" fill="#d4b173" opacity=".10"/>
      <circle cx="${488 - t * 3}" cy="840" r=".9" fill="#c9a366" opacity=".09"/>
      <rect width="${W}" height="${H}" fill="none" stroke="#000" stroke-width="150" opacity=".36" filter="url(#blur60)"/>
    </g>

    <g opacity="${dotOpacity}">
      <ellipse cx="${dotX}" cy="${dotY}" rx="${44 + contact * 22}" ry="${38 + contact * 12}" fill="url(#tapGlow)" filter="url(#blur16)"/>
      <circle cx="${dotX}" cy="${dotY}" r="${7.2 - tapMove * 1.2}" fill="#dca750"/>
      <circle cx="${dotX - 2}" cy="${dotY - 2}" r="2.2" fill="#fff0bf" opacity=".88"/>
    </g>
    <g transform="translate(960 454)" opacity="${wave}">
      <ellipse rx="${38 + wave * 42}" ry="${16 + wave * 19}" fill="none" stroke="#e1b361" stroke-width="1.5" stroke-dasharray="96 52" transform="rotate(-7)" opacity=".68"/>
      <ellipse rx="${24 + wave * 26}" ry="${10 + wave * 12}" fill="none" stroke="#8dc4a4" stroke-width="1" stroke-dasharray="46 38" transform="rotate(11)" opacity=".42"/>
      <ellipse rx="${120 + wave * 95}" ry="${56 + wave * 42}" fill="#2d8a60" opacity="${contact * .055}" filter="url(#blur34)"/>
    </g>

    <g font-family="Avenir Next, Helvetica Neue, Arial, sans-serif" font-size="160" font-weight="600" style="font-kerning:none">
      ${letterMarkup("none", outline * .34, "#c8d8cf", 1.15)}
      <g clip-path="url(#titleReveal)">
        ${letterMarkup("url(#goldText)", titleFill)}
      </g>
    </g>
    <g opacity="${titleFill * fadeOut(t, 1.24, 1.52)}">
      <ellipse cx="${frontL}" cy="500" rx="22" ry="92" fill="#e0b25e" opacity=".10" filter="url(#blur16)"/>
      <ellipse cx="${frontR}" cy="500" rx="22" ry="92" fill="#e0b25e" opacity=".10" filter="url(#blur16)"/>
    </g>
    <g opacity="${titleFill}">
      <path d="M 1219 515 l 22 -12" stroke="#061710" stroke-width="7"/>
      <path d="M 1220 514 l 17 -9" stroke="#c89548" stroke-width="2.4"/>
    </g>
    <text x="960" y="650" text-anchor="middle" fill="url(#garageGold)" opacity="${garage}" font-family="Avenir Next, Helvetica Neue, Arial, sans-serif" font-size="48" font-weight="500" letter-spacing="16">GARAGE</text>

    <g opacity="${menuReady}">
      <image href="data:image/png;base64,${menu64}" x="0" y="0" width="${W}" height="${H}" clip-path="url(#menuReveal)"/>
      <path d="${curve}" fill="none" stroke="#e4b65e" stroke-width="118" opacity="${passVisible * .07}" filter="url(#blur34)"/>
      <path d="${curve}" fill="none" stroke="#72bb91" stroke-width="64" opacity="${passVisible * .10}" filter="url(#blur16)"/>
      <path d="${curve}" fill="none" stroke="url(#passGold)" stroke-width="25" opacity="${passVisible * .48}" filter="url(#blur6)"/>
      <path d="${curve}" fill="none" stroke="url(#passGold)" stroke-width="5.2" opacity="${passVisible}"/>
      <path d="${curve}" fill="none" stroke="#fff0bd" stroke-width="1.6" opacity="${passVisible * .96}"/>
      <ellipse cx="${passX - 2}" cy="535" rx="34" ry="116" fill="#efd084" opacity="${passVisible * .13}" filter="url(#blur16)"/>
      <ellipse cx="${passX - 4}" cy="535" rx="7" ry="56" fill="#fff2c4" opacity="${passVisible * .34}" filter="url(#blur6)"/>
    </g>
  </svg>`;
}

async function renderRefined() {
  for (let i = 0; i < COUNT; i++) {
    const t = Math.min(DURATION, i / FPS);
    const output = path.join(REFINED_FRAMES, `frame_${String(i).padStart(3, "0")}.png`);
    if (!fs.existsSync(output)) await sharp(Buffer.from(refinedSvg(t))).png().toFile(output);
  }
}

async function renderCurrent1080() {
  for (let i = 0; i < COUNT; i++) {
    const oldIndex = Math.min(47, Math.round(Math.min(i / FPS, 1.57) * 30));
    const src = path.join(CURRENT, `frame_${String(oldIndex).padStart(3, "0")}.png`);
    const output = path.join(CURRENT_FRAMES, `frame_${String(i).padStart(3, "0")}.png`);
    if (fs.existsSync(output)) continue;
    const backdrop = await sharp(src).resize(W, H, { fit: "cover" }).blur(24).modulate({ brightness: .55 }).toBuffer();
    const foreground = await sharp(src).resize({ width: 1743, height: H, fit: "fill" }).toBuffer();
    await sharp(backdrop)
      .composite([{ input: foreground, left: Math.round((W - 1743) / 2), top: 0 }])
      .png()
      .toFile(output);
  }
}

await renderRefined();
await renderCurrent1080();

const keyTimes = [
  ["01_Deep_Background", .12],
  ["02_Precise_Touch", .41],
  ["03_One_Tap_Reveal", .72],
  ["04_Brand_Hold", 1.16],
  ["05_Volumetric_Light_Pass", 1.55],
  ["06_Real_CardMesh", 2.02]
];
for (const [name, t] of keyTimes) {
  const i = Math.min(COUNT - 1, Math.round(t * FPS));
  fs.copyFileSync(
    path.join(REFINED_FRAMES, `frame_${String(i).padStart(3, "0")}.png`),
    path.join(KEYFRAMES, `${name}.png`)
  );
}

fs.writeFileSync(path.join(OUT, "manifest.json"), JSON.stringify({
  title: "ONE TAP GARAGE — Refined Prototype B",
  resolution: `${W}x${H}`,
  fps: FPS,
  durationSeconds: DURATION,
  currentMenuSource: "Artifacts/MainMenuAmbient_GameView.png",
  currentMenuPreservedWithoutCrop: true,
  unityModified: false
}, null, 2));

console.log(`Rendered ${COUNT} refined and ${COUNT} current-comparison frames.`);
