#!/usr/bin/env python3
"""Extract the user's 10-icon atlas into ImageCards/Premium sprites.

The supplied atlas has a dark gradient background rather than a chroma key, so
this tool uses fixed crop windows for this exact atlas plus a conservative
foreground mask. It only targets the final 10 arrow-marked card ids.
"""

from __future__ import annotations

import argparse
import json
import shutil
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont


CANVAS = 240
TARGET_MAX_SIDE = 168

ITEMS = [
    {
        "id": "camping_backpack",
        "crop": (0, 120, 300, 505),
        "seed": 34,
        "grow": 20,
        "sat": 30,
        "bright": 42,
        "keep": 1,
        "pad": 22,
        "mask": [
            ("round", (8, 118, 240, 300), 36),
            ("round", (30, 78, 230, 150), 34),
            ("round", (215, 145, 282, 275), 30),
        ],
    },
    {
        "id": "city_park",
        "crop": (310, 110, 665, 510),
        "seed": 32,
        "grow": 17,
        "sat": 24,
        "bright": 42,
        "keep": 2,
        "pad": 22,
        "mask": [
            ("poly", [(42, 250), (95, 120), (185, 45), (330, 100), (335, 260), (245, 315), (70, 310)]),
        ],
    },
    {
        "id": "clothes_glove",
        "crop": (680, 105, 925, 505),
        "seed": 30,
        "grow": 17,
        "sat": 20,
        "bright": 50,
        "keep": 1,
        "pad": 24,
        "mask": [
            ("ellipse", (36, 42, 207, 256)),
            ("ellipse", (38, 180, 155, 315)),
        ],
    },
    {
        "id": "dessert_waffle",
        "crop": (900, 120, 1245, 510),
        "seed": 35,
        "grow": 20,
        "sat": 30,
        "bright": 50,
        "keep": 1,
        "pad": 20,
        "mask": [
            ("poly", [(58, 92), (173, 25), (315, 76), (318, 235), (250, 306), (75, 285), (28, 185)]),
        ],
    },
    {
        "id": "garden_soil",
        "crop": (1240, 120, 1536, 505),
        "seed": 27,
        "grow": 15,
        "sat": 18,
        "bright": 35,
        "keep": 1,
        "pad": 20,
        "mask": [
            ("ellipse", (26, 66, 278, 282)),
            ("poly", [(16, 240), (64, 156), (178, 103), (270, 162), (292, 255), (215, 320), (68, 308)]),
        ],
    },
    {
        "id": "emotions_warmth",
        "crop": (0, 600, 315, 925),
        "seed": 31,
        "grow": 18,
        "sat": 28,
        "bright": 50,
        "keep": 1,
        "pad": 18,
        "mask": [
            ("poly", [(2, 88), (152, 60), (268, 120), (254, 244), (76, 292), (0, 242)]),
        ],
    },
    {
        "id": "garden_bench",
        "crop": (315, 575, 620, 910),
        "seed": 32,
        "grow": 17,
        "sat": 22,
        "bright": 46,
        "keep": 1,
        "pad": 18,
        "mask": [
            ("poly", [(30, 115), (258, 78), (292, 235), (245, 305), (50, 305), (0, 225)]),
        ],
    },
    {
        "id": "sport_field",
        "crop": (615, 575, 945, 930),
        "seed": 28,
        "grow": 15,
        "sat": 20,
        "bright": 45,
        "keep": 2,
        "pad": 20,
        "mask": [
            ("poly", [(35, 95), (275, 70), (325, 245), (265, 340), (25, 305), (0, 170)]),
        ],
    },
    {
        "id": "tools_saw",
        "crop": (930, 600, 1210, 900),
        "seed": 26,
        "grow": 14,
        "sat": 14,
        "bright": 62,
        "keep": 1,
        "pad": 24,
        "mask": [
            ("poly", [(10, 155), (190, 35), (275, 85), (82, 218)]),
            ("round", (170, 75, 275, 175), 28),
        ],
        "mainOnly": True,
    },
    {
        "id": "toys_train",
        "crop": (1215, 590, 1536, 925),
        "seed": 34,
        "grow": 18,
        "sat": 28,
        "bright": 48,
        "keep": 1,
        "pad": 18,
        "mask": [
            ("poly", [(0, 120), (75, 75), (260, 80), (320, 170), (300, 280), (34, 310), (0, 250)]),
            ("ellipse", (28, 34, 155, 105)),
            ("ellipse", (95, 20, 250, 95)),
        ],
    },
]


