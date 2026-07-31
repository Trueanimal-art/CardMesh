import fs from "node:fs";
import path from "node:path";
import sharp from "/Users/vnm/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp/lib/index.js";

const ROOT = "/Users/vnm/Documents/CardMesh";
const OUT = path.join(ROOT, "Artifacts/OneTapGarageSplash/FinalLogoMaster");
const ASSETS = path.join(OUT, "Assets");
const ELEMENTS = path.join(ASSETS, "AnimationElements");
const FRAMES = path.join(OUT, "Final_Logo_Master_frames");
const MENU = path.join(
  ROOT,
  "Artifacts/OneTapGarageSplash/FinalMasterB/CardMesh_Current_Clean_1920x1080.png",
);
const W = 1920;
const H = 1080;
const FPS = 30;
const DURATION = 2.1;
const COUNT = Math.ceil(DURATION * FPS);
const LOGO_W = 1400;
const LOGO_H = 430;
const LOGO_X = 330;
const LOGO_Y = 350;
const LOGO_SCALE = 0.9;
const CONTACT_LOCAL = { x: 1044, y: 117 };
const CONTACT = {
  x: LOGO_X + CONTACT_LOCAL.x * LOGO_SCALE,
  y: LOGO_Y + CONTACT_LOCAL.y * LOGO_SCALE,
};

fs.mkdirSync(FRAMES, { recursive: true });

const clamp = (v, a = 0, b = 1) => Math.max(a, Math.min(b, v));
const ramp = (t, a, b) => clamp((t - a) / (b - a));
const cubicOut = (x) => 1 - Math.pow(1 - clamp(x), 3);
const cubicInOut = (x) => {
  x = clamp(x);
  return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
};
const smooth = (t, a, b) => cubicOut(ramp(t, a, b));
const fadeOut = (t, a, b) => 1 - cubicOut(ramp(t, a, b));
const cinematicPass = (x) => {
  x = clamp(x);
  return x * x * x * (x * (x * 6 - 15) + 10);
};

const dataUri = (file) =>
  `data:image/svg+xml;base64,${fs.readFileSync(file).toString("base64")}`;
const logoFull = dataUri(path.join(ASSETS, "Final_FullLogo.svg"));
const logoA = dataUri(path.join(ELEMENTS, "Final_A.svg"));
const logoLeft = dataUri(path.join(ELEMENTS, "Final_LeftLetters.svg"));
const logoRight = dataUri(path.join(ELEMENTS, "Final_RightLetters.svg"));
const logoLines = dataUri(path.join(ELEMENTS, "Final_Lines.svg"));
const logoGarage = dataUri(path.join(ELEMENTS, "Final_Garage_Animation.svg"));
const logoPoint = dataUri(path.join(ELEMENTS, "Final_Point_Animation.svg"));
const menu64 = fs.readFileSync(MENU).toString("base64");

