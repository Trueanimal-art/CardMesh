import fs from "node:fs/promises";
import path from "node:path";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const dependencyRoot = process.env.CARDMESH_NODE_MODULES;
if (!dependencyRoot) {
  throw new Error("CARDMESH_NODE_MODULES must point to the bundled node_modules.");
}

const sharp = require(path.join(dependencyRoot, "sharp"));
const outputDirectory =
  process.argv[2] ?? "Assets/Art/UI/MainMenuMobile";

const icons = {
  reward: `
    <path d="M78 119h100v83H78z" fill="url(#emerald2)" stroke="url(#gold)" stroke-width="10"/>
    <path d="M128 119v83M70 101h116v31H70z" fill="none" stroke="url(#gold)" stroke-width="10" stroke-linejoin="round"/>
    <path d="M128 101c-34-2-53-13-49-31 4-18 34-7 49 31zm0 0c34-2 53-13 49-31-4-18-34-7-49 31z" fill="none" stroke="url(#gold)" stroke-width="9" stroke-linejoin="round"/>
    <path d="M128 67v135" stroke="#fff0a0" stroke-width="3" opacity=".55"/>`,
  chests: `
    <path d="M65 109c3-31 27-50 63-50s60 19 63 50" fill="url(#emerald2)" stroke="url(#gold)" stroke-width="10"/>
    <path d="M58 104h140v95H58z" fill="url(#emerald2)" stroke="url(#gold)" stroke-width="10" stroke-linejoin="round"/>
    <path d="M58 132h140M87 104v95m82-95v95" fill="none" stroke="url(#gold)" stroke-width="8"/>
    <path d="M112 132h32v40h-32z" fill="url(#gold)" stroke="#7b3c00" stroke-width="4"/>
    <circle cx="128" cy="151" r="6" fill="#06462d"/>`,
  levels: `
    <path d="M77 58h103v144H77z" fill="url(#ivory)" stroke="url(#gold)" stroke-width="9" stroke-linejoin="round"/>
    <path d="M96 91l9 9 17-20m-26 55 9 9 17-20m-26 55 9 9 17-20" fill="none" stroke="#168458" stroke-width="8" stroke-linecap="round" stroke-linejoin="round"/>
    <path d="M135 91h28m-28 44h28m-28 44h28" stroke="#70400b" stroke-width="7" stroke-linecap="round"/>
    <path d="M105 49h46v22h-46z" fill="url(#gold)" stroke="#7b3c00" stroke-width="4" rx="8"/>`,
  collection: `
    <path d="M54 76c31-10 56-6 74 11v117c-20-15-45-19-74-9zm148 0c-31-10-56-6-74 11v117c20-15 45-19 74-9z" fill="url(#ivory)" stroke="url(#gold)" stroke-width="9" stroke-linejoin="round"/>
    <path d="M128 87v117" stroke="#9a5b08" stroke-width="7"/>
    <path d="M85 118h25m-25 23h25m36-23h25m-25 23h25" stroke="#a36a1c" stroke-width="5" stroke-linecap="round"/>`,
  play: `
    <path d="M82 57h92c18 0 32 14 32 32v78c0 18-14 32-32 32H82c-18 0-32-14-32-32V89c0-18 14-32 32-32z" fill="url(#emerald2)" stroke="url(#gold)" stroke-width="10"/>
    <path d="M112 92l56 36-56 36z" fill="url(#gold)" stroke="#fff0a0" stroke-width="4" stroke-linejoin="round"/>
    <path d="M82 57l10-18h72l10 18" fill="none" stroke="url(#gold)" stroke-width="7" stroke-linecap="round"/>`,
  shop: `
    <path d="M72 91h112l12 109H60z" fill="url(#emerald2)" stroke="url(#gold)" stroke-width="10" stroke-linejoin="round"/>
    <path d="M93 97V79c0-24 14-39 35-39s35 15 35 39v18" fill="none" stroke="url(#gold)" stroke-width="10" stroke-linecap="round"/>
    <path d="M62 91h132l-12 34H74z" fill="url(#gold)" stroke="#7b3c00" stroke-width="4" stroke-linejoin="round"/>
    <path d="M101 151h54" stroke="#fce78c" stroke-width="7" stroke-linecap="round"/>`,
  settings: `
    <path d="M128 50l15 6 12-12 20 12-4 17 14 10 16-6 11 21-13 11 2 17 16 7-2 24-18 4-7 15 10 14-17 17-14-10-15 7-4 18h-24l-7-16-17-2-11 13-21-11 6-16-10-14-17 4-12-20 12-12-6-15-18-2V111l17-7 4-17-13-11 12-20 16 6 13-11 4-17h24z" fill="url(#gold)" stroke="#7b3c00" stroke-width="5" stroke-linejoin="round"/>
    <circle cx="128" cy="128" r="39" fill="url(#emerald2)" stroke="#fff0a0" stroke-width="6"/>
    <circle cx="128" cy="128" r="15" fill="#e6a313"/>`,
};

function svg(body) {
  return Buffer.from(`
  <svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256">
    <defs>
      <linearGradient id="gold" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#fff3a3"/>
        <stop offset=".28" stop-color="#f7c532"/>
        <stop offset=".58" stop-color="#a85b04"/>
        <stop offset=".82" stop-color="#ffd85a"/>
        <stop offset="1" stop-color="#7a3500"/>
      </linearGradient>
      <linearGradient id="emerald2" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#12683f"/>
        <stop offset=".52" stop-color="#053f2b"/>
        <stop offset="1" stop-color="#01291d"/>
      </linearGradient>
      <linearGradient id="ivory" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#fff6ce"/>
        <stop offset="1" stop-color="#d9ad58"/>
      </linearGradient>
      <filter id="shadow" x="-30%" y="-30%" width="160%" height="170%">
        <feDropShadow dx="0" dy="6" stdDeviation="5" flood-color="#001b11" flood-opacity=".65"/>
      </filter>
    </defs>
    <g filter="url(#shadow)">${body}</g>
  </svg>`);
}

await fs.mkdir(outputDirectory, { recursive: true });
for (const [name, body] of Object.entries(icons)) {
  await sharp(svg(body))
    .resize(256, 256, { fit: "contain" })
    .png({ compressionLevel: 9 })
    .toFile(path.join(outputDirectory, `nav_${name}.png`));
}

console.log(`Generated ${Object.keys(icons).length} mobile menu icons in ${outputDirectory}`);
