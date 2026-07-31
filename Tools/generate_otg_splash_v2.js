const fs = require("fs");
const path = require("path");
const sharp = require("sharp");

const root = path.resolve("Artifacts/OneTapGarageSplash/V2");
const figmaDir = path.join(root, "FigmaImports");
fs.mkdirSync(figmaDir, { recursive: true });

const C = {
  deep: "#020906",
  deepGreen: "#03130D",
  emerald: "#0A5638",
  emeraldSoft: "#123E30",
  goldLight: "#FFE3A6",
  gold: "#E8B75E",
  goldMuted: "#C99D51",
  goldDark: "#8F5F28",
  ivory: "#F4EEE4",
  muted: "#93A49B",
};

function defs(prefix) {
  return `<defs>
    <linearGradient id="${prefix}-bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="${C.deep}"/>
      <stop offset=".5" stop-color="#062319"/>
      <stop offset="1" stop-color="${C.deep}"/>
    </linearGradient>
    <radialGradient id="${prefix}-ambient">
      <stop offset="0" stop-color="${C.emeraldSoft}" stop-opacity=".88"/>
      <stop offset=".5" stop-color="${C.emerald}" stop-opacity=".28"/>
      <stop offset="1" stop-color="${C.emerald}" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="${prefix}-core">
      <stop offset="0" stop-color="${C.gold}" stop-opacity=".12"/>
      <stop offset="1" stop-color="${C.gold}" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="${prefix}-vignette">
      <stop offset=".42" stop-color="${C.deep}" stop-opacity="0"/>
      <stop offset="1" stop-color="#010403" stop-opacity=".9"/>
    </radialGradient>
    <linearGradient id="${prefix}-gold" x1="45" y1="35" x2="214" y2="220" gradientUnits="userSpaceOnUse">
      <stop stop-color="${C.goldLight}"/>
      <stop offset=".42" stop-color="${C.gold}"/>
      <stop offset="1" stop-color="${C.goldDark}"/>
    </linearGradient>
    <filter id="${prefix}-blur"><feGaussianBlur stdDeviation="44"/></filter>
    <filter id="${prefix}-soft"><feGaussianBlur stdDeviation="18"/></filter>
  </defs>`;
}

function bg(prefix, w, h) {
  const dots = [
    [.17,.40,1.6,.16],[.28,.69,1.2,.12],[.39,.24,1.4,.15],[.62,.27,1.2,.13],
    [.73,.65,1.6,.14],[.82,.38,1,.12],[.47,.77,1,.10],[.58,.18,1.1,.11],
  ];
  return `<g id="Background">
    <rect id="Base" width="${w}" height="${h}" fill="url(#${prefix}-bg)"/>
    <ellipse id="RadialLight" cx="${w/2}" cy="${h*.44}" rx="${w*.34}" ry="${h*.35}" fill="url(#${prefix}-ambient)"/>
    <ellipse id="AmbientFog" cx="${w/2}" cy="${h*.42}" rx="${w*.15}" ry="${h*.14}" fill="url(#${prefix}-core)"/>
    <g id="Particles" fill="${C.gold}">${dots.map(([x,y,r,o])=>`<circle cx="${w*x}" cy="${h*y}" r="${r}" opacity="${o}"/>`).join("")}</g>
    <rect id="Vignette" width="${w}" height="${h}" fill="url(#${prefix}-vignette)"/>
  </g>`;
}

