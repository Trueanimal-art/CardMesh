#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from collections import deque
from pathlib import Path
from typing import Iterable

from normalize_imagecard_assets import (
    CANVAS_SIZE,
    alpha_bbox,
    crop_to_bbox,
    remove_tiny_components,
    resize_premultiplied,
    strip_low_alpha,
)
from process_imagecard_sheet import read_png_rgba, write_png_rgba


ROOT = Path(__file__).resolve().parents[1]
CARDS_JSON = ROOT / "Assets/Resources/Data/cards.json"
DEFAULT_SOURCE_ATLAS = ROOT / "Tools/ImageCardAudit/full_set_final_source_atlas.png"
RUNTIME_DIR = ROOT / "Assets/Resources/ImageCards/Premium"
DEFAULT_REPORT_DIR = ROOT / "Tools/ImageCardAudit/arrow_marked_source_repair"

TARGET_IDS = [
    "art_color",
    "art_frame",
    "art_paint",
    "body_smile",
    "books_cover",
    "breakfast_butter",
    "breakfast_cereal",
    "breakfast_porridge",
    "breakfast_toast",
    "breakfast_yogurt",
    "camping_backpack",
    "camping_lantern",
    "camping_tent",
    "cinema_ticket",
    "city_park",
    "clothes_glove",
    "clothes_scarf",
    "colors_blue",
    "colors_green",
    "colors_orange",
    "colors_pink",
    "colors_red",
    "dessert_cream",
    "dessert_honey",
    "dessert_icecream",
    "dessert_pie",
    "dessert_waffle",
    "emotions_joy",
    "emotions_warmth",
    "family_dinner",
    "family_home",
    "family_mother",
]


def load_card_ids() -> list[str]:
    with CARDS_JSON.open("r", encoding="utf-8") as handle:
        data = json.load(handle)
    ids = [card["id"] for set_data in data["goldCards"] for card in set_data["cards"]]
    if len(ids) != 400 or len(set(ids)) != 400:
        raise RuntimeError(f"Expected 400 unique cards in {CARDS_JSON}, got {len(ids)} / {len(set(ids))}")
    return sorted(ids)


def load_targets(path: Path | None) -> list[str]:
    if path is None:
        return TARGET_IDS
    targets = [line.strip() for line in path.read_text(encoding="utf-8").splitlines()]
    return [target for target in targets if target and not target.startswith("#")]


