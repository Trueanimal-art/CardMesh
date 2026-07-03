# ImageCard Audit Artifacts

This folder preserves source-level checkpoints for the premium ImageCard set.

- `full_set_before_sanitize_source_atlas.png` is the 400-card source atlas captured before the final edge/crop repair pass.
- `full_set_final_source_atlas.png` is the repaired 400-card source atlas, stored as 240x240 transparent cells in filename sort order.
- `final_preview_pages/page_01.png` through `page_14.png` are visual QA sheets matching the runtime admin page order.
- `final_margin_audit.json` is the final technical audit output.

The repaired sprites in `Assets/Resources/ImageCards/Premium` are 240x240 PNGs with each object fitted to a 160px max side. This keeps a stable transparent safety margin so card art does not visually hit or expose cropped source edges in runtime previews.
