#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from collections import deque
from pathlib import Path
from typing import Iterable

from process_imagecard_sheet import read_png_rgba, write_png_rgba


CANVAS_SIZE = 240
DEFAULT_TARGET_MAX = 160
DEFAULT_ALPHA_THRESHOLD = 10
DEFAULT_MIN_MARGIN = 20
DEFAULT_REMOVE_STRAY_AREA = 48


def alpha_bbox(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    threshold: int,
) -> tuple[int, int, int, int] | None:
    min_x = width
    min_y = height
    max_x = -1
    max_y = -1
    for y in range(height):
        for x in range(width):
            if pixels[(y * width) + x][3] <= threshold:
                continue
            min_x = min(min_x, x)
            min_y = min(min_y, y)
            max_x = max(max_x, x)
            max_y = max(max_y, y)

    if max_x < min_x or max_y < min_y:
        return None
    return min_x, min_y, max_x + 1, max_y + 1


def strip_low_alpha(
    pixels: list[tuple[int, int, int, int]],
    threshold: int,
) -> list[tuple[int, int, int, int]]:
    output = []
    for red, green, blue, alpha in pixels:
        if alpha <= threshold:
            output.append((0, 0, 0, 0))
        else:
            output.append((red, green, blue, alpha))
    return output


def remove_tiny_components(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    threshold: int,
    max_area: int,
) -> tuple[list[tuple[int, int, int, int]], int]:
    if max_area <= 0:
        return pixels, 0

    seen: set[tuple[int, int]] = set()
    remove_indices: set[int] = set()
    removed_components = 0

    for y in range(height):
        for x in range(width):
            if (x, y) in seen or pixels[(y * width) + x][3] <= threshold:
                continue

            queue = deque([(x, y)])
            seen.add((x, y))
            indices: list[int] = []

            while queue:
                px, py = queue.popleft()
                indices.append((py * width) + px)

                for nx in (px - 1, px, px + 1):
                    for ny in (py - 1, py, py + 1):
                        if nx < 0 or ny < 0 or nx >= width or ny >= height:
                            continue
                        if (nx, ny) in seen or pixels[(ny * width) + nx][3] <= threshold:
                            continue
                        seen.add((nx, ny))
                        queue.append((nx, ny))

            if len(indices) <= max_area:
                remove_indices.update(indices)
                removed_components += 1

    if not remove_indices:
        return pixels, 0

    output = list(pixels)
    for index in remove_indices:
        output[index] = (0, 0, 0, 0)
    return output, removed_components


def crop_to_bbox(
    width: int,
    pixels: list[tuple[int, int, int, int]],
    bbox: tuple[int, int, int, int],
) -> tuple[int, int, list[tuple[int, int, int, int]]]:
    left, top, right, bottom = bbox
    out_w = max(1, right - left)
    out_h = max(1, bottom - top)
    output = []
    for y in range(top, bottom):
        for x in range(left, right):
            output.append(pixels[(y * width) + x])
    return out_w, out_h, output


def resize_premultiplied(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    target_w: int,
    target_h: int,
) -> list[tuple[int, int, int, int]]:
    if width == target_w and height == target_h:
        return list(pixels)

    output: list[tuple[int, int, int, int]] = []
    for y in range(target_h):
        source_y = (y + 0.5) * height / target_h - 0.5
        y0 = max(0, int(source_y))
        y1 = min(height - 1, y0 + 1)
        fy = source_y - y0
        for x in range(target_w):
            source_x = (x + 0.5) * width / target_w - 0.5
            x0 = max(0, int(source_x))
            x1 = min(width - 1, x0 + 1)
            fx = source_x - x0
            samples = (
                (pixels[(y0 * width) + x0], (1 - fx) * (1 - fy)),
                (pixels[(y0 * width) + x1], fx * (1 - fy)),
                (pixels[(y1 * width) + x0], (1 - fx) * fy),
                (pixels[(y1 * width) + x1], fx * fy),
            )
            alpha = 0.0
            red = 0.0
            green = 0.0
            blue = 0.0
            for color, weight in samples:
                sample_alpha = color[3] / 255.0
                alpha += sample_alpha * weight
                red += color[0] * sample_alpha * weight
                green += color[1] * sample_alpha * weight
                blue += color[2] * sample_alpha * weight

            out_alpha = max(0, min(255, round(alpha * 255)))
            if out_alpha <= 0 or alpha <= 0.0001:
                output.append((0, 0, 0, 0))
            else:
                output.append(
                    (
                        max(0, min(255, round(red / alpha))),
                        max(0, min(255, round(green / alpha))),
                        max(0, min(255, round(blue / alpha))),
                        out_alpha,
                    )
                )
    return output


