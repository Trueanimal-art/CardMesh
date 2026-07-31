import fs from "node:fs";
import path from "node:path";
import sharp from "/Users/vnm/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp/lib/index.js";

const ROOT = "/Users/vnm/Documents/Solitaire";
const OUT = path.join(ROOT, "Artifacts/OneTapGarageSplash/LogoLockupRound");
const EXPORTS = path.join(OUT, "Exports");
const BOARDS = path.join(OUT, "Boards");
const SPLASH = path.join(OUT, "SplashMocks");
const MOTION = path.join(OUT, "MotionTests");
const SOURCE_FRAME = path.join(
  ROOT,
  "Artifacts/OneTapGarageSplash/FinalMasterB/Final_Master_frames/frame_004.png",
);

for (const dir of [OUT, EXPORTS, BOARDS, SPLASH, MOTION]) {
  fs.mkdirSync(dir, { recursive: true });
}

const COLORS = {
  bg0: "#06100f",
  bg1: "#0a1b18",
  bg2: "#102823",
  ivory: "#f4eddc",
  ivoryHi: "#fffaf0",
  ivoryLo: "#cdd8cd",
  champagne: "#d5b879",
  gold: "#f2c867",
  goldHi: "#ffe2a0",
  emerald: "#60d4ae",
  muted: "#80978f",
};

const variants = [
  {
    id: "V1",
    slug: "TAP_CUT",
    title: "TAP CUT",
    idea: "Касание прорезает A и запускает сборку вправо; нижняя опора замыкает GARAGE.",
    strength: "Самый компактный и уверенный силуэт; хорошо держится в малом размере.",
    risk: "Диагональный срез нужно точно дозировать, чтобы A не выглядела повреждённой.",
  },
  {
    id: "V2",
    slug: "IMPULSE_LINE",
    title: "IMPULSE LINE",
    idea: "Одна импульсная линия проходит через wordmark и физически связывает его с GARAGE.",
    strength: "Очень ясная механика «одно касание запускает действие» и сильное движение слева направо.",
    risk: "При слишком холодной подаче линия может приблизить образ к tech-бренду.",
  },
  {
    id: "V3",
    slug: "TOUCH_FRAME",
    title: "TOUCH FRAME",
    idea: "Точка замыкает открытый контур; нижний сегмент одновременно становится основанием GARAGE.",
    strength: "Самый цельный lockup и запоминаемый внешний силуэт без тяжёлой рамки.",
    risk: "В 64 px потребуется укороченная толщина контура и строгий контроль воздуха.",
  },
  {
    id: "V4",
    slug: "CUSTOM_LETTER",
    title: "CUSTOM LETTER",
    idea: "Разомкнутая O становится местом контакта; от неё собирается весь знак.",
    strength: "Самая собственная буква и хороший потенциал узнаваемости без отдельной иконки.",
    risk: "Важно сохранить O именно буквой, а не превратить её в самостоятельный знак.",
  },
];

const xml = (s) =>
  String(s)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");

function glyph(char, x, y, scale = 1, fill = "#fff") {
  const t = `transform="translate(${x} ${y}) scale(${scale})" fill="${fill}"`;
  const shapes = {
    O: `<path d="M18 0H82Q100 0 100 18V122Q100 140 82 140H18Q0 140 0 122V18Q0 0 18 0ZM25 24V116H75V24Z" fill-rule="evenodd"/>`,
    N: `<path d="M0 0H25V140H0ZM75 0H100V140H75ZM20 0H47L80 140H53Z"/>`,
    E: `<path d="M0 0H100V24H0ZM0 58H78V82H0ZM0 116H100V140H0ZM0 0H25V140H0Z"/>`,
    T: `<path d="M0 0H100V24H0ZM37 0H63V140H37Z"/>`,
    A: `<path d="M50 0L100 140H73L63 108H37L27 140H0L50 0ZM44 84H56L50 61Z" fill-rule="evenodd"/>`,
    P: `<path d="M0 0H25V140H0ZM20 0H78Q100 0 100 22V68Q100 90 78 90H20V66H75V24H20Z"/>`,
    G: `<path d="M18 0H88V24H25V116H75V88H55V65H100V122Q100 140 82 140H18Q0 140 0 122V18Q0 0 18 0Z"/>`,
    R: `<path d="M0 0H25V140H0ZM20 0H78Q100 0 100 22V63Q100 82 81 86L104 140H75L52 88H20V64H75V24H20Z"/>`,
  };
  return `<g ${t}>${shapes[char]}</g>`;
}