def alpha_bbox(img: Image.Image) -> tuple[int, int, int, int] | None:
    return img.getchannel("A").getbbox()


def largest_components(mask: np.ndarray, keep: int) -> np.ndarray:
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
            if len(comp) > 24:
                comps.append(comp)
    comps.sort(key=len, reverse=True)
    out = np.zeros((h, w), dtype=np.uint8)
    for comp in comps[:keep]:
        for y, x in comp:
            out[y, x] = 255
    return out


def extract_foreground(crop: Image.Image, cfg: dict[str, object]) -> Image.Image:
    crop = crop.convert("RGBA")
    rgb = crop.convert("RGB")
    bg = rgb.filter(ImageFilter.GaussianBlur(38))
    diff_img = ImageChops.difference(rgb, bg).convert("L")

    arr = np.asarray(rgb).astype(np.int16)
    diff = np.asarray(diff_img).astype(np.int16)
    mx = arr.max(axis=2)
    mn = arr.min(axis=2)
    sat = mx - mn
    bright = mx

    seed = int(cfg["seed"])
    grow = int(cfg["grow"])
    sat_min = int(cfg["sat"])
    bright_min = int(cfg["bright"])

    # Strong object core, then grow to moderately different neighboring pixels.
    core = ((diff > seed) & (bright > bright_min)) | ((sat > sat_min + 16) & (bright > bright_min + 12))
    growable = ((diff > grow) & (bright > bright_min - 12)) | ((sat > sat_min) & (bright > bright_min))

    # Keep only growable pixels connected to the core.
    h, w = core.shape
    seen = np.zeros((h, w), dtype=bool)
    mask = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    ys, xs = np.nonzero(core)
    for y, x in zip(ys.tolist(), xs.tolist()):
        seen[y, x] = True
        q.append((y, x))
    while q:
        y, x = q.popleft()
        mask[y, x] = True
        for ny in (y - 1, y, y + 1):
            for nx in (x - 1, x, x + 1):
                if ny == y and nx == x:
                    continue
                if 0 <= ny < h and 0 <= nx < w and not seen[ny, nx] and growable[ny, nx]:
                    seen[ny, nx] = True
                    q.append((ny, nx))

    comp = largest_components(mask, int(cfg["keep"]))
    if "mask" in cfg:
        manual = Image.new("L", crop.size, 0)
        d = ImageDraw.Draw(manual)
        for shape in cfg["mask"]:  # type: ignore[index]
            kind = shape[0]
            if kind == "round":
                _, box, radius = shape
                d.rounded_rectangle(box, radius=radius, fill=255)
            elif kind == "ellipse":
                _, box = shape
                d.ellipse(box, fill=255)
            elif kind == "poly":
                _, points = shape
                d.polygon(points, fill=255)
        manual = manual.filter(ImageFilter.GaussianBlur(5))
        manual_arr = np.asarray(manual)
        # Use the manual shape as a hard guard against rectangular background
        # chunks. The actual foreground still comes from the image-derived mask.
        comp = np.where(manual_arr > 16, comp, 0).astype(np.uint8)
    alpha = Image.fromarray(comp, "L")
    alpha = alpha.filter(ImageFilter.MaxFilter(5))
    alpha = alpha.filter(ImageFilter.GaussianBlur(1.2))
    alpha = alpha.point(lambda p: 255 if p > 126 else (0 if p < 20 else p))

    out = crop.copy()
    out.putalpha(alpha)
    bbox = alpha_bbox(out)
    if bbox is None:
        raise ValueError(f"empty extraction for {cfg['id']}")
    return out.crop(bbox)