def crop_source_cell(
    atlas_width: int,
    pixels: list[tuple[int, int, int, int]],
    index: int,
) -> tuple[int, int, list[tuple[int, int, int, int]]]:
    cell_size = 240
    columns = 20
    x0 = (index % columns) * cell_size
    y0 = (index // columns) * cell_size
    out: list[tuple[int, int, int, int]] = []
    for y in range(y0, y0 + cell_size):
        for x in range(x0, x0 + cell_size):
            out.append(pixels[(y * atlas_width) + x])
    return cell_size, cell_size, out


def remove_candidate_components(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    candidates: set[int],
    max_area: int,
    max_span: int,
) -> tuple[list[tuple[int, int, int, int]], int]:
    seen: set[int] = set()
    remove: set[int] = set()
    removed = 0
    for start in list(candidates):
        if start in seen:
            continue
        queue = deque([start])
        seen.add(start)
        component: list[int] = []
        min_x = width
        min_y = height
        max_x = -1
        max_y = -1
        while queue:
            index = queue.popleft()
            component.append(index)
            x = index % width
            y = index // width
            min_x = min(min_x, x)
            min_y = min(min_y, y)
            max_x = max(max_x, x)
            max_y = max(max_y, y)
            for ny in (y - 1, y, y + 1):
                for nx in (x - 1, x, x + 1):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height:
                        continue
                    neighbor = (ny * width) + nx
                    if neighbor in seen or neighbor not in candidates:
                        continue
                    seen.add(neighbor)
                    queue.append(neighbor)

        span = max(max_x - min_x + 1, max_y - min_y + 1)
        if len(component) <= max_area or span <= max_span:
            remove.update(component)
            removed += 1

    if not remove:
        return pixels, 0
    output = list(pixels)
    for index in remove:
        output[index] = (0, 0, 0, 0)
    return output, removed


def remove_chroma_fringe(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    threshold: int,
) -> tuple[list[tuple[int, int, int, int]], int]:
    bbox = alpha_bbox(width, height, pixels, threshold)
    if bbox is None:
        return pixels, 0
    left, top, right, bottom = bbox
    object_height = max(1, bottom - top)
    candidates: set[int] = set()
    for y in range(height):
        for x in range(width):
            index = (y * width) + x
            red, green, blue, alpha = pixels[index]
            if alpha <= threshold:
                continue
            near_edge = (
                x - left <= 5
                or right - 1 - x <= 5
                or bottom - 1 - y <= 8
                or y >= top + int(object_height * 0.62)
            )
            if not near_edge:
                continue
            is_green_key = green > 125 and green > red * 1.28 and green > blue * 1.18 and (red < 120 or blue < 120)
            is_magenta_key = red > 135 and blue > 120 and green < 115 and abs(red - blue) < 115
            if is_green_key or is_magenta_key:
                candidates.add(index)

    if not candidates:
        return pixels, 0
    return remove_candidate_components(width, height, pixels, candidates, max_area=650, max_span=28)


def normalize_from_cell(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    target_max: int,
    alpha_threshold: int,
) -> tuple[list[tuple[int, int, int, int]], dict[str, object]]:
    cleaned = strip_low_alpha(pixels, alpha_threshold)
    cleaned, tiny_before = remove_tiny_components(width, height, cleaned, alpha_threshold, max_area=72)
    cleaned, fringe_components = remove_chroma_fringe(width, height, cleaned, alpha_threshold)
    cleaned, tiny_after = remove_tiny_components(width, height, cleaned, alpha_threshold, max_area=32)
    bbox = alpha_bbox(width, height, cleaned, alpha_threshold)
    if bbox is None:
        raise RuntimeError("source cell became empty after cleanup")

    crop_w, crop_h, cropped = crop_to_bbox(width, cleaned, bbox)
    scale = target_max / max(crop_w, crop_h)
    scaled_w = max(1, round(crop_w * scale))
    scaled_h = max(1, round(crop_h * scale))
    scaled = resize_premultiplied(crop_w, crop_h, cropped, scaled_w, scaled_h)

    canvas = [(0, 0, 0, 0) for _ in range(CANVAS_SIZE * CANVAS_SIZE)]
    x0 = (CANVAS_SIZE - scaled_w) // 2
    y0 = (CANVAS_SIZE - scaled_h) // 2
    for y in range(scaled_h):
        for x in range(scaled_w):
            canvas[((y0 + y) * CANVAS_SIZE) + x0 + x] = scaled[(y * scaled_w) + x]

    final_bbox = alpha_bbox(CANVAS_SIZE, CANVAS_SIZE, canvas, alpha_threshold)
    margins = None
    if final_bbox is not None:
        left, top, right, bottom = final_bbox
        margins = [left, top, CANVAS_SIZE - right, CANVAS_SIZE - bottom]

    return canvas, {
        "sourceBbox": list(bbox),
        "scaledSize": [scaled_w, scaled_h],
        "finalBbox": list(final_bbox) if final_bbox else None,
        "finalMargins": margins,
        "removedTinyComponentsBefore": tiny_before,
        "removedFringeComponents": fringe_components,
        "removedTinyComponentsAfter": tiny_after,
    }


def load_source_image(
    card_id: str,
    source_atlas: Path,
    atlas_width: int,
    atlas_pixels: list[tuple[int, int, int, int]],
    source_index: int,
) -> tuple[int, int, list[tuple[int, int, int, int]], str]:
    cell_w, cell_h, cell_pixels = crop_source_cell(atlas_width, atlas_pixels, source_index)
    return cell_w, cell_h, cell_pixels, f"{source_atlas}#{source_index}"


def paste_tile(
    canvas: list[tuple[int, int, int, int]],
    canvas_w: int,
    tile_w: int,
    tile_h: int,
    tile: list[tuple[int, int, int, int]],
    x0: int,
    y0: int,
) -> None:
    for y in range(tile_h):
        for x in range(tile_w):
            canvas[((y0 + y) * canvas_w) + x0 + x] = tile[(y * tile_w) + x]


def build_contact_sheet(
    rows: list[tuple[str, list[tuple[int, int, int, int]], list[tuple[int, int, int, int]]]],
    output: Path,
) -> None:
    columns = 4
    tile = CANVAS_SIZE
    gap = 16
    width = (columns * tile * 2) + (columns - 1) * gap
    height = ((len(rows) + columns - 1) // columns) * tile
    height += (((len(rows) + columns - 1) // columns) - 1) * gap
    sheet = [(245, 242, 232, 255) for _ in range(width * height)]
    for i, (_, before, after) in enumerate(rows):
        col = i % columns
        row = i // columns
        x = col * ((tile * 2) + gap)
        y = row * (tile + gap)
        paste_tile(sheet, width, tile, tile, before, x, y)
        paste_tile(sheet, width, tile, tile, after, x + tile, y)
    write_png_rgba(output, width, height, sheet)


def build_repaired_atlas(rows: list[tuple[str, list[tuple[int, int, int, int]]]], output: Path) -> None:
    columns = 8
    tile = CANVAS_SIZE
    gap = 10
    width = columns * tile + (columns - 1) * gap
    height = ((len(rows) + columns - 1) // columns) * tile
    height += (((len(rows) + columns - 1) // columns) - 1) * gap
    sheet = [(245, 242, 232, 255) for _ in range(width * height)]
    for i, (_, pixels) in enumerate(rows):
        col = i % columns
        row = i // columns
        paste_tile(sheet, width, tile, tile, pixels, col * (tile + gap), row * (tile + gap))
    write_png_rgba(output, width, height, sheet)


def main(argv: Iterable[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Rebuild only currently arrow-marked ImageCards from the preserved source atlas.")
    parser.add_argument("--apply", action="store_true", help="Write repaired PNGs into Assets/Resources/ImageCards/Premium.")
    parser.add_argument("--target-max", type=int, default=150)
    parser.add_argument("--alpha-threshold", type=int, default=10)
    parser.add_argument("--source-atlas", default=str(DEFAULT_SOURCE_ATLAS))
    parser.add_argument("--targets-file", default=None)
    parser.add_argument("--report-dir", default=str(DEFAULT_REPORT_DIR))
    args = parser.parse_args(list(argv) if argv is not None else None)

    source_atlas = Path(args.source_atlas)
    report_dir = Path(args.report_dir)
    target_ids = load_targets(Path(args.targets_file) if args.targets_file else None)

    ids = load_card_ids()
    id_to_index = {card_id: index for index, card_id in enumerate(ids)}
    missing = [card_id for card_id in target_ids if card_id not in id_to_index]
    if missing:
        raise RuntimeError(f"Targets are not in cards.json: {missing}")

    atlas_w, atlas_h, atlas_pixels = read_png_rgba(source_atlas)
    if (atlas_w, atlas_h) != (4800, 4800):
        raise RuntimeError(f"Expected 4800x4800 source atlas, got {atlas_w}x{atlas_h}")

    report_dir.mkdir(parents=True, exist_ok=True)
    report: dict[str, object] = {
        "sourceAtlas": str(source_atlas),
        "sourceOrder": "sorted card ids from nested goldCards",
        "applied": args.apply,
        "targets": {},
    }
    before_after_rows = []
    repaired_rows = []

    for card_id in target_ids:
        runtime_path = RUNTIME_DIR / f"{card_id}.png"
        if not runtime_path.exists():
            raise RuntimeError(f"Missing runtime sprite: {runtime_path}")
        before_w, before_h, before_pixels = read_png_rgba(runtime_path)
        if (before_w, before_h) != (CANVAS_SIZE, CANVAS_SIZE):
            raise RuntimeError(f"{runtime_path} must be {CANVAS_SIZE}x{CANVAS_SIZE}")

        source_w, source_h, source_pixels, source_ref = load_source_image(card_id, source_atlas, atlas_w, atlas_pixels, id_to_index[card_id])
        repaired, details = normalize_from_cell(source_w, source_h, source_pixels, args.target_max, args.alpha_threshold)
        if args.apply:
            write_png_rgba(runtime_path, CANVAS_SIZE, CANVAS_SIZE, repaired)

        report["targets"][card_id] = {
            "sourceIndex": id_to_index[card_id],
            "source": source_ref,
            "runtimePath": str(runtime_path),
            **details,
        }
        before_after_rows.append((card_id, before_pixels, repaired))
        repaired_rows.append((card_id, repaired))

    build_contact_sheet(before_after_rows, report_dir / "before_after.png")
    build_repaired_atlas(repaired_rows, report_dir / "repaired_cards_atlas.png")
    (report_dir / "repair_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    action = "updated" if args.apply else "previewed"
    print(f"{action} {len(target_ids)} cards")
    print(report_dir / "before_after.png")
    print(report_dir / "repaired_cards_atlas.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