function wordMarkup(word, x, y, scale, fill, tracking = 14) {
  let cursor = x;
  let body = "";
  for (const char of word) {
    if (char === " ") {
      cursor += 58 * scale;
      continue;
    }
    body += glyph(char, cursor, y, scale, fill);
    cursor += (100 + tracking) * scale;
  }
  return { body, width: cursor - x - tracking * scale };
}

function defs() {
  return `
  <defs>
    <linearGradient id="ivory" x1="0" y1="0" x2="0.82" y2="1">
      <stop offset="0" stop-color="${COLORS.ivoryHi}"/>
      <stop offset=".54" stop-color="${COLORS.ivory}"/>
      <stop offset="1" stop-color="${COLORS.ivoryLo}"/>
    </linearGradient>
    <linearGradient id="gold" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="${COLORS.goldHi}"/>
      <stop offset=".45" stop-color="${COLORS.gold}"/>
      <stop offset="1" stop-color="${COLORS.champagne}"/>
    </linearGradient>
    <linearGradient id="emeraldReflect" x1="0" y1="1" x2="0" y2="0">
      <stop offset="0" stop-color="${COLORS.emerald}" stop-opacity=".26"/>
      <stop offset=".35" stop-color="${COLORS.emerald}" stop-opacity="0"/>
    </linearGradient>
    <filter id="softGlow" x="-40%" y="-40%" width="180%" height="180%">
      <feGaussianBlur stdDeviation="7"/>
    </filter>
  </defs>`;
}

function baseMain(variant, mono) {
  const x = 194;
  const y = 154;
  const scale = 1.55;
  const one = wordMarkup("ONE", x, y, scale, "#fff", 12);
  const tapX = x + one.width + 93;
  const tap = wordMarkup("TAP", tapX, y, scale, "#fff", 12);
  let cuts = "";
  if (variant === 1) {
    cuts = `
      <path d="M990 198l22-16 55 180-22 16z" fill="#000"/>
      <path d="M1214 290l21-15 35 112-21 15z" fill="#000"/>`;
  } else if (variant === 2) {
    cuts = `<rect x="500" y="257" width="870" height="15" rx="7.5" fill="#000"/>`;
  } else if (variant === 4) {
    cuts = `<path d="M310 145h50l-31 54h-34z" fill="#000"/>`;
  }
  const fill = mono ? "#ffffff" : "url(#ivory)";
  return `
    <mask id="mainMask">
      <rect width="1600" height="640" fill="#000"/>
      ${one.body}${tap.body}${cuts}
    </mask>
    <rect width="1600" height="640" fill="${fill}" mask="url(#mainMask)"/>
    ${
      mono
        ? ""
        : `<rect x="160" y="300" width="1280" height="105" fill="url(#emeraldReflect)" mask="url(#mainMask)" opacity=".78"/>`
    }`;
}

function garageMarkup(variant, mono) {
  const color = mono ? "#fff" : "url(#gold)";
  if (variant === 2) {
    const g = wordMarkup("GARAGE", 1004, 394, 0.44, "#fff", 12);
    return `
      <mask id="garageMask"><rect width="1600" height="640" fill="#000"/>${g.body}</mask>
      <rect width="1600" height="640" fill="${color}" mask="url(#garageMask)"/>`;
  }
  if (variant === 3) {
    const g = wordMarkup("GARAGE", 592, 431, 0.53, "#fff", 12);
    return `
      <mask id="garageMask"><rect width="1600" height="640" fill="#000"/>${g.body}</mask>
      <rect width="1600" height="640" fill="${color}" mask="url(#garageMask)"/>`;
  }
  if (variant === 4) {
    const g = wordMarkup("GARAGE", 830, 422, 0.50, "#fff", 12);
    return `
      <mask id="garageMask"><rect width="1600" height="640" fill="#000"/>${g.body}</mask>
      <rect width="1600" height="640" fill="${color}" mask="url(#garageMask)"/>`;
  }
  const g = wordMarkup("GARAGE", 586, 426, 0.54, "#fff", 12);
  return `
    <mask id="garageMask"><rect width="1600" height="640" fill="#000"/>${g.body}</mask>
    <rect width="1600" height="640" fill="${color}" mask="url(#garageMask)"/>`;
}