function markShape(kind, stroke, prefix) {
  const s = stroke || `url(#${prefix}-gold)`;
  if (kind === 1) return `<g id="OTG Relay">
    <path d="M174 38H91C66 38 50 54 50 79V177C50 202 66 218 91 218H169C192 218 206 204 206 181V147H164"
      fill="none" stroke="${s}" stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/>
    <path d="M92 91H174M133 91V174" fill="none" stroke="${s}" stroke-width="16" stroke-linecap="round"/>
    <circle cx="199" cy="62" r="10" fill="${s}"/>
  </g>`;
  if (kind === 2) return `<g id="Touch Orbit">
    <path d="M176 51A91 91 0 1 0 211 111" fill="none" stroke="${s}" stroke-width="16" stroke-linecap="round"/>
    <path d="M80 164L128 116L161 149" fill="none" stroke="${s}" stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/>
    <circle cx="193" cy="69" r="11" fill="${s}"/>
  </g>`;
  if (kind === 3) return `<g id="Kinetic Facet">
    <path d="M166 52L143 33Q128 21 113 34L35 113Q22 128 35 143L113 222Q128 235 143 222L221 143Q234 128 221 113L194 86"
      fill="none" stroke="${s}" stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/>
    <path d="M89 108H166M128 108V171" fill="none" stroke="${s}" stroke-width="14" stroke-linecap="round"/>
    <circle cx="181" cy="68" r="10" fill="${s}"/>
  </g>`;
  if (kind === 4) return `<g id="Ignition Rail">
    <path d="M55 184L126 63L202 184" fill="none" stroke="${s}" stroke-width="17" stroke-linecap="round" stroke-linejoin="round"/>
    <path d="M86 184L144 85M92 130H174" fill="none" stroke="${s}" stroke-width="14" stroke-linecap="round"/>
    <circle cx="137" cy="48" r="11" fill="${s}"/>
  </g>`;
  return `<g id="Idea Aperture">
    <path d="M91 48H57Q44 48 44 61V95M165 48H199Q212 48 212 61V95M91 208H57Q44 208 44 195V161M165 208H199Q212 208 212 195V161"
      fill="none" stroke="${s}" stroke-width="16" stroke-linecap="round"/>
    <path d="M92 128H164M128 92V164" fill="none" stroke="${s}" stroke-width="14" stroke-linecap="round"/>
    <circle cx="178" cy="104" r="10" fill="${s}"/>
  </g>`;
}

function mark(kind, prefix, size, x, y, glow = false, mono = null) {
  const sc = size / 256;
  return `<g id="LogoMark" transform="translate(${x} ${y}) scale(${sc})">
    ${glow ? `<circle cx="128" cy="128" r="98" fill="${C.emerald}" opacity=".22" filter="url(#${prefix}-soft)"/>` : ""}
    ${markShape(kind, mono, prefix)}
  </g>`;
}

function textLockup(cx, y, scale = 1) {
  return `<g id="BrandText" text-anchor="middle">
    <text id="OneTapText" x="${cx}" y="${y + 76*scale}" fill="${C.ivory}" font-family="Inter, Arial, sans-serif"
      font-size="${76*scale}" font-weight="600" letter-spacing="${6*scale}">ONE TAP</text>
    <text id="GarageText" x="${cx}" y="${y + 130*scale}" fill="${C.goldMuted}" font-family="Inter, Arial, sans-serif"
      font-size="${32*scale}" font-weight="600" letter-spacing="${9.5*scale}">GARAGE</text>
  </g>`;
}

function splash(w, h, orient, guide = false, brand = true) {
  const p = `v2-${orient}-${guide ? "figma" : "export"}`;
  const markSize = orient === "landscape" ? 246 : orient === "portrait" ? 285 : 310;
  const markY = orient === "landscape" ? 220 : orient === "portrait" ? 520 : 565;
  const textY = orient === "landscape" ? 500 : orient === "portrait" ? 850 : 935;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${defs(p)}
    <g id="SplashRoot">
      ${bg(p,w,h)}
      ${brand ? `<g id="BrandRoot">${mark(1,p,markSize,(w-markSize)/2,markY,true)}${textLockup(w/2,textY,1.08)}</g>` : ""}
      ${guide ? `<rect id="SafeAreaGuide" x="${w*.1}" y="${h*.1}" width="${w*.8}" height="${h*.8}" rx="18" fill="none" stroke="#FFFFFF" stroke-opacity=".12" stroke-width="2" stroke-dasharray="12 12"/>` : ""}
    </g>
  </svg>`;
}

function currentBoard() {
  const p="current-board", w=2400, h=1400;
  const current = fs.readFileSync(path.resolve("Artifacts/OneTapGarageSplash/OTG_Splash_L_1920x1080.svg"),"utf8")
    .replace(/<\?xml[^>]*>/g,"").replace(/<svg[^>]*>/,"").replace(/<\/svg>\s*$/,"");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${defs(p)}<rect width="${w}" height="${h}" fill="${C.deep}"/>
    <text x="80" y="85" fill="${C.ivory}" font-family="Inter,Arial" font-size="38" font-weight="600" letter-spacing="3">01 — CURRENT · PRESERVED</text>
    <g transform="translate(80 150) scale(1.1458)">${current}</g>
    <text x="80" y="1340" fill="${C.muted}" font-family="Inter,Arial" font-size="22">Контрольный вариант сохранён без изменений для прямого сравнения.</text>
  </svg>`;
}

