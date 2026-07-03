#!/usr/bin/env python3
from __future__ import annotations

import math
from pathlib import Path

from normalize_imagecard_assets import resize_premultiplied
from process_imagecard_sheet import write_png_rgba


CANVAS = 240
SCALE = 4
W = CANVAS * SCALE
H = CANVAS * SCALE
OUT_DIR = Path("Assets/Resources/ImageCards/Premium")


Color = tuple[int, int, int, int]


def blank() -> list[Color]:
    return [(0, 0, 0, 0) for _ in range(W * H)]


def clamp(value: float, lo: int = 0, hi: int = 255) -> int:
    return max(lo, min(hi, round(value)))


def lerp(a: float, b: float, t: float) -> float:
    return a + ((b - a) * t)


def mix(c1: Color, c2: Color, t: float) -> Color:
    return (
        clamp(lerp(c1[0], c2[0], t)),
        clamp(lerp(c1[1], c2[1], t)),
        clamp(lerp(c1[2], c2[2], t)),
        clamp(lerp(c1[3], c2[3], t)),
    )


def over(dst: Color, src: Color) -> Color:
    sa = src[3] / 255.0
    if sa <= 0:
        return dst
    da = dst[3] / 255.0
    out_a = sa + (da * (1 - sa))
    if out_a <= 0:
        return (0, 0, 0, 0)
    return (
        clamp(((src[0] * sa) + (dst[0] * da * (1 - sa))) / out_a),
        clamp(((src[1] * sa) + (dst[1] * da * (1 - sa))) / out_a),
        clamp(((src[2] * sa) + (dst[2] * da * (1 - sa))) / out_a),
        clamp(out_a * 255),
    )


def logical_bbox(bbox: tuple[float, float, float, float]) -> tuple[int, int, int, int]:
    left, top, right, bottom = bbox
    return (
        max(0, math.floor(left * SCALE)),
        max(0, math.floor(top * SCALE)),
        min(W, math.ceil(right * SCALE)),
        min(H, math.ceil(bottom * SCALE)),
    )


def draw_ellipse(
    pixels: list[Color],
    bbox: tuple[float, float, float, float],
    color_top: Color,
    color_bottom: Color | None = None,
) -> None:
    left, top, right, bottom = bbox
    x0, y0, x1, y1 = logical_bbox(bbox)
    cx = (left + right) * 0.5
    cy = (top + bottom) * 0.5
    rx = max(0.001, (right - left) * 0.5)
    ry = max(0.001, (bottom - top) * 0.5)
    color_bottom = color_top if color_bottom is None else color_bottom
    for y in range(y0, y1):
        ly = (y + 0.5) / SCALE
        ty = 0 if bottom == top else (ly - top) / (bottom - top)
        for x in range(x0, x1):
            lx = (x + 0.5) / SCALE
            distance = ((lx - cx) / rx) ** 2 + ((ly - cy) / ry) ** 2
            if distance > 1:
                continue
            color = mix(color_top, color_bottom, max(0, min(1, ty)))
            pixels[(y * W) + x] = over(pixels[(y * W) + x], color)


def clear_ellipse(pixels: list[Color], bbox: tuple[float, float, float, float]) -> None:
    left, top, right, bottom = bbox
    x0, y0, x1, y1 = logical_bbox(bbox)
    cx = (left + right) * 0.5
    cy = (top + bottom) * 0.5
    rx = max(0.001, (right - left) * 0.5)
    ry = max(0.001, (bottom - top) * 0.5)
    for y in range(y0, y1):
        ly = (y + 0.5) / SCALE
        for x in range(x0, x1):
            lx = (x + 0.5) / SCALE
            if ((lx - cx) / rx) ** 2 + ((ly - cy) / ry) ** 2 <= 1:
                pixels[(y * W) + x] = (0, 0, 0, 0)


def draw_ellipse_stroke(
    pixels: list[Color],
    bbox: tuple[float, float, float, float],
    color: Color,
    thickness: float,
) -> None:
    left, top, right, bottom = bbox
    x0, y0, x1, y1 = logical_bbox((left - thickness, top - thickness, right + thickness, bottom + thickness))
    cx = (left + right) * 0.5
    cy = (top + bottom) * 0.5
    rx = max(0.001, (right - left) * 0.5)
    ry = max(0.001, (bottom - top) * 0.5)
    band = thickness / max(1, min(rx, ry))
    for y in range(y0, y1):
        ly = (y + 0.5) / SCALE
        for x in range(x0, x1):
            lx = (x + 0.5) / SCALE
            norm = math.sqrt(((lx - cx) / rx) ** 2 + ((ly - cy) / ry) ** 2)
            if abs(norm - 1) <= band:
                pixels[(y * W) + x] = over(pixels[(y * W) + x], color)