function formMarkup(variant, mono) {
  const c = mono ? "#fff" : COLORS.champagne;
  if (variant === 1) {
    return `<path d="M298 468H548M1044 468h258" fill="none" stroke="${c}" stroke-width="8" stroke-linecap="square"/>`;
  }
  if (variant === 2) {
    return `<path d="M198 454H955" fill="none" stroke="${c}" stroke-width="7"/><path d="M955 454h26" stroke="${c}" stroke-width="7"/>`;
  }
  if (variant === 3) {
    return `
      <path d="M260 124H1266M1340 190v254M1294 496H334M260 444V190" fill="none" stroke="${c}" stroke-width="7"/>
      <path d="M260 124v42M1340 444v52" fill="none" stroke="${c}" stroke-width="7"/>`;
  }
  return `
    <path d="M706 493H1370" fill="none" stroke="${c}" stroke-width="8"/>
    <path d="M690 493h-68" fill="none" stroke="${c}" stroke-width="8"/>`;
}

function impulseMarkup(variant, mono) {
  const c = mono ? "#fff" : COLORS.gold;
  const hi = mono ? "#fff" : COLORS.goldHi;
  if (variant === 1) {
    return `
      <path d="M1003 196l38 124" stroke="${c}" stroke-width="10" stroke-linecap="round"/>
      <circle cx="1003" cy="196" r="17" fill="${hi}"/>
      <circle cx="1003" cy="196" r="39" fill="${c}" opacity=".16" filter="url(#softGlow)"/>`;
  }
  if (variant === 2) {
    return `
      <path d="M385 264H1396" stroke="${c}" stroke-width="8" stroke-linecap="round"/>
      <path d="M391 264l38-24 38 24-38 24z" fill="${hi}"/>
      <circle cx="1396" cy="264" r="15" fill="${hi}"/>`;
  }
  if (variant === 3) {
    return `
      <path d="M1266 124h74v66" fill="none" stroke="${c}" stroke-width="7"/>
      <circle cx="1304" cy="124" r="17" fill="${hi}"/>
      <circle cx="1304" cy="124" r="42" fill="${c}" opacity=".13" filter="url(#softGlow)"/>`;
  }
  return `
    <path d="M302 146l42 46" stroke="${c}" stroke-width="9" stroke-linecap="round"/>
    <circle cx="321" cy="167" r="18" fill="${hi}"/>
    <path d="M346 193h42" stroke="${c}" stroke-width="7"/>`;
}