const concepts = [
  {n:"01",name:"OTG RELAY",tag:"SELECTED",kind:1,scores:[9.2,9.3,9.4,9.0,9.7,9.4,9.2,9.5]},
  {n:"02",name:"TOUCH ORBIT",tag:"ONE TAP",kind:2,scores:[8.6,7.7,9.2,8.8,8.4,8.7,9.1,8.4]},
  {n:"03",name:"KINETIC FACET",tag:"CONTINUITY",kind:3,scores:[8.8,8.0,8.9,8.5,7.8,9.3,9.4,8.7]},
  {n:"04",name:"IGNITION RAIL",tag:"ACTION",kind:4,scores:[8.2,8.4,8.7,8.6,7.6,8.5,9.0,8.6]},
  {n:"05",name:"IDEA APERTURE",tag:"WORKSHOP",kind:5,scores:[8.0,8.2,8.5,8.3,7.9,8.1,8.7,8.8]},
];

function explorationBoard() {
  const p="explore",w=2400,h=1400;
  const cards=concepts.map((c,i)=>{
    const x=55+i*470, selected=i===0, avg=(c.scores.reduce((a,b)=>a+b,0)/c.scores.length).toFixed(1);
    return `<g id="Variant ${c.n} ${c.name}">
      <rect x="${x}" y="180" width="430" height="610" rx="28" fill="${selected?"#0B2B20":"#071A13"}" stroke="${selected?C.gold:"#1A3A2E"}" stroke-width="${selected?3:1}"/>
      ${mark(c.kind,p,190,x+120,235,false)}
      <text x="${x+30}" y="485" fill="${C.ivory}" font-family="Inter,Arial" font-size="27" font-weight="600" letter-spacing="1.5">${c.name}</text>
      <text x="${x+30}" y="525" fill="${selected?C.goldLight:C.muted}" font-family="Inter,Arial" font-size="15" font-weight="600" letter-spacing="3">${c.tag}</text>
      <text x="${x+30}" y="600" fill="${selected?C.gold:"#71867C"}" font-family="Inter,Arial" font-size="54" font-weight="600">${avg}</text>
      <text x="${x+30}" y="652" fill="${C.muted}" font-family="Inter,Arial" font-size="15">recognition · uniqueness · 64 px</text>
      <text x="${x+30}" y="688" fill="${C.muted}" font-family="Inter,Arial" font-size="15">simplicity · name link · CardMesh</text>
      <text x="${x+30}" y="724" fill="${C.muted}" font-family="Inter,Arial" font-size="15">motion · future products</text>
    </g>`;
  }).join("");
  const rows=concepts.map((c,i)=>`<text x="80" y="${905+i*62}" fill="${i===0?C.ivory:C.muted}" font-family="Inter,Arial" font-size="20">${c.n} ${c.name}</text>
    ${c.scores.map((s,j)=>`<text x="${720+j*170}" y="${905+i*62}" fill="${i===0?C.goldLight:"#A7B5AE"}" text-anchor="middle" font-family="Inter,Arial" font-size="20">${s.toFixed(1)}</text>`).join("")}`).join("");
  const headers=["RECOG","UNIQUE","64 PX","SIMPLE","NAME","CARDMESH","MOTION","FUTURE"];
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.deep}"/>
    <text x="55" y="78" fill="${C.ivory}" font-family="Inter,Arial" font-size="38" font-weight="600" letter-spacing="3">02 — LOGO EXPLORATION · FIVE DISTINCT DIRECTIONS</text>
    <text x="55" y="126" fill="${C.muted}" font-family="Inter,Arial" font-size="20">Winner: OTG Relay — the tap point activates a compact O / T / G monogram.</text>
    ${cards}
    <g id="Score Table"><rect x="55" y="820" width="2290" height="500" rx="24" fill="#061A13"/>
      ${headers.map((h,j)=>`<text x="${720+j*170}" y="855" fill="${C.goldMuted}" text-anchor="middle" font-family="Inter,Arial" font-size="14" font-weight="600" letter-spacing="1">${h}</text>`).join("")}
      ${rows}
    </g>
  </svg>`;
}

function testsBoard() {
  const p="tests",w=2400,h=1400;
  const cols=concepts.map((c,i)=>{
    const x=60+i*465;
    return `<g id="Tests ${c.name}">
      <text x="${x+205}" y="165" text-anchor="middle" fill="${C.ivory}" font-family="Inter,Arial" font-size="22" font-weight="600">${c.name}</text>
      <rect x="${x}" y="205" width="410" height="340" rx="24" fill="#071C14"/>
      ${mark(c.kind,p,256,x+77,245,false)}
      <rect x="${x}" y="580" width="410" height="250" rx="24" fill="#EFEAE1"/>
      ${mark(c.kind,p,128,x+52,635,false,"#111A16")}
      ${mark(c.kind,p,64,x+230,665,false,"#111A16")}
      <text x="${x+116}" y="802" fill="#38433E" text-anchor="middle" font-family="Inter,Arial" font-size="14">128 px</text>
      <text x="${x+262}" y="802" fill="#38433E" text-anchor="middle" font-family="Inter,Arial" font-size="14">64 px</text>
      <rect x="${x}" y="865" width="410" height="390" rx="24" fill="url(#${p}-bg)"/>
      ${mark(c.kind,p,128,x+141,918,true)}
      ${textLockup(x+205,1070,.42)}
    </g>`;
  }).join("");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.deep}"/>
    <text x="60" y="78" fill="${C.ivory}" font-family="Inter,Arial" font-size="38" font-weight="600" letter-spacing="3">03 — LOGO TESTS · 256 / 128 / 64 PX · MONO · SPLASH</text>
    <text x="60" y="122" fill="${C.muted}" font-family="Inter,Arial" font-size="19">All marks use one-color geometry; gradients are presentation-only. OTG Relay remains legible at 64 px.</text>
    ${cols}
  </svg>`;
}

