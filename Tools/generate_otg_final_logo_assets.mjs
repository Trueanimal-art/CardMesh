import fs from "node:fs";
import path from "node:path";
import sharp from "/Users/vnm/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp/lib/index.js";

const ROOT = "/Users/vnm/Documents/CardMesh";
const OUT = path.join(ROOT, "Artifacts/OneTapGarageSplash/FinalLogoMaster");
const ASSETS = path.join(OUT, "Assets");
const ELEMENTS = path.join(ASSETS, "AnimationElements");
const TESTS = path.join(OUT, "StaticTests");
for (const dir of [OUT, ASSETS, ELEMENTS, TESTS]) fs.mkdirSync(dir, { recursive: true });

export const LOGO_W = 1400;
export const LOGO_H = 430;
export const MAIN_Y = 24;
export const A_X = 922;
export const CONTACT = { x: A_X + 122, y: 117 };

const C = {
  ivory0: "#fffaf0",
  ivory1: "#f2ebdc",
  ivory2: "#d6ded7",
  gold: "#d2ad69",
  point: "#ffe1a0",
  green: "#65b994",
  dark: "#06110e",
};

const esc = (s) => String(s).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;");

function defs() {
  return `<defs>
    <linearGradient id="ivory" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="${C.ivory0}"/>
      <stop offset=".62" stop-color="${C.ivory1}"/>
      <stop offset="1" stop-color="${C.ivory2}"/>
    </linearGradient>
    <linearGradient id="emeraldReflection" x1="0" y1="0" x2="0" y2="1">
      <stop offset=".54" stop-color="${C.green}" stop-opacity="0"/>
      <stop offset="1" stop-color="${C.green}" stop-opacity=".20"/>
    </linearGradient>
    <filter id="pointGlow" x="-200%" y="-200%" width="500%" height="500%">
      <feGaussianBlur stdDeviation="8"/>
    </filter>
  </defs>`;
}

function mainGlyph(char, x, y = MAIN_Y, fill = "#fff", scaleX = 1) {
  const open = `<g transform="translate(${x} ${y}) scale(${scaleX} 1)" fill="${fill}">`;
  const close = `</g>`;
  const map = {
    O: `<path d="M22 0H128Q150 0 150 22V198Q150 220 128 220H22Q0 220 0 198V22Q0 0 22 0ZM47 34Q36 34 36 45V175Q36 186 47 186H103Q114 186 114 175V45Q114 34 103 34Z" fill-rule="evenodd"/>`,
    N: `<rect x="0" width="34" height="220" rx="6"/><rect x="116" width="34" height="220" rx="6"/><path d="M23 4Q24 0 30 0H56L127 216Q126 220 120 220H94Z"/>`,
    E: `<rect width="35" height="220" rx="6"/><rect width="150" height="34" rx="6"/><rect y="93" width="126" height="34" rx="6"/><rect y="186" width="150" height="34" rx="6"/>`,
    T: `<rect width="150" height="34" rx="6"/><rect x="57" width="36" height="220" rx="6"/>`,
    A: `<path d="M66 9Q69 0 78 0Q87 0 90 9L158 220H118L103 168H52L37 220H-3L66 9ZM62 134H93L78 82Z" fill-rule="evenodd"/>`,
    P: `<path d="M0 0H35V220H0ZM29 0H121Q150 0 150 29V102Q150 131 121 131H29V97H109Q115 97 115 91V40Q115 34 109 34H29Z"/>`,
    G: `<path d="M22 0H133V34H36V186H114V139H82V106H150V198Q150 220 128 220H22Q0 220 0 198V22Q0 0 22 0Z"/>`,
    R: `<path d="M0 0H35V220H0ZM29 0H123Q150 0 150 27V96Q150 118 128 125L158 220H118L88 131H29V97H114V34H29Z"/>`,
  };
  return `${open}${map[char]}${close}`;
}

const mainPositions = [
  ["O", 142, 1.025],
  ["N", 316, 1.01],
  ["E", 488, 0.99],
  ["T", 746, 1.01],
  ["A", A_X, 1],
  ["P", 1102, 1.015],
];

