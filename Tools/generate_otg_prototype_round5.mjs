import fs from "node:fs";
import path from "node:path";
import sharp from "/Users/vnm/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp/lib/index.js";

const ROOT = "/Users/vnm/Documents/CardMesh";
const OUT = path.join(ROOT, "Artifacts/OneTapGarageSplash/PrototypeRound5");
const MENU = path.join(ROOT, "Artifacts/MainMenuAmbient_GameView.png");
const W = 1136;
const H = 704;
const FPS = 30;
const DURATION = 1.6;
const FRAME_COUNT = Math.round(FPS * DURATION);

fs.mkdirSync(OUT, { recursive: true });
for (const variant of ["A", "B"]) {
  fs.mkdirSync(path.join(OUT, `Prototype_${variant}_frames`), { recursive: true });
}

const menuData = fs.readFileSync(MENU).toString("base64");
const clamp = (v, a = 0, b = 1) => Math.max(a, Math.min(b, v));
const easeOut = (x) => 1 - Math.pow(1 - clamp(x), 3);
const easeInOut = (x) => {
  x = clamp(x);
  return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
};
const ramp = (t, a, b) => clamp((t - a) / (b - a));
const fade = (t, a, b) => easeOut(ramp(t, a, b));

function splashSvg(t, variant) {
  const bgIn = fade(t, 0.0, 0.15);
  const dotIn = fade(t, 0.15, 0.34);
  const dotOut = 1 - fade(t, 0.46, 0.60);
  const tap = easeInOut(ramp(t, 0.34, 0.50));
  const dotOpacity = dotIn * dotOut;
  const dotY = 270 + 32 * tap;
  const dotR = 5 + 3 * dotIn - 2 * tap;
  const ring = Math.max(0, 1 - Math.abs((t - 0.47) / 0.13));
  const oneTap = fade(t, 0.45, 0.75);
  const garage = fade(t, 0.62, 0.90);
  const titleY = 346 + (1 - oneTap) * 9;
  const font = variant === "A"
    ? "Avenir Next, Helvetica Neue, Arial, sans-serif"
    : "Avenir Next Condensed, Avenir Next, Helvetica Neue, Arial, sans-serif";
  const weight = variant === "A" ? 500 : 600;
  const tracking = variant === "A" ? 5 : 3;
  const titleSize = variant === "A" ? 94 : 98;
  const fogShift = Math.sin(t * 0.9) * 18;
  const menuOpacity = variant === "A" ? easeInOut(ramp(t, 1.05, 1.55)) : 0;
  const passP = easeInOut(ramp(t, 1.04, 1.38));
  const passX = -80 + (W + 160) * passP;
  const revealW = clamp((passX + 24) / W) * W;
  const splashOpacity = variant === "B" ? 1 - easeOut(ramp(t, 1.05, 1.42)) : 1 - menuOpacity;
  const titleOpacity = Math.min(oneTap, splashOpacity);
  const garageOpacity = Math.min(garage, splashOpacity);
  const passOpacity = variant === "B" && t >= 1.02 && t <= 1.44
    ? Math.min(fade(t, 1.02, 1.10), 1 - fade(t, 1.34, 1.44))
    : 0;

  const menuLayer = variant === "A"
    ? `<image href="data:image/png;base64,${menuData}" x="0" y="0" width="${W}" height="${H}" opacity="${menuOpacity}"/>`
    : `<clipPath id="menuReveal"><rect x="0" y="0" width="${revealW}" height="${H}"/></clipPath>
       <image href="data:image/png;base64,${menuData}" x="0" y="0" width="${W}" height="${H}" clip-path="url(#menuReveal)" opacity="${easeOut(ramp(t, 1.03, 1.30))}"/>`;

  const detail = variant === "B" && titleOpacity > 0.3
    ? `<path d="M 677 ${titleY - 33} l 13 -7" stroke="#071510" stroke-width="4" opacity="${titleOpacity * 0.85}"/>`
    : "";

  return `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}">
    <defs>
      <radialGradient id="bg" cx="50%" cy="42%" r="78%">
        <stop offset="0" stop-color="#0d2a20"/>
        <stop offset=".42" stop-color="#071a14"/>
        <stop offset="1" stop-color="#020806"/>
      </radialGradient>
      <radialGradient id="fog" cx="50%" cy="50%" r="50%">
        <stop offset="0" stop-color="#2f6650" stop-opacity=".22"/>
        <stop offset="1" stop-color="#17392c" stop-opacity="0"/>
      </radialGradient>
      <radialGradient id="goldGlow" cx="50%" cy="50%" r="50%">
        <stop offset="0" stop-color="#ffe3a0" stop-opacity=".75"/>
        <stop offset=".35" stop-color="#d39a43" stop-opacity=".25"/>
        <stop offset="1" stop-color="#b47728" stop-opacity="0"/>
      </radialGradient>
      <linearGradient id="pass" x1="0" x2="1">
        <stop offset="0" stop-color="#e3aa52" stop-opacity="0"/>
        <stop offset=".42" stop-color="#efbd6e" stop-opacity=".18"/>
        <stop offset=".50" stop-color="#ffe2a0" stop-opacity=".92"/>
        <stop offset=".56" stop-color="#efbd6e" stop-opacity=".20"/>
        <stop offset="1" stop-color="#e3aa52" stop-opacity="0"/>
      </linearGradient>
      <filter id="blur8"><feGaussianBlur stdDeviation="8"/></filter>
      <filter id="blur24"><feGaussianBlur stdDeviation="24"/></filter>
      <filter id="soft"><feGaussianBlur stdDeviation="2.2"/></filter>
    </defs>
    <rect width="${W}" height="${H}" fill="#020806"/>
    <g opacity="${bgIn * splashOpacity}">
      <rect width="${W}" height="${H}" fill="url(#bg)"/>
      <ellipse cx="${568 + fogShift}" cy="315" rx="420" ry="260" fill="url(#fog)" filter="url(#blur24)"/>
      <circle cx="198" cy="174" r="1.2" fill="#c9a26a" opacity=".18"/>
      <circle cx="930" cy="518" r=".9" fill="#d9b77b" opacity=".14"/>
      <circle cx="814" cy="132" r=".7" fill="#d9b77b" opacity=".12"/>
    </g>
    <g opacity="${dotOpacity * splashOpacity}">
      <circle cx="568" cy="${dotY}" r="${34 + ring * 20}" fill="url(#goldGlow)" filter="url(#blur8)" opacity="${0.65 + ring * 0.25}"/>
      <circle cx="568" cy="${dotY}" r="${dotR}" fill="#e3ad58"/>
      <circle cx="566.5" cy="${dotY - 1.5}" r="${dotR * .34}" fill="#ffe7b0" opacity=".8"/>
      <circle cx="568" cy="${dotY}" r="${18 + ring * 15}" fill="none" stroke="#d69e4a" stroke-width="1.2" opacity="${ring * .55}"/>
    </g>
    <g text-anchor="middle">
      <text x="568" y="${titleY}" fill="#f3efe6" opacity="${titleOpacity}" font-family="${font}" font-size="${titleSize}" font-weight="${weight}" letter-spacing="${tracking}" style="font-kerning:normal">ONE TAP</text>
      ${detail}
      <text x="568" y="405" fill="#c8954b" opacity="${garageOpacity}" font-family="Avenir Next, Helvetica Neue, Arial, sans-serif" font-size="27" font-weight="500" letter-spacing="10">GARAGE</text>
    </g>
    ${menuLayer}
    <g opacity="${passOpacity}">
      <rect x="${passX - 74}" y="-50" width="148" height="${H + 100}" fill="url(#pass)" transform="rotate(-3 ${passX} ${H / 2})" filter="url(#soft)"/>
      <rect x="${passX - 1.2}" y="-50" width="2.4" height="${H + 100}" fill="#ffe6ad" transform="rotate(-3 ${passX} ${H / 2})"/>
    </g>
  </svg>`;
}