function comparisonBoard() {
  const p="compare",w=2400,h=1400;
  const oldSvg=fs.readFileSync(path.resolve("Artifacts/OneTapGarageSplash/OTG_Splash_L_1920x1080.svg"),"utf8").replace(/<svg[^>]*>/,"").replace(/<\/svg>\s*$/,"");
  const newSvg=splash(1920,1080,"landscape",false,true).replace(/<svg[^>]*>/,"").replace(/<\/svg>\s*$/,"");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="#010604"/>
    <text x="60" y="78" fill="${C.ivory}" font-family="Inter,Arial" font-size="38" font-weight="600" letter-spacing="3">CURRENT / FINAL — DIRECT COMPARISON</text>
    <text x="60" y="135" fill="${C.muted}" font-family="Inter,Arial" font-size="20">Current generic facet preserved at left · ownable OTG Relay and +12% brand block at right.</text>
    <g transform="translate(60 210) scale(.57)">${oldSvg}</g>
    <g transform="translate(1245 210) scale(.57)">${newSvg}</g>
    <text x="60" y="880" fill="${C.muted}" font-family="Inter,Arial" font-size="20">CURRENT</text>
    <text x="1245" y="880" fill="${C.goldLight}" font-family="Inter,Arial" font-size="20">FINAL · OTG RELAY</text>
    <g transform="translate(60 955)">${mark(3,p,240,0,0,false)}${mark(1,p,240,470,0,false)}
      <path d="M305 120H405" stroke="${C.goldMuted}" stroke-width="3"/>
      <path d="M390 105L407 120L390 135" fill="none" stroke="${C.goldMuted}" stroke-width="3"/>
      <text x="760" y="75" fill="${C.ivory}" font-family="Inter,Arial" font-size="26" font-weight="600">WHAT CHANGED</text>
      <text x="760" y="125" fill="${C.muted}" font-family="Inter,Arial" font-size="20">• symbol now encodes O / T / G and a tap point</text>
      <text x="760" y="165" fill="${C.muted}" font-family="Inter,Arial" font-size="20">• GARAGE enlarged and strengthened</text>
      <text x="760" y="205" fill="${C.muted}" font-family="Inter,Arial" font-size="20">• central group enlarged by 12%</text>
    </g>
  </svg>`;
}

function responsiveBoard() {
  const p="responsive",w=4800,h=2300;
  const l=splash(1920,1080,"landscape",true,true).replace(/<svg[^>]*>/,"").replace(/<\/svg>\s*$/,"");
  const po=splash(1080,1920,"portrait",true,true).replace(/<svg[^>]*>/,"").replace(/<\/svg>\s*$/,"");
  const sq=splash(2048,2048,"square",true,true).replace(/<svg[^>]*>/,"").replace(/<\/svg>\s*$/,"");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="#010604"/>
    <g id="Landscape 1920x1080" transform="translate(40 160)">${l}</g>
    <g id="Portrait 1080x1920" transform="translate(2020 160)">${po}</g>
    <g id="Square 2048x2048" transform="translate(3160 160)">${sq}</g>
    <text x="40" y="100" fill="${C.ivory}" font-family="Inter,Arial" font-size="42" font-weight="600">05 — RESPONSIVE · 10% SAFE AREA</text>
  </svg>`;
}

