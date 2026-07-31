const fs = require("fs");
const path = require("path");
const sharp = require("sharp");

const out = path.resolve("Artifacts/OneTapGarageSplash/EmotionalRound4");
const imports = path.join(out, "FigmaImports");
fs.mkdirSync(imports, { recursive: true });

const C = {
  black: "#010403",
  deep: "#03120D",
  green: "#063423",
  green2: "#0A5136",
  gold: "#E7BE69",
  pale: "#FFE5A4",
  bronze: "#9A6331",
  ivory: "#F4F0E8",
  mist: "#899B92",
  red: "#F08372",
};

const concepts = [
  {
    id: "01",
    name: "THE FIRST DOT",
    emotion: "Тишина превращается в предвкушение.",
    idea: "Одна золотая точка появляется только после касания. Она не становится логотипом — её свет проявляет слова ONE TAP.",
    timing: ["0.00 · DARK", "0.32 · TOUCH", "0.58 · ONE TAP", "0.88 · GARAGE", "1.35 · CARDMESH"],
    accent: "dot",
  },
  {
    id: "02",
    name: "SOFT IGNITION",
    emotion: "Тёплый запуск вместо технической загрузки.",
    idea: "Касание рождает мягкую вспышку без линзового flare. Свет проходит сквозь буквы ONE TAP и оставляет спокойный GARAGE.",
    timing: ["0.00 · BREATH", "0.28 · GLOW", "0.52 · REVEAL", "0.92 · SETTLE", "1.45 · DISSOLVE"],
    accent: "glow",
  },
  {
    id: "03",
    name: "ONE BEAT",
    emotion: "Короткий удар перед началом игры.",
    idea: "Экран отвечает одним визуальным битом. ONE TAP появляется на первом ударе, GARAGE — тихим послезвучием.",
    timing: ["0.00 · SILENCE", "0.36 · BEAT", "0.50 · ONE TAP", "0.82 · GARAGE", "1.30 · CUT THROUGH"],
    accent: "beat",
  },
  {
    id: "04",
    name: "GOLD DUST",
    emotion: "Игра собирается прямо перед игроком.",
    idea: "Небольшое облако золотой пыли уплотняется в буквы. Никакого отдельного знака: запоминается момент рождения бренда.",
    timing: ["0.00 · STILL", "0.30 · DUST", "0.62 · ASSEMBLE", "1.00 · GARAGE", "1.55 · CARRY OVER"],
    accent: "dust",
  },
  {
    id: "05",
    name: "LIGHT PASS",
    emotion: "Одно касание открывает дорогу в игру.",
    idea: "Узкий проход света скользит по экрану и на секунду делает ONE TAP материальным. Затем свет продолжает движение уже в CardMesh.",
    timing: ["0.00 · CLOSED", "0.34 · PASS", "0.66 · ONE TAP", "1.02 · GARAGE", "1.48 · INTO GAME"],
    accent: "pass",
  },
];

function defs(p) {
  return `<defs>
    <linearGradient id="${p}-bg" x1="0" y1="0" x2="1" y2="1">
      <stop stop-color="${C.black}"/><stop offset=".52" stop-color="${C.deep}"/><stop offset="1" stop-color="#010604"/>
    </linearGradient>
    <radialGradient id="${p}-amb"><stop stop-color="${C.green2}" stop-opacity=".42"/><stop offset=".48" stop-color="${C.green}" stop-opacity=".16"/><stop offset="1" stop-color="${C.green}" stop-opacity="0"/></radialGradient>
    <radialGradient id="${p}-goldGlow"><stop stop-color="${C.pale}" stop-opacity=".8"/><stop offset=".13" stop-color="${C.gold}" stop-opacity=".42"/><stop offset="1" stop-color="${C.gold}" stop-opacity="0"/></radialGradient>
    <linearGradient id="${p}-typeGold" x1="0" y1="0" x2="1" y2="1"><stop stop-color="${C.pale}"/><stop offset=".48" stop-color="${C.gold}"/><stop offset="1" stop-color="${C.bronze}"/></linearGradient>
    <filter id="${p}-blur18"><feGaussianBlur stdDeviation="18"/></filter>
    <filter id="${p}-blur45"><feGaussianBlur stdDeviation="45"/></filter>
    <pattern id="${p}-grain" width="19" height="19" patternUnits="userSpaceOnUse"><circle cx="3" cy="4" r=".7" fill="#fff" opacity=".023"/><circle cx="14" cy="11" r=".55" fill="#fff" opacity=".018"/></pattern>
  </defs>`;
}