def normalize_png(
    path: Path,
    target_max: int,
    alpha_threshold: int,
    remove_stray_area: int,
) -> dict[str, object]:
    width, height, pixels = read_png_rgba(path)
    original_bbox = alpha_bbox(width, height, pixels, alpha_threshold)
    if original_bbox is None:
        return {"file": path.name, "status": "empty"}

    cleaned = strip_low_alpha(pixels, alpha_threshold)
    cleaned, removed_components = remove_tiny_components(
        width,
        height,
        cleaned,
        alpha_threshold,
        remove_stray_area,
    )
    bbox = alpha_bbox(width, height, cleaned, alpha_threshold)
    if bbox is None:
        return {"file": path.name, "status": "empty_after_clean"}

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

    write_png_rgba(path, CANVAS_SIZE, CANVAS_SIZE, canvas)
    final_bbox = alpha_bbox(CANVAS_SIZE, CANVAS_SIZE, canvas, alpha_threshold)
    margins = None
    if final_bbox is not None:
        left, top, right, bottom = final_bbox
        margins = (left, top, CANVAS_SIZE - right, CANVAS_SIZE - bottom)

    return {
        "file": path.name,
        "status": "normalized",
        "originalSize": [width, height],
        "originalBbox": list(original_bbox),
        "cleanBbox": list(bbox),
        "finalBbox": list(final_bbox) if final_bbox else None,
        "finalMargins": list(margins) if margins else None,
        "removedTinyComponents": removed_components,
    }


def audit_png(
    path: Path,
    alpha_threshold: int,
    min_margin: int,
) -> dict[str, object] | None:
    width, height, pixels = read_png_rgba(path)
    bbox = alpha_bbox(width, height, pixels, alpha_threshold)
    if bbox is None:
        return {"file": path.name, "reason": "empty"}
    left, top, right, bottom = bbox
    margins = (left, top, width - right, height - bottom)
    edge_pixels = 0
    for y in range(height):
        for x in range(width):
            if pixels[(y * width) + x][3] > alpha_threshold and (
                x < 2 or y < 2 or x >= width - 2 or y >= height - 2
            ):
                edge_pixels += 1
    reasons = []
    if (width, height) != (CANVAS_SIZE, CANVAS_SIZE):
        reasons.append(f"size={width}x{height}")
    if min(margins) < min_margin:
        reasons.append(f"margins={margins}")
    if edge_pixels > 0:
        reasons.append(f"edgePixels={edge_pixels}")
    if not reasons:
        return None
    return {"file": path.name, "reasons": reasons, "bbox": list(bbox), "margins": list(margins)}


def main(argv: Iterable[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Normalize premium ImageCard PNG crop, scale, and transparent padding.")
    parser.add_argument("--root", default="Assets/Resources/ImageCards/Premium")
    parser.add_argument("--target-max", type=int, default=DEFAULT_TARGET_MAX)
    parser.add_argument("--alpha-threshold", type=int, default=DEFAULT_ALPHA_THRESHOLD)
    parser.add_argument("--min-margin", type=int, default=DEFAULT_MIN_MARGIN)
    parser.add_argument("--remove-stray-area", type=int, default=DEFAULT_REMOVE_STRAY_AREA)
    parser.add_argument("--only", nargs="*", default=None)
    parser.add_argument("--audit-only", action="store_true")
    parser.add_argument("--report", default="/private/tmp/imagecard_normalize_report.json")
    args = parser.parse_args(argv)

    root = Path(args.root)
    if args.only:
        names = {name if name.endswith(".png") else f"{name}.png" for name in args.only}
        paths = [root / name for name in sorted(names)]
    else:
        paths = sorted(root.glob("*.png"))

    report = []
    if not args.audit_only:
        for path in paths:
            if path.exists():
                report.append(normalize_png(path, args.target_max, args.alpha_threshold, args.remove_stray_area))

    failures = []
    for path in paths:
        if path.exists():
            failure = audit_png(path, args.alpha_threshold, args.min_margin)
            if failure is not None:
                failures.append(failure)

    payload = {"normalized": report, "failures": failures}
    Path(args.report).write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"checked={len(paths)} normalized={len(report)} failures={len(failures)} report={args.report}")
    for item in failures[:80]:
        print(item)
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
