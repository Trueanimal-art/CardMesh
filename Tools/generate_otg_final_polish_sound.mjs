import fs from "node:fs";
import path from "node:path";

const OUT = "/Users/vnm/Documents/Solitaire/Artifacts/OneTapGarageSplash/FinalPolishB";
const sampleRate = 48000;
const duration = 2.10;
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

  const tapE = env(t, .365, .006, .020, .115);
  const tap = tapE * (
    Math.sin(2 * Math.PI * 220 * (t - .365)) * .125 +
    Math.sin(2 * Math.PI * 1120 * (t - .365)) * .029
  );

  const revealE = env(t, .49, .042, .16, .25);
  const revealTime = Math.max(0, t - .49);
  const revealFreq = 390 + 240 * Math.min(1, revealTime / .42);
  const reveal = revealE * (
    Math.sin(2 * Math.PI * revealFreq * revealTime) * .040 +
    Math.sin(2 * Math.PI * revealFreq * 2.01 * revealTime) * .014
  );

  const sweepE = env(t, 1.34, .08, .38, .34);
  const sweepP = Math.max(0, Math.min(1, (t - 1.34) / .76));
  noiseState = noiseState * .92 + rand() * .08;
  const sweepTone = Math.sin(2 * Math.PI * (410 + 760 * sweepP) * Math.max(0, t - 1.34));
  const goldCatch = env(t, 1.64, .018, .024, .11) * Math.sin(2 * Math.PI * 720 * Math.max(0, t - 1.64)) * .022;
  const sweep = sweepE * (noiseState * .078 + sweepTone * .018) + goldCatch;
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
fs.writeFileSync(path.join(OUT, "Final_Polish_B_Sound_Preview_Raw.wav"), Buffer.concat([header, pcm]));
console.log("Generated original phone-safe three-layer sound preview.");
