# Emerald Vault production asset prompts

Mode: built-in `imagegen`, using `C_emerald_vault.png` as the visual reference.

Common constraints for every generation:

- match the approved Emerald Vault / CardMesh art direction;
- front-facing orthographic 2D mobile UI;
- deep near-black emerald enamel and restrained antique gold;
- no baked text, numbers, watermarks, extra objects, or cast shadows;
- one isolated subject (or an exact atlas grid) on uniform `#FF00FF`;
- no magenta in the subject; generous padding for chroma-key removal.

Generated source groups:

1. `top_activity_frame_source.png` — empty wide rounded activity card, subtle embossed texture and double gold border.
2. `bottom_rail_source.png` — empty five-cell navigation rail with subtle separators and gold trim.
3. `play_crest_source.png` — empty symmetrical heraldic play crest with no symbol or text.
4. `icon_atlas_source.png` — 4×2 atlas: reward, chest, levels, collection, shop, settings, play/spade, diamond badge.

The sources were chroma-keyed, trimmed, normalized, and sliced into the Unity-ready files in
`Assets/Art/UI/MainMenuEmeraldVault`.
