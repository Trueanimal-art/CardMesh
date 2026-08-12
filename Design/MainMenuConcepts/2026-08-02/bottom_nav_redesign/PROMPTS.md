# CardMesh bottom navigation redesign

Mode: built-in `image_gen`, followed by local magenta chroma-key removal and
deterministic Pillow composition for exact Russian labels.

## Shared constraints

- Approved Emerald Vault material language: dark emerald enamel, restrained
  antique gold relief, warm ivory accents.
- Exactly five actions: `УРОВНИ`, `КОЛЛЕКЦИЯ`, `ИГРАТЬ`, `МАГАЗИН`,
  `НАСТРОЙКИ`.
- No text baked into generated raster sources. Labels are composed separately
  so Russian spelling, fit and typography remain exact.
- No card-suit emblem on the primary CTA. The CardMesh signature is linked
  cards plus connection nodes.
- No copied UI from the competitor reference, no casino aesthetic, no thin
  placeholder line art.
- The generated sources use flat `#ff00ff` chroma-key backdrops.

## A — Royal Command Bar

Low unified emerald rail with four sculpted secondary actions and a raised
central crest. The central emblem is two interlocking cards connected by three
nodes. The crest has a solid interior and a reserved label band.

## B — Illuminated Card Dock

Four individual arched card-tab stations inside a shallow rail. The central
lower-profile portal uses three connected ivory cards and a subtle secondary
play triangle. Secondary icons use bolder, more readable card-game metaphors.

## C — CardMesh Command Deck

Two-tier mobile layout. A wide, strong `ИГРАТЬ` plaque floats above four broad
equal lower tabs. It maximizes the width available for long Russian labels and
uses a linked-card mesh as the primary brand mark.