function logoSvg(
  variant,
  {
    mono = false,
    showMain = true,
    showGarage = true,
    showForm = true,
    showImpulse = true,
    background = false,
    stage = 4,
  } = {},
) {
  const bg = background
    ? `<rect width="1600" height="640" fill="${COLORS.bg0}"/><ellipse cx="800" cy="510" rx="760" ry="300" fill="${COLORS.emerald}" opacity=".035"/>`
    : "";
  const mainOpacity = stage >= 2 ? 1 : 0;
  const garageOpacity = stage >= 3 ? 1 : 0;
  const formOpacity = stage >= 4 ? 1 : 0;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="640" viewBox="0 0 1600 640">
    ${defs()}${bg}
    <g opacity="${showMain ? mainOpacity : 0}">${baseMain(variant, mono)}</g>
    <g opacity="${showGarage ? garageOpacity : 0}">${garageMarkup(variant, mono)}</g>
    <g opacity="${showForm ? formOpacity : 0}">${formMarkup(variant, mono)}</g>
    <g opacity="${showImpulse ? 1 : 0}">${impulseMarkup(variant, mono)}</g>
  </svg>`;
}

function dataUri(svg) {
  return `data:image/svg+xml;base64,${Buffer.from(svg).toString("base64")}`;
}

async function writeSvgAndPng(svg, basePath, widths = [2048, 1024]) {
  fs.writeFileSync(`${basePath}.svg`, svg);
  for (const width of widths) {
    await sharp(Buffer.from(svg))
      .resize({ width, withoutEnlargement: false })
      .png()
      .toFile(`${basePath}_${width}.png`);
  }
}

async function exportVariants() {
  const componentFlags = {
    Full: {},
    OneTap: { showGarage: false, showForm: false, showImpulse: false },
    Garage: { showMain: false, showForm: false, showImpulse: false },
    Form: { showMain: false, showGarage: false, showImpulse: false },
    Impulse: { showMain: false, showGarage: false, showForm: false },
  };
  for (let i = 0; i < variants.length; i++) {
    const v = variants[i];
    const dir = path.join(EXPORTS, `${v.id}_${v.slug}`);
    fs.mkdirSync(dir, { recursive: true });
    for (const [component, flags] of Object.entries(componentFlags)) {
      await writeSvgAndPng(
        logoSvg(i + 1, flags),
        path.join(dir, `${v.id}_${v.slug}_${component}`),
      );
    }
    fs.writeFileSync(
      path.join(dir, `${v.id}_${v.slug}_Full_Monochrome.svg`),
      logoSvg(i + 1, { mono: true }),
    );
    for (const width of [64, 128, 256]) {
      await sharp(Buffer.from(logoSvg(i + 1)))
        .resize({ width })
        .png()
        .toFile(path.join(dir, `${v.id}_${v.slug}_Scale_${width}.png`));
    }
  }
}

function boardShell(title, subtitle, content, height = 2000) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="3200" height="${height}" viewBox="0 0 3200 ${height}">
    <defs>
      <radialGradient id="boardBg" cx=".5" cy=".15" r=".9">
        <stop offset="0" stop-color="#102722"/>
        <stop offset=".52" stop-color="#071210"/>
        <stop offset="1" stop-color="#040908"/>
      </radialGradient>
      <filter id="shadow"><feDropShadow dx="0" dy="18" stdDeviation="28" flood-color="#000" flood-opacity=".45"/></filter>
    </defs>
    <rect width="3200" height="${height}" fill="url(#boardBg)"/>
    <text x="140" y="128" fill="${COLORS.gold}" font-family="Arial, sans-serif" font-size="28" letter-spacing="8">ONE TAP GARAGE · LOGO LOCKUP ROUND</text>
    <text x="140" y="230" fill="${COLORS.ivory}" font-family="Arial, sans-serif" font-size="76" font-weight="700">${xml(title)}</text>
    <text x="140" y="292" fill="${COLORS.muted}" font-family="Arial, sans-serif" font-size="30">${xml(subtitle)}</text>
    ${content}
  </svg>`;
}

function label(x, y, text, size = 28, color = COLORS.ivory) {
  return `<text x="${x}" y="${y}" fill="${color}" font-family="Arial, sans-serif" font-size="${size}" font-weight="600" letter-spacing="2">${xml(text)}</text>`;
}

function board18() {
  let content = "";
  variants.forEach((v, i) => {
    const col = i % 2;
    const row = Math.floor(i / 2);
    const x = 140 + col * 1530;
    const y = 380 + row * 770;
    const svg = logoSvg(i + 1, { background: true });
    const mono = logoSvg(i + 1, { mono: true });
    content += `
      <g filter="url(#shadow)">
        <rect x="${x}" y="${y}" width="1390" height="650" rx="34" fill="#071310" stroke="#8ed8c0" stroke-opacity=".12"/>
        ${label(x + 54, y + 64, `${v.id} — ${v.title}`, 34, COLORS.gold)}
        <image x="${x + 38}" y="${y + 86}" width="1314" height="526" href="${dataUri(svg)}"/>
        <rect x="${x + 1010}" y="${y + 508}" width="320" height="96" rx="18" fill="#020605" stroke="#fff" stroke-opacity=".12"/>
        <image x="${x + 1020}" y="${y + 518}" width="300" height="76" href="${dataUri(mono)}"/>
      </g>`;
  });
  return boardShell(
    "18 — Logo Lockup Exploration",
    "Четыре разные конструкции. Ни одна не является финальным выбором.",
    content,
  );
}