function wordmarkSvg(variant) {
  const font = variant === "A"
    ? "Avenir Next, Helvetica Neue, Arial, sans-serif"
    : "Avenir Next Condensed, Avenir Next, Helvetica Neue, Arial, sans-serif";
  const weight = variant === "A" ? 500 : 600;
  const tracking = variant === "A" ? 6 : 4;
  const detail = variant === "B"
    ? `<path d="M 846 127 l 18 -9" stroke="#000" stroke-width="6"/>`
    : "";
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1400" height="360" viewBox="0 0 1400 360">
    <text x="700" y="172" text-anchor="middle" fill="#f3efe6" font-family="${font}" font-size="132" font-weight="${weight}" letter-spacing="${tracking}">ONE TAP</text>
    ${detail}
    <text x="700" y="245" text-anchor="middle" fill="#c8954b" font-family="Avenir Next, Helvetica Neue, Arial, sans-serif" font-size="38" font-weight="500" letter-spacing="14">GARAGE</text>
  </svg>`;
}

async function renderFrames(variant) {
  for (let i = 0; i < FRAME_COUNT; i++) {
    const t = i / FPS;
    const png = path.join(OUT, `Prototype_${variant}_frames`, `frame_${String(i).padStart(3, "0")}.png`);
    await sharp(Buffer.from(splashSvg(t, variant))).png().toFile(png);
  }
  await sharp(Buffer.from(splashSvg(DURATION, variant)))
    .png()
    .toFile(path.join(OUT, `Prototype_${variant}_final.png`));
  await sharp(Buffer.from(wordmarkSvg(variant)))
    .png()
    .toFile(path.join(OUT, `OTG_Wordmark_${variant}.png`));
  fs.writeFileSync(path.join(OUT, `OTG_Wordmark_${variant}.svg`), wordmarkSvg(variant));
}

function imageHref(file) {
  return `data:image/png;base64,${fs.readFileSync(file).toString("base64")}`;
}

function boardBase(title, subtitle, body) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="2500" height="1600" viewBox="0 0 2500 1600">
    <defs>
      <linearGradient id="board" x1="0" y1="0" x2="1" y2="1">
        <stop stop-color="#06130f"/><stop offset="1" stop-color="#020806"/>
      </linearGradient>
    </defs>
    <rect width="2500" height="1600" fill="url(#board)"/>
    <text x="96" y="112" fill="#d4a35b" font-family="Avenir Next, Arial" font-size="28" letter-spacing="6">ONE TAP GARAGE / FINAL PROTOTYPE</text>
    <text x="96" y="190" fill="#f4f0e8" font-family="Avenir Next, Arial" font-size="64" font-weight="600">${title}</text>
    <text x="98" y="238" fill="#8ea398" font-family="Avenir Next, Arial" font-size="25">${subtitle}</text>
    ${body}
  </svg>`;
}