function mainMask({ include = "all", withCut = true } = {}) {
  const selected = mainPositions.filter(([char, x]) => {
    if (include === "left") return x < A_X;
    if (include === "right") return x > A_X;
    if (include === "a") return char === "A";
    return true;
  });
  return `${selected.map(([char, x, scaleX]) => mainGlyph(char, x, MAIN_Y, "#fff", scaleX)).join("")}
    ${
      withCut && (include === "all" || include === "a")
        ? `<path d="M${A_X + 105} 101l31-17 9 18-31 17z" fill="#000"/>`
        : ""
    }`;
}

function mainRendered({ mono = null, include = "all", withCut = true } = {}) {
  const fill = mono || "url(#ivory)";
  return `
    <mask id="mainMask-${include}">
      <rect width="${LOGO_W}" height="${LOGO_H}" fill="#000"/>
      ${mainMask({ include, withCut })}
    </mask>
    <rect width="${LOGO_W}" height="${LOGO_H}" fill="${fill}" mask="url(#mainMask-${include})"/>
    ${
      mono
        ? ""
        : `<rect y="135" width="${LOGO_W}" height="125" fill="url(#emeraldReflection)" mask="url(#mainMask-${include})"/>`
    }`;
}

function garageGlyph(char, x, y = 316, fill = "#fff", strokeWidth = 9.5) {
  const stroke = `fill="none" stroke="${fill}" stroke-width="${strokeWidth}" stroke-linecap="round" stroke-linejoin="round"`;
  const map = {
    G: `<path d="M48 8H16Q8 8 8 16V70Q8 78 16 78H48V47H30" ${stroke}/>`,
    A: `<path d="M6 78L28 8Q30 3 34 8L56 78M16 50H46" ${stroke}/>`,
    R: `<path d="M8 78V8H42Q52 8 52 18V38Q52 48 42 48H8M35 48L57 78" ${stroke}/>`,
    E: `<path d="M52 8H8V78H52M8 42H43" ${stroke}/>`,
  };
  return `<g transform="translate(${x} ${y})">${map[char]}</g>`;
}

const garagePositions = [
  ["G", 500],
  ["A", 568],
  ["R", 636],
  ["A", 708],
  ["G", 776],
  ["E", 844],
];

function garageMarkup(color = C.gold, small = false) {
  const weight = small ? 1.18 : 1;
  const word = garagePositions
    .map(
      ([char, x]) =>
        `<g transform="translate(${(x - 700) * weight + 700} 0)">${garageGlyph(char, 0, 316, color, small ? 11 : 9.5)}</g>`,
    )
    .join("");
  const lineY = 356;
  const leftStart = small ? 328 : 333;
  const leftEnd = small ? 468 : 463;
  const rightStart = small ? 928 : 933;
  const rightEnd = small ? 1068 : 1063;
  return `
    <path d="M${leftStart} ${lineY}H${leftEnd}M${rightStart} ${lineY}H${rightEnd}" stroke="${color}" stroke-width="${small ? 6 : 3.6}" stroke-linecap="round"/>
    ${word}`;
}

function pointMarkup({ mono = null, small = false, glow = true } = {}) {
  const color = mono || C.point;
  const r = small ? 16 : 13;
  return `
    ${glow && !mono ? `<circle cx="${CONTACT.x}" cy="${CONTACT.y}" r="${r + 13}" fill="${C.point}" opacity=".17" filter="url(#pointGlow)"/>` : ""}
    <path d="M${CONTACT.x - 2} ${CONTACT.y + 1}l-12 7" stroke="${color}" stroke-width="${small ? 7 : 5}" stroke-linecap="round"/>
    <circle cx="${CONTACT.x}" cy="${CONTACT.y}" r="${r}" fill="${color}"/>
    ${mono ? "" : `<circle cx="${CONTACT.x - 3}" cy="${CONTACT.y - 3}" r="3.5" fill="#fff4ca" opacity=".92"/>`}`;
}