function masterSvg(t) {
  const atmosphere = 0.42 + 0.58 * smooth(t, 0, 0.12);
  const breath = Math.sin((t / 3.6) * Math.PI * 2);
  const fogDrift = Math.sin(t * 0.72) * 34;

  const dotIn = smooth(t, 0.15, 0.25);
  const dotMove = cubicInOut(ramp(t, 0.19, 0.34));
  const movingDotX = CONTACT.x - 10 + 10 * dotMove;
  const movingDotY = CONTACT.y - 92 + 92 * dotMove;
  const contactImpulse = Math.max(0, 1 - Math.abs((t - 0.39) / 0.11));
  const pointLock = smooth(t, 0.30, 0.42);

  const aReveal = cubicOut(ramp(t, 0.36, 0.56));
  const lettersReveal = cubicOut(ramp(t, 0.46, 0.78));
  const lineReveal = cubicOut(ramp(t, 0.68, 0.88));
  const garageReveal = cubicOut(ramp(t, 0.76, 0.96));
  const logoSettle = smooth(t, 0.92, 1.08);
  const logoOut = fadeOut(t, 1.34, 1.62);
  const logoOpacity = Math.min(1, Math.max(aReveal, lettersReveal, garageReveal)) * logoOut;

  const aRadius = 265 * aReveal;
  const leftStart = CONTACT_LOCAL.x - 890 * lettersReveal;
  const leftWidth = CONTACT_LOCAL.x - leftStart;
  const rightWidth = 360 * lettersReveal;
  const linesHalf = 390 * lineReveal;
  const garageHalf = 230 * garageReveal;

  const transitionLift =
    cubicOut(ramp(t, 1.26, 1.88)) * fadeOut(t, 1.93, 2.08);
  const passP = cinematicPass(ramp(t, 1.26, 1.88));
  const passX = -220 + (W + 440) * passP;
  const passVisible =
    t >= 1.24
      ? Math.min(smooth(t, 1.24, 1.35), fadeOut(t, 1.90, 2.10))
      : 0;
  const menuReady = t >= 1.24 ? 1 : 0;
  const menuBrightness = smooth(t, 1.32, 1.51);

  const xTop = passX - 82;
  const xA = passX + 4;
  const xB = passX - 18;
  const xBottom = passX + 92;
  const curve = `M ${xTop} -80 C ${passX - 128} 178, ${xA + 58} 334, ${xA} 510 C ${xB - 58} 702, ${xBottom + 45} 838, ${xBottom} 1160`;
  const trailCurve1 = `M ${xTop - 34} -80 C ${passX - 162} 178, ${xA + 24} 334, ${xA - 34} 510 C ${xB - 92} 702, ${xBottom + 11} 838, ${xBottom - 34} 1160`;
  const trailCurve2 = `M ${xTop - 72} -80 C ${passX - 200} 178, ${xA - 14} 334, ${xA - 72} 510 C ${xB - 130} 702, ${xBottom - 27} 838, ${xBottom - 72} 1160`;
  const revealPath = `M 0 0 L ${xTop} 0 C ${passX - 128} 178, ${xA + 58} 334, ${xA} 510 C ${xB - 58} 702, ${xBottom + 45} 838, ${xBottom} 1080 L 0 1080 Z`;
  const logoGlint =
    Math.exp(-Math.pow((passX - 960) / 260, 2)) * passVisible;
  const leftButtonGlint =
    Math.exp(-Math.pow((passX - 420) / 170, 2)) * passVisible;
  const midButtonGlint =
    Math.exp(-Math.pow((passX - 960) / 170, 2)) * passVisible;
  const rightButtonGlint =
    Math.exp(-Math.pow((passX - 1500) / 170, 2)) * passVisible;
  const launchGlow =
    Math.max(0, 1 - Math.abs((t - 1.27) / 0.10)) * logoOut;

  return `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}">
    <defs>
      <radialGradient id="base" cx="50%" cy="44%" r="82%">
        <stop offset="0" stop-color="#123a2a"/>
        <stop offset=".30" stop-color="#0a251b"/>
        <stop offset=".64" stop-color="#04130e"/>
        <stop offset="1" stop-color="#010503"/>
      </radialGradient>
      <radialGradient id="emerald" cx="50%" cy="50%" r="50%">
        <stop offset="0" stop-color="#2e8b61" stop-opacity="${0.30 + contactImpulse * 0.20 + breath * 0.012}"/>
        <stop offset=".33" stop-color="#176143" stop-opacity=".15"/>
        <stop offset="1" stop-color="#0c3828" stop-opacity="0"/>
      </radialGradient>
      <radialGradient id="tapGlow">
        <stop stop-color="#ffefc2" stop-opacity=".82"/>
        <stop offset=".22" stop-color="#e4b45f" stop-opacity=".43"/>
        <stop offset="1" stop-color="#b87522" stop-opacity="0"/>
      </radialGradient>
      <linearGradient id="passGold" x1="0" y1="0" x2="0" y2="1">
        <stop stop-color="#77b993" stop-opacity=".08"/>
        <stop offset=".26" stop-color="#d7aa59" stop-opacity=".42"/>
        <stop offset=".50" stop-color="#fff0bd" stop-opacity=".98"/>
        <stop offset=".74" stop-color="#d7aa59" stop-opacity=".38"/>
        <stop offset="1" stop-color="#77b993" stop-opacity=".06"/>
      </linearGradient>
      <clipPath id="clipA"><circle cx="${CONTACT_LOCAL.x}" cy="${CONTACT_LOCAL.y}" r="${aRadius}"/></clipPath>
      <clipPath id="clipLeft"><rect x="${leftStart}" y="0" width="${leftWidth}" height="${LOGO_H}"/></clipPath>
      <clipPath id="clipRight"><rect x="${CONTACT_LOCAL.x}" y="0" width="${rightWidth}" height="${LOGO_H}"/></clipPath>
      <clipPath id="clipLines"><rect x="${700 - linesHalf}" y="300" width="${linesHalf * 2}" height="120"/></clipPath>
      <clipPath id="clipGarage"><rect x="${700 - garageHalf}" y="290" width="${garageHalf * 2}" height="125"/></clipPath>
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
      <ellipse cx="${630 - fogDrift * 0.4}" cy="400" rx="390" ry="185" fill="#1b4f39" opacity="${0.065 + breath * 0.003}" filter="url(#blur60)"/>
      <ellipse cx="${1325 + fogDrift * 0.25}" cy="650" rx="420" ry="210" fill="#267153" opacity="${0.045 - breath * 0.002}" filter="url(#blur60)"/>
      <ellipse cx="960" cy="540" rx="490" ry="215" fill="#be842d" opacity="${0.018 + transitionLift * 0.026}" filter="url(#blur60)"/>
      <ellipse cx="960" cy="525" rx="620" ry="300" fill="#249063" opacity="${transitionLift * 0.075}" filter="url(#blur60)"/>
      <rect width="${W}" height="${H}" filter="url(#texture)" opacity=".10"/>
      <path d="M170 780C540 620 1370 760 1760 560" fill="none" stroke="#4f8b6c" stroke-width="2" opacity=".025" filter="url(#blur6)"/>
      <circle cx="${310 + t * 5}" cy="260" r="1.4" fill="#d6b373" opacity=".16"/>
      <circle cx="${1530 - t * 4}" cy="742" r="1.1" fill="#e0bf7c" opacity=".13"/>
      <circle cx="${1398 + t * 2}" cy="228" r=".8" fill="#d4b173" opacity=".10"/>
      <circle cx="${488 - t * 3}" cy="840" r=".9" fill="#c9a366" opacity=".09"/>
      <rect width="${W}" height="${H}" fill="none" stroke="#000" stroke-width="150" opacity=".36" filter="url(#blur60)"/>
    </g>

    <g opacity="${dotIn * (1 - pointLock)}">
      <ellipse cx="${movingDotX}" cy="${movingDotY}" rx="${42 + contactImpulse * 15}" ry="${36 + contactImpulse * 10}" fill="url(#tapGlow)" filter="url(#blur16)"/>
      <circle cx="${movingDotX}" cy="${movingDotY}" r="${9.5 - dotMove}" fill="#e1ad58"/>
      <circle cx="${movingDotX - 2.5}" cy="${movingDotY - 2.5}" r="3" fill="#fff2c7"/>
    </g>

    <g transform="translate(${LOGO_X} ${LOGO_Y}) scale(${LOGO_SCALE})" opacity="${logoOpacity}">
      <image href="${logoA}" width="${LOGO_W}" height="${LOGO_H}" clip-path="url(#clipA)"/>
      <image href="${logoLeft}" width="${LOGO_W}" height="${LOGO_H}" clip-path="url(#clipLeft)"/>
      <image href="${logoRight}" width="${LOGO_W}" height="${LOGO_H}" clip-path="url(#clipRight)"/>
      <image href="${logoLines}" width="${LOGO_W}" height="${LOGO_H}" clip-path="url(#clipLines)"/>
      <image href="${logoGarage}" width="${LOGO_W}" height="${LOGO_H}" clip-path="url(#clipGarage)"/>
      <image href="${logoPoint}" width="${LOGO_W}" height="${LOGO_H}" opacity="${pointLock}"/>
      <image href="${logoFull}" width="${LOGO_W}" height="${LOGO_H}" opacity="${logoSettle * 0.001}"/>
    </g>

    <g transform="translate(${CONTACT.x} ${CONTACT.y})" opacity="${contactImpulse * logoOut}">
      <ellipse rx="${31 + contactImpulse * 26}" ry="${14 + contactImpulse * 12}" fill="none" stroke="#e1b361" stroke-width="1.4" stroke-dasharray="74 38" transform="rotate(-9)"/>
      <ellipse rx="${62 + contactImpulse * 48}" ry="${29 + contactImpulse * 21}" fill="none" stroke="#78b897" stroke-width=".8" stroke-dasharray="110 62" transform="rotate(6)" opacity=".55"/>
      <ellipse rx="112" ry="58" fill="#2d8a60" opacity="${contactImpulse * 0.055}" filter="url(#blur34)"/>
    </g>
    <ellipse cx="${CONTACT.x}" cy="${CONTACT.y}" rx="80" ry="58" fill="#f1c66e" opacity="${launchGlow * 0.16}" filter="url(#blur16)"/>

    <g opacity="${menuReady}">
      <image href="data:image/png;base64,${menu64}" width="${W}" height="${H}" clip-path="url(#menuReveal)" opacity="${0.78 + menuBrightness * 0.22}"/>
      <path d="${revealPath}" fill="#062017" opacity="${(1 - menuBrightness) * 0.16}"/>
      <g clip-path="url(#menuReveal)">
        <ellipse cx="960" cy="535" rx="500" ry="270" fill="#efc56c" opacity="${logoGlint * 0.075}" filter="url(#blur34)"/>
        <ellipse cx="420" cy="930" rx="178" ry="80" fill="#ffd77b" opacity="${leftButtonGlint * 0.13}" filter="url(#blur16)"/>
        <ellipse cx="960" cy="930" rx="205" ry="86" fill="#ffd77b" opacity="${midButtonGlint * 0.15}" filter="url(#blur16)"/>
        <ellipse cx="1500" cy="930" rx="178" ry="80" fill="#ffd77b" opacity="${rightButtonGlint * 0.13}" filter="url(#blur16)"/>
      </g>
      <path d="${trailCurve2}" fill="none" stroke="#3fa274" stroke-width="104" opacity="${passVisible * 0.035}" filter="url(#blur34)"/>
      <path d="${trailCurve1}" fill="none" stroke="#c49442" stroke-width="68" opacity="${passVisible * 0.050}" filter="url(#blur34)"/>
      <path d="${trailCurve1}" fill="none" stroke="#5db487" stroke-width="30" opacity="${passVisible * 0.075}" filter="url(#blur16)"/>
      <path d="${curve}" fill="none" stroke="#e4b65e" stroke-width="138" opacity="${passVisible * 0.075}" filter="url(#blur34)"/>
      <path d="${curve}" fill="none" stroke="#72bb91" stroke-width="76" opacity="${passVisible * 0.12}" filter="url(#blur16)"/>
      <path d="${curve}" fill="none" stroke="url(#passGold)" stroke-width="31" opacity="${passVisible * 0.52}" filter="url(#blur6)"/>
      <path d="${curve}" fill="none" stroke="url(#passGold)" stroke-width="6.4" opacity="${passVisible}"/>
      <path d="${curve}" fill="none" stroke="#fff0bd" stroke-width="1.8" opacity="${passVisible * 0.98}"/>
      <ellipse cx="${passX - 2}" cy="535" rx="42" ry="142" fill="#efd084" opacity="${passVisible * 0.17}" filter="url(#blur16)"/>
      <ellipse cx="${passX - 4}" cy="535" rx="9" ry="72" fill="#fff2c4" opacity="${passVisible * 0.39}" filter="url(#blur6)"/>
    </g>
  </svg>`;
}