function atmosphere(p, w, h, x = 0, y = 0, rx = 0) {
  return `<g id="Atmosphere">
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${rx}" fill="url(#${p}-bg)"/>
    <ellipse cx="${x + w * .5}" cy="${y + h * .48}" rx="${w * .36}" ry="${h * .55}" fill="url(#${p}-amb)"/>
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${rx}" fill="url(#${p}-grain)"/>
  </g>`;
}

function brand(cx, y, scale = 1, opacity = 1, garageOpacity = 1, mode = 0) {
  const family = mode === 1 ? "Futura, Arial Black, sans-serif" : mode === 2 ? "Avenir Next, Arial, sans-serif" : "Arial Black, Arial, sans-serif";
  const spacing = mode === 1 ? 4 : mode === 2 ? 9 : 1;
  const weight = mode === 2 ? 650 : 850;
  return `<g id="ONE TAP Brand" text-anchor="middle" opacity="${opacity}">
    <text x="${cx}" y="${y}" fill="${C.ivory}" font-family="${family}" font-size="${76 * scale}" font-weight="${weight}" letter-spacing="${spacing * scale}">ONE TAP</text>
    <text x="${cx}" y="${y + 48 * scale}" fill="${C.gold}" opacity="${garageOpacity}" font-family="Avenir Next, Arial, sans-serif" font-size="${21 * scale}" font-weight="650" letter-spacing="${10 * scale}">GARAGE</text>
  </g>`;
}

function particles(cx, cy, amount = 22, spread = 160, opacity = .65) {
  let s = "";
  for (let i = 0; i < amount; i++) {
    const a = i * 2.399;
    const r = 18 + (i % 7) * spread / 7;
    const x = cx + Math.cos(a) * r;
    const y = cy + Math.sin(a) * r * .48;
    const rr = 1.2 + (i % 4) * .8;
    s += `<circle cx="${x.toFixed(1)}" cy="${y.toFixed(1)}" r="${rr}" fill="${i % 3 ? C.gold : C.pale}" opacity="${(opacity * (.35 + (i % 5) / 7)).toFixed(2)}"/>`;
  }
  return `<g id="Gold Dust">${s}</g>`;
}

function wrappedText(text, x, y, maxChars, lineHeight, attrs = "") {
  const words = text.split(/\s+/);
  const lines = [];
  let line = "";
  for (const word of words) {
    const next = line ? `${line} ${word}` : word;
    if (next.length > maxChars && line) {
      lines.push(line);
      line = word;
    } else {
      line = next;
    }
  }
  if (line) lines.push(line);
  return `<text x="${x}" y="${y}" ${attrs}>${lines.map((l, i) => `<tspan x="${x}" dy="${i ? lineHeight : 0}">${l}</tspan>`).join("")}</text>`;
}

function eventVisual(kind, p, cx, cy, phase = 1, size = 1) {
  if (kind === "dot") {
    return `<g id="Golden Contact"><circle cx="${cx}" cy="${cy}" r="${110 * size}" fill="url(#${p}-goldGlow)" opacity="${.45 * phase}"/><circle cx="${cx}" cy="${cy}" r="${7 * size}" fill="${C.pale}"/></g>`;
  }
  if (kind === "glow") {
    return `<g id="Soft Ignition"><ellipse cx="${cx}" cy="${cy}" rx="${230 * size}" ry="${120 * size}" fill="url(#${p}-goldGlow)" opacity="${.72 * phase}" filter="url(#${p}-blur18)"/><ellipse cx="${cx}" cy="${cy}" rx="${90 * size}" ry="${24 * size}" fill="${C.pale}" opacity="${.18 * phase}"/></g>`;
  }
  if (kind === "beat") {
    return `<g id="One Beat"><path d="M${cx - 190 * size} ${cy}H${cx - 35 * size}L${cx} ${cy - 32 * size}L${cx + 30 * size} ${cy + 21 * size}L${cx + 60 * size} ${cy}H${cx + 190 * size}" fill="none" stroke="${C.gold}" stroke-width="${4 * size}" stroke-linecap="round" stroke-linejoin="round" opacity="${.75 * phase}"/><circle cx="${cx}" cy="${cy - 32 * size}" r="${5 * size}" fill="${C.pale}"/></g>`;
  }
  if (kind === "dust") return particles(cx, cy, Math.round(28 * phase), 210 * size, .8);
  return `<g id="Light Pass"><rect x="${cx - 10 * size}" y="${cy - 185 * size}" width="${20 * size}" height="${370 * size}" rx="${10 * size}" fill="${C.pale}" opacity="${.54 * phase}" filter="url(#${p}-blur18)"/><rect x="${cx - 2 * size}" y="${cy - 190 * size}" width="${4 * size}" height="${380 * size}" fill="${C.pale}" opacity="${.9 * phase}"/></g>`;
}