function board19() {
  let content = "";
  variants.forEach((v, i) => {
    const y = 390 + i * 380;
    const dir = path.join(EXPORTS, `${v.id}_${v.slug}`);
    const sizes = [64, 128, 256];
    content += `
      <rect x="140" y="${y}" width="2920" height="310" rx="30" fill="#081512" stroke="#fff" stroke-opacity=".08"/>
      ${label(200, y + 65, `${v.id} — ${v.title}`, 32, COLORS.gold)}
      <image x="530" y="${y + 62}" width="900" height="220" href="${dataUri(logoSvg(i + 1, { mono: true }))}"/>`;
    sizes.forEach((s, j) => {
      const p = path.join(dir, `${v.id}_${v.slug}_Scale_${s}.png`);
      const bytes = fs.readFileSync(p).toString("base64");
      const shown = s === 64 ? 128 : s;
      const x = 1660 + j * 410;
      content += `
        ${label(x, y + 58, `${s} px`, 24, COLORS.muted)}
        <rect x="${x}" y="${y + 82}" width="340" height="190" rx="20" fill="#030706" stroke="#fff" stroke-opacity=".08"/>
        <image x="${x + (340 - shown) / 2}" y="${y + 118}" width="${shown}" height="${shown * 0.4}" href="data:image/png;base64,${bytes}"/>`;
    });
  });
  return boardShell(
    "19 — Logo Scale Tests",
    "Монохром и реальный raster check 64 / 128 / 256 px.",
    content,
  );
}

async function createSplashMocks() {
  const bg = await sharp(SOURCE_FRAME)
    .resize(1920, 1080)
    .modulate({ brightness: 0.88, saturation: 0.92 })
    .toBuffer();
  for (let i = 0; i < variants.length; i++) {
    const overlay = await sharp(Buffer.from(logoSvg(i + 1)))
      .resize({ width: 1040 })
      .png()
      .toBuffer();
    await sharp(bg)
      .composite([{ input: overlay, gravity: "centre", top: 278, left: 440 }])
      .png()
      .toFile(path.join(SPLASH, `${variants[i].id}_${variants[i].slug}_In_Splash.png`));
  }
}

function board20() {
  let content = "";
  variants.forEach((v, i) => {
    const x = 140 + (i % 2) * 1530;
    const y = 390 + Math.floor(i / 2) * 720;
    const p = path.join(SPLASH, `${v.id}_${v.slug}_In_Splash.png`);
    const bytes = fs.readFileSync(p).toString("base64");
    content += `
      <rect x="${x}" y="${y}" width="1390" height="642" rx="30" fill="#050b09" stroke="#fff" stroke-opacity=".08" filter="url(#shadow)"/>
      ${label(x + 44, y + 58, `${v.id} — ${v.title}`, 30, COLORS.gold)}
      <image x="${x + 40}" y="${y + 88}" width="1310" height="737" preserveAspectRatio="xMidYMid slice" href="data:image/png;base64,${bytes}" clip-path="inset(0 0 183 0 round 18px)"/>`;
  });
  return boardShell(
    "20 — Logo In Splash",
    "Текущая сцена и атмосфера сохранены; заменён только центральный lockup.",
    content,
  );
}

function board21() {
  let content = "";
  const phaseNames = ["TOUCH", "WORDMARK", "GARAGE", "LOCK"];
  variants.forEach((v, i) => {
    const y = 385 + i * 390;
    content += `
      ${label(150, y + 44, `${v.id} — ${v.title}`, 31, COLORS.gold)}
      <text x="150" y="${y + 84}" fill="${COLORS.muted}" font-family="Arial, sans-serif" font-size="23">${xml(v.idea)}</text>`;
    for (let stage = 1; stage <= 4; stage++) {
      const x = 150 + (stage - 1) * 750;
      content += `
        <rect x="${x}" y="${y + 112}" width="680" height="240" rx="24" fill="#06100e" stroke="#fff" stroke-opacity=".08"/>
        ${label(x + 24, y + 146, `0${stage}  ${phaseNames[stage - 1]}`, 19, COLORS.muted)}
        <image x="${x + 20}" y="${y + 146}" width="640" height="256" href="${dataUri(logoSvg(i + 1, { stage }))}"/>`;
    }
  });
  return boardShell(
    "21 — Motion Tests",
    "Ключевые стадии сборки внутри принятой структуры: точка → logo lockup → Light Pass.",
    content,
  );
}