for (let i = 0; i < COUNT; i++) {
  const t = Math.min(DURATION, i / FPS);
  const file = path.join(FRAMES, `frame_${String(i).padStart(3, "0")}.png`);
  await sharp(Buffer.from(masterSvg(t))).png().toFile(file);
}

await sharp(path.join(FRAMES, "frame_000.png"))
  .png()
  .toFile(path.join(ASSETS, "Final_Background_1920x1080.png"));
await sharp(path.join(FRAMES, "frame_032.png"))
  .png()
  .toFile(path.join(OUT, "Final_Logo_In_Splash.png"));

const keyFrames = [0, 7, 11, 15, 22, 31, 43, 60];
const thumbW = 640;
const thumbH = 360;
const sheetW = 1280;
const sheetH = 1440;
const composites = [];
for (let i = 0; i < keyFrames.length; i++) {
  const frame = keyFrames[i];
  const input = await sharp(
    path.join(FRAMES, `frame_${String(frame).padStart(3, "0")}.png`),
  )
    .resize(thumbW, thumbH)
    .png()
    .toBuffer();
  composites.push({
    input,
    left: (i % 2) * thumbW,
    top: Math.floor(i / 2) * thumbH,
  });
}
await sharp({
  create: {
    width: sheetW,
    height: sheetH,
    channels: 4,
    background: "#020706",
  },
})
  .composite(composites)
  .png()
  .toFile(path.join(OUT, "ONE_TAP_GARAGE_Final_Logo_Contact_Sheet.png"));

fs.writeFileSync(
  path.join(OUT, "manifest.json"),
  JSON.stringify(
    {
      title: "ONE TAP GARAGE — Final Logo Master",
      resolution: `${W}x${H}`,
      fps: FPS,
      durationSeconds: DURATION,
      finalDirection: "TAP CUT",
      contactScreenPosition: CONTACT,
      soundSource:
        "Artifacts/OneTapGarageSplash/FinalPolishB/Final_Polish_B_Sound_Preview.wav",
      menuSource:
        "Artifacts/OneTapGarageSplash/FinalMasterB/CardMesh_Current_Clean_1920x1080.png",
      unityModified: false,
    },
    null,
    2,
  ),
);
console.log(JSON.stringify({ frames: COUNT, contact: CONTACT, out: OUT }, null, 2));