function finalScreen(c, p, x, y, w, h, mode = 0) {
  const cx = x + w / 2, cy = y + h * .47;
  return `<g id="${c.id} ${c.name} Final Emotion">
    ${atmosphere(p, w, h, x, y, 22)}
    ${eventVisual(c.accent, p, c.accent === "pass" ? cx + w * .19 : cx, cy - 10, .7, w / 1500)}
    ${brand(cx, cy + 28, w / 1450, 1, .92, mode)}
  </g>`;
}

function frame(c, p, x, y, w, h, step) {
  const cx = x + w / 2, cy = y + h * .49;
  const showEvent = step >= 1;
  const showOne = step >= 2;
  const showGarage = step >= 3;
  const isGame = step === 4;
  return `<g id="${c.id} Frame ${step + 1}">
    ${atmosphere(p, w, h, x, y, 14)}
    ${showEvent ? eventVisual(c.accent, p, c.accent === "pass" ? x + w * (.22 + step * .17) : cx, cy, step === 1 ? .55 : .82, w / 1100) : ""}
    ${showOne && !isGame ? brand(cx, cy + 15, w / 1050, 1, showGarage ? 1 : 0, step % 3) : ""}
    ${isGame ? `<text x="${cx}" y="${cy}" text-anchor="middle" fill="${C.ivory}" font-family="Avenir Next, Arial, sans-serif" font-size="${34 * w / 500}" font-weight="700" letter-spacing="${5 * w / 500}">CARDMESH</text><text x="${cx}" y="${cy + 40 * w / 500}" text-anchor="middle" fill="${C.mist}" font-family="Arial, sans-serif" font-size="${13 * w / 500}">TRANSITION TARGET / PLACEHOLDER</text>` : ""}
    <text x="${x + 18}" y="${y + h - 16}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="13">${c.timing[step]}</text>
  </g>`;
}

function board08() {
  const p = "emotions", w = 3200, h = 2050;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.black}"/>
    <text x="60" y="78" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">08 — EMOTIONAL DIRECTIONS · NOT LOGOS</text>
    <text x="60" y="128" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">The memorable object is the first-launch moment. ONE TAP is always the hero. No mark is proposed.</text>
    ${concepts.map((c, i) => {
      const x = 60 + (i % 2) * 1570, y = 175 + Math.floor(i / 2) * 600, ww = i === 4 ? 3080 : 1510;
      return `<g id="${c.id} ${c.name}">
        <rect x="${x}" y="${y}" width="${ww}" height="545" rx="30" fill="#061912" stroke="#153C2D"/>
        ${finalScreen(c, p, x + 25, y + 25, ww * .52, 330, i % 3)}
        <text x="${x + ww * .56}" y="${y + 74}" fill="${C.gold}" font-family="Arial, sans-serif" font-size="18" font-weight="800" letter-spacing="3">${c.id} · EMOTIONAL SCENARIO</text>
        <text x="${x + ww * .56}" y="${y + 122}" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="31" font-weight="800">${c.name}</text>
        <text x="${x + ww * .56}" y="${y + 169}" fill="${C.pale}" font-family="Arial, sans-serif" font-size="20">${c.emotion}</text>
        ${wrappedText(c.idea, x + ww * .56, y + 225, i === 4 ? 105 : 52, 30, `fill="${C.mist}" font-family="Arial, sans-serif" font-size="19"`)}
        <text x="${x + 30}" y="${y + 505}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="15">No icon · no monogram · no standalone geometric mark</text>
      </g>`;
    }).join("")}
  </svg>`;
}

function board09() {
  const p = "story", w = 3400, h = 2150;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.black}"/>
    <text x="55" y="78" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">09 — STORYBOARDS · ONE TOUCH STARTS THE GAME</text>
    <text x="55" y="128" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">Five sequences, five frames each. The dot, pulse and light exist only in time.</text>
    ${concepts.map((c, row) => {
      const y = 175 + row * 385;
      return `<g id="${c.id} ${c.name} Storyboard">
        <text x="55" y="${y + 35}" fill="${C.gold}" font-family="Arial, sans-serif" font-size="21" font-weight="800">${c.id} · ${c.name}</text>
        <text x="55" y="${y + 70}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="15">${c.emotion}</text>
        ${[0, 1, 2, 3, 4].map((s) => frame(c, p, 430 + s * 585, y, 550, 310, s)).join("")}
      </g>`;
    }).join("")}
  </svg>`;
}