function storyboardBoard(variant) {
  const times = [0.03, 0.42, 0.65, 0.88, 1.16, 1.57];
  const labels = ["01 / START", "02 / TOUCH", "03 / ONE TAP", "04 / GARAGE", "05 / TRANSITION", "06 / REAL CARDMESH"];
  const notesA = ["Dark field", "One warm dot", "Word appears", "Lockup settles", "Soft dissolve", "Current menu"];
  const notesB = ["Dark field", "One warm dot", "Word appears", "Lockup settles", "Light pass", "Current menu"];
  const notes = variant === "A" ? notesA : notesB;
  let cards = "";
  times.forEach((t, i) => {
    const frame = Math.min(FRAME_COUNT - 1, Math.round(t * FPS));
    const file = path.join(OUT, `Prototype_${variant}_frames`, `frame_${String(frame).padStart(3, "0")}.png`);
    const col = i % 3;
    const row = Math.floor(i / 3);
    const x = 96 + col * 775;
    const y = 305 + row * 555;
    cards += `<rect x="${x}" y="${y}" width="720" height="446" rx="18" fill="#0b1b15" stroke="#17352a"/>
      <image href="${imageHref(file)}" x="${x + 10}" y="${y + 10}" width="700" height="434" preserveAspectRatio="xMidYMid slice"/>
      <text x="${x}" y="${y + 482}" fill="#d3a25a" font-family="Avenir Next, Arial" font-size="20" font-weight="600" letter-spacing="2">${labels[i]}</text>
      <text x="${x}" y="${y + 516}" fill="#9fb0a8" font-family="Avenir Next, Arial" font-size="21">${notes[i]} · ${t.toFixed(2)}s</text>`;
  });
  return boardBase(
    `${variant === "A" ? "14" : "15"} — PROTOTYPE ${variant} / ${variant === "A" ? "FIRST DOT" : "LIGHT PASS"}`,
    variant === "A" ? "Strict wordmark A · 1.60 s · soft dissolve into the real CardMesh menu" : "Game-character wordmark B · 1.60 s · causal light reveal into the real CardMesh menu",
    cards
  );
}

function transitionBoard() {
  const times = [1.02, 1.10, 1.18, 1.26, 1.34, 1.46];
  let cards = "";
  times.forEach((t, i) => {
    const frame = Math.min(FRAME_COUNT - 1, Math.round(t * FPS));
    const file = path.join(OUT, "Prototype_B_frames", `frame_${String(frame).padStart(3, "0")}.png`);
    const x = 96 + (i % 3) * 775;
    const y = 305 + Math.floor(i / 3) * 555;
    cards += `<rect x="${x}" y="${y}" width="720" height="446" rx="18" fill="#0b1b15" stroke="#244b3b"/>
      <image href="${imageHref(file)}" x="${x + 10}" y="${y + 10}" width="700" height="434" preserveAspectRatio="xMidYMid slice"/>
      <rect x="${x + 20}" y="${y + 20}" width="132" height="44" rx="22" fill="#04100c" opacity=".88"/>
      <text x="${x + 86}" y="${y + 50}" text-anchor="middle" fill="#e4b565" font-family="Avenir Next, Arial" font-size="19">${t.toFixed(2)} s</text>
      <text x="${x}" y="${y + 486}" fill="#9fb0a8" font-family="Avenir Next, Arial" font-size="21">Live-menu reveal · ${Math.round(clamp((t - 1.04) / .34) * 100)}%</text>`;
  });
  return boardBase(
    "16 — REAL CARDMESH TRANSITION",
    "Light Pass is a reveal mask, not a separate screen. The target is the current production main menu.",
    `${cards}
     <text x="96" y="1455" fill="#9fb0a8" font-family="Avenir Next, Arial" font-size="24">Implementation intent: reveal the live MainMenu canvas beneath the splash overlay; the screenshot is review-only.</text>`
  );
}