export function finalLogoSvg({
  mono = null,
  background = null,
  small = false,
  showMain = true,
  showGarage = true,
  showPoint = true,
  include = "all",
  viewBox = `0 0 ${LOGO_W} ${LOGO_H}`,
  width = LOGO_W,
  height = LOGO_H,
} = {}) {
  const bg = background ? `<rect width="${LOGO_W}" height="${LOGO_H}" fill="${background}"/>` : "";
  const monoGarage = mono || C.gold;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="${viewBox}">
    ${defs()}${bg}
    ${showMain ? mainRendered({ mono, include }) : ""}
    ${showGarage ? garageMarkup(monoGarage, small) : ""}
    ${showPoint ? pointMarkup({ mono, small }) : ""}
  </svg>`;
}

function animationElementSvg(body) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${LOGO_W}" height="${LOGO_H}" viewBox="0 0 ${LOGO_W} ${LOGO_H}">${defs()}${body}</svg>`;
}

async function png(svg, file, width) {
  await sharp(Buffer.from(svg)).resize({ width }).png().toFile(file);
}

async function writeAssets() {
  const full = finalLogoSvg();
  const oneTap = finalLogoSvg({
    showGarage: false,
    viewBox: "100 8 1160 245",
    width: 1160,
    height: 245,
  });
  const garage = finalLogoSvg({
    showMain: false,
    showPoint: false,
    viewBox: "300 300 800 105",
    width: 800,
    height: 105,
  });
  const point = finalLogoSvg({
    showMain: false,
    showGarage: false,
    viewBox: "975 72 95 95",
    width: 95,
    height: 95,
  });
  const small = finalLogoSvg({ small: true });
  const monoDark = finalLogoSvg({ mono: "#ffffff" });
  const monoLight = finalLogoSvg({ mono: "#101512" });
  const files = {
    "Final_FullLogo.svg": full,
    "Final_OneTap.svg": oneTap,
    "Final_Garage.svg": garage,
    "Final_TapPoint.svg": point,
    "Final_Monochrome_Dark.svg": monoDark,
    "Final_Monochrome_Light.svg": monoLight,
    "Final_SmallSize.svg": small,
  };
  for (const [name, svg] of Object.entries(files)) fs.writeFileSync(path.join(ASSETS, name), svg);
  await png(full, path.join(ASSETS, "Final_FullLogo_2048.png"), 2048);
  await png(full, path.join(ASSETS, "Final_FullLogo_1024.png"), 1024);
  await png(oneTap, path.join(ASSETS, "Final_OneTap_2048.png"), 2048);
  await png(garage, path.join(ASSETS, "Final_Garage_1024.png"), 1024);
  await png(point, path.join(ASSETS, "Final_TapPoint_512.png"), 512);
  for (const width of [512, 256, 128, 64]) {
    await png(width === 64 ? small : full, path.join(TESTS, `Final_Logo_${width}px.png`), width);
  }

  const elements = {
    "Final_A.svg": animationElementSvg(mainRendered({ include: "a" })),
    "Final_LeftLetters.svg": animationElementSvg(mainRendered({ include: "left", withCut: false })),
    "Final_RightLetters.svg": animationElementSvg(mainRendered({ include: "right", withCut: false })),
    "Final_Lines.svg": animationElementSvg(garageMarkup(C.gold).replace(/<g[\s\S]*$/m, "")),
    "Final_Garage_Animation.svg": animationElementSvg(
      garagePositions.map(([char, x]) => garageGlyph(char, x, 316, C.gold, 9.5)).join(""),
    ),
    "Final_Point_Animation.svg": animationElementSvg(pointMarkup()),
  };
  for (const [name, svg] of Object.entries(elements)) fs.writeFileSync(path.join(ELEMENTS, name), svg);
}

function imageUri(svg) {
  return `data:image/svg+xml;base64,${Buffer.from(svg).toString("base64")}`;
}

function label(x, y, text, size = 26, color = "#f2ebdc") {
  return `<text x="${x}" y="${y}" fill="${color}" font-family="Arial,sans-serif" font-size="${size}" font-weight="600">${esc(text)}</text>`;
}

