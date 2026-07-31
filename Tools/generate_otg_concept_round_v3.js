const fs = require("fs");
const path = require("path");
const sharp = require("sharp");

const out = path.resolve("Artifacts/OneTapGarageSplash/ConceptRound3");
const imports = path.join(out, "FigmaImports");
fs.mkdirSync(imports, { recursive: true });

const C = {
  ink: "#010604",
  forest: "#02110C",
  emerald: "#073C2A",
  emeraldLight: "#0C5B3C",
  champagne: "#E9C477",
  champagneLight: "#FFE4A8",
  bronze: "#A9743C",
  ivory: "#F3EEE5",
  white: "#FFFDF7",
  mist: "#879B91",
  darkCard: "#061A13",
  black: "#101512",
};

function defs(p) {
  return `<defs>
    <linearGradient id="${p}-bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="${C.ink}"/>
      <stop offset=".52" stop-color="#05251A"/>
      <stop offset="1" stop-color="${C.ink}"/>
    </linearGradient>
    <radialGradient id="${p}-amb">
      <stop offset="0" stop-color="${C.emeraldLight}" stop-opacity=".38"/>
      <stop offset=".48" stop-color="${C.emerald}" stop-opacity=".16"/>
      <stop offset="1" stop-color="${C.emerald}" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="${p}-gold" x1="40" y1="35" x2="215" y2="220" gradientUnits="userSpaceOnUse">
      <stop stop-color="${C.champagneLight}"/>
      <stop offset=".48" stop-color="${C.champagne}"/>
      <stop offset="1" stop-color="${C.bronze}"/>
    </linearGradient>
    <filter id="${p}-fog"><feGaussianBlur stdDeviation="55"/></filter>
    <filter id="${p}-soft"><feGaussianBlur stdDeviation="12"/></filter>
    <pattern id="${p}-grain" width="17" height="17" patternUnits="userSpaceOnUse">
      <circle cx="2" cy="3" r=".7" fill="#FFFFFF" opacity=".025"/>
      <circle cx="12" cy="9" r=".5" fill="#FFFFFF" opacity=".018"/>
    </pattern>
  </defs>`;
}

function atmosphere(p,w,h) {
  return `<g id="Atmosphere">
    <rect width="${w}" height="${h}" fill="url(#${p}-bg)"/>
    <ellipse cx="${w*.5}" cy="${h*.46}" rx="${w*.38}" ry="${h*.42}" fill="url(#${p}-amb)"/>
    <path d="M${w*.12} ${h*.7}C${w*.34} ${h*.52} ${w*.62} ${h*.75} ${w*.9} ${h*.47}" fill="none" stroke="#2F785A" stroke-opacity=".055" stroke-width="${h*.12}" filter="url(#${p}-fog)"/>
    <rect width="${w}" height="${h}" fill="url(#${p}-grain)"/>
    <g fill="${C.champagne}">
      <circle cx="${w*.19}" cy="${h*.43}" r="1.4" opacity=".11"/>
      <circle cx="${w*.34}" cy="${h*.69}" r="1" opacity=".09"/>
      <circle cx="${w*.71}" cy="${h*.31}" r="1.2" opacity=".10"/>
      <circle cx="${w*.83}" cy="${h*.63}" r=".9" opacity=".08"/>
    </g>
  </g>`;
}

