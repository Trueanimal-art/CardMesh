#!/usr/bin/env python3
"""Prepare and validate regenerated premium ImageCard assets.

This script deliberately does not call an image provider. It creates a prompt
manifest for all 400 cards and processes externally generated images once they
exist. Use it after enabling FAL/ComfyUI or after receiving generated source
PNGs.
"""
from __future__ import annotations

import argparse
import json
import re
import shutil
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw

PROJECT_ROOT = Path(__file__).resolve().parents[1]
CARDS_JSON = PROJECT_ROOT / "Assets/Resources/Data/cards.json"
RUNTIME_DIR = PROJECT_ROOT / "Assets/Resources/ImageCards/Premium"
CANVAS_SIZE = 240
MIN_MARGIN = 24
TARGET_MAX_OBJECT = 164
TARGET_MIN_OBJECT = 105
CHROMA = (0, 255, 0)

ENGLISH_HINTS = {
    "art_paint": "bundle of colorful paint tubes with one paint brush",
    "books_cover": "closed brown book cover with a small bookmark ribbon",
    "breakfast_butter": "yellow butter cube on a small plate",
    "breakfast_cereal": "bowl of colorful breakfast cereal",
    "breakfast_porridge": "bowl of oatmeal porridge with berries",
    "breakfast_toast": "single golden-brown toast slice",
    "breakfast_yogurt": "small yogurt cup with pink yogurt and berries",
    "camping_fire": "campfire with stones and complete flames",
    "cinema_film": "film reel with a short film strip, full reel visible",
    "cinema_ticket": "orange cinema ticket with stars, full ticket visible",
    "city_traffic": "small traffic light with two tiny cars, complete base visible",
    "colors_green": "glossy green paint drop, full teardrop visible",
    "colors_orange": "glossy orange paint drop, full teardrop visible",
    "colors_pink": "glossy pink paint drop, full teardrop visible",
    "colors_red": "glossy red paint drop, full teardrop visible",
    "colors_white": "glossy white pearl paint drop, full teardrop visible",
    "colors_yellow": "glossy yellow paint drop, full teardrop visible",
}

BASE_NEGATIVE = (
    "cropped, cut off, truncated, close-up, zoomed-in, partial object, "
    "object touching edge, shadow cut off, base cut off, text, letters, label, "
    "logo, watermark, UI frame, card border, multiple unrelated objects"
)


def load_cards() -> list[dict[str, str]]:
    data = json.loads(CARDS_JSON.read_text(encoding="utf-8"))
    cards: list[dict[str, str]] = []
    for category in data["goldCards"]:
        for card in category["cards"]:
            cards.append(
                {
                    "id": card["id"],
                    "title": card["title"],
                    "category": category["id"],
                    "categoryTitle": category["title"],
                    "englishHint": ENGLISH_HINTS.get(card["id"], fallback_hint(card["id"], card["title"])),
                }
            )
    return cards


def fallback_hint(card_id: str, title: str) -> str:
    words = card_id.split("_", 1)
    concept = words[1] if len(words) > 1 else card_id
    concept = concept.replace("_", " ")
    return f"simple recognizable icon representing {concept} ({title})"


def prompt_for(card: dict[str, str]) -> str:
    return (
        f"A single complete soft 3D casual mobile puzzle-game icon of {card['englishHint']}. "
        "Centered on a square canvas. Full object visible. Full base and full soft oval shadow visible. "
        "No part cropped, no part touching the image edge. Rounded toy-like commercial game asset style, "
        "clean high quality, consistent lighting, slight top-left highlight, soft ambient occlusion. "
        "Object occupies about 60 to 65 percent of the canvas with 20 percent empty safe margin on every side. "
        "No text, no letters, no label, no logo, no watermark, no UI frame. "
        "Solid pure chroma green background #00FF00, flat color, no gradient."
    )


