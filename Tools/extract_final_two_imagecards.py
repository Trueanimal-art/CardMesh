#!/usr/bin/env python3
"""Extract the final two user-supplied ImageCard replacements.

The source files look like transparent PNGs at first glance, but their alpha
still spans almost the whole image because the generated gradient background is
present. This script removes that background, fits the object onto the standard
240x240 transparent card sprite canvas, and only applies the two requested ids.
"""

from __future__ import annotations

import argparse
import json
import shutil
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont


CANVAS = 240
TARGET_MAX_SIDE = 168
ITEMS = {
    "kitchen_kettle": {
        "default_source": "/Users/vnm/Downloads/ChatGPT Image 7 июл. 2026 г., 14_42_20.png",
        "pad": 36,
        "keep_components": 3,
        "min_component_area": 36,
    },
    "music_piano": {
        "default_source": "/Users/vnm/Downloads/ChatGPT Image 7 июл. 2026 г., 14_44_50.png",
        "pad": 36,
        "keep_components": 1,
        "min_component_area": 80,
    },
}


def alpha_bbox(img: Image.Image, threshold: int = 8) -> tuple[int, int, int, int] | None:
    alpha = np.asarray(img.getchannel("A"))
    ys, xs = np.nonzero(alpha > threshold)
    if len(xs) == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def filter_alpha_components(
    img: Image.Image,
    *,
    keep_components: int,
    min_component_area: int,
) -> Image.Image:
    alpha = np.asarray(img.getchannel("A"))
    mask = alpha > 8
    h, w = mask.shape
    seen = np.zeros((h, w), dtype=bool)
    comps: list[list[tuple[int, int]]] = []

    for y in range(h):
        for x in range(w):
            if seen[y, x] or not mask[y, x]:
                continue
            q: deque[tuple[int, int]] = deque([(y, x)])
            seen[y, x] = True
            comp: list[tuple[int, int]] = []
            while q:
                cy, cx = q.popleft()
                comp.append((cy, cx))
                for ny in (cy - 1, cy, cy + 1):
                    for nx in (cx - 1, cx, cx + 1):
                        if ny == cy and nx == cx:
                            continue
                        if 0 <= ny < h and 0 <= nx < w and not seen[ny, nx] and mask[ny, nx]:
                            seen[ny, nx] = True
                            q.append((ny, nx))
            if len(comp) >= min_component_area:
                comps.append(comp)

    comps.sort(key=len, reverse=True)
    keep = np.zeros((h, w), dtype=bool)
    for comp in comps[:keep_components]:
        for y, x in comp:
            keep[y, x] = True

    out = img.copy()
    alpha_out = np.array(out.getchannel("A"))
    alpha_out[~keep] = 0
    out.putalpha(Image.fromarray(alpha_out.astype(np.uint8), "L"))
    return out


def fit_sprite(img: Image.Image, pad: int) -> tuple[Image.Image, dict[str, object]]:
    bbox = alpha_bbox(img)
    if bbox is None:
        raise ValueError("empty foreground")
    fg = img.crop(bbox)
    max_side = min(TARGET_MAX_SIDE, CANVAS - pad * 2)
    scale = min(max_side / max(fg.size), 1.0)
    size = (max(1, round(fg.width * scale)), max(1, round(fg.height * scale)))
    fg = fg.resize(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    canvas.alpha_composite(fg, ((CANVAS - fg.width) // 2, (CANVAS - fg.height) // 2))
    final = alpha_bbox(canvas)
    if final is None:
        raise ValueError("empty final canvas")
    l, t, r, b = final
    return canvas, {"bbox": list(final), "margins": [l, t, CANVAS - r, CANVAS - b], "maxSide": max(r - l, b - t)}


def make_contact(rows: list[tuple[str, Image.Image, Image.Image, Image.Image]], out: Path) -> None:
    tile = 148
    label = 145
    header = 32
    sheet = Image.new("RGB", (label + tile * 3, header + tile * len(rows)), (31, 34, 37))
    d = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("Arial.ttf", 13)
    except OSError:
        font = ImageFont.load_default()
    for i, title in enumerate(["before", "source", "final Assets PNG"]):
        d.text((label + i * tile + 8, 9), title, fill=(235, 235, 235), font=font)
    for row, (card_id, before, source, final) in enumerate(rows):
        y = header + row * tile
        d.text((8, y + 14), card_id, fill=(235, 235, 235), font=font)
        images = [before, source, final]
        for col, img in enumerate(images):
            bg = Image.new("RGBA", img.size, (246, 244, 235, 255))
            bg.alpha_composite(img.convert("RGBA"))
            thumb = bg.convert("RGB").resize((136, 136), Image.Resampling.NEAREST)
            sheet.paste(thumb, (label + col * tile + 6, y + 6))
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default="Assets/Resources/ImageCards/Premium")
    parser.add_argument("--out-dir", default="Tools/ImageCardAudit/final_two_replacements")
    parser.add_argument("--kettle", default=ITEMS["kitchen_kettle"]["default_source"])
    parser.add_argument("--piano", default=ITEMS["music_piano"]["default_source"])
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    from rembg import new_session, remove

    sources = {
        "kitchen_kettle": Path(args.kettle),
        "music_piano": Path(args.piano),
    }
    root = Path(args.root)
    out_dir = Path(args.out_dir)
    sliced = out_dir / "sliced"
    backup = out_dir / "before_apply"
    sliced.mkdir(parents=True, exist_ok=True)
    session = new_session("u2netp")

    report = []
    contacts = []
    for card_id, source_path in sources.items():
        cfg = ITEMS[card_id]
        before_path = root / f"{card_id}.png"
        before = Image.open(before_path).convert("RGBA")
        source = Image.open(source_path).convert("RGBA")
        removed = remove(source, session=session).convert("RGBA")
        cleaned = filter_alpha_components(
            removed,
            keep_components=int(cfg["keep_components"]),
            min_component_area=int(cfg["min_component_area"]),
        )
        final, item_report = fit_sprite(cleaned, int(cfg["pad"]))
        final.save(sliced / f"{card_id}.png")
        if args.apply:
            backup.mkdir(parents=True, exist_ok=True)
            shutil.copy2(before_path, backup / before_path.name)
            final.save(before_path)
            final_for_contact = Image.open(before_path).convert("RGBA")
        else:
            final_for_contact = final
        contacts.append((card_id, before, source, final_for_contact))
        report.append(
            {
                "id": card_id,
                "source": str(source_path),
                "sourceSize": list(source.size),
                **item_report,
            }
        )

    make_contact(contacts, out_dir / "before_source_after.png")
    (out_dir / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"processed={len(report)} apply={args.apply}")
    print(out_dir / "before_source_after.png")
    print(out_dir / "report.json")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