function markShape(kind,p,mono=null) {
  const gold = mono || `url(#${p}-gold)`;
  const light = mono || C.ivory;
  if (kind === 1) return `<g id="Pulse Cut">
    <path d="M36 133L105 83L91 118L127 118L108 139L69 139Z" fill="${light}"/>
    <circle cx="143" cy="128" r="11" fill="${gold}"/>
    <path d="M164 105L219 67L184 119Z" fill="${gold}"/>
    <path d="M166 122H231L187 143Z" fill="${light}"/>
    <path d="M161 151L218 194L181 145Z" fill="${gold}"/>
  </g>`;
  if (kind === 2) return `<g id="Maker Stack">
    <path d="M55 51H142L161 76L137 105H55Z" fill="${light}"/>
    <path d="M93 104H205V154H127L105 136Z" fill="${gold}"/>
    <path d="M46 154H143L172 184L151 211H46Z" fill="${light}"/>
    <rect x="67" y="116" width="22" height="22" rx="5" fill="${mono || C.emeraldLight}"/>
  </g>`;
  if (kind === 3) return `<g id="Lucky Chip">
    <path d="M62 50H169L207 88V166L172 206H80L47 174V88Z" fill="${light}"/>
    <path d="M159 50H191L207 66V96L178 81Z" fill="${mono || C.ink}"/>
    <path d="M93 105L128 82L164 109L148 156L105 166L79 134Z" fill="${mono || C.emerald}"/>
    <path d="M193 54L219 38L225 69L207 82Z" fill="${gold}"/>
  </g>`;
  if (kind === 4) return `<g id="Spark Trace">
    <path d="M129 34L146 104L211 81L161 130L213 170L146 151L128 222L111 153L45 177L95 128L43 87L111 106Z" fill="${light}"/>
    <path d="M40 199C78 222 108 224 137 205" fill="none" stroke="${gold}" stroke-width="13" stroke-linecap="round"/>
    <circle cx="151" cy="196" r="9" fill="${gold}"/>
  </g>`;
  if (kind === 5) return `<g id="Assembly Bloom">
    <path d="M42 94L95 48L126 78L99 126L49 131Z" fill="${light}"/>
    <path d="M129 73L188 54L218 99L178 132L134 115Z" fill="${gold}"/>
    <path d="M87 140L133 119L188 151L170 211H102L63 174Z" fill="${light}"/>
    <circle cx="128" cy="127" r="13" fill="${mono || C.emeraldLight}"/>
    <circle cx="221" cy="78" r="9" fill="${gold}"/>
  </g>`;
  return `<g id="Sidekick">
    <path d="M42 62H126L147 91L115 121L145 149L118 202H42L70 164L45 131L71 99Z" fill="${light}"/>
    <path d="M142 54H214L186 94L213 124L184 154L211 201H142L164 161L137 132L166 99Z" fill="${gold}"/>
    <circle cx="119" cy="91" r="10" fill="${mono || C.ink}"/>
  </g>`;
}

function mark(kind,p,size,x,y,mono=null) {
  return `<g id="LogoMark" transform="translate(${x} ${y}) scale(${size/256})">${markShape(kind,p,mono)}</g>`;
}

const concepts = [
  {id:"01",name:"PULSE CUT",kind:1,idea:"One precise contact splits stillness into motion.",motion:"Dot lands → three shards fire outward → wordmark cuts on.",scores:[9.2,8.9,9.8,7.2,9.3,8.8,9.0,9.1,9.6,9.1],short:true},
  {id:"02",name:"MAKER STACK",kind:2,idea:"A deliberately uneven object assembled from game-making parts.",motion:"Three modules arrive on separate axes and lock with one click.",scores:[9.0,9.1,7.8,9.8,9.4,8.6,8.8,9.2,9.3,9.4],short:true},
  {id:"03",name:"LUCKY CHIP",kind:3,idea:"A premium solid token with one memorable broken-off chip.",motion:"Solid form resolves; the detached chip snaps into its imperfect orbit.",scores:[8.8,9.0,7.2,7.6,8.6,9.4,9.1,9.3,8.7,8.8],short:false},
  {id:"04",name:"SPARK TRACE",kind:4,idea:"A living ignition moment, followed by one restrained trace.",motion:"Short arc draws in; the spark flashes once and settles.",scores:[8.7,8.2,8.9,6.8,9.1,8.4,8.7,8.6,9.7,9.0],short:false},
  {id:"05",name:"ASSEMBLY BLOOM",kind:5,idea:"Different materials converge into a new playable object.",motion:"Three mismatched pieces converge after the gold point touches.",scores:[8.5,8.8,8.4,9.3,9.0,8.5,8.3,9.0,9.4,8.7],short:false},
  {id:"06",name:"SIDEKICK",kind:6,idea:"An abstract game-studio companion: sharp, odd and self-confident.",motion:"Two halves step in; the hidden eye opens with a gold impulse.",scores:[9.4,9.5,7.0,7.5,9.7,8.8,8.5,9.7,9.1,9.6],short:true},
];

function avg(a){return (a.reduce((x,y)=>x+y,0)/a.length).toFixed(1);}