function board10() {
  const p = "reveal", w = 3000, h = 1750;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.black}"/>
    <text x="60" y="78" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">10 — ONE TAP · TYPE IN MOTION</text>
    <text x="60" y="128" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">Typography is revealed by light and timing; it is not paired with a logo.</text>
    ${concepts.map((c, i) => {
      const x = 60 + (i % 2) * 1450, y = 180 + Math.floor(i / 2) * 495, ww = i === 4 ? 2880 : 1390;
      return `<g id="${c.id} Type Reveal">
        <rect x="${x}" y="${y}" width="${ww}" height="440" rx="28" fill="#061912"/>
        ${eventVisual(c.accent, p, x + ww * .5, y + 205, .7, .9)}
        ${brand(x + ww * .5, y + 220, 1.25, 1, .9, i % 3)}
        <text x="${x + 30}" y="${y + 400}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="15">${c.timing.join("  →  ")}</text>
      </g>`;
    }).join("")}
  </svg>`;
}

function board11() {
  const p = "launch", w = 3800, h = 3400;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.black}"/>
    <text x="60" y="78" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">11 — FIRST-LAUNCH SCREENS · EMOTION AT REST</text>
    <text x="60" y="128" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">A still can only suggest the event. The actual identity lives in the 1.3–1.6 second sequence.</text>
    ${concepts.map((c, i) => {
      const x = 60 + (i % 2) * 1860, y = 175 + Math.floor(i / 2) * 1070, ww = i === 4 ? 3620 : 1760;
      return `<g>${finalScreen(c, p, x, y, ww, 990, i % 3)}<text x="${x}" y="${y + 1030}" fill="${C.gold}" font-family="Arial, sans-serif" font-size="22" font-weight="800">${c.id} · ${c.name} · ${c.emotion}</text></g>`;
    }).join("")}
  </svg>`;
}