function motionBoard() {
  const p="motion",w=2400,h=1400;
  const times=["0.00 s","0.10 s","0.46 s","0.78 s","1.10 s"];
  const frames=times.map((t,i)=>{
    const x=55+i*465, alpha=[0,.3,1,1,0][i], sc=[.96,.975,1,1,1][i];
    return `<g id="Keyframe ${t}"><rect x="${x}" y="190" width="420" height="236" rx="18" fill="url(#${p}-bg)"/>
      <g opacity="${alpha}" transform="translate(${x+210} 308) scale(${sc}) translate(-128 -128)">${mark(1,p,128,64,70,false)}</g>
      <text x="${x}" y="470" fill="${i===4?C.muted:C.ivory}" font-family="Inter,Arial" font-size="24" font-weight="600">${t}</text>
    </g>`;
  }).join("");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.deep}"/>
    <text x="55" y="78" fill="${C.ivory}" font-family="Inter,Arial" font-size="38" font-weight="600" letter-spacing="3">07 — MOTION · SINGLE TAP / RELAY · 1.10 S</text>
    <text x="55" y="128" fill="${C.muted}" font-family="Inter,Arial" font-size="20">No loop · no loading UI · no custom shader · background visible immediately.</text>
    ${frames}
    <g id="Timeline"><rect x="55" y="550" width="2290" height="720" rx="28" fill="#061A13"/>
      <text x="100" y="625" fill="${C.goldLight}" font-family="Inter,Arial" font-size="26" font-weight="600">TIMING / UNITY IMPLEMENTATION</text>
      <text x="100" y="690" fill="${C.ivory}" font-family="Inter,Arial" font-size="21">
        <tspan x="100" dy="0">0.00  Background visible. BrandRoot alpha 0, scale 96%.</tspan>
        <tspan x="100" dy="44">0.10–0.46  LogoMark alpha 0→1, scale 96→100%, Ease Out Cubic.</tspan>
        <tspan x="100" dy="44">0.36–0.68  ONE TAP alpha 0→1, Y +8→0, Ease Out Cubic.</tspan>
        <tspan x="100" dy="44">0.50–0.78  GARAGE alpha 0→1, Y +6→0, Ease Out Quad.</tspan>
        <tspan x="100" dy="44">0.27–0.56  Tap point arrives; one restrained RGBA ripple fades.</tspan>
        <tspan x="100" dy="44">0.88–1.10  If SceneReady: SplashRoot alpha 1→0, Ease InOut Quad.</tspan>
        <tspan x="100" dy="66">Components: CanvasGroup + RectTransform + Image + TextMeshPro.</tspan>
        <tspan x="100" dy="44">DOTween only if already installed. Otherwise Animator clips. Never delay scene readiness.</tspan>
        <tspan x="100" dy="44">If SceneReady is late: hold final static state; begin the 0.22 s fade only when ready.</tspan>
      </text>
    </g>
  </svg>`;
}

function exportBoard() {
  const p="exports",w=2400,h=1400;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.deep}"/>
    <text x="60" y="78" fill="${C.ivory}" font-family="Inter,Arial" font-size="38" font-weight="600" letter-spacing="3">06 — EXPORT · UNITY-READY ELEMENTS</text>
    <g id="LogoMark Export"><rect x="60" y="160" width="520" height="520" rx="24" fill="#071A13"/>${mark(1,p,420,110,210,false)}</g>
    <g id="FullLogo Export"><rect x="640" y="160" width="760" height="520" rx="24" fill="#071A13"/>${mark(1,p,220,910,205,false)}${textLockup(1020,445,.82)}</g>
    <g id="Text Exports"><rect x="1460" y="160" width="880" height="520" rx="24" fill="#071A13"/>${textLockup(1900,310,1.25)}</g>
    <text x="60" y="760" fill="${C.goldLight}" font-family="Inter,Arial" font-size="25" font-weight="600">FILES</text>
    <text x="60" y="815" fill="${C.muted}" font-family="Inter,Arial" font-size="20">
      <tspan x="60" dy="0">Splash_L_1920x1080.png · opaque RGB</tspan>
      <tspan x="60" dy="38">Background_L_1920x1080.png · opaque RGB</tspan>
      <tspan x="60" dy="38">LogoMark_1024x1024.png / .svg · RGBA / vector</tspan>
      <tspan x="60" dy="38">OneTap_1024x256.png · RGBA</tspan>
      <tspan x="60" dy="38">Garage_1024x256.png · RGBA</tspan>
      <tspan x="60" dy="38">FullLogo_1024x1024.png / .svg · RGBA / vector</tspan>
      <tspan x="60" dy="55">No guides, labels or Figma UI in exported assets.</tspan>
    </text>
    <text x="1320" y="815" fill="${C.ivory}" font-family="Inter,Arial" font-size="22">
      <tspan x="1320" dy="0">Font: Inter Semi Bold</tspan>
      <tspan x="1320" dy="42">Background: ${C.deep} → #062319</tspan>
      <tspan x="1320" dy="42">Mark: ${C.goldLight} → ${C.gold} → ${C.goldDark}</tspan>
      <tspan x="1320" dy="42">ONE TAP: ${C.ivory}</tspan>
      <tspan x="1320" dy="42">GARAGE: ${C.goldMuted}</tspan>
    </text>
  </svg>`;
}