function wordmark(cx,y,mode=1,scale=1,color=C.ivory) {
  if(mode===1) return `<g id="Wordmark Cut Grotesk" text-anchor="middle">
    <text x="${cx}" y="${y}" fill="${color}" font-family="Avenir Next, Arial, sans-serif" font-size="${72*scale}" font-weight="700" letter-spacing="${5*scale}">ONE TAP</text>
    <path d="M${cx+34*scale} ${y-62*scale}l${16*scale} ${-8*scale}" stroke="${C.champagne}" stroke-width="${6*scale}" stroke-linecap="square"/>
    <text x="${cx}" y="${y+48*scale}" fill="${C.champagne}" font-family="Avenir Next, Arial, sans-serif" font-size="${24*scale}" font-weight="600" letter-spacing="${8*scale}">GARAGE</text>
  </g>`;
  if(mode===2) return `<g id="Wordmark Kinetic Geo" text-anchor="middle">
    <text x="${cx-11*scale}" y="${y}" fill="${color}" font-family="Futura, Arial Black, sans-serif" font-size="${70*scale}" font-weight="700" letter-spacing="${2*scale}" transform="skewX(-5)">ONE TAP</text>
    <circle cx="${cx+158*scale}" cy="${y-48*scale}" r="${6*scale}" fill="${C.champagne}"/>
    <rect x="${cx-170*scale}" y="${y+23*scale}" width="${340*scale}" height="${3*scale}" fill="${C.champagne}" opacity=".55"/>
    <text x="${cx}" y="${y+58*scale}" fill="${C.ivory}" font-family="Futura, Arial, sans-serif" font-size="${22*scale}" font-weight="600" letter-spacing="${6*scale}">ONE TAP GARAGE</text>
  </g>`;
  return `<g id="Wordmark Hand Tuned" text-anchor="middle">
    <text x="${cx}" y="${y}" fill="${color}" font-family="Arial Black, Arial, sans-serif" font-size="${68*scale}" font-weight="900" letter-spacing="${-1*scale}">ONE</text>
    <text x="${cx}" y="${y+66*scale}" fill="${C.champagne}" font-family="Arial Black, Arial, sans-serif" font-size="${68*scale}" font-weight="900" letter-spacing="${-2*scale}">TAP</text>
    <path d="M${cx-90*scale} ${y+18*scale}H${cx+78*scale}" stroke="${C.ivory}" stroke-width="${5*scale}"/>
    <circle cx="${cx+96*scale}" cy="${y+18*scale}" r="${7*scale}" fill="${C.champagne}"/>
    <text x="${cx+150*scale}" y="${y+63*scale}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="${18*scale}" font-weight="700" letter-spacing="${5*scale}" transform="rotate(90 ${cx+150*scale} ${y+63*scale})">GARAGE</text>
  </g>`;
}

function lockup(c,p,cx,y,scale=1,wordMode=1) {
  const ms=180*scale;
  return `<g id="${c.name} Lockup">${mark(c.kind,p,ms,cx-ms/2,y)}${wordmark(cx,y+ms+90*scale,wordMode,.7*scale)}</g>`;
}

function miniSplash(c,p,x,y,w=560,h=315,wordMode=1) {
  return `<g id="${c.name} Landscape">
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="18" fill="url(#${p}-bg)"/>
    <ellipse cx="${x+w/2}" cy="${y+h*.47}" rx="${w*.28}" ry="${h*.42}" fill="url(#${p}-amb)"/>
    ${mark(c.kind,p,104,x+w/2-52,y+43)}
    ${wordmark(x+w/2,y+206,wordMode,.55)}
  </g>`;
}