def fit_sprite(fg: Image.Image, pad: int, main_only: bool = False) -> tuple[Image.Image, dict[str, object]]:
    bbox = alpha_bbox(fg)
    if bbox is None:
        raise ValueError("empty foreground")
    fg = fg.crop(bbox)
    max_side = min(TARGET_MAX_SIDE, CANVAS - pad * 2)
    scale = min(max_side / max(fg.size), 1.0)
    resized = fg.resize((max(1, round(fg.width * scale)), max(1, round(fg.height * scale))), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    canvas.alpha_composite(resized, ((CANVAS - resized.width) // 2, (CANVAS - resized.height) // 2))
    canvas = remove_tiny_alpha_components(canvas, min_area=80)
    if main_only:
        canvas = keep_largest_alpha_component(canvas)
    final = alpha_bbox(canvas)
    if final is None:
        raise ValueError("empty canvas")
    l, t, r, b = final
    return canvas, {"bbox": list(final), "margins": [l, t, CANVAS - r, CANVAS - b], "maxSide": max(r - l, b - t)}


def keep_largest_alpha_component(img: Image.Image) -> Image.Image:
    alpha = np.asarray(img.getchannel("A"))
    mask = alpha > 0
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
            comps.append(comp)
    if len(comps) <= 1:
        return img
    comps.sort(key=len, reverse=True)
    keep = np.zeros((h, w), dtype=bool)
    for y, x in comps[0]:
        keep[y, x] = True
    out = img.copy()
    alpha_out = np.array(out.getchannel("A"))
    alpha_out[~keep] = 0
    out.putalpha(Image.fromarray(alpha_out.astype(np.uint8), "L"))
    return out


def remove_tiny_alpha_components(img: Image.Image, min_area: int) -> Image.Image:
    alpha = np.asarray(img.getchannel("A"))
    mask = alpha > 16
    h, w = mask.shape
    seen = np.zeros((h, w), dtype=bool)
    remove_mask = np.zeros((h, w), dtype=bool)
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
            if len(comp) < min_area:
                for cy, cx in comp:
                    remove_mask[cy, cx] = True
    if not remove_mask.any():
        return img
    out = img.copy()
    alpha_out = np.array(out.getchannel("A"))
    alpha_out[remove_mask] = 0
    out.putalpha(Image.fromarray(alpha_out.astype(np.uint8), "L"))
    return out


def make_contact(items: list[tuple[str, Image.Image, Image.Image]], out: Path) -> None:
    tile = 152
    label = 185
    header = 34
    sheet = Image.new("RGB", (label + tile * 2, header + tile * len(items)), (31, 34, 37))
    d = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("Arial.ttf", 13)
    except OSError:
        font = ImageFont.load_default()
    d.text((label + 12, 10), "atlas crop", fill=(235, 235, 235), font=font)
    d.text((label + tile + 12, 10), "final PNG", fill=(235, 235, 235), font=font)
    for i, (card_id, crop, final) in enumerate(items):
        y = header + i * tile
        d.text((8, y + 12), card_id, fill=(235, 235, 235), font=font)
        crop_thumb = crop.convert("RGB").resize((140, 140), Image.Resampling.LANCZOS)
        bg = Image.new("RGBA", final.size, (246, 244, 235, 255))
        bg.alpha_composite(final)
        final_thumb = bg.convert("RGB").resize((140, 140), Image.Resampling.NEAREST)
        sheet.paste(crop_thumb, (label + 6, y + 6))
        sheet.paste(final_thumb, (label + tile + 6, y + 6))
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--atlas", required=True)
    parser.add_argument("--root", default="Assets/Resources/ImageCards/Premium")
    parser.add_argument("--out-dir", default="Tools/ImageCardAudit/user_atlas_last10")
    parser.add_argument("--method", choices=["local-mask", "rembg"], default="local-mask")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    atlas = Image.open(args.atlas).convert("RGBA")
    root = Path(args.root)
    out_dir = Path(args.out_dir)
    sliced = out_dir / "sliced"
    sliced.mkdir(parents=True, exist_ok=True)

    report = []
    contacts = []
    rembg_session = None
    if args.method == "rembg":
        from rembg import new_session

        rembg_session = new_session("u2netp")

    for cfg in ITEMS:
        card_id = str(cfg["id"])
        crop = atlas.crop(tuple(cfg["crop"]))
        if args.method == "rembg":
            from rembg import remove

            fg = remove(crop, session=rembg_session).convert("RGBA")
            bbox = alpha_bbox(fg)
            if bbox is None:
                raise ValueError(f"empty rembg extraction for {card_id}")
            fg = fg.crop(bbox)
        else:
            fg = extract_foreground(crop, cfg)
        final, item_report = fit_sprite(fg, int(cfg["pad"]), bool(cfg.get("mainOnly", False)))
        final.save(sliced / f"{card_id}.png")
        if args.apply:
            backup = out_dir / "before_apply"
            backup.mkdir(parents=True, exist_ok=True)
            target = root / f"{card_id}.png"
            shutil.copy2(target, backup / target.name)
            final.save(target)
        contacts.append((card_id, crop, final))
        report.append({"id": card_id, "crop": list(cfg["crop"]), **item_report})

    make_contact(contacts, out_dir / "contact_sheet.png")
    (out_dir / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"processed={len(report)} apply={args.apply}")
    print(out_dir / "contact_sheet.png")
    print(out_dir / "report.json")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