function markExport() {
  const p="mark-export";
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 1024 1024">${defs(p)}${mark(1,p,1280,-128,-128,false)}</svg>`;
}
function oneTapExport() {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="256" viewBox="0 0 1024 256">
    <text x="512" y="173" text-anchor="middle" fill="${C.ivory}" font-family="Inter,Arial" font-size="150" font-weight="600" letter-spacing="12">ONE TAP</text></svg>`;
}
function garageExport() {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="256" viewBox="0 0 1024 256">
    <text x="512" y="166" text-anchor="middle" fill="${C.goldMuted}" font-family="Inter,Arial" font-size="112" font-weight="600" letter-spacing="32">GARAGE</text></svg>`;
}
function fullLogoExport() {
  const p="full-export";
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 1024 1024">${defs(p)}
    ${mark(1,p,720,152,-40,false)}${textLockup(512,610,2)}</svg>`;
}

const figmaPages = [
  ["01_Current.svg", currentBoard()],
  ["02_Logo_Exploration.svg", explorationBoard()],
  ["03_Logo_Tests.svg", testsBoard()],
  ["04_Final_Splash.svg", comparisonBoard()],
  ["05_Responsive.svg", responsiveBoard()],
  ["06_Export.svg", exportBoard()],
  ["07_Motion.svg", motionBoard()],
];

const assets = [
  ["OTG_V2_Splash_L_1920x1080", splash(1920,1080,"landscape",false,true), true],
  ["OTG_V2_Splash_P_1080x1920", splash(1080,1920,"portrait",false,true), true],
  ["OTG_V2_Splash_S_2048x2048", splash(2048,2048,"square",false,true), true],
  ["OTG_V2_Background_L_1920x1080", splash(1920,1080,"landscape",false,false), true],
  ["OTG_V2_LogoMark_1024x1024", markExport(), false],
  ["OTG_V2_OneTap_1024x256", oneTapExport(), false],
  ["OTG_V2_Garage_1024x256", garageExport(), false],
  ["OTG_V2_FullLogo_1024x1024", fullLogoExport(), false],
];

async function writeSvgPng(name, svg, opaque) {
  fs.writeFileSync(path.join(root, `${name}.svg`), svg);
  let pipeline = sharp(Buffer.from(svg), { density: 72 });
  if (opaque) pipeline = pipeline.removeAlpha();
  await pipeline.png({compressionLevel:9,adaptiveFiltering:true}).toFile(path.join(root, `${name}.png`));
}

async function main() {
  for (const [name,svg,opaque] of assets) await writeSvgPng(name,svg,opaque);
  for (const [name,svg] of figmaPages) {
    fs.writeFileSync(path.join(figmaDir,name),svg);
    await sharp(Buffer.from(svg),{density:72}).png({compressionLevel:9}).toFile(path.join(root,name.replace(".svg",".png")));
  }
  fs.writeFileSync(path.join(root,"Motion_Spec.md"),`# ONE TAP GARAGE — Motion Specification V2

Nominal ready-early duration: **1.10 s**. No loop, progress UI, spinner, loading label, or artificial hold.

| Time | Motion |
|---|---|
| 0.00 s | Background visible immediately. BrandRoot alpha 0, scale 96%. |
| 0.10–0.46 s | LogoMark alpha 0→1, scale 96→100%, Ease Out Cubic. |
| 0.27–0.56 s | Tap point arrives; one simple RGBA ripple expands and fades. |
| 0.36–0.68 s | ONE TAP alpha 0→1, Y +8→0, Ease Out Cubic. |
| 0.50–0.78 s | GARAGE alpha 0→1, Y +6→0, Ease Out Quad. |
| 0.78–0.88 s | Calm final lockup hold. |
| 0.88–1.10 s | If SceneReady: SplashRoot CanvasGroup alpha 1→0, Ease InOut Quad. |

Unity implementation: CanvasGroup, RectTransform, Image, TextMeshPro. DOTween only if already installed; otherwise Animator clips. No custom shaders. Use unscaled time. If loading lasts longer, hold the final static state and run the 0.22 s fade only when the next scene is ready.
`);
  fs.writeFileSync(path.join(root,"manifest.json"),JSON.stringify({
    version:2,selectedConcept:"OTG Relay",font:"Inter Semi Bold",
    colors:C,safeArea:"10% each edge",unityProjectModified:false,
    assets:assets.map(([name,,opaque])=>({name,png:`${name}.png`,svg:`${name}.svg`,mode:opaque?"opaque RGB":"transparent RGBA"})),
    figmaFrames:figmaPages.map(([name])=>name)
  },null,2));
}

main().catch(e=>{console.error(e);process.exit(1);});