function rejectedBoard() {
  const p="rejected",w=2600,h=1500;
  const v2=fs.readFileSync(path.resolve("Artifacts/OneTapGarageSplash/V2/FigmaImports/04_Final_Splash.svg"),"utf8").replace(/<svg[^>]*>/,"").replace(/<\/svg>\s*$/,"");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.ink}"/>
    <text x="70" y="82" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="40" font-weight="700">01 — REJECTED V2 / ARCHIVE</text>
    <text x="70" y="132" fill="#F08B7E" font-family="Arial, sans-serif" font-size="22" font-weight="700">NOT APPROVED · DO NOT USE AS A BASE</text>
    <g opacity=".55" transform="translate(70 190) scale(.8)">${v2}</g>
    <rect x="1980" y="190" width="550" height="1030" rx="28" fill="#101612"/>
    <text x="2025" y="255" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="25" font-weight="700">WHY IT WAS REJECTED</text>
    <text x="2025" y="320" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">
      <tspan x="2025" dy="0">• reads as SaaS / consulting</tspan>
      <tspan x="2025" dy="46">• generic GT / OTG monogram</tspan>
      <tspan x="2025" dy="46">• sterile and emotionally flat</tspan>
      <tspan x="2025" dy="46">• no independent-game character</tspan>
      <tspan x="2025" dy="46">• predictable rounded geometry</tspan>
      <tspan x="2025" dy="46">• weak standalone image</tspan>
    </text>
    <path d="M2040 700L2470 1130M2470 700L2040 1130" stroke="#F08B7E" stroke-width="18" opacity=".55"/>
  </svg>`;
}

function directionsBoard() {
  const p="directions",w=3200,h=1900;
  const cards=concepts.map((c,i)=>{
    const col=i%3,row=Math.floor(i/3),x=60+col*1045,y=180+row*780;
    return `<g id="Concept ${c.id} ${c.name}">
      <rect x="${x}" y="${y}" width="990" height="720" rx="34" fill="${C.darkCard}" stroke="${c.short?C.champagne:"#19392C"}" stroke-width="${c.short?3:1}"/>
      ${mark(c.kind,p,240,x+55,y+55)}
      <text x="${x+340}" y="${y+92}" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="35" font-weight="800">${c.id} · ${c.name}</text>
      <text x="${x+340}" y="${y+140}" fill="${c.short?C.champagne:C.mist}" font-family="Arial, sans-serif" font-size="17" font-weight="700" letter-spacing="3">${c.short?"SHORTLIST":"NEW DIRECTION"}</text>
      <text x="${x+340}" y="${y+205}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">
        <tspan x="${x+340}" dy="0">${c.idea}</tspan>
        <tspan x="${x+340}" dy="42">${c.motion}</tspan>
      </text>
      ${wordmark(x+490,y+420,(i%3)+1,.78)}
      <rect x="${x+55}" y="${y+550}" width="880" height="105" rx="18" fill="#0C231A"/>
      <text x="${x+85}" y="${y+617}" fill="${C.champagneLight}" font-family="Arial, sans-serif" font-size="34" font-weight="700">${avg(c.scores)}</text>
      <text x="${x+170}" y="${y+610}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="17">character · game feel · originality · motion · anti-SaaS</text>
    </g>`;
  }).join("");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.ink}"/>
    <text x="60" y="78" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">02 — NEW DIRECTIONS · SIX DIFFERENT BRAND IDEAS</text>
    <text x="60" y="130" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">No monograms. No winner selected. Gold borders indicate internal shortlist only.</text>
    ${cards}
  </svg>`;
}

function typographyBoard() {
  const p="type",w=2600,h=1500;
  const rows=[
    {name:"A · CUT GROTESK",desc:"Modern grotesk with a hand-cut activation notch.",mode:1},
    {name:"B · KINETIC GEO",desc:"Forward-leaning geometry and an integrated one-line signature.",mode:2},
    {name:"C · HAND-TUNED STACK",desc:"Custom stacked rhythm, hard interruption and vertical GARAGE.",mode:3},
  ];
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.ink}"/>
    <text x="70" y="82" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">03 — TYPOGRAPHY · THREE WORDMARK DIRECTIONS</text>
    <text x="70" y="132" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">Inter is not used as the final answer. Each route contains a custom interruption or composition.</text>
    ${rows.map((r,i)=>{const y=210+i*405;return `<g id="${r.name}">
      <rect x="70" y="${y}" width="2460" height="350" rx="30" fill="${i===2?"#0A241A":C.darkCard}"/>
      <text x="120" y="${y+65}" fill="${C.champagne}" font-family="Arial, sans-serif" font-size="24" font-weight="800">${r.name}</text>
      <text x="120" y="${y+108}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="18">${r.desc}</text>
      ${wordmark(1350,y+190,r.mode,1.4)}
    </g>`}).join("")}
  </svg>`;
}

function screensBoard() {
  const p="screens",w=3800,h=1900;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.ink}"/>
    <text x="60" y="82" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">04 — CONCEPT SCREENS · LANDSCAPE STUDY</text>
    <text x="60" y="132" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">Atmosphere is intentionally deeper and less sterile; these are comparison screens, not final exports.</text>
    ${concepts.map((c,i)=>{const x=60+(i%2)*1860,y=185+Math.floor(i/2)*545;return `<g>
      ${miniSplash(c,p,x,y,1760,990,(i%3)+1)}
      <text x="${x}" y="${y+1030}" fill="${C.champagne}" font-family="Arial, sans-serif" font-size="22" font-weight="700">${c.id} · ${c.name}</text>
    </g>`}).join("")}
  </svg>`;
}

