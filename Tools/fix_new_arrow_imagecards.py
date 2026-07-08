#!/usr/bin/env python3
"""Second-pass visual repair for the latest arrow-marked ImageCards.

The previous margin/import checks pass for these files, but the current Unity
screenshots still mark them as visually bad. This pass keeps the existing
detailed PNG art, removes leftover key/atlas fragments, and places the object
onto a clean 240x240 transparent canvas with a deliberately larger safe area.
"""

from __future__ import annotations

import argparse
import json
import math
import shutil
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


TARGET_IDS = [
    "breakfast_cereal",
    "cinema_actor",
    "city_park",
    "clothes_glove",
    "dessert_waffle",
    "emotions_calm",
    "emotions_warmth",
    "garden_soil",
    "hobbies_drawing",
    "home_key",
    "insects_bee",
    "insects_butterfly",
    "kitchen_knife",
    "music_piano",
    "nature_cloud",
    "school_desk",
    "shoes_pair",
    "sport_field",
    "sport_race",
    "sport_team",
    "summer_lake",
    "tools_drill",
    "tools_saw",
    "tools_wrench",
    "toys_car",
    "toys_train",
]

CANVAS_SIZE = 240
DEFAULT_TARGET_MAX_SIDE = 138
MIN_SAFE_MARGIN = 40


def alpha_bbox(img: Image.Image) -> tuple[int, int, int, int] | None:
    return img.getchannel("A").getbbox()


def suspicious_key_pixel(r: int, g: int, b: int, a: int) -> bool:
    if a == 0:
        return False
    hard_magenta = r > 125 and b > 85 and g < 135 and min(r, b) - g > 28
    hard_green = g > 125 and r < 130 and b < 145 and g - max(r, b) > 28
    soft_magenta = a < 245 and b > 45 and r > 45 and b > g + 12 and r > g + 6
    soft_green = a < 245 and g > 70 and g > r + 16 and g > b + 6
    return hard_magenta or hard_green or soft_magenta or soft_green


def opaque_neighbors(px, x: int, y: int, width: int, height: int) -> int:
    count = 0
    for yy in range(max(0, y - 1), min(height, y + 2)):
        for xx in range(max(0, x - 1), min(width, x + 2)):
            if px[xx, yy][3] > 8:
                count += 1
    return count


def remove_key_fringes(img: Image.Image) -> Image.Image:
    out = img.convert("RGBA").copy()
    px = out.load()
    width, height = out.size
    bbox = alpha_bbox(out)
    if bbox is None:
        return out
    _, _, _, bottom = bbox

    column_bottom = [-1] * width
    for x in range(width):
        for y in range(height - 1, -1, -1):
            if px[x, y][3] > 18:
                column_bottom[x] = y
                break

    to_clear: list[tuple[int, int]] = []
    for y in range(height):
        for x in range(width):
            r, g, b, a = px[x, y]
            if 0 < a <= 12:
                to_clear.append((x, y))
                continue
            if not suspicious_key_pixel(r, g, b, a):
                continue
            near_bbox_bottom = y >= bottom - 18
            near_column_bottom = column_bottom[x] >= 0 and y >= column_bottom[x] - 8
            isolated = opaque_neighbors(px, x, y, width, height) <= 5
            if near_bbox_bottom or near_column_bottom or isolated:
                to_clear.append((x, y))

    for x, y in to_clear:
        px[x, y] = (0, 0, 0, 0)
    return out


def alpha_components(img: Image.Image):
    alpha = img.getchannel("A")
    width, height = img.size
    seen = bytearray(width * height)
    px = alpha.load()
    components = []

    for y in range(height):
        for x in range(width):
            index = y * width + x
            if seen[index] or px[x, y] <= 8:
                continue
            q = deque([(x, y)])
            seen[index] = 1
            points = []
            minx = maxx = x
            miny = maxy = y
            while q:
                cx, cy = q.popleft()
                points.append((cx, cy))
                minx = min(minx, cx)
                maxx = max(maxx, cx)
                miny = min(miny, cy)
                maxy = max(maxy, cy)
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if nx < 0 or nx >= width or ny < 0 or ny >= height:
                        continue
                    ni = ny * width + nx
                    if seen[ni] or px[nx, ny] <= 8:
                        continue
                    seen[ni] = 1
                    q.append((nx, ny))
            components.append(
                {
                    "points": points,
                    "area": len(points),
                    "bbox": (minx, miny, maxx, maxy),
                    "width": maxx - minx + 1,
                    "height": maxy - miny + 1,
                }
            )
    return components


def remove_detached_fragments(img: Image.Image) -> Image.Image:
    out = img.copy()
    px = out.load()
    components = alpha_components(out)
    if not components:
        return out
    largest = max(c["area"] for c in components)
    for comp in components:
        if comp["area"] == largest:
            continue
        thin = comp["width"] <= 18 or comp["height"] <= 5
        small = comp["area"] <= 90
        modest_fragment = comp["area"] <= 220 and thin
        if (small or modest_fragment) and comp["area"] < largest * 0.10:
            for x, y in comp["points"]:
                px[x, y] = (0, 0, 0, 0)
    return out


