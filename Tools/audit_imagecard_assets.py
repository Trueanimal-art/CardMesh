#!/usr/bin/env python3
"""Audit ImageCard PNG assets for import-safe geometry and visual review.

The automatic checks catch technical defects. The generated contact sheets are
for final human review of semantic defects such as a cropped object inside the
transparent canvas.
"""

from __future__ import annotations

import argparse
import json
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw


EXPECTED_SIZE = (240, 240)
MIN_CANVAS_MARGIN = 10


def alpha_bbox(path: Path) -> tuple[int, int, int, int] | None:
    image = Image.open(path).convert("RGBA")
    return image.getchannel("A").getbbox()


def find_stray_components(path: Path) -> list[dict[str, object]]:
    image = Image.open(path).convert("RGBA")
    alpha = image.getchannel("A")
    pixels = alpha.load()
    width, height = image.size
    seen: set[tuple[int, int]] = set()
    components: list[tuple[int, tuple[int, int, int, int]]] = []

    for y in range(height):
        for x in range(width):
            if (x, y) in seen or pixels[x, y] <= 40:
                continue

            queue = deque([(x, y)])
            seen.add((x, y))
            xs: list[int] = []
            ys: list[int] = []
            area = 0

            while queue:
                px, py = queue.popleft()
                area += 1
                xs.append(px)
                ys.append(py)

                for nx in range(px - 1, px + 2):
                    for ny in range(py - 1, py + 2):
                        if nx < 0 or ny < 0 or nx >= width or ny >= height:
                            continue
                        if (nx, ny) in seen or pixels[nx, ny] <= 40:
                            continue
                        seen.add((nx, ny))
                        queue.append((nx, ny))

            components.append((area, (min(xs), min(ys), max(xs) + 1, max(ys) + 1)))

    components.sort(reverse=True)
    if not components:
        return []

    total_area = sum(area for area, _ in components)
    stray_components = []
    for area, bbox in components[1:]:
        ratio = area / max(1, total_area)
        if 20 <= area <= 850 and ratio < 0.035:
            stray_components.append({"area": area, "bbox": bbox, "ratio": round(ratio, 4)})
    return stray_components


def audit_pngs(root: Path) -> dict[str, list[dict[str, object]]]:
    report: dict[str, list[dict[str, object]]] = {
        "bad_size": [],
        "empty_alpha": [],
        "canvas_edge": [],
        "stray_components": [],
    }

    for path in sorted(root.glob("*.png")):
        image = Image.open(path).convert("RGBA")
        if image.size != EXPECTED_SIZE:
            report["bad_size"].append({"file": path.name, "size": image.size})
            continue

        bbox = image.getchannel("A").getbbox()
        if bbox is None:
            report["empty_alpha"].append({"file": path.name})
            continue

        left, top, right, bottom = bbox
        margins = (left, top, EXPECTED_SIZE[0] - right, EXPECTED_SIZE[1] - bottom)
        if min(margins) < MIN_CANVAS_MARGIN:
            report["canvas_edge"].append({"file": path.name, "margins": margins})

        strays = find_stray_components(path)
        if strays:
            report["stray_components"].append({"file": path.name, "components": strays[:8]})

    return report


def write_contact_sheets(root: Path, out_dir: Path, per_sheet: int = 80) -> list[str]:
    out_dir.mkdir(parents=True, exist_ok=True)
    files = [path for path in sorted(root.glob("*.png")) if not path.stem.endswith("_unused")]
    cell = 170
    label_height = 26
    columns = 8
    outputs: list[str] = []

    for sheet_index in range((len(files) + per_sheet - 1) // per_sheet):
        subset = files[sheet_index * per_sheet : (sheet_index + 1) * per_sheet]
        rows = (len(subset) + columns - 1) // columns
        sheet = Image.new("RGBA", (columns * cell, rows * (cell + label_height)), (238, 238, 238, 255))
        draw = ImageDraw.Draw(sheet)

        for item_index, path in enumerate(subset):
            image = Image.open(path).convert("RGBA")
            x = (item_index % columns) * cell
            y = (item_index // columns) * (cell + label_height)
            card = Image.new("RGBA", (cell, cell), (255, 255, 255, 255))
            card.alpha_composite(image.resize((150, 150), Image.Resampling.LANCZOS), (10, 10))
            sheet.alpha_composite(card, (x, y))
            draw.rectangle((x, y, x + cell - 1, y + cell - 1), outline=(190, 190, 190, 255), width=2)
            draw.text((x + 4, y + cell + 6), path.stem[:24], fill=(0, 0, 0, 255))

        output = out_dir / f"audit_{sheet_index + 1:02d}.png"
        sheet.save(output)
        outputs.append(str(output))

    return outputs


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default="Assets/Resources/ImageCards/Premium")
    parser.add_argument("--out-dir", default="/private/tmp/imagecard_audit")
    parser.add_argument("--write-contact-sheets", action="store_true")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()

    root = Path(args.root)
    report = audit_pngs(root)
    sheets = write_contact_sheets(root, Path(args.out_dir)) if args.write_contact_sheets else []

    if args.json:
        print(json.dumps({"report": report, "contact_sheets": sheets}, ensure_ascii=False, indent=2))
    else:
        for key, items in report.items():
            print(f"{key}: {len(items)}")
            for item in items[:80]:
                print(f"  {item}")
        if sheets:
            print("contact_sheets:")
            for sheet in sheets:
                print(f"  {sheet}")

    hard_failures = len(report["bad_size"]) + len(report["empty_alpha"]) + len(report["canvas_edge"])
    return 1 if hard_failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