function sizeBoard() {
  const p="sizes",w=3200,h=1700;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.ink}"/>
    <text x="60" y="82" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">05 — SIZE TESTS · 256 / 128 / 64 PX · MONO</text>
    <text x="60" y="132" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">64 px is a reduction test, not proof of final trademark readiness.</text>
    ${concepts.map((c,i)=>{const x=60+(i%3)*1045,y=190+Math.floor(i/3)*720;return `<g id="${c.name} Size Tests">
      <rect x="${x}" y="${y}" width="990" height="640" rx="30" fill="${C.darkCard}"/>
      <text x="${x+45}" y="${y+62}" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="27" font-weight="800">${c.id} · ${c.name}</text>
      ${mark(c.kind,p,256,x+60,y+115)}
      ${mark(c.kind,p,128,x+420,y+155)}
      ${mark(c.kind,p,64,x+620,y+185)}
      <rect x="${x+45}" y="${y+420}" width="900" height="160" rx="20" fill="${C.ivory}"/>
      ${mark(c.kind,p,128,x+100,y+438,C.black)}
      ${mark(c.kind,p,64,x+320,y+468,C.black)}
      <text x="${x+560}" y="${y+510}" fill="${C.black}" font-family="Arial, sans-serif" font-size="18">MONO / TRANSPARENT-READY GEOMETRY</text>
    </g>`}).join("")}
  </svg>`;
}

function motionBoard() {
  const p="motion3",w=3200,h=1900;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.ink}"/>
    <text x="60" y="82" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">06 — MOTION IDEAS · 0.8–1.2 S · CONCEPT-SPECIFIC</text>
    <text x="60" y="132" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">No bounce, spin, zoom, flare or loading UI.</text>
    ${concepts.map((c,i)=>{const x=60+(i%2)*1560,y=190+Math.floor(i/2)*530;return `<g id="${c.name} Motion">
      <rect x="${x}" y="${y}" width="1490" height="450" rx="30" fill="${C.darkCard}"/>
      <text x="${x+40}" y="${y+58}" fill="${C.champagne}" font-family="Arial, sans-serif" font-size="25" font-weight="800">${c.id} · ${c.name} · ${i%2===0?"0.95":"1.05"} S</text>
      ${[0,.38,.72,1].map((a,j)=>`<g opacity="${a}">${mark(c.kind,p,112,x+65+j*310,y+115)}</g>`).join("")}
      <path d="M${x+120} ${y+310}H${x+1160}" stroke="#416457" stroke-width="3"/>
      <circle cx="${x+120}" cy="${y+310}" r="7" fill="${C.champagne}"/>
      <circle cx="${x+430}" cy="${y+310}" r="7" fill="${C.champagne}"/>
      <circle cx="${x+740}" cy="${y+310}" r="7" fill="${C.champagne}"/>
      <circle cx="${x+1050}" cy="${y+310}" r="7" fill="${C.champagne}"/>
      <text x="${x+40}" y="${y+390}" fill="${C.mist}" font-family="Arial, sans-serif" font-size="19">${c.motion}</text>
    </g>`}).join("")}
  </svg>`;
}

