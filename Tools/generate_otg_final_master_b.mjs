import fs from "node:fs";
import path from "node:path";
import sharp from "/Users/vnm/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp/lib/index.js";

const ROOT = "/Users/vnm/Documents/Solitaire";
const SOURCE_MENU = path.join(ROOT, "Artifacts/MainMenuAmbient_GameView.png");
const OUT = path.join(ROOT, "Artifacts/OneTapGarageSplash/FinalMasterB");
const REFINED_FRAMES = path.join(OUT, "Final_Master_frames");
const W = 1920;
const H = 1080;
const FPS = 30;
const DURATION = 2.10;
const COUNT = Math.ceil(DURATION * FPS);

for (const dir of [OUT, REFINED_FRAMES]) fs.mkdirSync(dir, { recursive: true });

const clamp = (v, a = 0, b = 1) => Math.max(a, Math.min(b, v));
const ramp = (t, a, b) => clamp((t - a) / (b - a));
const cubicOut = (x) => 1 - Math.pow(1 - clamp(x), 3);
const cubicInOut = (x) => {
  x = clamp(x);
  return x < .5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
};
const cinematicPass = (x) => {
  x = clamp(x);
  return x * x * x * (x * (x * 6 - 15) + 10);
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
    ["O", 580], ["N", 735], ["E", 885],
    ["T", 1096], ["A", 1245], ["P", 1400]
  ];
  return letters.map(([char, x]) =>
    `<text x="${x}" y="555" text-anchor="middle" fill="${fill}" opacity="${opacity}" stroke="${stroke}" stroke-width="${strokeWidth}" ${extra}>${char}</text>`
  ).join("");
}

