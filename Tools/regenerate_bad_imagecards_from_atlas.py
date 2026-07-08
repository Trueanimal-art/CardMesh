#!/usr/bin/env python3
"""Regenerate the known-bad premium ImageCards through a separate atlas.

The script intentionally uses the local deterministic generator instead of
trying to repair the current PNGs. It writes an auditable source atlas, slices
that atlas back into per-card sources, processes them to runtime 240x240 PNGs,
and optionally installs the result over the runtime sprites.
"""
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw


PROJECT_ROOT = Path(__file__).resolve().parents[1]
BAD_MANIFEST = PROJECT_ROOT / "Tools/ImageCardAudit/bad_cards_manifest.json"
RUNTIME_DIR = PROJECT_ROOT / "Assets/Resources/ImageCards/Premium"
WORK_ROOT = PROJECT_ROOT / "Tools/ImageCardAudit/regenerated_bad_cards"
RAW_ALL_DIR = WORK_ROOT / "raw_all"
RAW_BAD_DIR = WORK_ROOT / "raw_bad_from_atlas"
PROCESSED_BAD_DIR = WORK_ROOT / "processed_bad"
SOURCE_ATLAS = WORK_ROOT / "bad_cards_regenerated_source_atlas.png"
PROCESSED_ATLAS = WORK_ROOT / "bad_cards_regenerated_processed_atlas.png"
MANIFEST_OUT = WORK_ROOT / "bad_cards_regenerated_manifest.json"
CELL = 1024
PROCESSED_CELL = 240
COLUMNS = 8


def load_bad_ids() -> list[str]:
    data = json.loads(BAD_MANIFEST.read_text(encoding="utf-8"))
    return [card["id"] for card in data["cards"]]


def run(args: list[str]) -> None:
    subprocess.run(args, cwd=PROJECT_ROOT, check=True)


def build_atlas(source_dir: Path, ids: list[str], atlas_path: Path, cell: int) -> None:
    rows = (len(ids) + COLUMNS - 1) // COLUMNS
    atlas = Image.new("RGBA", (COLUMNS * cell, rows * cell), (0, 0, 0, 0))
    for index, card_id in enumerate(ids):
        source = source_dir / f"{card_id}.png"
        if not source.exists():
            raise FileNotFoundError(source)
        image = Image.open(source).convert("RGBA")
        if image.size != (cell, cell):
            image = image.resize((cell, cell), Image.Resampling.LANCZOS)
        x = (index % COLUMNS) * cell
        y = (index // COLUMNS) * cell
        atlas.alpha_composite(image, (x, y))
    atlas_path.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(atlas_path)


def slice_atlas(atlas_path: Path, ids: list[str], out_dir: Path, cell: int) -> None:
    atlas = Image.open(atlas_path).convert("RGBA")
    out_dir.mkdir(parents=True, exist_ok=True)
    for index, card_id in enumerate(ids):
        x = (index % COLUMNS) * cell
        y = (index // COLUMNS) * cell
        atlas.crop((x, y, x + cell, y + cell)).save(out_dir / f"{card_id}.png")


def build_labeled_atlas(source_dir: Path, ids: list[str], atlas_path: Path) -> None:
    cell_w, cell_h = 170, 188
    rows = (len(ids) + COLUMNS - 1) // COLUMNS
    atlas = Image.new("RGBA", (COLUMNS * cell_w, rows * cell_h), (18, 33, 42, 255))
    draw = ImageDraw.Draw(atlas)
    for index, card_id in enumerate(ids):
        x = (index % COLUMNS) * cell_w
        y = (index // COLUMNS) * cell_h
        draw.rectangle((x + 5, y + 5, x + cell_w - 6, y + cell_h - 6), fill=(245, 243, 235, 255))
        draw.text((x + 10, y + 11), card_id[:20], fill=(55, 50, 42, 255))
        image = Image.open(source_dir / f"{card_id}.png").convert("RGBA")
        image.thumbnail((122, 122), Image.Resampling.LANCZOS)
        atlas.alpha_composite(image, (x + (cell_w - image.width) // 2, y + 38 + (122 - image.height) // 2))
        draw.text((x + 10, y + cell_h - 24), card_id[:20], fill=(120, 113, 104, 255))
    atlas_path.save(atlas_path) if False else atlas.save(atlas_path)


def install_processed(ids: list[str]) -> None:
    for card_id in ids:
        source = PROCESSED_BAD_DIR / f"{card_id}.png"
        destination = RUNTIME_DIR / f"{card_id}.png"
        if not source.exists():
            raise FileNotFoundError(source)
        shutil.copy2(source, destination)


def write_manifest(ids: list[str]) -> None:
    data = {
        "count": len(ids),
        "columns": COLUMNS,
        "sourceCellSize": CELL,
        "processedCellSize": PROCESSED_CELL,
        "sourceAtlas": str(SOURCE_ATLAS.relative_to(PROJECT_ROOT)),
        "processedAtlas": str(PROCESSED_ATLAS.relative_to(PROJECT_ROOT)),
        "runtimeDirectory": str(RUNTIME_DIR.relative_to(PROJECT_ROOT)),
        "cards": [
            {
                "id": card_id,
                "sourceAtlasCell": {
                    "x": (index % COLUMNS) * CELL,
                    "y": (index // COLUMNS) * CELL,
                    "width": CELL,
                    "height": CELL,
                },
                "processedAtlasCell": {
                    "x": (index % COLUMNS) * PROCESSED_CELL,
                    "y": (index // COLUMNS) * PROCESSED_CELL,
                    "width": PROCESSED_CELL,
                    "height": PROCESSED_CELL,
                },
                "runtimeFile": str((RUNTIME_DIR / f"{card_id}.png").relative_to(PROJECT_ROOT)),
            }
            for index, card_id in enumerate(ids)
        ],
    }
    MANIFEST_OUT.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--install", action="store_true")
    parser.add_argument("--clean", action="store_true")
    args = parser.parse_args()

    if args.clean and WORK_ROOT.exists():
        shutil.rmtree(WORK_ROOT)
    WORK_ROOT.mkdir(parents=True, exist_ok=True)

    ids = load_bad_ids()
    run([sys.executable, "Tools/generate_procedural_imagecards.py", "--out-dir", str(RAW_ALL_DIR), "--clean"])
    build_atlas(RAW_ALL_DIR, ids, SOURCE_ATLAS, CELL)
    slice_atlas(SOURCE_ATLAS, ids, RAW_BAD_DIR, CELL)
    run(
        [
            sys.executable,
            "Tools/regenerate_imagecard_assets.py",
            "process",
            "--input-dir",
            str(RAW_BAD_DIR),
            "--output-dir",
            str(PROCESSED_BAD_DIR),
            "--only",
            *ids,
        ]
    )
    build_atlas(PROCESSED_BAD_DIR, ids, PROCESSED_ATLAS, PROCESSED_CELL)
    build_labeled_atlas(PROCESSED_BAD_DIR, ids, WORK_ROOT / "bad_cards_regenerated_review_atlas.png")
    write_manifest(ids)
    if args.install:
        install_processed(ids)
    print(f"regenerated_bad_count={len(ids)} installed={args.install} work_root={WORK_ROOT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