function shortlistBoard() {
  const p="short",w=3000,h=1700;
  const selected=concepts.filter(c=>c.short);
  const headers=["CHAR","RECOG","ONE TAP","GARAGE","GAME","PREMIUM","READ","ORIGINAL","MOTION","ANTI-SaaS"];
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${defs(p)}
    <rect width="${w}" height="${h}" fill="${C.ink}"/>
    <text x="60" y="82" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="42" font-weight="800">07 — SHORTLIST · NOT A FINAL SELECTION</text>
    <text x="60" y="132" fill="${C.mist}" font-family="Arial, sans-serif" font-size="21">Three internally strongest directions. Manual approval is required before any final design or Unity export.</text>
    ${selected.map((c,i)=>{const x=60+i*975;return `<g id="Shortlist ${c.name}">
      <rect x="${x}" y="190" width="920" height="760" rx="34" fill="${C.darkCard}" stroke="${C.champagne}" stroke-width="3"/>
      ${mark(c.kind,p,280,x+320,240)}
      <text x="${x+45}" y="590" fill="${C.ivory}" font-family="Arial, sans-serif" font-size="34" font-weight="800">${c.id} · ${c.name}</text>
      <text x="${x+45}" y="645" fill="${C.mist}" font-family="Arial, sans-serif" font-size="20">${c.idea}</text>
      <text x="${x+45}" y="715" fill="${C.champagne}" font-family="Arial, sans-serif" font-size="18">${c.motion}</text>
      <text x="${x+45}" y="850" fill="${C.champagneLight}" font-family="Arial, sans-serif" font-size="68" font-weight="800">${avg(c.scores)}</text>
    </g>`}).join("")}
    <g id="Score Table"><rect x="60" y="1040" width="2860" height="560" rx="28" fill="#061A13"/>
      ${headers.map((h,j)=>`<text x="${780+j*205}" y="1100" fill="${C.champagne}" text-anchor="middle" font-family="Arial, sans-serif" font-size="14" font-weight="800">${h}</text>`).join("")}
      ${concepts.map((c,i)=>`<text x="100" y="${1170+i*66}" fill="${c.short?C.ivory:C.mist}" font-family="Arial, sans-serif" font-size="20">${c.id} ${c.name}</text>${c.scores.map((s,j)=>`<text x="${780+j*205}" y="${1170+i*66}" fill="${c.short?C.champagneLight:"#71867C"}" text-anchor="middle" font-family="Arial, sans-serif" font-size="19">${s.toFixed(1)}</text>`).join("")}`).join("")}
    </g>
  </svg>`;
}

const pages = [
  ["01_Rejected_V2",rejectedBoard()],
  ["02_New_Directions",directionsBoard()],
  ["03_Typography",typographyBoard()],
  ["04_Concept_Screens",screensBoard()],
  ["05_Size_Tests",sizeBoard()],
  ["06_Motion_Ideas",motionBoard()],
  ["07_Shortlist",shortlistBoard()],
];

async function main() {
  for (const [name,svg] of pages) {
    fs.writeFileSync(path.join(imports,`${name}.svg`),svg);
    await sharp(Buffer.from(svg),{density:72}).png({compressionLevel:9,adaptiveFiltering:true}).toFile(path.join(out,`${name}.png`));
  }
  fs.writeFileSync(path.join(out,"Concept_Report.md"),`# ONE TAP GARAGE — Concept Round 3

Status: exploratory only. No winner selected. No Unity export package created.

## Concepts

${concepts.map(c=>`- **${c.id} ${c.name}** — ${c.idea} Motion: ${c.motion} Average: ${avg(c.scores)}.`).join("\n")}

## Internal shortlist

- 01 Pulse Cut — strongest ONE TAP / impulse relationship.
- 02 Maker Stack — strongest Garage of Ideas / workshop relationship.
- 06 Sidekick — strongest independent-game character and standalone silhouette.

The shortlist is not a final selection. Manual approval is required.

## Why V2 was rejected

The GT/OTG Relay monogram was technically clean but read as a generic SaaS, consulting or IT-service identity. Rounded G/T construction, sterile spacing and neutral typography did not create an emotional or game-studio image.

## Typography

- A Cut Grotesk — contemporary grotesk with a custom activation notch.
- B Kinetic Geo — forward-leaning geometry with an integrated one-line signature.
- C Hand-Tuned Stack — custom stacked rhythm with a hard interruption and vertical GARAGE.

## Constraints observed

No GT/OTG monogram, rounded G, diamond, empty outline, power button, cursor, tap-circle, gear, wrench, garage door, controller, crown, card suit or CardMesh mark was used as the basis.

Unity/CardMesh was not modified. No final Unity-ready export package was created.
`);
  fs.writeFileSync(path.join(out,"manifest.json"),JSON.stringify({
    round:3,status:"concept-only",winner:null,shortlist:["Pulse Cut","Maker Stack","Sidekick"],
    unityModified:false,finalExportsCreated:false,pages:pages.map(([n])=>n)
  },null,2));
}
main().catch(e=>{console.error(e);process.exit(1);});