function board22() {
  return boardShell(
    "22 — Final Selection",
    "Страница намеренно оставлена пустой до выбора пользователя.",
    `<g transform="translate(1600 1080)">
       <circle r="7" fill="${COLORS.gold}"/>
       <text y="86" text-anchor="middle" fill="${COLORS.muted}" font-family="Arial, sans-serif" font-size="30" letter-spacing="8">AWAITING SELECTION</text>
     </g>`,
  );
}

async function saveBoard(name, svg) {
  fs.writeFileSync(path.join(BOARDS, `${name}.svg`), svg);
  await sharp(Buffer.from(svg)).png().toFile(path.join(BOARDS, `${name}.png`));
}

async function writeMotionTests() {
  for (let i = 0; i < variants.length; i++) {
    for (let stage = 1; stage <= 4; stage++) {
      const svg = logoSvg(i + 1, { background: true, stage });
      fs.writeFileSync(
        path.join(MOTION, `${variants[i].id}_${variants[i].slug}_Stage_${stage}.svg`),
        svg,
      );
      await sharp(Buffer.from(svg))
        .resize({ width: 1280 })
        .png()
        .toFile(path.join(MOTION, `${variants[i].id}_${variants[i].slug}_Stage_${stage}.png`));
    }
  }
}

function report() {
  const rows = variants
    .map(
      (v) =>
        `| ${v.id} — ${v.title} | ${v.strength} | ${v.risk} | ${v.idea} |`,
    )
    .join("\n");
  return `# ONE TAP GARAGE — Logo Lockup Exploration

Статус: исследовательский раунд. Финальный вариант не выбран. Текущая структура splash, Light Pass, меню, sound structure и Unity не изменялись.

## Что подготовлено

- четыре самостоятельных векторных lockup-конструкции;
- для каждого: цвет, монохром, 64/128/256 px, прозрачный фон и кадр в текущем splash;
- раздельные SVG/PNG-экспорты: Full, OneTap, Garage, Form, Impulse;
- ключевые стадии сборки без создания нового Final Master.

## Сравнение

| Вариант | Плюсы | Риски | Motion-идея |
|---|---|---|---|
${rows}

## Рекомендованный shortlist

1. **V1 — TAP CUT**: лучший баланс мобильной читаемости, игровой дерзости и компактного силуэта.
2. **V3 — TOUCH FRAME**: наиболее цельный брендовый объект; GARAGE сильнее всего встроен в общую конструкцию.

Это shortlist, а не финальный выбор. V2 и V4 сохраняются как полноценные кандидаты и не исключаются до просмотра всех тестов.

## Интеграция

Принятая структура остаётся: фон → точка → касание → сборка полного lockup → фиксация → Light Pass → текущее меню CardMesh. Логотип собирается как единый объект, а не как два независимых текстовых слоя. Целевой общий хронометраж остаётся около 2.1 секунды и не должен превышать 2.3 секунды.

## Файлы

- \`Boards/18_Logo_Lockup_Exploration.svg|png\`
- \`Boards/19_Logo_Scale_Tests.svg|png\`
- \`Boards/20_Logo_In_Splash.svg|png\`
- \`Boards/21_Motion_Tests.svg|png\`
- \`Boards/22_Final_Selection.svg|png\` — пустая до выбора
- \`Exports/<variant>/\` — полный production export-пакет
- \`SplashMocks/\` — четыре интеграционных кадра
- \`MotionTests/\` — 16 ключевых кадров
`;
}

await exportVariants();
await createSplashMocks();
await writeMotionTests();
await saveBoard("18_Logo_Lockup_Exploration", board18());
await saveBoard("19_Logo_Scale_Tests", board19());
await saveBoard("20_Logo_In_Splash", board20());
await saveBoard("21_Motion_Tests", board21());
await saveBoard("22_Final_Selection", board22());
fs.writeFileSync(path.join(OUT, "Logo_Lockup_Report.md"), report());
fs.writeFileSync(
  path.join(OUT, "manifest.json"),
  JSON.stringify(
    {
      status: "exploration",
      finalSelection: null,
      unityModified: false,
      finalMasterCreated: false,
      variants,
      figmaPages: [
        "18 — Logo Lockup Exploration",
        "19 — Logo Scale Tests",
        "20 — Logo In Splash",
        "21 — Motion Tests",
        "22 — Final Selection",
      ],
    },
    null,
    2,
  ),
);

console.log(JSON.stringify({ out: OUT, variants: variants.length }, null, 2));
