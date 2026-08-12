# Variant I — Emerald Vault Capsule Dock

Built-in `image_gen` prompt:

```text
Use case: ui-mockup
Asset type: production-oriented Unity mobile game UI asset sheet for the lower Main Menu, Emerald Vault fantasy card-game style
Primary request: create one cohesive set of exactly five separate UI controls with NO shared navigation rail: one wide primary PLAY CTA plaque centered in the upper half, and below it one row of exactly four independent horizontal mini-plaques/capsules with clearly visible gaps. The four mini-plaques must be visually identical in size and family but each has a different left-side pictogram: 1) checklist/levels, 2) open collection book, 3) elegant shopping bag, 4) settings gear. Each mini-plaque must have a wide clean empty label zone to the right of its pictogram, sized for long Russian labels. The wide CTA must have a prominent spade-shaped medallion containing a triangular play symbol plus a large clean empty text zone. No text anywhere.
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background, uniform edge to edge, for background removal
Style/medium: polished premium 2D game UI, hand-painted yet production-clean, dark emerald enamel, restrained antique gold and warm brass trim, subtle inner highlights, crisp silhouette, refined CardMesh / Emerald Vault visual language, elegant rather than casino-like
Composition/framing: landscape asset sheet, all five controls fully visible and separated by generous #ff00ff padding; wide CTA at top center; four horizontal capsules in a single straight row underneath; front orthographic view, no perspective; strict symmetry; consistent lighting and scale
Lighting/mood: subtle soft top highlight contained within each control, no cast shadows outside silhouettes
Color palette: near-black emerald, deep forest green, muted aged gold, tiny warm ivory accents; do not use magenta inside subjects
Materials/textures: smooth enamel, lightly engraved brass, restrained bevels, no excessive filigree, no gritty noise
Constraints: exactly five controls; exactly four bottom capsules; no connected background plate; no common rail; no words, letters, numbers, typography, watermark, logo, mock device, scene, floor, particles, glow outside the shapes, or drop shadows; preserve large blank text zones; crisp antialiased edges; opaque controls suitable for chroma key extraction
Background-removal requirement: background must be one perfectly uniform flat #ff00ff with no gradient, texture, shadows, lighting variation, reflection, or contact shadow. Keep every control separated from the background with crisp edges and generous padding. Do not use #ff00ff anywhere in the controls.
```

Post-processing:

- Auto-key sampled from border: `#fa03f9`
- Soft matte and despill enabled
- Transparent threshold: `12`
- Opaque threshold: `220`
- Final canvas: `1691 × 930`
- Transparent pixels: `1,053,530 / 1,572,630`
- Partially transparent edge pixels: `8,012`

The generated sheet is a visual-direction asset, not a production Unity import. Text remains intentionally absent and should be authored with TMP.