function presentationBoard() {
  const dark = finalLogoSvg({ background: "#071712" });
  const black = finalLogoSvg({ background: "#000000" });
  const white = finalLogoSvg({ background: "#f4f1e8", mono: "#111712" });
  const mono = finalLogoSvg({ background: "#071712", mono: "#ffffff" });
  return `<svg xmlns="http://www.w3.org/2000/svg" width="3000" height="1900" viewBox="0 0 3000 1900">
    <defs><radialGradient id="bg"><stop stop-color="#15372c"/><stop offset=".55" stop-color="#071511"/><stop offset="1" stop-color="#020706"/></radialGradient></defs>
    <rect width="3000" height="1900" fill="url(#bg)"/>
    ${label(130, 110, "ONE TAP GARAGE · FINAL LOGO LOCKUP", 28, "#d2ad69")}
    ${label(130, 205, "Final TAP CUT", 72)}
    ${label(130, 260, "Одна активная деталь: точка касается читаемой A и создаёт короткий срез.", 27, "#80968d")}
    <rect x="130" y="340" width="2740" height="720" rx="36" fill="#06110e" stroke="#fff" stroke-opacity=".08"/>
    <image x="300" y="365" width="2400" height="737" href="${imageUri(dark)}"/>
    ${[
      ["DARK GREEN", dark],
      ["BLACK", black],
      ["WHITE", white],
      ["MONO", mono],
    ]
      .map(([name, img], i) => {
        const x = 130 + i * 690;
        return `<rect x="${x}" y="1160" width="620" height="470" rx="28" fill="#07110e" stroke="#fff" stroke-opacity=".08"/>
          ${label(x + 28, 1210, name, 22, "#d2ad69")}
          <image x="${x + 20}" y="1250" width="580" height="330" href="${imageUri(img)}"/>`;
      })
      .join("")}
    ${label(130, 1765, "FINAL DIRECTION · NO ALTERNATES", 25, "#80968d")}
  </svg>`;
}

async function scaleBoard() {
  const sizes = [512, 256, 128, 64];
  const cards = [];
  for (let i = 0; i < sizes.length; i++) {
    const w = sizes[i];
    const bytes = fs.readFileSync(path.join(TESTS, `Final_Logo_${w}px.png`)).toString("base64");
    const cardX = 120 + i * 700;
    const shown = Math.min(w, 520);
    cards.push(`
      <rect x="${cardX}" y="410" width="620" height="820" rx="34" fill="#06110e" stroke="#fff" stroke-opacity=".08"/>
      ${label(cardX + 34, 474, `${w} px`, 34, "#d2ad69")}
      <rect x="${cardX + 45}" y="535" width="530" height="560" rx="24" fill="#010302"/>
      <image x="${cardX + (620 - shown) / 2}" y="${735 - shown * .15}" width="${shown}" height="${shown * (LOGO_H / LOGO_W)}" href="data:image/png;base64,${bytes}"/>
      ${label(cardX + 34, 1175, w === 64 ? "SIMPLIFIED SMALL-SIZE" : "FULL CONSTRUCTION", 19, "#80968d")}`);
  }
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="3000" height="1500" viewBox="0 0 3000 1500">
    <rect width="3000" height="1500" fill="#03100c"/>
    ${label(120, 118, "ONE TAP GARAGE · SCALE TEST", 28, "#d2ad69")}
    ${label(120, 210, "512 / 256 / 128 / 64 px", 70)}
    ${label(120, 270, "В 64 px усилены нижний модуль и точка без изменения конструкции.", 27, "#80968d")}
    ${cards.join("")}
  </svg>`;
  fs.writeFileSync(path.join(TESTS, "Final_Scale_Test.svg"), svg);
  await sharp(Buffer.from(svg)).png().toFile(path.join(TESTS, "Final_Scale_Test.png"));
}

await writeAssets();
const presentation = presentationBoard();
fs.writeFileSync(path.join(TESTS, "Final_Logo_Presentation.svg"), presentation);
await sharp(Buffer.from(presentation)).png().toFile(path.join(TESTS, "Final_Logo_Presentation.png"));
await scaleBoard();
fs.writeFileSync(
  path.join(ASSETS, "animation_pivots.json"),
  JSON.stringify(
    {
      canvas: { width: LOGO_W, height: LOGO_H },
      fullLogoPivot: { x: LOGO_W / 2, y: LOGO_H / 2 },
      tapPointPivot: CONTACT,
      aPivot: { x: A_X + 78, y: MAIN_Y + 110 },
      garagePivot: { x: LOGO_W / 2, y: 356 },
    },
    null,
    2,
  ),
);
console.log(JSON.stringify({ out: OUT, contact: CONTACT }, null, 2));