def fit_safe_canvas(img: Image.Image, target_max_side: int) -> tuple[Image.Image, dict[str, object]]:
    cleaned = remove_detached_fragments(remove_key_fringes(img))
    bbox = alpha_bbox(cleaned)
    if bbox is None:
        raise ValueError("empty image after cleanup")

    cropped = cleaned.crop(bbox)
    width, height = cropped.size
    scale = min(target_max_side / max(width, height), 1.0)
    target_width = max(1, round(width * scale))
    target_height = max(1, round(height * scale))
    resized = cropped.resize((target_width, target_height), Image.Resampling.LANCZOS)

    out = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    x = (CANVAS_SIZE - target_width) // 2
    y = (CANVAS_SIZE - target_height) // 2
    out.alpha_composite(resized, (x, y))

    final_bbox = alpha_bbox(out)
    if final_bbox is None:
        raise ValueError("empty image after fit")
    left, top, right, bottom = final_bbox
    margins = [left, top, CANVAS_SIZE - right, CANVAS_SIZE - bottom]
    return out, {
        "sourceBbox": list(bbox),
        "finalBbox": list(final_bbox),
        "margins": margins,
        "maxSide": max(right - left, bottom - top),
        "result": "ok" if min(margins) >= MIN_SAFE_MARGIN else "margin-warning",
    }


def tile_on_background(img: Image.Image, size: int = 150) -> Image.Image:
    bg = Image.new("RGBA", img.size, (246, 244, 235, 255))
    bg.alpha_composite(img)
    return bg.convert("RGB").resize((size, size), Image.Resampling.NEAREST)


def make_contact_sheet(items: list[tuple[str, Image.Image, Image.Image]], output: Path) -> None:
    tile = 160
    label_width = 210
    header = 42
    width = label_width + tile * 2
    height = header + tile * len(items)
    sheet = Image.new("RGB", (width, height), (31, 34, 37))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("Arial.ttf", 13)
    except OSError:
        font = ImageFont.load_default()
    draw.text((label_width, 12), "before", fill=(230, 230, 230), font=font)
    draw.text((label_width + tile, 12), "after", fill=(230, 230, 230), font=font)
    for index, (card_id, before, after) in enumerate(items):
        y = header + index * tile
        draw.text((8, y + 10), card_id, fill=(230, 230, 230), font=font)
        sheet.paste(tile_on_background(before), (label_width + 5, y + 5))
        sheet.paste(tile_on_background(after), (label_width + tile + 5, y + 5))
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output)


def make_runtime_pages(root: Path, out_dir: Path) -> None:
    ids = [p.stem for p in sorted(root.glob("*.png"))]
    cols = 6
    rows = 5
    card_w = 126
    card_h = 184
    gap = 14
    page_w = cols * card_w + (cols - 1) * gap + 48
    page_h = rows * card_h + (rows - 1) * gap + 88
    try:
        font = ImageFont.truetype("Arial.ttf", 12)
        small = ImageFont.truetype("Arial.ttf", 9)
    except OSError:
        font = ImageFont.load_default()
        small = ImageFont.load_default()

    out_dir.mkdir(parents=True, exist_ok=True)
    for page in range(math.ceil(len(ids) / (cols * rows))):
        page_ids = ids[page * cols * rows : (page + 1) * cols * rows]
        img = Image.new("RGB", (page_w, page_h), (7, 28, 24))
        draw = ImageDraw.Draw(img)
        draw.text((24, 16), f"ImageCards page {page + 1}", fill=(240, 240, 240), font=font)
        for i, card_id in enumerate(page_ids):
            x = 24 + (i % cols) * (card_w + gap)
            y = 52 + (i // cols) * (card_h + gap)
            draw.rectangle((x, y, x + card_w, y + card_h), fill=(246, 244, 235))
            icon = Image.open(root / f"{card_id}.png").convert("RGBA")
            preview = Image.new("RGBA", (86, 86), (0, 0, 0, 0))
            icon.thumbnail((86, 86), Image.Resampling.LANCZOS)
            preview.alpha_composite(icon, ((86 - icon.width) // 2, (86 - icon.height) // 2))
            icon_bg = Image.new("RGBA", (86, 86), (246, 244, 235, 255))
            icon_bg.alpha_composite(preview)
            img.paste(icon_bg.convert("RGB"), (x + 20, y + 46))
            draw.text((x + 8, y + 10), card_id[:16], fill=(74, 70, 64), font=small)
            draw.text((x + 8, y + card_h - 22), card_id[:18], fill=(150, 146, 140), font=small)
        img.save(out_dir / f"page_{page + 1:02d}.png")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default="Assets/Resources/ImageCards/Premium")
    parser.add_argument("--out-dir", default="Tools/ImageCardAudit/new_arrow_second_pass")
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--max-side", type=int, default=DEFAULT_TARGET_MAX_SIDE)
    parser.add_argument("--ids", nargs="*")
    args = parser.parse_args()

    root = Path(args.root)
    out_dir = Path(args.out_dir)
    sliced_dir = out_dir / "sliced"
    sliced_dir.mkdir(parents=True, exist_ok=True)

    items: list[tuple[str, Image.Image, Image.Image]] = []
    report = []
    target_ids = args.ids if args.ids else TARGET_IDS
    for card_id in target_ids:
        source = root / f"{card_id}.png"
        if not source.exists():
            raise FileNotFoundError(source)
        before = Image.open(source).convert("RGBA")
        after, item_report = fit_safe_canvas(before, args.max_side)
        after_path = sliced_dir / f"{card_id}.png"
        after.save(after_path)
        if args.apply:
            backup_dir = out_dir / "before_apply"
            backup_dir.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, backup_dir / source.name)
            after.save(source)
        items.append((card_id, before, after))
        report.append({"id": card_id, "file": str(source), **item_report})

    make_contact_sheet(items, out_dir / "before_after.png")
    make_runtime_pages(root, out_dir / "runtime_pages_by_id")
    (out_dir / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"processed={len(items)} apply={args.apply}")
    print(out_dir / "before_after.png")
    print(out_dir / "runtime_pages_by_id")
    print(out_dir / "report.json")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
