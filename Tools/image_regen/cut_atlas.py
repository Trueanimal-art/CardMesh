#!/usr/bin/env python3
"""Cut a 4x4 ChatGPT atlas into 240x240 card images.

Usage: python3 -I cut_atlas.py <atlas.png> <A|B|C> <out_dir>

- Uses the PNG alpha when the atlas is really transparent; otherwise keys out the
  flat background colour sampled from the cell corners and removes the coloured
  fringe (despill), which is what left green/magenta edges on the old art.
- Each object is cropped to its alpha bounding box, centred and fitted into a
  224 px box on a 240x240 transparent canvas (same size as the existing art).
- Prints a warning when an object touches its cell border (likely cut or bleeding
  in from a neighbour) so that cell can be regenerated.
"""
import json
import pathlib
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

CANVAS = 240
FIT = 224


def key_background(rgb: np.ndarray) -> np.ndarray:
    h, w, _ = rgb.shape
    corners = np.concatenate([
        rgb[:6, :6].reshape(-1, 3), rgb[:6, -6:].reshape(-1, 3),
        rgb[-6:, :6].reshape(-1, 3), rgb[-6:, -6:].reshape(-1, 3)])
    bg = np.median(corners, axis=0)
    dist = np.sqrt(((rgb.astype(np.float32) - bg) ** 2).sum(axis=2))
    alpha = np.clip((dist - 28.0) / 40.0, 0.0, 1.0)
    return alpha, bg


def despill(rgba: np.ndarray, bg: np.ndarray) -> np.ndarray:
    """Pull semi-transparent edge pixels away from the key colour."""
    out = rgba.astype(np.float32)
    a = out[..., 3:4] / 255.0
    edge = (a > 0.0) & (a < 0.98)
    rgb = out[..., :3]
    # Un-premultiply against the key colour: c = a*fg + (1-a)*bg  ->  fg = (c - (1-a)*bg) / a
    fg = (rgb - (1.0 - a) * bg) / np.maximum(a, 0.05)
    rgb = np.where(edge, np.clip(fg, 0, 255), rgb)
    out[..., :3] = rgb
    return out.astype(np.uint8)


def main() -> None:
    atlas_path, name, out_dir = sys.argv[1], sys.argv[2], pathlib.Path(sys.argv[3])
    spec = json.load(open(pathlib.Path(__file__).with_name("atlases.json"), encoding="utf-8"))
    cells = spec["atlases"][name]
    cols, rows = spec["grid"]
    out_dir.mkdir(parents=True, exist_ok=True)

    image = Image.open(atlas_path).convert("RGBA")
    data = np.array(image)
    has_alpha = (data[..., 3] < 250).mean() > 0.2
    width, height = image.size
    cw, ch = width / cols, height / rows
    report = []

    # Alpha for the whole atlas (real alpha, or keyed per cell when the PNG is opaque).
    full = data.copy()
    if not has_alpha:
        for index in range(len(cells)):
            cx, cy = index % cols, index // cols
            x0, y0, x1, y1 = round(cx * cw), round(cy * ch), round((cx + 1) * cw), round((cy + 1) * ch)
            cell = full[y0:y1, x0:x1]
            alpha, bg = key_background(cell[..., :3])
            cell[..., 3] = (alpha * 255).astype(np.uint8)
            full[y0:y1, x0:x1] = despill(cell, bg)

    # Some atlases come with a faint semi-transparent glow/haze around every object.
    # Objects themselves are opaque, so drop everything below ~50% alpha and stretch the rest.
    if has_alpha:
        haze = ((full[..., 3] > 24) & (full[..., 3] < 128)).mean()
        if haze > 0.15:
            a = full[..., 3].astype(np.float32)
            full[..., 3] = np.clip((a - 128.0) * 255.0 / 127.0, 0, 255).astype(np.uint8)
            report.append(f"haze removed ({haze:.0%} of pixels)")

    # Objects may poke a few pixels into a neighbour cell: group pixels into blobs
    # (slightly dilated so a note and its scroll stay together) and give each blob
    # to the cell that holds its centre.
    solid = full[..., 3] > 24
    labels, count = ndimage.label(ndimage.binary_dilation(solid, iterations=4))
    labels = np.where(solid, labels, 0)
    owner = {}
    for label, (sy, sx) in zip(range(1, count + 1), ndimage.center_of_mass(solid, labels, range(1, count + 1))):
        if np.isnan(sx):
            continue
        owner[label] = min(cols - 1, int(sx // cw)) + cols * min(rows - 1, int(sy // ch))

    for index, (key, _) in enumerate(cells):
        mine = [label for label, cell_index in owner.items() if cell_index == index]
        mask = np.isin(labels, mine)
        if not mask.any():
            report.append(f"{key}: EMPTY")
            continue
        ys, xs = np.where(mask)
        obj = full.copy()
        obj[..., 3] = np.where(mask | (ndimage.binary_dilation(mask, iterations=2) & (full[..., 3] > 0)), full[..., 3], 0)
        crop = Image.fromarray(obj).crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
        edge = xs.min() <= 0 or ys.min() <= 0 or xs.max() >= width - 1 or ys.max() >= height - 1
        scale = min(FIT / crop.width, FIT / crop.height)
        crop = crop.resize((max(1, round(crop.width * scale)), max(1, round(crop.height * scale))), Image.LANCZOS)
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        canvas.alpha_composite(crop, ((CANVAS - crop.width) // 2, (CANVAS - crop.height) // 2))
        canvas.save(out_dir / f"{key}.png")
        report.append(f"{key}: ok {crop.width}x{crop.height} blobs={len(mine)}{'  WARNING touches atlas edge' if edge else ''}")

    print(f"atlas {name}: {width}x{height}, {'alpha' if has_alpha else 'keyed background'}")
    print("\n".join(report))


if __name__ == "__main__":
    main()