function comparisonBoard() {
  const a = path.join(OUT, "Prototype_A_frames", "frame_034.png");
  const b = path.join(OUT, "Prototype_B_frames", "frame_037.png");
  const menu = MENU;
  return boardBase(
    "17 — FINAL COMPARISON",
    "Two executions of one emotional idea. No permanent dot, no monogram, no loading metaphor.",
    `<text x="96" y="330" fill="#f1ede5" font-family="Avenir Next, Arial" font-size="38" font-weight="600">A / FIRST DOT + DISSOLVE</text>
     <image href="${imageHref(a)}" x="96" y="370" width="1080" height="669"/>
     <text x="96" y="1090" fill="#a8b7b0" font-family="Avenir Next, Arial" font-size="24">Quiet, premium, lowest implementation risk.</text>
     <text x="96" y="1132" fill="#788d83" font-family="Avenir Next, Arial" font-size="22">Weakness: the final transition is emotionally neutral.</text>
     <text x="1325" y="330" fill="#f1ede5" font-family="Avenir Next, Arial" font-size="38" font-weight="600">B / FIRST DOT + LIGHT PASS</text>
     <image href="${imageHref(b)}" x="1325" y="370" width="1080" height="669"/>
     <text x="1325" y="1090" fill="#e0b267" font-family="Avenir Next, Arial" font-size="24" font-weight="600">RECOMMENDED — the touch visibly starts the game.</text>
     <text x="1325" y="1132" fill="#8fa39a" font-family="Avenir Next, Arial" font-size="22">The pass hides the scene handoff without feeling like loading.</text>
     <line x1="96" x2="2405" y1="1215" y2="1215" stroke="#214034"/>
     <text x="96" y="1270" fill="#d0a158" font-family="Avenir Next, Arial" font-size="22" letter-spacing="3">SHARED TIMING</text>
     <text x="96" y="1320" fill="#a8b7b0" font-family="Avenir Next, Arial" font-size="22">0.00–0.15 atmosphere · 0.15–0.50 dot/touch · 0.45–0.75 ONE TAP · 0.62–0.90 GARAGE · 1.05–1.55 transition</text>
     <text x="96" y="1380" fill="#d0a158" font-family="Avenir Next, Arial" font-size="22" letter-spacing="3">FINAL TARGET</text>
     <image href="${imageHref(menu)}" x="96" y="1410" width="250" height="155"/>
     <text x="375" y="1470" fill="#a8b7b0" font-family="Avenir Next, Arial" font-size="22">Actual current CardMesh main menu · no placeholder scene</text>`
  );
}

async function writeBoard(name, svg) {
  const svgFile = path.join(OUT, `${name}.svg`);
  const pngFile = path.join(OUT, `${name}.png`);
  fs.writeFileSync(svgFile, svg);
  await sharp(Buffer.from(svg)).png().toFile(pngFile);
}

await renderFrames("A");
await renderFrames("B");
await writeBoard("14_Prototype_A_First_Dot", storyboardBoard("A"));
await writeBoard("15_Prototype_B_Light_Pass", storyboardBoard("B"));
await writeBoard("16_Real_CardMesh_Transition", transitionBoard());
await writeBoard("17_Final_Comparison", comparisonBoard());

fs.writeFileSync(
  path.join(OUT, "manifest.json"),
  JSON.stringify({
    title: "ONE TAP GARAGE — Final Splash Prototypes",
    fps: FPS,
    duration: DURATION,
    resolution: `${W}x${H}`,
    sourceMenu: "Artifacts/MainMenuAmbient_GameView.png",
    prototypes: {
      A: "First Dot + dissolve",
      B: "First Dot + Light Pass (recommended)"
    },
    unityProjectModified: false
  }, null, 2)
);

console.log(`Generated ${FRAME_COUNT * 2} motion frames and pages 14–17 in ${OUT}`);