def write_manifest(path: Path, pilot_only: bool) -> None:
    cards = load_cards()
    if pilot_only:
        pilot = set(ENGLISH_HINTS)
        cards = [card for card in cards if card["id"] in pilot]
    manifest = []
    for card in cards:
        manifest.append(
            {
                **card,
                "prompt": prompt_for(card),
                "negativePrompt": BASE_NEGATIVE,
                "outputFile": f"{card['id']}.png",
            }
        )
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"wrote {len(manifest)} prompts to {path}")


def is_chroma_pixel(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, a = pixel
    if a == 0:
        return True
    # tolerate antialiasing around the requested flat green background
    return g >= 160 and r <= 120 and b <= 120 and g - max(r, b) >= 55


def has_transparent_border(image: Image.Image) -> bool:
    """Generated PNGs with real alpha should not have green pixels stripped."""
    image = image.convert("RGBA")
    alpha = image.getchannel("A")
    width, height = image.size
    border = []
    for x in range(width):
        border.append(alpha.getpixel((x, 0)))
        border.append(alpha.getpixel((x, height - 1)))
    for y in range(height):
        border.append(alpha.getpixel((0, y)))
        border.append(alpha.getpixel((width - 1, y)))
    transparent = sum(1 for value in border if value <= 8)
    return transparent >= max(1, int(len(border) * 0.80))


def remove_chroma(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    if has_transparent_border(image):
        return image

    width, height = image.size
    pixels = image.load()
    visited: set[tuple[int, int]] = set()
    stack: list[tuple[int, int]] = []
    for x in range(width):
        stack.append((x, 0))
        stack.append((x, height - 1))
    for y in range(height):
        stack.append((0, y))
        stack.append((width - 1, y))

    while stack:
        x, y = stack.pop()
        if (x, y) in visited or x < 0 or y < 0 or x >= width or y >= height:
            continue
        pixel = pixels[x, y]
        if not is_chroma_pixel(pixel):
            continue
        visited.add((x, y))
        pixels[x, y] = (0, 0, 0, 0)
        stack.extend(((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))
    return image


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int] | None:
    return image.getchannel("A").getbbox()


def fit_to_canvas(image: Image.Image) -> tuple[Image.Image, dict[str, object]]:
    image = remove_chroma(image)
    bbox = alpha_bbox(image)
    if bbox is None:
        raise ValueError("empty alpha after chroma removal")
    cropped = image.crop(bbox)
    w, h = cropped.size
    scale = min(TARGET_MAX_OBJECT / max(w, h), 1.0)
    target_w = max(1, round(w * scale))
    target_h = max(1, round(h * scale))
    resized = cropped.resize((target_w, target_h), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    x = (CANVAS_SIZE - target_w) // 2
    y = (CANVAS_SIZE - target_h) // 2
    canvas.alpha_composite(resized, (x, y))
    final_bbox = alpha_bbox(canvas)
    if final_bbox is None:
        raise ValueError("empty alpha after fit")
    left, top, right, bottom = final_bbox
    margins = [left, top, CANVAS_SIZE - right, CANVAS_SIZE - bottom]
    max_side = max(right - left, bottom - top)
    min_side = min(right - left, bottom - top)
    report = {
        "sourceBbox": list(bbox),
        "finalBbox": list(final_bbox),
        "margins": margins,
        "maxSide": max_side,
        "minSide": min_side,
        "warnings": [],
    }
    if min(margins) < MIN_MARGIN:
        report["warnings"].append(f"margin<{MIN_MARGIN}: {margins}")
    if max_side > TARGET_MAX_OBJECT:
        report["warnings"].append(f"maxSide>{TARGET_MAX_OBJECT}: {max_side}")
    if max_side < TARGET_MIN_OBJECT:
        report["warnings"].append(f"maxSide<{TARGET_MIN_OBJECT}: {max_side}")
    return canvas, report


def process_generated(input_dir: Path, output_dir: Path, only: set[str] | None) -> None:
    cards = load_cards()
    output_dir.mkdir(parents=True, exist_ok=True)
    report = []
    for card in cards:
        card_id = card["id"]
        if only and card_id not in only:
            continue
        source = find_source(input_dir, card_id)
        if source is None:
            report.append({"id": card_id, "status": "missing"})
            continue
        try:
            final, item_report = fit_to_canvas(Image.open(source))
            destination = output_dir / f"{card_id}.png"
            final.save(destination)
            meta_source = RUNTIME_DIR / f"{card_id}.png.meta"
            meta_dest = output_dir / f"{card_id}.png.meta"
            if meta_source.exists() and not meta_dest.exists():
                shutil.copy2(meta_source, meta_dest)
            report.append({"id": card_id, "status": "processed", "source": str(source), **item_report})
        except Exception as exc:  # noqa: BLE001 - report every bad generated file
            report.append({"id": card_id, "status": "error", "source": str(source), "error": str(exc)})
    (output_dir / "regeneration_process_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(f"processed report: {output_dir / 'regeneration_process_report.json'}")


def find_source(input_dir: Path, card_id: str) -> Path | None:
    candidates = [input_dir / f"{card_id}.png", input_dir / f"{card_id}.jpg", input_dir / f"{card_id}.jpeg"]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    regex = re.compile(rf"(^|[^a-zA-Z0-9]){re.escape(card_id)}([^a-zA-Z0-9]|$)")
    for path in sorted(input_dir.glob("*")):
        if path.suffix.lower() not in {".png", ".jpg", ".jpeg", ".webp"}:
            continue
        if regex.search(path.stem):
            return path
    return None


def build_pages(root: Path, out_dir: Path) -> None:
    cards = load_cards()
    files = [root / f"{card['id']}.png" for card in cards if (root / f"{card['id']}.png").exists()]
    out_dir.mkdir(parents=True, exist_ok=True)
    cell_w, cell_h = 132, 166
    columns = 6
    for page in range((len(files) + 29) // 30):
        subset = files[page * 30 : (page + 1) * 30]
        rows = (len(subset) + columns - 1) // columns
        sheet = Image.new("RGBA", (columns * cell_w, rows * cell_h), (245, 244, 238, 255))
        draw = ImageDraw.Draw(sheet)
        for index, path in enumerate(subset):
            x = (index % columns) * cell_w
            y = (index // columns) * cell_h
            image = Image.open(path).convert("RGBA")
            draw.rectangle((x, y, x + cell_w - 1, y + cell_h - 1), outline=(0, 0, 0, 45), width=1)
            draw.text((x + 5, y + 8), path.stem[:18], fill=(45, 40, 35, 255))
            size = 99
            resized = image.resize((size, size), Image.Resampling.LANCZOS)
            icon_x = x + round(cell_w * 0.06) + (round(cell_w * 0.88) - size) // 2
            icon_y = y + round(cell_h * 0.20) + (round(cell_h * 0.60) - size) // 2
            sheet.alpha_composite(resized, (icon_x, icon_y))
            draw.text((x + 5, y + cell_h - 17), path.stem[:18], fill=(120, 115, 105, 255))
        sheet.save(out_dir / f"page_{page + 1:02d}.png")
    print(f"wrote Runtime-like pages to {out_dir}")


def main(argv: Iterable[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)

    manifest = sub.add_parser("manifest")
    manifest.add_argument("--out", default="/tmp/imagecard_regeneration_prompts.json")
    manifest.add_argument("--pilot", action="store_true")

    process = sub.add_parser("process")
    process.add_argument("--input-dir", required=True)
    process.add_argument("--output-dir", default="/tmp/imagecards_regenerated_processed")
    process.add_argument("--only", nargs="*")

    pages = sub.add_parser("pages")
    pages.add_argument("--root", default="/tmp/imagecards_regenerated_processed")
    pages.add_argument("--out-dir", default="/tmp/imagecard_regenerated_pages")

    args = parser.parse_args(argv)
    if args.command == "manifest":
        write_manifest(Path(args.out), args.pilot)
    elif args.command == "process":
        process_generated(Path(args.input_dir), Path(args.output_dir), set(args.only) if args.only else None)
    elif args.command == "pages":
        build_pages(Path(args.root), Path(args.out_dir))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