def rounded_rect_mask(px: float, py: float, bbox: tuple[float, float, float, float], radius: float) -> bool:
    left, top, right, bottom = bbox
    cx = min(max(px, left + radius), right - radius)
    cy = min(max(py, top + radius), bottom - radius)
    return ((px - cx) ** 2 + (py - cy) ** 2) <= radius ** 2


def draw_round_rect(
    pixels: list[Color],
    bbox: tuple[float, float, float, float],
    radius: float,
    color_top: Color,
    color_bottom: Color | None = None,
) -> None:
    left, top, right, bottom = bbox
    x0, y0, x1, y1 = logical_bbox(bbox)
    color_bottom = color_top if color_bottom is None else color_bottom
    for y in range(y0, y1):
        ly = (y + 0.5) / SCALE
        ty = 0 if bottom == top else (ly - top) / (bottom - top)
        for x in range(x0, x1):
            lx = (x + 0.5) / SCALE
            if not rounded_rect_mask(lx, ly, bbox, radius):
                continue
            color = mix(color_top, color_bottom, max(0, min(1, ty)))
            pixels[(y * W) + x] = over(pixels[(y * W) + x], color)


def draw_line(
    pixels: list[Color],
    points: list[tuple[float, float]],
    color: Color,
    radius: float,
) -> None:
    for start, end in zip(points, points[1:]):
        x1, y1 = start
        x2, y2 = end
        length = max(1, math.hypot(x2 - x1, y2 - y1))
        steps = max(1, round(length * 2))
        for index in range(steps + 1):
            t = index / steps
            draw_ellipse(
                pixels,
                (
                    lerp(x1, x2, t) - radius,
                    lerp(y1, y2, t) - radius,
                    lerp(x1, x2, t) + radius,
                    lerp(y1, y2, t) + radius,
                ),
                color,
            )


def shadow(pixels: list[Color], bbox: tuple[float, float, float, float], alpha: int = 58) -> None:
    draw_ellipse(pixels, bbox, (34, 24, 16, alpha))


def add_highlight(pixels: list[Color], bbox: tuple[float, float, float, float], alpha: int = 85) -> None:
    draw_ellipse(pixels, bbox, (255, 255, 255, alpha))


def save(name: str, pixels: list[Color]) -> None:
    small = resize_premultiplied(W, H, pixels, CANVAS, CANVAS)
    write_png_rgba(OUT_DIR / f"{name}.png", CANVAS, CANVAS, small)


def draw_mug(name: str, body_top: Color, body_bottom: Color, liquid: Color | None = None) -> None:
    p = blank()
    shadow(p, (54, 180, 186, 211), 45)
    draw_ellipse(p, (151, 78, 209, 150), body_top, body_bottom)
    clear_ellipse(p, (163, 91, 197, 136))
    draw_round_rect(p, (55, 67, 162, 190), 18, body_top, body_bottom)
    draw_ellipse(p, (55, 52, 162, 82), mix(body_top, (255, 255, 255, 255), 0.22), body_top)
    draw_ellipse(p, (67, 58, 151, 76), (248, 238, 218, 255))
    if liquid is not None:
        draw_ellipse(p, (72, 60, 146, 74), liquid, mix(liquid, (20, 10, 5, 255), 0.28))
    draw_ellipse_stroke(p, (55, 52, 162, 82), (255, 234, 184, 190), 2.0)
    add_highlight(p, (76, 78, 101, 167), 60)
    save(name, p)


def draw_teacup(name: str, porcelain: Color, pattern: bool = False, tea: bool = False) -> None:
    p = blank()
    shadow(p, (53, 178, 193, 211), 40)
    draw_ellipse(p, (47, 164, 193, 200), (240, 218, 170, 210))
    draw_ellipse(p, (57, 169, 183, 192), (252, 249, 238, 255))
    draw_ellipse(p, (151, 88, 205, 148), (236, 220, 180, 255))
    clear_ellipse(p, (164, 99, 194, 135))
    draw_round_rect(p, (62, 79, 162, 169), 21, (255, 255, 249, 255), porcelain)
    draw_ellipse(p, (58, 64, 166, 92), (255, 255, 250, 255), (242, 227, 195, 255))
    draw_ellipse(p, (70, 70, 154, 86), (245, 236, 216, 255))
    if tea:
        draw_ellipse(p, (75, 72, 149, 84), (211, 122, 25, 255), (142, 73, 17, 255))
        draw_ellipse(p, (163, 154, 193, 176), (55, 152, 62, 255), (28, 104, 41, 255))
        draw_ellipse(p, (183, 151, 213, 172), (75, 177, 79, 255), (31, 112, 42, 255))
    if pattern:
        for cx, cy in [(92, 113), (116, 107), (135, 124), (104, 138)]:
            draw_ellipse(p, (cx - 6, cy - 4, cx + 6, cy + 4), (68, 126, 204, 170))
            draw_ellipse(p, (cx - 3, cy - 8, cx + 3, cy + 8), (68, 126, 204, 150))
        draw_ellipse_stroke(p, (72, 95, 153, 153), (69, 126, 204, 150), 1.2)
    draw_ellipse_stroke(p, (58, 64, 166, 92), (212, 150, 54, 210), 2.2)
    add_highlight(p, (77, 86, 102, 137), 55)
    save(name, p)


