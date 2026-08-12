import path from "node:path";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const dependencyRoot = process.env.CARDMESH_NODE_MODULES;
if (!dependencyRoot) {
  throw new Error("CARDMESH_NODE_MODULES must point to the bundled node_modules.");
}

const sharp = require(path.join(dependencyRoot, "sharp"));
const sourceDirectory =
  process.argv[2] ?? "Design/MainMenuConcepts/2026-08-01";
const outputPath =
  process.argv[3] ?? path.join(sourceDirectory, "ABC_comparison.png");

const variants = [
  ["A", "ROYAL EMERALD", "A_royal_emerald_enamel.png"],
  ["B", "CARD RELIQUARY", "B_card_reliquary.png"],
  ["C", "EMERALD VAULT", "C_emerald_vault.png"],
];

const cardWidth = 450;
const cardHeight = 758;
const gap = 24;
const margin = 32;
const headerHeight = 76;
const canvasWidth = (margin * 2) + (cardWidth * variants.length) + (gap * 2);
const canvasHeight = margin + headerHeight + cardHeight + margin;

const composites = [];
for (let index = 0; index < variants.length; index += 1) {
  const [letter, title, filename] = variants[index];
  const left = margin + (index * (cardWidth + gap));
  const image = await sharp(path.join(sourceDirectory, filename))
    .resize(cardWidth, cardHeight, { fit: "cover", position: "top" })
    .png()
    .toBuffer();
  composites.push({ input: image, left, top: margin + headerHeight });
  composites.push({
    input: Buffer.from(`
      <svg width="${cardWidth}" height="${headerHeight}">
        <rect width="100%" height="100%" rx="18" fill="#06261b"/>
        <rect x="2" y="2" width="${cardWidth - 4}" height="${headerHeight - 4}"
          rx="16" fill="none" stroke="#d7a93d" stroke-width="3"/>
        <circle cx="40" cy="38" r="25" fill="#b88620"/>
        <text x="40" y="47" text-anchor="middle" font-family="Arial"
          font-size="29" font-weight="700" fill="#fff2c8">${letter}</text>
        <text x="82" y="47" font-family="Arial" font-size="25"
          font-weight="700" fill="#fff2c8">${title}</text>
      </svg>`),
    left,
    top: margin,
  });
}

await sharp({
  create: {
    width: canvasWidth,
    height: canvasHeight,
    channels: 4,
    background: "#03120d",
  },
})
  .composite(composites)
  .png()
  .toFile(outputPath);

console.log(outputPath);
