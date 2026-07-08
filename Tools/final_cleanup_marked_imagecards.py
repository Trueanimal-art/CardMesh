#!/usr/bin/env python3
"""Clean residual atlas/chroma artifacts from marked premium image cards.

This intentionally preserves the current glossy source art. It only removes
small detached fragments and obvious magenta/green keying leftovers that came
from slicing/transparent-background cleanup.
"""

from __future__ import annotations

import argparse
import json
import math
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


LATEST_MARKED_IDS = [
    "books_cover",
    "breakfast_butter",
    "breakfast_cereal",
    "business_office",
    "business_team",
    "camping_backpack",
    "city_park",
    "clothes_glove",
    "colors_black",
    "colors_blue",
    "colors_green",
    "colors_white",
    "colors_yellow",
    "dessert_waffle",
    "drinks_coffee",
    "emotions_calm",
    "emotions_warmth",
    "garden_soil",
    "hobbies_drawing",
    "kitchen_knife",
    "shoes_pair",
    "sport_field",
    "summer_lake",
    "summer_picnic",
    "technology_code",
    "tools_pliers",
    "tools_saw",
    "toys_car",
    "work_desk",
]


MARKED_IDS = LATEST_MARKED_IDS


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
    out = img.copy()
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
            if 0 < a <= 14:
                to_clear.append((x, y))
                continue
            if not suspicious_key_pixel(r, g, b, a):
                continue
            near_card_bottom = y >= bottom - 18
            near_column_bottom = column_bottom[x] >= 0 and y >= column_bottom[x] - 7
            isolated = opaque_neighbors(px, x, y, width, height) <= 5
            if near_card_bottom or near_column_bottom or isolated:
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


def remove_small_detached_components(img: Image.Image) -> Image.Image:
    out = img.copy()
    px = out.load()
    components = alpha_components(out)
    if not components:
        return out

    largest = max(c["area"] for c in components)
    for comp in components:
        if comp["area"] == largest:
            continue

        thin = comp["width"] <= 18 or comp["height"] <= 4
        small = comp["area"] <= 80
        modest_fragment = comp["area"] <= 180 and thin

        if (small or modest_fragment) and comp["area"] < largest * 0.08:
            for x, y in comp["points"]:
                px[x, y] = (0, 0, 0, 0)

    return out


def clean_card(img: Image.Image) -> Image.Image:
    cleaned = remove_key_fringes(img.convert("RGBA"))
    cleaned = remove_small_detached_components(cleaned)
    return cleaned


def read_bad_cards_manifest(project_root: Path) -> tuple[dict[str, dict[str, int]], Path | None]:
    manifest_path = project_root / "Tools/ImageCardAudit/bad_cards_manifest.json"
    if not manifest_path.exists():
        return {}, None

    data = json.loads(manifest_path.read_text(encoding="utf-8"))
    atlas_path = data.get("afterSourceAtlas") or data.get("sourceAtlas")
    atlas = project_root / atlas_path if atlas_path else None
    cells: dict[str, dict[str, int]] = {}
    for item in data.get("cards", []):
        cell = item.get("atlasCell")
        if cell:
            cells[item["id"]] = cell
    return cells, atlas


def source_image_for_card(card_id: str, runtime_source: Path, atlas: Image.Image | None, cells: dict[str, dict[str, int]]) -> Image.Image:
    if atlas is not None and card_id in cells:
        cell = cells[card_id]
        box = (
            int(cell["x"]),
            int(cell["y"]),
            int(cell["x"]) + int(cell["width"]),
            int(cell["y"]) + int(cell["height"]),
        )
        return atlas.crop(box).convert("RGBA")
    return Image.open(runtime_source).convert("RGBA")


def make_contact_sheet(items: list[tuple[str, Image.Image, Image.Image]], output: Path) -> None:
    cell = 170
    label_width = 190
    header = 44
    width = label_width + cell * 2
    height = header + len(items) * cell

    sheet = Image.new("RGB", (width, height), (31, 34, 37))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("Arial.ttf", 13)
    except OSError:
        font = ImageFont.load_default()

    draw.text((label_width, 12), "before", fill=(230, 230, 230), font=font)
    draw.text((label_width + cell, 12), "after", fill=(230, 230, 230), font=font)

    for i, (card_id, before, after) in enumerate(items):
        y = header + i * cell
        draw.text((8, y + 8), card_id, fill=(230, 230, 230), font=font)
        for x, img in ((label_width, before), (label_width + cell, after)):
            bg = Image.new("RGBA", img.size, (246, 244, 235, 255))
            bg.alpha_composite(img)
            preview = bg.convert("RGB").resize((128, 128), Image.Resampling.NEAREST)
            sheet.paste(preview, (x + 20, y + 14))

    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output)


def make_atlas(items: list[tuple[str, Image.Image, Image.Image]], output: Path) -> None:
    cols = 8
    tile = 256
    rows = math.ceil(len(items) / cols)
    atlas = Image.new("RGBA", (cols * tile, rows * tile), (0, 0, 0, 0))
    for index, (_, _, img) in enumerate(items):
        x = (index % cols) * tile
        y = (index // cols) * tile
        atlas.alpha_composite(img, (x, y))
    output.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(output)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default="Assets/Resources/ImageCards/Premium")
    parser.add_argument("--out-dir", default="Tools/ImageCardAudit/final_cleanup_preview")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    root = Path(args.root)
    out_dir = Path(args.out_dir)
    sliced_dir = out_dir / "sliced"
    sliced_dir.mkdir(parents=True, exist_ok=True)
    project_root = Path(__file__).resolve().parents[1]
    cells, atlas_path = read_bad_cards_manifest(project_root)
    atlas = Image.open(atlas_path).convert("RGBA") if atlas_path and atlas_path.exists() else None

    items: list[tuple[str, Image.Image, Image.Image]] = []
    for card_id in MARKED_IDS:
        source = root / f"{card_id}.png"
        if not source.exists():
            raise FileNotFoundError(source)

        before = Image.open(source).convert("RGBA")
        source_img = source_image_for_card(card_id, source, atlas, cells)
        if source_img.size != before.size:
            source_img = source_img.resize(before.size, Image.Resampling.LANCZOS)
        base = source_img if card_id in cells else before
        if alpha_bbox(base) is None:
            base = before
        after = clean_card(base)
        (sliced_dir / f"{card_id}.png").parent.mkdir(parents=True, exist_ok=True)
        after.save(sliced_dir / f"{card_id}.png")
        if args.apply:
            after.save(source)
        items.append((card_id, before, after))

    make_contact_sheet(items, out_dir / "before_after.png")
    latest_items = [item for item in items if item[0] in set(LATEST_MARKED_IDS)]
    make_contact_sheet(latest_items, out_dir / "latest_before_after.png")
    make_atlas(items, out_dir / "final_marked_cleanup_atlas.png")
    print(f"cleaned={len(items)}")
    print(f"atlas_source={atlas_path if atlas else 'runtime'}")
    print(out_dir / "before_after.png")
    print(out_dir / "latest_before_after.png")
    print(out_dir / "final_marked_cleanup_atlas.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
