# Regenerated Bad ImageCards

This folder contains the replacement batch for the 142 ImageCards listed in
`Tools/ImageCardAudit/bad_cards_manifest.json`.

Pipeline:

1. `raw_all/` was generated from scratch by `Tools/generate_procedural_imagecards.py`.
2. `bad_cards_regenerated_source_atlas.png` packs only the 142 bad card sources in
   1024x1024 transparent cells.
3. `raw_bad_from_atlas/` was sliced back from that source atlas.
4. `processed_bad/` contains the final 240x240 transparent runtime sprites.
5. `bad_cards_regenerated_processed_atlas.png` packs the final 240x240 outputs.
6. `bad_cards_regenerated_review_atlas.png` is a labeled visual QA sheet.
7. `runtime_preview_pages/` shows the full Runtime Dev Tools order after installing
   these replacements into `Assets/Resources/ImageCards/Premium`.

The runtime replacements were installed only for the 142 known-bad card ids.