def draw_glass_milk(name: str) -> None:
    p = blank()
    shadow(p, (71, 183, 169, 211), 38)
    draw_round_rect(p, (76, 50, 164, 190), 15, (220, 245, 255, 95), (115, 180, 205, 120))
    draw_round_rect(p, (83, 82, 157, 184), 10, (255, 250, 224, 240), (235, 218, 178, 245))
    draw_ellipse(p, (75, 43, 165, 68), (235, 250, 255, 170))
    draw_ellipse(p, (83, 49, 157, 63), (255, 255, 245, 190))
    draw_ellipse_stroke(p, (75, 43, 165, 68), (128, 190, 218, 220), 2.0)
    draw_ellipse_stroke(p, (77, 173, 163, 194), (119, 184, 213, 200), 2.0)
    add_highlight(p, (92, 62, 110, 170), 70)
    save(name, p)


def draw_handle(name: str) -> None:
    p = blank()
    shadow(p, (52, 178, 196, 211), 38)
    draw_ellipse(p, (147, 87, 207, 154), (225, 205, 164, 255))
    clear_ellipse(p, (160, 100, 194, 141))
    draw_round_rect(p, (60, 79, 166, 174), 21, (255, 255, 247, 255), (232, 218, 188, 255))
    draw_ellipse(p, (57, 62, 169, 94), (255, 255, 250, 255), (236, 217, 181, 255))
    draw_ellipse(p, (70, 70, 156, 87), (229, 154, 55, 255), (146, 82, 27, 255))
    draw_ellipse_stroke(p, (57, 62, 169, 94), (202, 128, 39, 220), 2.2)
    draw_ellipse_stroke(p, (147, 87, 207, 154), (202, 128, 39, 220), 2.4)
    add_highlight(p, (80, 91, 105, 143), 56)
    save(name, p)


def draw_saucer(name: str) -> None:
    p = blank()
    shadow(p, (39, 167, 201, 207), 44)
    draw_ellipse(p, (39, 74, 201, 172), (255, 253, 244, 255), (232, 216, 184, 255))
    draw_ellipse_stroke(p, (42, 77, 198, 169), (213, 144, 31, 230), 3.3)
    draw_ellipse_stroke(p, (48, 83, 192, 162), (25, 76, 156, 240), 4.4)
    draw_ellipse(p, (72, 98, 168, 148), (251, 244, 229, 255), (228, 214, 190, 255))
    draw_ellipse_stroke(p, (77, 102, 163, 145), (209, 187, 150, 170), 2.0)
    add_highlight(p, (65, 88, 120, 112), 48)
    save(name, p)


def draw_thermos(name: str) -> None:
    p = blank()
    shadow(p, (72, 183, 170, 211), 42)
    draw_ellipse(p, (72, 175, 158, 199), (30, 89, 43, 220))
    draw_round_rect(p, (76, 57, 158, 187), 17, (43, 143, 82, 255), (16, 83, 49, 255))
    draw_ellipse(p, (77, 45, 157, 68), (218, 224, 215, 255), (95, 104, 101, 255))
    draw_round_rect(p, (88, 31, 146, 55), 7, (214, 221, 218, 255), (90, 98, 96, 255))
    draw_ellipse(p, (145, 83, 198, 160), (24, 96, 57, 255), (10, 60, 38, 255))
    clear_ellipse(p, (157, 96, 185, 149))
    draw_round_rect(p, (151, 93, 181, 152), 9, (33, 118, 70, 255), (12, 65, 42, 255))
    clear_ellipse(p, (160, 104, 176, 143))
    add_highlight(p, (92, 73, 108, 165), 58)
    save(name, p)


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    draw_mug("cup_coffee", (236, 45, 35, 255), (162, 16, 28, 255), (70, 32, 15, 255))
    draw_teacup("cup_cup", (236, 229, 202, 255), pattern=True)
    draw_glass_milk("cup_drink")
    draw_handle("cup_handle")
    draw_mug("cup_mug", (33, 124, 190, 255), (8, 77, 136, 255), None)
    draw_saucer("cup_saucer")
    draw_teacup("cup_tea", (245, 236, 214, 255), pattern=False, tea=True)
    draw_thermos("cup_thermos")
    for name in (
        "cup_coffee",
        "cup_cup",
        "cup_drink",
        "cup_handle",
        "cup_mug",
        "cup_saucer",
        "cup_tea",
        "cup_thermos",
    ):
        print(f"wrote {OUT_DIR / (name + '.png')}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