function board12() {
  const p = "transition", w = 3200, h = 1900;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.black}"/>
    <text x="60" y="78" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">12 — TRANSITION TO CARDMESH</text>
    <text x="60" y="128" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">The final light behavior is carried into the game. CardMesh artwork is intentionally represented only as a destination placeholder.</text>
    ${concepts.map((c, i) => {
      const y = 180 + i * 330;
      return `<g id="${c.id} CardMesh Transition">
        <text x="60" y="${y + 45}" fill="${C.gold}" font-family="Arial, sans-serif" font-size="22" font-weight="800">${c.id} · ${c.name}</text>
        ${frame(c, p, 420, y, 760, 260, 3)}
        <path d="M1210 ${y + 130}H1510" stroke="${C.gold}" stroke-width="3" stroke-dasharray="8 12" opacity=".7"/>
        <path d="M1485 ${y + 115}L1510 ${y + 130}L1485 ${y + 145}" fill="none" stroke="${C.gold}" stroke-width="3"/>
        ${frame(c, p, 1550, y, 760, 260, 4)}
        ${wrappedText(c.idea, 2380, y + 65, 68, 28, `fill="${C.mist}" font-family="Arial, sans-serif" font-size="18"`)}
      </g>`;
    }).join("")}
  </svg>`;
}

function board13() {
  const p = "evaluation", w = 3000, h = 1600;
  const rows = [
    ["THE FIRST DOT", "9.7", "9.5", "9.4", "9.6", "Minimal, ownable first-touch ritual"],
    ["SOFT IGNITION", "9.2", "9.4", "9.6", "9.0", "Warmest and most premium"],
    ["ONE BEAT", "9.5", "9.6", "8.8", "9.5", "Strong rhythm; needs excellent sound"],
    ["GOLD DUST", "9.1", "9.2", "9.3", "8.8", "Most magical; heavier production"],
    ["LIGHT PASS", "9.0", "9.0", "9.5", "9.1", "Best bridge into CardMesh"],
  ];
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.black}"/>
    <text x="60" y="78" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">13 — EMOTIONAL EVALUATION · NO FINAL SELECTION</text>
    <text x="60" y="128" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">Evaluate the remembered feeling, not the attractiveness of a static icon.</text>
    <rect x="60" y="195" width="2880" height="1040" rx="28" fill="#061912"/>
    ${["SCENARIO", "ONE TAP IDEA", "MEMORY", "PREMIUM", "GAME FEEL", "NOTE"].map((h, i) => `<text x="${[100, 850, 1120, 1390, 1660, 1970][i]}" y="265" fill="${C.gold}" font-family="Arial, sans-serif" font-size="17" font-weight="800">${h}</text>`).join("")}
    ${rows.map((r, i) => {
      const y = 350 + i * 165;
      return `<g><path d="M95 ${y - 55}H2900" stroke="#183A2D"/><text x="100" y="${y}" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="25" font-weight="800">${r[0]}</text>${r.slice(1, 5).map((v, j) => `<text x="${[850, 1120, 1390, 1660][j]}" y="${y}" fill="${C.pale}" font-family="Arial, sans-serif" font-size="29" font-weight="800">${v}</text>`).join("")}<text x="1970" y="${y}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="19">${r[5]}</text></g>`;
    }).join("")}
    <rect x="60" y="1295" width="2880" height="220" rx="26" fill="#0A241A" stroke="${C.gold}" stroke-opacity=".45"/>
    <text x="105" y="1365" fill="${C.gold}" font-family="Arial, sans-serif" font-size="22" font-weight="800">DECISION GATE</text>
    <text x="105" y="1415" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="22">Choose 2 scenarios for animated prototypes. Only after motion testing decide whether a standalone sign is needed at all.</text>
    <text x="105" y="1462" fill="${C.mist}" font-family="Arial, sans-serif" font-size="18">No logo shortlist. No monogram research. No Unity export in this round.</text>
  </svg>`;
}

const boards = [
  ["08_Emotional_Directions", board08()],
  ["09_Storyboards", board09()],
  ["10_One_Tap_Type_In_Motion", board10()],
  ["11_First_Launch_Screens", board11()],
  ["12_Transition_To_CardMesh", board12()],
  ["13_Emotional_Evaluation", board13()],
];

async function main() {
  for (const [name, svg] of boards) {
    fs.writeFileSync(path.join(imports, `${name}.svg`), svg);
    await sharp(Buffer.from(svg)).png().toFile(path.join(out, `${name}.png`));
  }
  const report = `# ONE TAP GARAGE — Emotional Round 4

Status: exploratory first-launch direction. No logo and no final selection.

## Strategic reset

GT/OTG monograms, lettermarks, standalone abstract geometric signs and corporate symbols are no longer part of the research. The identity is designed as a remembered first-launch event. The main object is the brand **ONE TAP**.

## Five emotional scenarios

${concepts.map(c => `### ${c.id} — ${c.name}\n\n${c.emotion}\n\n${c.idea}\n\nTiming: ${c.timing.join(" → ")}.\n`).join("\n")}

## Decision gate

Select two scenarios for animated prototypes. Evaluate them with sound and the real CardMesh opening frame. Only after motion testing decide whether a standalone mark is needed at all.

## Constraints

- No OTG or GT.
- No letter monograms.
- No standalone abstract geometric icon.
- No corporate symbol.
- No Unity or CardMesh project changes.
- No final export package.
`;
  fs.writeFileSync(path.join(out, "Emotional_Report.md"), report);
  fs.writeFileSync(path.join(out, "manifest.json"), JSON.stringify({
    status: "exploratory",
    finalSelected: false,
    logoProposed: false,
    concepts: concepts.map(c => ({ id: c.id, name: c.name, timing: c.timing })),
    boards: boards.map(([name]) => `${name}.png`),
  }, null, 2));
}

main().catch(err => { console.error(err); process.exit(1); });
