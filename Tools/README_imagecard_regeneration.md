# ImageCard regeneration pipeline

This document replaces the failed cleanup/normalization attempts.

## Current state

Previous approaches failed because they tried to repair already-bad generated PNGs:

- global downscale / transparent-margin normalization made icons smaller and did not solve semantic cropping;
- chroma/alpha cleanup removed some artifacts but the user still found many cards with cropped bases, shadows, or incomplete objects in the first 3 Runtime Dev Tools pages.

The correct next step is to regenerate the card art, not keep repairing the existing PNGs.

## Hard requirements for regenerated cards

For every `cardId` in `Assets/Resources/Data/cards.json`:

- one single recognizable object or simple scene matching the Russian title;
- soft 3D casual mobile puzzle-game icon style;
- full object visible, no crop, no cut-off shadow, no cut-off base;
- object centered in a square canvas;
- 18-22% safe empty margin on all sides;
- no text, no letters, no labels, no UI badges;
- no card frame/background; output must become transparent PNG;
- final runtime file: `Assets/Resources/ImageCards/Premium/<cardId>.png`;
- final size: `240x240` RGBA PNG;
- Unity import as single Sprite must remain unchanged via existing `.meta` files.

## Generation strategy

Preferred generator output for each card:

- 1024x1024 or larger square image;
- solid `#00FF00` chroma-key background, or true alpha if provider supports it;
- full object and full soft shadow inside the frame;
- object uses ~60-65% of the canvas, never reaches edge.

After generation:

1. Remove chroma-key background or preserve alpha.
2. Compute alpha bbox.
3. Reject automatically if:
   - any non-transparent pixels touch the edge;
   - minimum final transparent margin would be under 24 px on 240x240 canvas;
   - object bbox max side exceeds 170 px after fitting;
   - object bbox max side is under 105 px;
   - image appears empty.
4. Fit to 240x240 without cropping.
5. Preserve existing `.meta` files.
6. Build Runtime Dev Tools-like contact pages for all 14 pages.
7. Human-review every page before considering complete.

## Infrastructure blocker observed

At the time this file was written:

- built-in Hermes image generation failed because `FAL_KEY` was not set;
- no local ComfyUI server was available at `127.0.0.1:8188`;
- `comfy` CLI was not available on PATH.

So the actual regeneration cannot be completed in this environment until one of these is provided:

1. set `FAL_KEY` / configure Hermes image generation provider;
2. start/configure local ComfyUI with a suitable model;
3. provide externally generated source images/sheets.

## Pilot set before full batch

Before replacing all 400 cards, regenerate and review this pilot set from the user's screenshots:

- `art_paint`
- `books_cover`
- `breakfast_butter`
- `breakfast_cereal`
- `breakfast_porridge`
- `breakfast_toast`
- `breakfast_yogurt`
- `camping_fire`
- `cinema_film`
- `cinema_ticket`
- `city_traffic`
- `colors_green`
- `colors_orange`
- `colors_pink`
- `colors_red`
- `colors_white`
- `colors_yellow`

Only after these pass visual review should the process scale to all 400.
