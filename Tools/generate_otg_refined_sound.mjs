import fs from "node:fs";
import path from "node:path";

const OUT = "/Users/vnm/Documents/CardMesh/Artifacts/OneTapGarageSplash/RefinedB";
const sampleRate = 48000;
const duration = 2.05;
const frames = Math.ceil(sampleRate * duration);
const channels = 2;
const pcm = Buffer.alloc(frames * channels * 2);
let seed = 170319;
let noiseState = 0;

const clamp = (v) => Math.max(-1, Math.min(1, v));
const env = (t, start, attack, hold, release) => {
  const x = t - start;
  if (x < 0 || x > attack + hold + release) return 0;
  if (x < attack) return x / attack;
  if (x < attack + hold) return 1;
  return 1 - (x - attack - hold) / release;
};
const rand = () => {
  seed = (seed * 1664525 + 1013904223) >>> 0;
  return (seed / 0xffffffff) * 2 - 1;
};

for (let i = 0; i < frames; i++) {
  const t = i / sampleRate;

  const tapE = env(t, .39, .006, .018, .105);
  const tap = tapE * (
    Math.sin(2 * Math.PI * 178 * (t - .39)) * .115 +
    Math.sin(2 * Math.PI * 890 * (t - .39)) * .026
  );

  const revealE = env(t, .48, .045, .14, .24);
  const revealTime = Math.max(0, t - .48);
  const revealFreq = 330 + 190 * Math.min(1, revealTime / .38);
  const reveal = revealE * (
    Math.sin(2 * Math.PI * revealFreq * revealTime) * .040 +
    Math.sin(2 * Math.PI * revealFreq * 2.01 * revealTime) * .014
  );

  const sweepE = env(t, 1.25, .08, .35, .34);
  const sweepP = Math.max(0, Math.min(1, (t - 1.25) / .77));
  noiseState = noiseState * .94 + rand() * .06;
  const sweepTone = Math.sin(2 * Math.PI * (410 + 760 * sweepP) * Math.max(0, t - 1.25));
  const sweep = sweepE * (noiseState * .072 + sweepTone * .018);
  const pan = sweepP * 2 - 1;
  const left = tap + reveal + sweep * Math.sqrt((1 - pan) * .5);
  const right = tap * .98 + reveal * 1.02 + sweep * Math.sqrt((1 + pan) * .5);

  pcm.writeInt16LE(Math.round(clamp(left) * 32767), (i * 2) * 2);
  pcm.writeInt16LE(Math.round(clamp(right) * 32767), (i * 2 + 1) * 2);
}

const header = Buffer.alloc(44);
header.write("RIFF", 0);
header.writeUInt32LE(36 + pcm.length, 4);
header.write("WAVE", 8);
header.write("fmt ", 12);
header.writeUInt32LE(16, 16);
header.writeUInt16LE(1, 20);
header.writeUInt16LE(channels, 22);
header.writeUInt32LE(sampleRate, 24);
header.writeUInt32LE(sampleRate * channels * 2, 28);
header.writeUInt16LE(channels * 2, 32);
header.writeUInt16LE(16, 34);
header.write("data", 36);
header.writeUInt32LE(pcm.length, 40);

fs.mkdirSync(OUT, { recursive: true });
fs.writeFileSync(path.join(OUT, "Refined_B_Sound_Preview.wav"), Buffer.concat([header, pcm]));
console.log("Generated original three-layer sound preview.");