function refinedSvg(t) {
  const atmosphere = .42 + .58 * smooth(t, 0, .12);
  const dotIn = smooth(t, .12, .25);
  const tapMove = cubicInOut(ramp(t, .24, .38));
  const dotOut = fadeOut(t, .50, .64);
  const dotOpacity = dotIn * dotOut;
  const dotX = 960 + Math.sin(t * 7) * .8;
  const dotY = 414 + 40 * tapMove;
  const contact = Math.max(0, 1 - Math.abs((t - .38) / .19));
  const wave = cubicOut(ramp(t, .32, .58)) * fadeOut(t, .51, .70);
  const outline = smooth(t, .43, .61) * fadeOut(t, 1.35, 1.62);
  const titleFill = smooth(t, .50, .92) * fadeOut(t, 1.36, 1.65);
  const revealWidth = 500 * cubicOut(ramp(t, .50, .90));
  const garage = smooth(t, .66, 1.02) * fadeOut(t, 1.37, 1.66);
  const garageRevealY = 690 - 82 * garage;
  const garageRevealH = 82 * garage;
  const fogDrift = Math.sin(t * .72) * 34;
  const breath = Math.sin((t / 3.6) * Math.PI * 2);
  const depthPulse = contact * .31;
  const transitionLift = cubicOut(ramp(t, 1.30, 1.88)) * fadeOut(t, 1.93, 2.08);
  const passP = cinematicPass(ramp(t, 1.36, 1.92));
  const passX = -220 + (W + 440) * passP;
  const passVisible = t >= 1.32 ? Math.min(smooth(t, 1.32, 1.42), fadeOut(t, 1.92, 2.10)) : 0;
  const menuReady = t >= 1.32 ? 1 : 0;
  const xTop = passX - 82;
  const xA = passX + 4;
  const xB = passX - 18;
  const xBottom = passX + 92;
  const curve = `M ${xTop} -80 C ${passX - 128} 178, ${xA + 58} 334, ${xA} 510 C ${xB - 58} 702, ${xBottom + 45} 838, ${xBottom} 1160`;
  const trailCurve1 = `M ${xTop - 34} -80 C ${passX - 162} 178, ${xA + 24} 334, ${xA - 34} 510 C ${xB - 92} 702, ${xBottom + 11} 838, ${xBottom - 34} 1160`;
  const trailCurve2 = `M ${xTop - 72} -80 C ${passX - 200} 178, ${xA - 14} 334, ${xA - 72} 510 C ${xB - 130} 702, ${xBottom - 27} 838, ${xBottom - 72} 1160`;
  const revealPath = `M 0 0 L ${xTop} 0 C ${passX - 128} 178, ${xA + 58} 334, ${xA} 510 C ${xB - 58} 702, ${xBottom + 45} 838, ${xBottom} 1080 L 0 1080 Z`;
  const logoGlint = Math.exp(-Math.pow((passX - 960) / 260, 2)) * passVisible;
  const leftButtonGlint = Math.exp(-Math.pow((passX - 420) / 170, 2)) * passVisible;
  const midButtonGlint = Math.exp(-Math.pow((passX - 960) / 170, 2)) * passVisible;
  const rightButtonGlint = Math.exp(-Math.pow((passX - 1500) / 170, 2)) * passVisible;
  const menuBrightness = smooth(t, 1.38, 1.56);
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
        <stop offset="0" stop-color="#2e8b61" stop-opacity="${.30 + depthPulse + breath * .012}"/>
        <stop offset=".33" stop-color="#176143" stop-opacity=".15"/>
        <stop offset="1" stop-color="#0c3828" stop-opacity="0"/>
      </radialGradient>
      <radialGradient id="tapGlow">
        <stop stop-color="#ffecb6" stop-opacity=".82"/>
        <stop offset=".20" stop-color="#e1ae59" stop-opacity=".46"/>
        <stop offset="1" stop-color="#b87522" stop-opacity="0"/>
      </radialGradient>
      <linearGradient id="goldText" x1="0" y1="0" x2="0" y2="1">
        <stop stop-color="#f6f2ea"/>
        <stop offset=".48" stop-color="#eeeae1"/>
        <stop offset=".78" stop-color="#dce7e0"/>
        <stop offset="1" stop-color="#c8d7cf"/>
      </linearGradient>
      <linearGradient id="garageGold" x1="0" y1="0" x2="1" y2="0">
        <stop stop-color="#a56d31"/><stop offset=".5" stop-color="#dda95b"/><stop offset="1" stop-color="#a56d31"/>
      </linearGradient>
      <linearGradient id="passGold" x1="0" y1="0" x2="0" y2="1">
        <stop stop-color="#77b993" stop-opacity=".08"/>
        <stop offset=".26" stop-color="#d7aa59" stop-opacity=".42"/>
        <stop offset=".50" stop-color="#fff0bd" stop-opacity=".98"/>
        <stop offset=".74" stop-color="#d7aa59" stop-opacity=".38"/>
        <stop offset="1" stop-color="#77b993" stop-opacity=".06"/>
      </linearGradient>
      <clipPath id="titleReveal"><rect x="${960 - revealWidth}" y="355" width="${revealWidth * 2}" height="260"/></clipPath>
      <clipPath id="garageReveal"><rect x="560" y="${garageRevealY}" width="800" height="${garageRevealH}"/></clipPath>
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
      <ellipse cx="${960 + fogDrift}" cy="485" rx="${710 + breath * 5}" ry="${445 + breath * 3}" fill="url(#emerald)" filter="url(#blur34)"/>
      <ellipse cx="${630 - fogDrift * .4}" cy="400" rx="390" ry="185" fill="#1b4f39" opacity="${.065 + breath * .003}" filter="url(#blur60)"/>
      <ellipse cx="${1325 + fogDrift * .25}" cy="650" rx="420" ry="210" fill="#267153" opacity="${.045 - breath * .002}" filter="url(#blur60)"/>
      <ellipse cx="960" cy="540" rx="490" ry="215" fill="#be842d" opacity="${.018 + transitionLift * .026}" filter="url(#blur60)"/>
      <ellipse cx="960" cy="525" rx="620" ry="300" fill="#249063" opacity="${transitionLift * .075}" filter="url(#blur60)"/>
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
      <circle cx="${dotX}" cy="${dotY}" r="${9.2 - tapMove * 1.3}" fill="#dca750"/>
      <circle cx="${dotX}" cy="${dotY}" r="${15 + contact * 4}" fill="none" stroke="#deb15d" stroke-width="1" opacity="${.22 + contact * .24}"/>
      <circle cx="${dotX - 2.4}" cy="${dotY - 2.4}" r="3.0" fill="#fff2c7" opacity=".92"/>
    </g>
    <g transform="translate(960 454)" opacity="${wave}">
      <ellipse rx="${38 + wave * 42}" ry="${16 + wave * 19}" fill="none" stroke="#e1b361" stroke-width="1.5" stroke-dasharray="96 52" transform="rotate(-7)" opacity=".68"/>
      <ellipse rx="${24 + wave * 26}" ry="${10 + wave * 12}" fill="none" stroke="#8dc4a4" stroke-width="1" stroke-dasharray="46 38" transform="rotate(11)" opacity=".42"/>
      <ellipse rx="${62 + wave * 58}" ry="${27 + wave * 24}" fill="none" stroke="#cfa054" stroke-width=".8" stroke-dasharray="128 76" transform="rotate(4)" opacity=".34"/>
      <ellipse rx="${120 + wave * 95}" ry="${56 + wave * 42}" fill="#2d8a60" opacity="${contact * .055}" filter="url(#blur34)"/>
    </g>

    <g font-family="Avenir Next, Helvetica Neue, Arial, sans-serif" font-size="176" font-weight="650" style="font-kerning:none">
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
      <path d="M 924 505 l 17 -5" stroke="#061710" stroke-width="5.4"/>
      <path d="M 925 504 l 13 -3.8" stroke="#b8cfc2" stroke-width="1.15" opacity=".62"/>
      <path d="M 1243 512 l 22 -12" stroke="#061710" stroke-width="6"/>
      <path d="M 1244 511 l 17 -9" stroke="#c99b55" stroke-width="1.55" opacity=".78"/>
      <path d="M 760 433 l 86 0" stroke="#d6a454" stroke-width="7" opacity=".025" filter="url(#blur6)"/>
    </g>
    <g clip-path="url(#garageReveal)" opacity="${garage}">
      <text x="960" y="682" text-anchor="middle" fill="url(#garageGold)" font-family="Avenir Next, Helvetica Neue, Arial, sans-serif" font-size="57" font-weight="500" letter-spacing="12">GARAGE</text>
      <line x1="690" x2="1230" y1="${garageRevealY}" y2="${garageRevealY}" stroke="#d5a252" stroke-width="1.2" opacity="${(1 - garage) * .44}" filter="url(#blur6)"/>
    </g>

    <g opacity="${menuReady}">
      <image href="data:image/png;base64,${menu64}" x="0" y="0" width="${W}" height="${H}" clip-path="url(#menuReveal)" opacity="${.78 + menuBrightness * .22}"/>
      <path d="${revealPath}" fill="#062017" opacity="${(1 - menuBrightness) * .16}"/>
      <g clip-path="url(#menuReveal)">
        <ellipse cx="960" cy="535" rx="500" ry="270" fill="#efc56c" opacity="${logoGlint * .075}" filter="url(#blur34)"/>
        <ellipse cx="420" cy="930" rx="178" ry="80" fill="#ffd77b" opacity="${leftButtonGlint * .13}" filter="url(#blur16)"/>
        <ellipse cx="960" cy="930" rx="205" ry="86" fill="#ffd77b" opacity="${midButtonGlint * .15}" filter="url(#blur16)"/>
        <ellipse cx="1500" cy="930" rx="178" ry="80" fill="#ffd77b" opacity="${rightButtonGlint * .13}" filter="url(#blur16)"/>
      </g>
      <path d="${trailCurve2}" fill="none" stroke="#3fa274" stroke-width="104" opacity="${passVisible * .035}" filter="url(#blur34)"/>
      <path d="${trailCurve1}" fill="none" stroke="#c49442" stroke-width="68" opacity="${passVisible * .050}" filter="url(#blur34)"/>
      <path d="${trailCurve1}" fill="none" stroke="#5db487" stroke-width="30" opacity="${passVisible * .075}" filter="url(#blur16)"/>
      <path d="${curve}" fill="none" stroke="#e4b65e" stroke-width="138" opacity="${passVisible * .075}" filter="url(#blur34)"/>
      <path d="${curve}" fill="none" stroke="#72bb91" stroke-width="76" opacity="${passVisible * .12}" filter="url(#blur16)"/>
      <path d="${curve}" fill="none" stroke="url(#passGold)" stroke-width="31" opacity="${passVisible * .52}" filter="url(#blur6)"/>
      <path d="${curve}" fill="none" stroke="url(#passGold)" stroke-width="6.4" opacity="${passVisible}"/>
      <path d="${curve}" fill="none" stroke="#fff0bd" stroke-width="1.8" opacity="${passVisible * .98}"/>
      <ellipse cx="${passX - 2}" cy="535" rx="42" ry="142" fill="#efd084" opacity="${passVisible * .17}" filter="url(#blur16)"/>
      <ellipse cx="${passX - 4}" cy="535" rx="9" ry="72" fill="#fff2c4" opacity="${passVisible * .39}" filter="url(#blur6)"/>
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

await renderRefined();

fs.writeFileSync(path.join(OUT, "manifest.json"), JSON.stringify({
  title: "ONE TAP GARAGE — Final Master",
  resolution: `${W}x${H}`,
  fps: FPS,
  durationSeconds: DURATION,
  currentMenuSource: "Artifacts/MainMenuAmbient_GameView.png",
  currentMenuPreservedWithoutCrop: true,
  unityModified: false
}, null, 2));

console.log(`Rendered ${COUNT} final-master frames.`);
