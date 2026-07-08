#!/usr/bin/env python3
"""Generate a complete procedural ImageCard source set.

This intentionally writes only to /tmp by default. Runtime replacement should be
done as a separate reviewed step.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import shutil
from pathlib import Path
from typing import Callable

from PIL import Image, ImageDraw, ImageFilter

PROJECT_ROOT = Path(__file__).resolve().parents[1]
CARDS_JSON = PROJECT_ROOT / "Assets/Resources/Data/cards.json"
RAW_SIZE = 1024
LOGICAL_SIZE = 240


Color = tuple[int, int, int, int]


def rgba(color: tuple[int, int, int] | tuple[int, int, int, int], alpha: int | None = None) -> Color:
    if len(color) == 4:
        r, g, b, a = color
        return (r, g, b, a if alpha is None else alpha)
    r, g, b = color
    return (r, g, b, 255 if alpha is None else alpha)


def mix(a: Color, b: Color, t: float) -> Color:
    t = max(0.0, min(1.0, t))
    return tuple(round(a[i] * (1 - t) + b[i] * t) for i in range(4))  # type: ignore[return-value]


def lighten(color: Color, amount: float) -> Color:
    return mix(color, (255, 255, 255, color[3]), amount)


def darken(color: Color, amount: float) -> Color:
    return mix(color, (0, 0, 0, color[3]), amount)


def seeded_angle(key: str) -> float:
    digest = hashlib.sha1(key.encode("utf-8")).digest()
    return (digest[0] / 255.0 - 0.5) * 0.28


class IconCanvas:
    def __init__(self, card_id: str) -> None:
        self.card_id = card_id
        self.size = RAW_SIZE
        self.scale = RAW_SIZE / LOGICAL_SIZE
        self.image = Image.new("RGBA", (self.size, self.size), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)

    def p(self, value: float) -> int:
        return int(round(value * self.scale))

    def box(self, values: tuple[float, float, float, float]) -> tuple[int, int, int, int]:
        return tuple(self.p(v) for v in values)  # type: ignore[return-value]

    def pts(self, points: list[tuple[float, float]]) -> list[tuple[int, int]]:
        return [(self.p(x), self.p(y)) for x, y in points]

    def layer(self) -> Image.Image:
        return Image.new("RGBA", (self.size, self.size), (0, 0, 0, 0))

    def composite(self, layer: Image.Image) -> None:
        self.image.alpha_composite(layer)

    def shadow(self, cx: float = 120, cy: float = 188, w: float = 118, h: float = 25, alpha: int = 72) -> None:
        layer = self.layer()
        d = ImageDraw.Draw(layer)
        d.ellipse(self.box((cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)), fill=(0, 0, 0, alpha))
        layer = layer.filter(ImageFilter.GaussianBlur(self.p(4.2)))
        self.composite(layer)

    def gradient(self, width: int, height: int, top: Color, bottom: Color) -> Image.Image:
        img = Image.new("RGBA", (width, height))
        d = ImageDraw.Draw(img)
        denom = max(1, height - 1)
        for y in range(height):
            d.line((0, y, width, y), fill=mix(top, bottom, y / denom))
        return img

    def fill_mask(self, mask: Image.Image, top: Color, bottom: Color | None = None) -> None:
        bbox = mask.getbbox()
        if not bbox:
            return
        x0, y0, x1, y1 = bbox
        fill = self.gradient(x1 - x0, y1 - y0, top, bottom or top)
        layer = self.layer()
        layer.paste(fill, (x0, y0), mask.crop(bbox))
        self.composite(layer)

    def ellipse(
        self,
        box: tuple[float, float, float, float],
        color: Color,
        bottom: Color | None = None,
        outline: Color | None = None,
        width: float = 2,
    ) -> None:
        mask = Image.new("L", (self.size, self.size), 0)
        d = ImageDraw.Draw(mask)
        d.ellipse(self.box(box), fill=255)
        self.fill_mask(mask, lighten(color, 0.18), bottom or darken(color, 0.10))
        if outline:
            self.draw.ellipse(self.box(box), outline=outline, width=self.p(width))

    def round_rect(
        self,
        box: tuple[float, float, float, float],
        radius: float,
        color: Color,
        bottom: Color | None = None,
        outline: Color | None = None,
        width: float = 2,
    ) -> None:
        mask = Image.new("L", (self.size, self.size), 0)
        d = ImageDraw.Draw(mask)
        d.rounded_rectangle(self.box(box), radius=self.p(radius), fill=255)
        self.fill_mask(mask, lighten(color, 0.16), bottom or darken(color, 0.12))
        if outline:
            self.draw.rounded_rectangle(self.box(box), radius=self.p(radius), outline=outline, width=self.p(width))

    def rect(self, box: tuple[float, float, float, float], color: Color, outline: Color | None = None) -> None:
        self.draw.rectangle(self.box(box), fill=color, outline=outline)

    def polygon(
        self,
        points: list[tuple[float, float]],
        color: Color,
        bottom: Color | None = None,
        outline: Color | None = None,
        width: float = 2,
    ) -> None:
        mask = Image.new("L", (self.size, self.size), 0)
        d = ImageDraw.Draw(mask)
        d.polygon(self.pts(points), fill=255)
        self.fill_mask(mask, lighten(color, 0.12), bottom or darken(color, 0.12))
        if outline:
            self.draw.line(self.pts(points + [points[0]]), fill=outline, width=self.p(width), joint="curve")

    def line(self, points: list[tuple[float, float]], color: Color, width: float = 8) -> None:
        self.draw.line(self.pts(points), fill=color, width=self.p(width), joint="curve")

    def highlight(self, box: tuple[float, float, float, float], alpha: int = 76) -> None:
        self.ellipse(box, (255, 255, 255, alpha), (255, 255, 255, alpha))

    def small_highlight(self, x: float, y: float, w: float = 24, h: float = 10) -> None:
        layer = self.layer()
        d = ImageDraw.Draw(layer)
        d.ellipse(self.box((x, y, x + w, y + h)), fill=(255, 255, 255, 84))
        self.composite(layer.filter(ImageFilter.GaussianBlur(self.p(0.6))))

    def erase_ellipse(self, box: tuple[float, float, float, float]) -> None:
        mask = Image.new("L", (self.size, self.size), 0)
        d = ImageDraw.Draw(mask)
        d.ellipse(self.box(box), fill=255)
        alpha = self.image.getchannel("A")
        alpha.paste(0, mask=mask)
        self.image.putalpha(alpha)
        self.draw = ImageDraw.Draw(self.image)

    def erase_round_rect(self, box: tuple[float, float, float, float], radius: float) -> None:
        mask = Image.new("L", (self.size, self.size), 0)
        d = ImageDraw.Draw(mask)
        d.rounded_rectangle(self.box(box), radius=self.p(radius), fill=255)
        alpha = self.image.getchannel("A")
        alpha.paste(0, mask=mask)
        self.image.putalpha(alpha)
        self.draw = ImageDraw.Draw(self.image)

    def regular_polygon(self, cx: float, cy: float, radius: float, sides: int, color: Color, rotation: float = -math.pi / 2) -> None:
        pts = [
            (cx + math.cos(rotation + i * math.tau / sides) * radius, cy + math.sin(rotation + i * math.tau / sides) * radius)
            for i in range(sides)
        ]
        self.polygon(pts, color, outline=darken(color, 0.30))

    def star(self, cx: float, cy: float, outer: float, inner: float, color: Color) -> None:
        points = []
        for i in range(10):
            radius = outer if i % 2 == 0 else inner
            angle = -math.pi / 2 + i * math.pi / 5
            points.append((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))
        self.polygon(points, color, outline=darken(color, 0.22))

    def save(self, path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        self.image.save(path)


BLUE = rgba((78, 155, 232))
TEAL = rgba((69, 190, 182))
GREEN = rgba((79, 178, 92))
LIME = rgba((136, 205, 91))
YELLOW = rgba((246, 200, 63))
ORANGE = rgba((241, 137, 54))
RED = rgba((225, 82, 76))
PINK = rgba((236, 108, 162))
PURPLE = rgba((135, 105, 220))
BROWN = rgba((151, 101, 58))
TAN = rgba((225, 184, 123))
CREAM = rgba((248, 236, 204))
GRAY = rgba((158, 168, 178))
DARK = rgba((66, 74, 86))
WHITE = rgba((245, 247, 249))
BLACK = rgba((42, 46, 52))


PALETTES: dict[str, list[Color]] = {
    "business": [BLUE, TEAL, YELLOW, ORANGE],
    "cup": [CREAM, ORANGE, BLUE, BROWN],
    "food": [ORANGE, GREEN, RED, CREAM],
    "time": [BLUE, YELLOW, PURPLE, GRAY],
    "travel": [TEAL, ORANGE, BLUE, TAN],
    "music": [PURPLE, PINK, YELLOW, BLUE],
    "sport": [GREEN, ORANGE, BLUE, RED],
    "nature": [GREEN, LIME, TEAL, BROWN],
    "weather": [YELLOW, BLUE, GRAY, WHITE],
    "city": [BLUE, GRAY, TEAL, ORANGE],
    "home": [TAN, BLUE, ORANGE, CREAM],
    "kitchen": [TEAL, GRAY, ORANGE, CREAM],
    "school": [BLUE, YELLOW, GREEN, ORANGE],
    "books": [BROWN, BLUE, RED, CREAM],
    "art": [PINK, YELLOW, BLUE, GREEN],
    "cinema": [RED, ORANGE, DARK, YELLOW],
    "family": [PINK, BLUE, ORANGE, GREEN],
    "pets": [TAN, BROWN, ORANGE, BLUE],
    "garden": [GREEN, BROWN, ORANGE, LIME],
    "transport": [BLUE, ORANGE, TEAL, GRAY],
    "sea": [TEAL, BLUE, CREAM, ORANGE],
    "space": [PURPLE, BLUE, YELLOW, GRAY],
    "colors": [RED, BLUE, GREEN, YELLOW],
    "shapes": [BLUE, ORANGE, PURPLE, GREEN],
    "clothes": [PINK, BLUE, ORANGE, GREEN],
    "shoes": [BROWN, BLUE, RED, GRAY],
    "health": [TEAL, RED, BLUE, WHITE],
    "body": [TAN, RED, BLUE, PINK],
    "work": [BLUE, ORANGE, TEAL, GRAY],
    "office": [BLUE, GRAY, ORANGE, TEAL],
    "technology": [TEAL, PURPLE, BLUE, GRAY],
    "internet": [BLUE, TEAL, ORANGE, PURPLE],
    "phone": [BLUE, TEAL, PURPLE, GRAY],
    "photo": [BLUE, ORANGE, DARK, GRAY],
    "holidays": [PINK, YELLOW, RED, BLUE],
    "winter": [BLUE, WHITE, TEAL, RED],
    "summer": [YELLOW, TEAL, ORANGE, GREEN],
    "breakfast": [ORANGE, CREAM, PINK, YELLOW],
    "dessert": [PINK, CREAM, ORANGE, BROWN],
    "drinks": [TEAL, ORANGE, BROWN, CREAM],
    "farm": [GREEN, BROWN, RED, YELLOW],
    "forest": [GREEN, BROWN, ORANGE, LIME],
    "flowers": [PINK, RED, GREEN, PURPLE],
    "insects": [YELLOW, BLACK, RED, GREEN],
    "emotions": [YELLOW, PINK, TEAL, ORANGE],
    "friendship": [PINK, TEAL, BLUE, YELLOW],
    "hobbies": [PURPLE, ORANGE, BLUE, GREEN],
    "camping": [GREEN, ORANGE, BROWN, BLUE],
    "toys": [YELLOW, RED, BLUE, GREEN],
    "tools": [ORANGE, GRAY, BLUE, BROWN],
}


def palette(category: str) -> list[Color]:
    return PALETTES.get(category, [BLUE, ORANGE, GREEN, PURPLE])


def draw_badge(ic: IconCanvas, color: Color = BLUE, shape: str = "round") -> None:
    ic.shadow(w=112, h=23)
    if shape == "square":
        ic.round_rect((61, 61, 179, 179), 22, color, outline=darken(color, 0.25))
    elif shape == "triangle":
        ic.polygon([(120, 48), (184, 180), (56, 180)], color, outline=darken(color, 0.25))
    elif shape == "star":
        ic.star(120, 122, 70, 34, color)
    else:
        ic.ellipse((57, 55, 183, 181), color, outline=darken(color, 0.25))
    ic.small_highlight(83, 78, 35, 15)


def draw_drop(ic: IconCanvas, color: Color) -> None:
    ic.shadow(w=102, h=22)
    points = [(120, 38), (160, 95), (169, 135), (153, 174), (120, 190), (87, 174), (71, 135), (80, 95)]
    ic.polygon(points, color, outline=darken(color, 0.24))
    ic.ellipse((82, 99, 158, 185), color, outline=darken(color, 0.24))
    ic.small_highlight(94, 83, 28, 14)


def draw_box(ic: IconCanvas, color: Color = ORANGE, ribbon: bool = False) -> None:
    ic.shadow()
    top = lighten(color, 0.28)
    side = darken(color, 0.10)
    ic.polygon([(72, 84), (120, 56), (168, 84), (120, 111)], top, outline=darken(color, 0.25))
    ic.polygon([(72, 84), (120, 111), (120, 178), (72, 150)], color, outline=darken(color, 0.25))
    ic.polygon([(168, 84), (120, 111), (120, 178), (168, 150)], side, outline=darken(color, 0.25))
    if ribbon:
        ic.line([(120, 62), (120, 176)], RED, 8)
        ic.line([(80, 89), (159, 89)], RED, 7)
        ic.ellipse((97, 48, 121, 74), RED, outline=darken(RED, 0.2))
        ic.ellipse((119, 48, 143, 74), RED, outline=darken(RED, 0.2))
    ic.small_highlight(84, 93, 28, 11)


def draw_document(ic: IconCanvas, color: Color = WHITE, accent: Color = BLUE, folder: bool = False) -> None:
    ic.shadow(w=104, h=20)
    if folder:
        ic.round_rect((55, 84, 185, 177), 14, darken(accent, 0.03), outline=darken(accent, 0.28))
        ic.round_rect((64, 69, 119, 96), 9, lighten(accent, 0.18), outline=darken(accent, 0.25))
        ic.round_rect((55, 91, 185, 178), 14, accent, outline=darken(accent, 0.28))
        ic.small_highlight(73, 105, 38, 10)
        return
    ic.round_rect((70, 50, 166, 182), 10, color, outline=(197, 203, 211, 255))
    ic.polygon([(140, 50), (166, 76), (140, 76)], (225, 230, 238, 255), outline=(190, 198, 207, 255))
    for y, w in [(97, 58), (115, 70), (133, 54), (151, 64)]:
        ic.round_rect((88, y, 88 + w, y + 7), 3, rgba((*accent[:3], 185)), outline=None)
    ic.small_highlight(83, 65, 28, 9)


def draw_person(ic: IconCanvas, color: Color = BLUE, hair: Color = BROWN, variant: str = "single") -> None:
    ic.shadow(w=104 if variant != "group" else 132, h=21)
    if variant == "group":
        for x, c in [(85, TEAL), (120, color), (155, ORANGE)]:
            ic.ellipse((x - 18, 75, x + 18, 111), TAN, outline=darken(TAN, 0.18))
            ic.round_rect((x - 25, 112, x + 25, 176), 18, c, outline=darken(c, 0.25))
        return
    if variant == "reader":
        draw_book(ic, RED, open_book=True, shadow=False, y_offset=30)
    ic.round_rect((84, 106, 156, 181), 24, color, outline=darken(color, 0.25))
    ic.ellipse((87, 57, 153, 123), TAN, outline=darken(TAN, 0.18))
    ic.ellipse((87, 52, 153, 94), hair, outline=darken(hair, 0.15))
    if variant == "smile":
        ic.ellipse((101, 84, 107, 90), DARK)
        ic.ellipse((133, 84, 139, 90), DARK)
        ic.line([(105, 101), (118, 110), (136, 101)], RED, 3)
    ic.small_highlight(100, 67, 22, 9)


def draw_two_people(ic: IconCanvas, color1: Color = BLUE, color2: Color = PINK) -> None:
    ic.shadow(w=124, h=22)
    for x, c in [(100, color1), (142, color2)]:
        ic.round_rect((x - 27, 105, x + 27, 178), 19, c, outline=darken(c, 0.24))
        ic.ellipse((x - 24, 60, x + 24, 108), TAN, outline=darken(TAN, 0.18))
        ic.ellipse((x - 24, 55, x + 24, 84), BROWN, outline=darken(BROWN, 0.12))


def draw_handshake(ic: IconCanvas) -> None:
    ic.shadow(w=125, h=20)
    ic.line([(67, 122), (102, 145), (123, 128)], TAN, 19)
    ic.line([(173, 122), (138, 145), (117, 128)], lighten(TAN, 0.08), 19)
    ic.round_rect((54, 102, 90, 137), 9, BLUE, outline=darken(BLUE, 0.22))
    ic.round_rect((150, 102, 186, 137), 9, PINK, outline=darken(PINK, 0.22))
    ic.ellipse((105, 123, 135, 153), TAN, outline=darken(TAN, 0.20))


def draw_book(ic: IconCanvas, color: Color = BROWN, open_book: bool = False, shadow: bool = True, y_offset: float = 0) -> None:
    if shadow:
        ic.shadow(w=112, h=20)
    y = y_offset
    if open_book:
        ic.round_rect((55, 75 + y, 120, 172 + y), 12, WHITE, outline=(189, 195, 202, 255))
        ic.round_rect((120, 75 + y, 185, 172 + y), 12, WHITE, outline=(189, 195, 202, 255))
        ic.line([(120, 78 + y), (120, 172 + y)], (170, 176, 184, 255), 3)
        for yy in [101 + y, 121 + y, 141 + y]:
            ic.line([(71, yy), (107, yy)], rgba((*BLUE[:3], 140)), 3)
            ic.line([(133, yy), (169, yy)], rgba((*BLUE[:3], 140)), 3)
        return
    ic.round_rect((68, 55 + y, 172, 181 + y), 13, color, outline=darken(color, 0.25))
    ic.round_rect((78, 65 + y, 162, 171 + y), 9, lighten(color, 0.10), outline=lighten(color, 0.28))
    ic.line([(92, 61 + y), (92, 178 + y)], darken(color, 0.24), 5)
    ic.round_rect((132, 58 + y, 146, 123 + y), 4, RED, outline=darken(RED, 0.22))
    ic.small_highlight(99, 76 + y, 28, 9)


def draw_stack_books(ic: IconCanvas) -> None:
    ic.shadow(w=124, h=20)
    for y, c, dx in [(145, BLUE, 0), (113, ORANGE, -7), (81, GREEN, 6)]:
        ic.round_rect((64 + dx, y, 176 + dx, y + 28), 7, c, outline=darken(c, 0.25))
        ic.round_rect((76 + dx, y + 7, 153 + dx, y + 14), 3, lighten(c, 0.32))


def draw_cup(ic: IconCanvas, drink: Color = BROWN, body: Color = CREAM, handle: bool = True, steam: bool = False, thermos: bool = False) -> None:
    ic.shadow(w=112, h=22)
    if thermos:
        ic.round_rect((87, 45, 153, 183), 18, body, outline=darken(body, 0.28))
        ic.round_rect((94, 39, 146, 58), 8, darken(body, 0.08), outline=darken(body, 0.30))
        ic.round_rect((96, 84, 144, 136), 10, lighten(body, 0.15), outline=lighten(body, 0.26))
    else:
        ic.ellipse((70, 58, 170, 88), WHITE, outline=(197, 190, 180, 255))
        ic.round_rect((76, 68, 164, 178), 20, body, outline=darken(body, 0.22))
        ic.ellipse((83, 62, 157, 84), drink, outline=darken(drink, 0.10))
        if handle:
            ic.ellipse((148, 94, 191, 146), body, outline=darken(body, 0.22), width=4)
            ic.erase_ellipse((158, 105, 180, 135))
        if steam:
            for x in [101, 121, 141]:
                ic.line([(x, 48), (x - 8, 34), (x + 4, 23)], rgba((180, 185, 195), 150), 4)
    ic.small_highlight(91, 83, 24, 9)


def draw_bowl(ic: IconCanvas, fill: Color = ORANGE, contents: str = "soup") -> None:
    ic.shadow(w=122, h=22)
    ic.ellipse((62, 75, 178, 121), WHITE, outline=(192, 198, 206, 255))
    ic.ellipse((76, 83, 164, 112), fill, outline=darken(fill, 0.10))
    if contents == "cereal":
        for x, y, c in [(96, 94, RED), (117, 101, YELLOW), (137, 92, BLUE), (148, 104, PINK), (108, 87, ORANGE)]:
            ic.ellipse((x - 6, y - 5, x + 6, y + 7), c, outline=darken(c, 0.18))
    elif contents == "berries":
        for x, y, c in [(98, 93, RED), (122, 100, BLUE), (144, 91, PINK)]:
            ic.ellipse((x - 8, y - 7, x + 8, y + 8), c, outline=darken(c, 0.18))
    elif contents == "salad":
        for x, y, c in [(98, 96, GREEN), (117, 88, LIME), (135, 101, RED), (149, 91, GREEN)]:
            ic.ellipse((x - 10, y - 8, x + 10, y + 8), c, outline=darken(c, 0.18))
    ic.round_rect((69, 105, 171, 176), 26, lighten(fill, 0.24), bottom=darken(fill, 0.06), outline=darken(fill, 0.18))
    ic.small_highlight(85, 119, 30, 9)


def draw_plate(ic: IconCanvas, kind: str = "food") -> None:
    ic.shadow(w=125, h=18)
    ic.ellipse((55, 85, 185, 174), WHITE, outline=(195, 199, 207, 255))
    ic.ellipse((75, 103, 165, 158), (232, 237, 241, 255), outline=(205, 211, 218, 255))
    if kind == "fruit":
        draw_apple(ic, cx=113, cy=125, scale=0.66)
        ic.ellipse((128, 112, 158, 143), ORANGE, outline=darken(ORANGE, 0.18))
    elif kind == "salad":
        for x, y, c in [(98, 125, GREEN), (116, 114, LIME), (135, 133, RED), (146, 117, GREEN)]:
            ic.ellipse((x - 11, y - 9, x + 12, y + 9), c, outline=darken(c, 0.18))
    elif kind == "lunch":
        ic.ellipse((92, 111, 127, 145), ORANGE, outline=darken(ORANGE, 0.16))
        ic.round_rect((130, 111, 154, 151), 8, GREEN, outline=darken(GREEN, 0.18))
    else:
        ic.ellipse((95, 112, 145, 147), ORANGE, outline=darken(ORANGE, 0.18))


def draw_bread(ic: IconCanvas, toast: bool = False) -> None:
    ic.shadow(w=100, h=20)
    color = ORANGE if toast else TAN
    ic.round_rect((73, 73, 167, 178), 23, color, outline=darken(color, 0.24))
    ic.ellipse((78, 49, 119, 103), color, outline=darken(color, 0.24))
    ic.ellipse((121, 49, 162, 103), color, outline=darken(color, 0.24))
    if toast:
        ic.round_rect((93, 104, 147, 155), 12, lighten(color, 0.22), outline=darken(color, 0.12))
    ic.small_highlight(91, 82, 24, 9)


def draw_egg(ic: IconCanvas) -> None:
    ic.shadow(w=95, h=19)
    ic.ellipse((70, 64, 170, 179), WHITE, outline=(200, 204, 210, 255))
    ic.ellipse((102, 104, 141, 144), YELLOW, outline=darken(YELLOW, 0.18))
    ic.small_highlight(89, 83, 25, 10)


def draw_apple(ic: IconCanvas, cx: float = 120, cy: float = 128, scale: float = 1.0) -> None:
    r = 37 * scale
    ic.ellipse((cx - r, cy - r, cx + r, cy + r + 9 * scale), RED, outline=darken(RED, 0.20))
    ic.line([(cx + 2 * scale, cy - r + 5 * scale), (cx + 12 * scale, cy - r - 18 * scale)], BROWN, 5 * scale)
    ic.ellipse((cx + 9 * scale, cy - r - 18 * scale, cx + 37 * scale, cy - r + 4 * scale), GREEN, outline=darken(GREEN, 0.18))
    ic.small_highlight(cx - 19 * scale, cy - 18 * scale, 18 * scale, 8 * scale)


def draw_clock(ic: IconCanvas, alarm: bool = False, night: bool = False) -> None:
    ic.shadow(w=105, h=20)
    face = DARK if night else WHITE
    rim = PURPLE if night else BLUE
    if alarm:
        ic.ellipse((70, 43, 99, 72), rim, outline=darken(rim, 0.2))
        ic.ellipse((141, 43, 170, 72), rim, outline=darken(rim, 0.2))
    ic.ellipse((66, 55, 174, 163), rim, outline=darken(rim, 0.30))
    ic.ellipse((78, 67, 162, 151), face, outline=darken(face, 0.12))
    hand = YELLOW if night else DARK
    ic.line([(120, 109), (120, 82)], hand, 5)
    ic.line([(120, 109), (144, 124)], hand, 5)
    ic.ellipse((114, 103, 126, 115), hand)
    if alarm:
        ic.line([(91, 163), (80, 184)], rim, 6)
        ic.line([(149, 163), (160, 184)], rim, 6)


def draw_calendar(ic: IconCanvas) -> None:
    ic.shadow(w=105, h=20)
    ic.round_rect((66, 61, 174, 176), 13, WHITE, outline=(185, 193, 202, 255))
    ic.round_rect((66, 61, 174, 91), 13, RED, outline=darken(RED, 0.20))
    for x in [91, 149]:
        ic.line([(x, 53), (x, 73)], DARK, 5)
    for x in [88, 118, 148]:
        for y in [110, 137, 164]:
            ic.ellipse((x - 5, y - 5, x + 5, y + 5), BLUE)


def draw_moon(ic: IconCanvas) -> None:
    ic.shadow(w=88, h=18)
    ic.ellipse((75, 47, 170, 168), YELLOW, outline=darken(YELLOW, 0.18))
    ic.ellipse((111, 37, 192, 143), (0, 0, 0, 0), outline=None)
    layer = ic.layer()
    d = ImageDraw.Draw(layer)
    d.ellipse(ic.box((111, 37, 192, 143)), fill=(0, 0, 0, 0))
    ic.ellipse((103, 53, 185, 151), (238, 244, 249, 255), outline=None)


def draw_ticket(ic: IconCanvas, color: Color = ORANGE, travel: bool = False) -> None:
    ic.shadow(w=118, h=20)
    ic.round_rect((57, 81, 183, 157), 14, color, outline=darken(color, 0.25))
    ic.ellipse((47, 109, 72, 132), (0, 0, 0, 0))
    ic.ellipse((168, 109, 193, 132), (0, 0, 0, 0))
    if travel:
        ic.line([(87, 122), (128, 97), (153, 132)], BLUE, 6)
        ic.ellipse((81, 116, 93, 128), BLUE)
        ic.ellipse((147, 126, 159, 138), BLUE)
    else:
        ic.star(100, 119, 13, 6, YELLOW)
        ic.star(139, 119, 13, 6, YELLOW)
    ic.small_highlight(75, 96, 35, 9)


def draw_suitcase(ic: IconCanvas) -> None:
    ic.shadow(w=116, h=21)
    ic.line([(98, 72), (98, 55), (142, 55), (142, 72)], DARK, 6)
    ic.round_rect((63, 75, 177, 178), 17, ORANGE, outline=darken(ORANGE, 0.26))
    ic.line([(94, 78), (94, 175)], darken(ORANGE, 0.22), 5)
    ic.line([(146, 78), (146, 175)], darken(ORANGE, 0.22), 5)
    ic.small_highlight(78, 92, 26, 9)


def draw_vehicle(ic: IconCanvas, kind: str = "car", color: Color = BLUE) -> None:
    ic.shadow(w=130, h=21)
    if kind == "plane":
        ic.polygon([(54, 124), (184, 82), (155, 125), (185, 156)], color, outline=darken(color, 0.25))
        ic.polygon([(97, 111), (66, 76), (127, 101)], lighten(color, 0.12), outline=darken(color, 0.22))
        ic.polygon([(111, 136), (90, 174), (139, 127)], darken(color, 0.04), outline=darken(color, 0.22))
        return
    if kind == "train":
        ic.round_rect((54, 73, 186, 160), 18, color, outline=darken(color, 0.26))
        ic.round_rect((68, 89, 104, 119), 8, WHITE, outline=darken(WHITE, 0.12))
        ic.round_rect((113, 89, 149, 119), 8, WHITE, outline=darken(WHITE, 0.12))
        ic.round_rect((157, 88, 176, 145), 7, darken(color, 0.08))
    elif kind == "bus":
        ic.round_rect((52, 75, 188, 156), 16, color, outline=darken(color, 0.26))
        for x in [68, 105, 142]:
            ic.round_rect((x, 89, x + 28, 119), 7, WHITE, outline=darken(WHITE, 0.12))
    elif kind == "taxi":
        ic.round_rect((55, 94, 185, 150), 17, YELLOW, outline=darken(YELLOW, 0.26))
        ic.polygon([(83, 94), (103, 70), (139, 70), (160, 94)], YELLOW, outline=darken(YELLOW, 0.22))
        ic.round_rect((107, 65, 134, 78), 4, ORANGE, outline=darken(ORANGE, 0.18))
    else:
        ic.round_rect((55, 95, 185, 151), 17, color, outline=darken(color, 0.26))
        ic.polygon([(82, 95), (103, 72), (139, 72), (162, 95)], color, outline=darken(color, 0.22))
        ic.round_rect((101, 82, 139, 100), 5, WHITE, outline=darken(WHITE, 0.12))
    for x in [84, 158]:
        ic.ellipse((x - 15, 142, x + 15, 172), DARK, outline=BLACK)
        ic.ellipse((x - 7, 150, x + 7, 164), GRAY)


def draw_map_route(ic: IconCanvas, compass: bool = False) -> None:
    ic.shadow(w=120, h=20)
    if compass:
        ic.ellipse((69, 57, 171, 159), WHITE, outline=BLUE, width=4)
        ic.polygon([(120, 76), (134, 120), (120, 145), (106, 120)], RED, outline=darken(RED, 0.18))
        ic.polygon([(120, 76), (120, 145), (106, 120)], BLUE, outline=darken(BLUE, 0.18))
        return
    ic.polygon([(61, 76), (101, 60), (139, 78), (179, 63), (179, 163), (139, 179), (101, 161), (61, 177)], CREAM, outline=darken(CREAM, 0.30))
    ic.line([(82, 143), (112, 112), (143, 130), (162, 96)], BLUE, 6)
    for x, y, c in [(82, 143, GREEN), (112, 112, ORANGE), (162, 96, RED)]:
        ic.ellipse((x - 8, y - 8, x + 8, y + 8), c, outline=darken(c, 0.2))


def draw_hotel(ic: IconCanvas) -> None:
    ic.shadow(w=112, h=21)
    ic.round_rect((70, 55, 170, 178), 13, BLUE, outline=darken(BLUE, 0.25))
    ic.round_rect((87, 77, 108, 100), 5, WHITE)
    ic.round_rect((132, 77, 153, 100), 5, WHITE)
    ic.round_rect((87, 116, 108, 139), 5, WHITE)
    ic.round_rect((132, 116, 153, 139), 5, WHITE)
    ic.round_rect((105, 139, 135, 178), 8, DARK)


def draw_instrument(ic: IconCanvas, kind: str = "note") -> None:
    ic.shadow(w=112, h=20)
    if kind == "guitar":
        ic.ellipse((62, 103, 122, 171), ORANGE, outline=darken(ORANGE, 0.24))
        ic.ellipse((100, 86, 153, 148), ORANGE, outline=darken(ORANGE, 0.24))
        ic.line([(135, 95), (181, 51)], BROWN, 13)
        ic.ellipse((95, 117, 119, 141), DARK)
        ic.line([(109, 128), (175, 60)], DARK, 2)
    elif kind == "piano":
        ic.round_rect((58, 83, 182, 158), 14, DARK, outline=BLACK)
        for i in range(7):
            x = 67 + i * 16
            ic.round_rect((x, 101, x + 13, 153), 3, WHITE, outline=(206, 210, 216, 255))
        for x in [78, 110, 126, 158]:
            ic.round_rect((x, 101, x + 8, 130), 2, BLACK)
    else:
        stem_x = 139 if kind in {"melody", "song"} else 128
        ic.ellipse((71, 132, 112, 173), PURPLE, outline=darken(PURPLE, 0.24))
        ic.line([(107, 151), (107, 64)], PURPLE, 9)
        ic.line([(107, 64), (stem_x, 78)], PURPLE, 8)
        if kind in {"rhythm", "band", "concert"}:
            ic.ellipse((132, 120, 173, 161), PINK, outline=darken(PINK, 0.24))
            ic.line([(168, 139), (168, 58)], PINK, 9)


def draw_sport_ball(ic: IconCanvas, kind: str = "ball") -> None:
    ic.shadow(w=105, h=20)
    if kind == "medal":
        ic.polygon([(92, 52), (113, 52), (120, 103), (99, 103)], BLUE)
        ic.polygon([(127, 52), (148, 52), (141, 103), (120, 103)], RED)
        ic.ellipse((82, 92, 158, 168), YELLOW, outline=darken(YELLOW, 0.22))
        ic.star(120, 130, 21, 10, ORANGE)
        return
    color = ORANGE if kind == "ball" else GREEN
    ic.ellipse((67, 57, 173, 163), color, outline=darken(color, 0.25))
    ic.line([(74, 111), (166, 111)], darken(color, 0.28), 4)
    ic.line([(120, 60), (120, 160)], darken(color, 0.28), 4)
    ic.line([(85, 75), (155, 145)], darken(color, 0.22), 3)
    ic.line([(155, 75), (85, 145)], darken(color, 0.22), 3)


def draw_field(ic: IconCanvas) -> None:
    ic.shadow(w=126, h=21)
    ic.round_rect((55, 77, 185, 159), 17, GREEN, outline=darken(GREEN, 0.25))
    ic.line([(120, 80), (120, 157)], WHITE, 4)
    ic.ellipse((101, 102, 139, 140), (0, 0, 0, 0), outline=WHITE, width=3)
    ic.round_rect((55, 103, 75, 135), 4, (0, 0, 0, 0), outline=WHITE)
    ic.round_rect((165, 103, 185, 135), 4, (0, 0, 0, 0), outline=WHITE)


def draw_tree(ic: IconCanvas, kind: str = "tree") -> None:
    ic.shadow(w=110, h=22)
    if kind == "leaf":
        ic.ellipse((67, 55, 171, 159), GREEN, outline=darken(GREEN, 0.22))
        ic.line([(89, 138), (151, 77)], darken(GREEN, 0.24), 5)
        return
    trunk_color = BROWN
    ic.round_rect((107, 111, 133, 181), 8, trunk_color, outline=darken(trunk_color, 0.22))
    if kind == "pine":
        for y, w in [(57, 72), (88, 93), (120, 112)]:
            ic.polygon([(120, y), (120 - w / 2, y + 58), (120 + w / 2, y + 58)], GREEN, outline=darken(GREEN, 0.22))
    else:
        for x, y, r, c in [(91, 88, 32, GREEN), (124, 72, 38, LIME), (149, 99, 34, GREEN), (117, 111, 42, GREEN)]:
            ic.ellipse((x - r, y - r, x + r, y + r), c, outline=darken(c, 0.18))


def draw_mountain(ic: IconCanvas) -> None:
    ic.shadow(w=126, h=21)
    ic.polygon([(54, 169), (111, 61), (167, 169)], BLUE, outline=darken(BLUE, 0.25))
    ic.polygon([(94, 94), (111, 61), (128, 94), (115, 86), (106, 99)], WHITE)
    ic.polygon([(107, 169), (153, 88), (194, 169)], PURPLE, outline=darken(PURPLE, 0.25))
    ic.polygon([(139, 113), (153, 88), (166, 113), (154, 107)], WHITE)


def draw_river(ic: IconCanvas) -> None:
    ic.shadow(w=126, h=18)
    ic.line([(79, 62), (124, 92), (97, 122), (148, 157), (116, 183)], TEAL, 22)
    ic.line([(82, 62), (126, 92), (100, 122), (150, 157), (120, 183)], lighten(TEAL, 0.25), 8)


def draw_cloud(ic: IconCanvas, weather: str = "cloud") -> None:
    ic.shadow(w=112, h=18)
    color = GRAY if weather in {"storm", "fog"} else WHITE
    for x, y, r in [(87, 106, 28), (119, 84, 36), (153, 108, 30)]:
        ic.ellipse((x - r, y - r, x + r, y + r), color, outline=darken(color, 0.10))
    ic.round_rect((67, 109, 173, 143), 17, color, outline=darken(color, 0.10))
    if weather == "rain":
        for x in [91, 120, 149]:
            ic.line([(x, 155), (x - 10, 180)], BLUE, 6)
    elif weather == "snow":
        for x, y in [(91, 159), (122, 177), (151, 158)]:
            ic.star(x, y, 10, 4, BLUE)
    elif weather == "storm":
        ic.polygon([(120, 144), (101, 182), (125, 178), (114, 209), (146, 163), (123, 166)], YELLOW, outline=darken(YELLOW, 0.18))
    elif weather == "fog":
        for y in [154, 171, 188]:
            ic.line([(70, y), (170, y)], rgba((180, 188, 195), 160), 5)


def draw_sun(ic: IconCanvas, heat: bool = False) -> None:
    ic.shadow(w=100, h=19)
    for a in range(0, 360, 45):
        dx, dy = math.cos(math.radians(a)), math.sin(math.radians(a))
        ic.line([(120 + dx * 47, 112 + dy * 47), (120 + dx * 64, 112 + dy * 64)], YELLOW, 8)
    ic.ellipse((77, 69, 163, 155), YELLOW, outline=darken(YELLOW, 0.18))
    if heat:
        for x in [92, 120, 148]:
            ic.line([(x, 172), (x - 9, 187), (x + 3, 202)], ORANGE, 4)
    ic.small_highlight(96, 86, 22, 9)


def draw_building(ic: IconCanvas, kind: str = "building") -> None:
    ic.shadow(w=124, h=21)
    if kind == "bridge":
        ic.line([(55, 150), (185, 150)], BLUE, 12)
        ic.line([(76, 150), (76, 90)], BLUE, 9)
        ic.line([(164, 150), (164, 90)], BLUE, 9)
        ic.line([(76, 96), (120, 134), (164, 96)], TEAL, 6)
        ic.line([(76, 96), (120, 60), (164, 96)], TEAL, 6)
        return
    if kind == "tower":
        ic.polygon([(91, 178), (120, 50), (149, 178)], GRAY, outline=darken(GRAY, 0.25))
        for y in [88, 120, 152]:
            ic.line([(98, y), (142, y)], darken(GRAY, 0.20), 5)
        ic.ellipse((108, 35, 132, 59), RED, outline=darken(RED, 0.20))
        return
    if kind == "cafe":
        ic.round_rect((62, 90, 178, 178), 12, TAN, outline=darken(TAN, 0.25))
        ic.polygon([(58, 90), (182, 90), (166, 60), (74, 60)], RED, outline=darken(RED, 0.22))
        ic.round_rect((98, 126, 142, 178), 8, DARK)
        return
    if kind == "subway":
        ic.round_rect((64, 67, 176, 166), 18, TEAL, outline=darken(TEAL, 0.25))
        ic.round_rect((83, 86, 157, 122), 9, WHITE)
        for x in [92, 148]:
            ic.ellipse((x - 12, 154, x + 12, 178), DARK)
        return
    ic.round_rect((63, 68, 177, 178), 13, BLUE if kind != "park" else GREEN, outline=darken(BLUE if kind != "park" else GREEN, 0.25))
    if kind == "park":
        draw_tree(ic, "tree")
        return
    for x in [82, 113, 144]:
        for y in [88, 118, 148]:
            ic.round_rect((x, y, x + 18, y + 18), 4, WHITE)


def draw_meadow(ic: IconCanvas) -> None:
    ic.shadow(w=125, h=19)
    ic.ellipse((55, 103, 185, 171), GREEN, outline=darken(GREEN, 0.18))
    for x in [75, 99, 130, 162]:
        ic.line([(x, 154), (x + 6, 124)], darken(GREEN, 0.12), 4)
    for x, y, c in [(91, 111, YELLOW), (121, 101, PINK), (151, 116, WHITE)]:
        flower_head(ic, x, y, c, 0.35)


def draw_home_item(ic: IconCanvas, kind: str = "sofa") -> None:
    ic.shadow(w=122, h=21)
    if kind == "sofa":
        ic.round_rect((58, 106, 182, 165), 18, BLUE, outline=darken(BLUE, 0.24))
        ic.round_rect((72, 78, 168, 126), 16, lighten(BLUE, 0.05), outline=darken(BLUE, 0.20))
        ic.line([(86, 165), (78, 183)], DARK, 5)
        ic.line([(154, 165), (162, 183)], DARK, 5)
    elif kind == "lamp":
        ic.polygon([(88, 72), (152, 72), (166, 125), (74, 125)], YELLOW, outline=darken(YELLOW, 0.22))
        ic.line([(120, 125), (120, 178)], DARK, 8)
        ic.ellipse((88, 170, 152, 186), DARK)
    elif kind == "window":
        ic.round_rect((65, 58, 175, 174), 12, BLUE, outline=darken(BLUE, 0.25))
        ic.line([(120, 62), (120, 171)], WHITE, 5)
        ic.line([(69, 116), (171, 116)], WHITE, 5)
    elif kind == "door":
        ic.round_rect((78, 48, 162, 181), 12, BROWN, outline=darken(BROWN, 0.25))
        ic.ellipse((140, 113, 150, 123), YELLOW)
    elif kind == "carpet":
        ic.ellipse((52, 86, 188, 173), PINK, outline=darken(PINK, 0.23))
        ic.ellipse((77, 105, 163, 154), lighten(PINK, 0.20), outline=darken(PINK, 0.10))
    elif kind == "shelf":
        for y in [83, 124, 165]:
            ic.round_rect((60, y, 180, y + 12), 5, BROWN, outline=darken(BROWN, 0.20))
        for x, c in [(76, BLUE), (99, RED), (130, GREEN), (154, YELLOW)]:
            ic.round_rect((x, 94, x + 15, 124), 4, c)
    elif kind == "pillow":
        ic.round_rect((62, 76, 178, 161), 25, PINK, outline=darken(PINK, 0.23))
        ic.small_highlight(84, 94, 30, 10)
    else:
        ic.round_rect((94, 63, 140, 159), 18, YELLOW, outline=darken(YELLOW, 0.22))
        ic.line([(132, 151), (168, 181)], YELLOW, 11)
        ic.ellipse((84, 54, 150, 120), YELLOW, outline=darken(YELLOW, 0.22))


def draw_utensil(ic: IconCanvas, kind: str = "spoon") -> None:
    ic.shadow(w=112, h=19)
    if kind == "stove":
        ic.round_rect((61, 62, 179, 177), 14, GRAY, outline=darken(GRAY, 0.26))
        for x, y in [(91, 96), (149, 96), (91, 140), (149, 140)]:
            ic.ellipse((x - 17, y - 17, x + 17, y + 17), DARK)
        return
    if kind == "fridge":
        ic.round_rect((79, 48, 161, 182), 13, WHITE, outline=(184, 191, 200, 255))
        ic.line([(79, 102), (161, 102)], (184, 191, 200, 255), 4)
        ic.line([(146, 67), (146, 91)], BLUE, 5)
        ic.line([(146, 122), (146, 153)], BLUE, 5)
        return
    if kind == "oven":
        ic.round_rect((62, 65, 178, 176), 14, DARK, outline=BLACK)
        ic.round_rect((78, 92, 162, 154), 7, rgba((105, 155, 190), 255), outline=lighten(DARK, 0.2))
        for x in [88, 120, 152]:
            ic.ellipse((x - 5, 76, x + 5, 86), ORANGE)
        return
    if kind == "pan":
        ic.ellipse((63, 91, 157, 162), DARK, outline=BLACK)
        ic.line([(149, 120), (195, 100)], DARK, 13)
        ic.ellipse((82, 107, 138, 149), GRAY)
        return
    if kind == "kettle":
        ic.ellipse((74, 89, 166, 174), TEAL, outline=darken(TEAL, 0.25))
        ic.ellipse((91, 64, 149, 96), TEAL, outline=darken(TEAL, 0.25))
        ic.line([(96, 72), (120, 51), (144, 72)], DARK, 7)
        ic.polygon([(163, 108), (196, 96), (165, 130)], TEAL, outline=darken(TEAL, 0.25))
        return
    if kind == "knife":
        ic.polygon([(72, 166), (134, 63), (154, 78), (91, 177)], GRAY, outline=darken(GRAY, 0.25))
        ic.line([(91, 177), (70, 194)], BROWN, 13)
    elif kind == "fork":
        for x in [96, 108, 120, 132]:
            ic.line([(x, 56), (x, 104)], GRAY, 5)
        ic.line([(114, 104), (114, 181)], GRAY, 12)
    else:
        ic.ellipse((84, 52, 138, 108), GRAY, outline=darken(GRAY, 0.25))
        ic.line([(113, 103), (113, 181)], GRAY, 13)


def draw_board(ic: IconCanvas, kind: str = "board") -> None:
    ic.shadow(w=118, h=20)
    if kind == "desk":
        ic.round_rect((57, 104, 183, 138), 10, TAN, outline=darken(TAN, 0.25))
        ic.line([(82, 138), (73, 181)], BROWN, 7)
        ic.line([(158, 138), (167, 181)], BROWN, 7)
        ic.round_rect((94, 72, 146, 105), 7, BLUE)
    elif kind == "bell":
        ic.ellipse((78, 76, 162, 153), YELLOW, outline=darken(YELLOW, 0.22))
        ic.round_rect((68, 145, 172, 162), 7, YELLOW, outline=darken(YELLOW, 0.22))
        ic.ellipse((107, 51, 133, 77), ORANGE, outline=darken(ORANGE, 0.20))
    elif kind == "notebook":
        draw_document(ic, WHITE, BLUE)
        for y in [70, 92, 114, 136, 158]:
            ic.line([(76, y), (166, y)], rgba((*BLUE[:3], 90)), 2)
        ic.line([(93, 51), (93, 181)], RED, 4)
    elif kind == "marker":
        ic.round_rect((73, 102, 171, 128), 9, BLUE, outline=darken(BLUE, 0.24))
        ic.round_rect((163, 101, 190, 129), 8, GRAY, outline=darken(GRAY, 0.20))
        ic.round_rect((51, 104, 78, 126), 7, DARK)
    else:
        ic.round_rect((58, 62, 182, 153), 12, GREEN if kind == "board" else BLUE, outline=DARK)
        ic.line([(80, 167), (160, 167)], BROWN, 8)
        ic.line([(100, 153), (88, 184)], BROWN, 6)
        ic.line([(140, 153), (152, 184)], BROWN, 6)


def draw_paint(ic: IconCanvas, kind: str = "paint") -> None:
    ic.shadow(w=118, h=21)
    if kind == "brush":
        ic.line([(73, 167), (151, 89)], BROWN, 14)
        ic.polygon([(148, 86), (172, 57), (183, 69), (157, 95)], GRAY, outline=darken(GRAY, 0.20))
        ic.polygon([(173, 56), (196, 38), (186, 69)], ORANGE, outline=darken(ORANGE, 0.18))
    elif kind == "canvas" or kind == "frame":
        ic.round_rect((62, 63, 178, 158), 8, CREAM, outline=BROWN, width=6)
        if kind == "canvas":
            ic.ellipse((91, 91, 123, 124), BLUE)
            ic.line([(118, 131), (145, 94), (164, 132)], GREEN, 6)
        ic.line([(95, 158), (82, 187)], BROWN, 6)
        ic.line([(145, 158), (158, 187)], BROWN, 6)
    elif kind == "statue":
        ic.ellipse((91, 54, 149, 112), GRAY, outline=darken(GRAY, 0.20))
        ic.round_rect((86, 104, 154, 164), 18, GRAY, outline=darken(GRAY, 0.20))
        ic.round_rect((70, 160, 170, 180), 7, darken(GRAY, 0.10))
    elif kind == "gallery":
        ic.round_rect((57, 67, 183, 157), 10, WHITE, outline=BROWN, width=5)
        ic.round_rect((72, 82, 109, 141), 6, BLUE)
        ic.round_rect((130, 82, 168, 141), 6, PINK)
    elif kind == "sketch":
        draw_document(ic, WHITE, GRAY)
        ic.line([(88, 143), (111, 100), (139, 137), (156, 84)], DARK, 4)
    else:
        for x, c in [(83, RED), (120, BLUE), (157, YELLOW)]:
            ic.round_rect((x - 14, 78, x + 14, 169), 8, c, outline=darken(c, 0.22))
            ic.ellipse((x - 15, 65, x + 15, 92), c, outline=darken(c, 0.22))
        ic.line([(70, 168), (178, 96)], BROWN, 8)


def draw_film(ic: IconCanvas, kind: str = "film") -> None:
    ic.shadow(w=120, h=20)
    if kind == "camera":
        ic.round_rect((69, 88, 161, 154), 13, DARK, outline=BLACK)
        ic.ellipse((98, 103, 140, 145), BLUE, outline=lighten(BLUE, 0.20))
        ic.polygon([(161, 105), (194, 90), (194, 152), (161, 137)], DARK, outline=BLACK)
        ic.ellipse((78, 55, 112, 89), DARK)
        ic.ellipse((124, 55, 158, 89), DARK)
    elif kind == "popcorn":
        ic.round_rect((76, 86, 164, 181), 11, RED, outline=darken(RED, 0.25))
        for x in [88, 120, 152]:
            ic.round_rect((x - 8, 88, x + 8, 178), 3, WHITE)
        for x, y in [(82, 75), (103, 62), (128, 69), (152, 61), (164, 78)]:
            ic.ellipse((x - 13, y - 11, x + 13, y + 13), YELLOW, outline=darken(YELLOW, 0.12))
    elif kind == "screen":
        ic.round_rect((54, 67, 186, 153), 11, DARK, outline=BLACK)
        ic.round_rect((67, 80, 173, 140), 7, BLUE)
        ic.line([(105, 154), (95, 180)], DARK, 6)
        ic.line([(135, 154), (145, 180)], DARK, 6)
    else:
        ic.ellipse((67, 55, 151, 139), DARK, outline=BLACK)
        for x, y in [(94, 82), (124, 82), (94, 112), (124, 112)]:
            ic.ellipse((x - 10, y - 10, x + 10, y + 10), WHITE)
        ic.round_rect((116, 130, 187, 162), 8, DARK, outline=BLACK)
        for x in [128, 151, 174]:
            ic.round_rect((x, 137, x + 9, 155), 2, WHITE)


def draw_pet(ic: IconCanvas, kind: str = "dog") -> None:
    ic.shadow(w=113, h=21)
    if kind == "bowl":
        ic.ellipse((67, 88, 173, 128), BLUE, outline=darken(BLUE, 0.22))
        ic.round_rect((76, 111, 164, 172), 22, BLUE, outline=darken(BLUE, 0.22))
        for x in [103, 122, 141]:
            ic.ellipse((x - 8, 94, x + 8, 110), BROWN)
        return
    if kind == "leash":
        ic.line([(76, 161), (114, 113), (159, 82)], RED, 10)
        ic.ellipse((144, 63, 182, 101), RED, outline=darken(RED, 0.2), width=5)
        return
    if kind == "paw":
        ic.ellipse((91, 111, 149, 169), BROWN, outline=darken(BROWN, 0.2))
        for x, y in [(84, 91), (108, 76), (132, 76), (156, 91)]:
            ic.ellipse((x - 13, y - 13, x + 13, y + 13), BROWN, outline=darken(BROWN, 0.2))
        return
    if kind == "tail":
        ic.line([(82, 151), (117, 111), (95, 80), (132, 63), (166, 91)], GRAY, 18)
        ic.line([(82, 151), (117, 111), (95, 80), (132, 63), (166, 91)], lighten(GRAY, 0.18), 8)
        return
    if kind == "vet":
        draw_document(ic, WHITE, TEAL)
        ic.line([(105, 115), (135, 115)], RED, 7)
        ic.line([(120, 100), (120, 130)], RED, 7)
        return
    if kind == "toy":
        ic.ellipse((78, 88, 162, 172), ORANGE, outline=darken(ORANGE, 0.22))
        ic.line([(84, 130), (156, 130)], WHITE, 8)
        return
    body = TAN if kind == "dog" else GRAY
    ic.ellipse((78, 79, 162, 163), body, outline=darken(body, 0.20))
    if kind == "cat":
        ic.polygon([(86, 88), (93, 53), (112, 83)], body, outline=darken(body, 0.20))
        ic.polygon([(154, 88), (147, 53), (128, 83)], body, outline=darken(body, 0.20))
    else:
        ic.ellipse((62, 93, 95, 135), darken(body, 0.05), outline=darken(body, 0.18))
        ic.ellipse((145, 93, 178, 135), darken(body, 0.05), outline=darken(body, 0.18))
    ic.ellipse((99, 111, 107, 119), DARK)
    ic.ellipse((133, 111, 141, 119), DARK)
    ic.ellipse((116, 129, 126, 139), DARK)


def draw_garden(ic: IconCanvas, kind: str = "bench") -> None:
    ic.shadow(w=126, h=20)
    if kind == "bench":
        for y in [94, 119, 144]:
            ic.round_rect((60, y, 180, y + 14), 5, BROWN, outline=darken(BROWN, 0.20))
        ic.line([(80, 152), (70, 184)], DARK, 6)
        ic.line([(160, 152), (170, 184)], DARK, 6)
    elif kind == "shovel":
        ic.line([(82, 178), (149, 70)], BROWN, 9)
        ic.ellipse((137, 45, 171, 83), GRAY, outline=darken(GRAY, 0.22))
        ic.polygon([(70, 170), (95, 179), (78, 202)], GRAY, outline=darken(GRAY, 0.22))
    elif kind == "seed":
        ic.ellipse((95, 98, 145, 164), BROWN, outline=darken(BROWN, 0.20))
        ic.line([(120, 98), (120, 54)], GREEN, 6)
        ic.ellipse((121, 62, 158, 87), GREEN, outline=darken(GREEN, 0.18))
    elif kind == "soil":
        ic.ellipse((60, 94, 180, 168), BROWN, outline=darken(BROWN, 0.22))
        for x, y in [(89, 125), (113, 106), (144, 135), (132, 151)]:
            ic.ellipse((x - 8, y - 5, x + 8, y + 5), darken(BROWN, 0.18))
    elif kind == "hose":
        ic.ellipse((62, 71, 178, 174), GREEN, outline=darken(GREEN, 0.24), width=9)
        ic.erase_ellipse((82, 91, 158, 154))
        ic.draw.ellipse(ic.box((82, 91, 158, 154)), outline=darken(GREEN, 0.18), width=ic.p(4))
        ic.line([(149, 121), (188, 98)], GREEN, 10)
        ic.round_rect((181, 90, 198, 108), 4, GRAY)
    elif kind == "apple":
        draw_apple(ic)
    elif kind == "path":
        ic.line([(77, 184), (101, 147), (117, 111), (146, 76)], TAN, 31)
        ic.line([(77, 184), (101, 147), (117, 111), (146, 76)], lighten(TAN, 0.25), 14)
    else:
        for x, y, r in [(86, 111, 30), (120, 86, 34), (154, 115, 31), (120, 130, 41)]:
            ic.ellipse((x - r, y - r, x + r, y + r), GREEN, outline=darken(GREEN, 0.18))


def draw_boat_sea(ic: IconCanvas, kind: str = "wave") -> None:
    ic.shadow(w=122, h=19)
    if kind == "wave":
        ic.line([(55, 137), (87, 111), (119, 137), (153, 111), (185, 137)], TEAL, 22)
        ic.line([(58, 139), (88, 122), (118, 139), (153, 122), (182, 139)], lighten(TEAL, 0.28), 8)
    elif kind == "shell":
        ic.polygon([(74, 165), (120, 60), (166, 165)], CREAM, outline=darken(CREAM, 0.28))
        for x in [91, 108, 132, 149]:
            ic.line([(120, 63), (x, 162)], darken(CREAM, 0.17), 4)
    elif kind == "beach":
        ic.ellipse((58, 113, 182, 178), TAN, outline=darken(TAN, 0.18))
        ic.line([(77, 116), (131, 68)], BROWN, 6)
        ic.polygon([(130, 68), (184, 94), (139, 110)], RED, outline=darken(RED, 0.18))
    elif kind == "boat":
        ic.polygon([(58, 126), (182, 126), (159, 166), (81, 166)], BROWN, outline=darken(BROWN, 0.24))
        ic.polygon([(119, 59), (119, 126), (168, 126)], WHITE, outline=GRAY)
        ic.line([(119, 58), (119, 130)], DARK, 5)
    elif kind == "sail":
        ic.line([(116, 54), (116, 176)], DARK, 6)
        ic.polygon([(121, 62), (121, 133), (174, 133)], WHITE, outline=GRAY)
        ic.polygon([(111, 75), (111, 140), (68, 140)], TEAL, outline=darken(TEAL, 0.20))
    elif kind == "island":
        ic.ellipse((63, 135, 177, 176), TAN, outline=darken(TAN, 0.18))
        ic.line([(116, 136), (132, 76)], BROWN, 8)
        ic.ellipse((100, 62, 138, 91), GREEN)
        ic.ellipse((126, 57, 166, 86), GREEN)
    elif kind == "fish":
        ic.ellipse((66, 88, 156, 151), ORANGE, outline=darken(ORANGE, 0.22))
        ic.polygon([(151, 118), (190, 86), (190, 150)], ORANGE, outline=darken(ORANGE, 0.22))
        ic.ellipse((93, 106, 102, 115), DARK)
    else:
        ic.round_rect((92, 62, 148, 172), 10, WHITE, outline=GRAY)
        ic.round_rect((77, 143, 163, 176), 8, RED, outline=darken(RED, 0.18))
        ic.line([(120, 62), (120, 37)], DARK, 6)
        ic.ellipse((105, 82, 135, 112), YELLOW, outline=darken(YELLOW, 0.16))


def draw_space(ic: IconCanvas, kind: str = "planet") -> None:
    ic.shadow(w=104, h=19)
    if kind == "rocket":
        ic.polygon([(120, 43), (154, 105), (142, 169), (120, 184), (98, 169), (86, 105)], WHITE, outline=GRAY)
        ic.polygon([(86, 134), (59, 171), (100, 158)], RED, outline=darken(RED, 0.18))
        ic.polygon([(154, 134), (181, 171), (140, 158)], RED, outline=darken(RED, 0.18))
        ic.ellipse((103, 88, 137, 122), BLUE, outline=darken(BLUE, 0.18))
        ic.polygon([(103, 183), (120, 215), (137, 183)], ORANGE)
    elif kind == "moon":
        draw_moon(ic)
    elif kind == "star":
        ic.star(120, 113, 68, 31, YELLOW)
    elif kind == "orbit":
        ic.ellipse((84, 79, 156, 151), BLUE, outline=darken(BLUE, 0.20))
        ic.ellipse((49, 93, 191, 138), (0, 0, 0, 0), outline=PURPLE, width=5)
    elif kind == "comet":
        ic.line([(63, 70), (136, 123)], ORANGE, 16)
        ic.line([(76, 105), (140, 132)], YELLOW, 10)
        ic.ellipse((125, 108, 174, 157), BLUE, outline=darken(BLUE, 0.18))
    elif kind == "astronaut":
        ic.ellipse((80, 53, 160, 133), WHITE, outline=GRAY)
        ic.round_rect((89, 115, 151, 181), 18, WHITE, outline=GRAY)
        ic.round_rect((96, 76, 144, 111), 12, BLUE, outline=darken(BLUE, 0.18))
    elif kind == "galaxy":
        ic.ellipse((67, 75, 173, 149), PURPLE, outline=darken(PURPLE, 0.18))
        ic.line([(75, 134), (110, 105), (165, 84)], YELLOW, 5)
        for x, y in [(88, 82), (144, 139), (160, 104)]:
            ic.star(x, y, 9, 4, WHITE)
    else:
        ic.ellipse((70, 61, 170, 161), BLUE, outline=darken(BLUE, 0.22))
        ic.ellipse((47, 99, 193, 130), (0, 0, 0, 0), outline=ORANGE, width=8)


def draw_shape(ic: IconCanvas, kind: str = "circle") -> None:
    color_map = {
        "circle": BLUE,
        "square": ORANGE,
        "triangle": GREEN,
        "line": PURPLE,
        "oval": TEAL,
        "cube": YELLOW,
        "star": PINK,
        "ring": RED,
    }
    c = color_map.get(kind, BLUE)
    if kind == "cube":
        draw_box(ic, c)
    elif kind == "ring":
        ic.shadow(w=103, h=19)
        ic.ellipse((66, 57, 174, 165), c, outline=darken(c, 0.22))
        ic.erase_ellipse((95, 86, 145, 136))
        ic.draw.ellipse(ic.box((95, 86, 145, 136)), outline=WHITE, width=ic.p(5))
    elif kind == "line":
        ic.shadow(w=105, h=17)
        ic.line([(65, 154), (175, 74)], c, 18)
    else:
        draw_badge(ic, c, "square" if kind == "square" else "triangle" if kind == "triangle" else "star" if kind == "star" else "round")
        if kind == "oval":
            ic.image = Image.new("RGBA", (ic.size, ic.size), (0, 0, 0, 0))
            ic.draw = ImageDraw.Draw(ic.image)
            ic.shadow(w=110, h=19)
            ic.ellipse((54, 76, 186, 156), c, outline=darken(c, 0.22))


def draw_clothing(ic: IconCanvas, kind: str = "shirt") -> None:
    ic.shadow(w=112, h=20)
    if kind == "hat":
        ic.ellipse((68, 129, 172, 158), BLUE, outline=darken(BLUE, 0.20))
        ic.round_rect((86, 74, 154, 140), 23, BLUE, outline=darken(BLUE, 0.20))
    elif kind == "scarf":
        ic.round_rect((82, 65, 151, 101), 13, RED, outline=darken(RED, 0.22))
        ic.round_rect((113, 93, 145, 178), 9, RED, outline=darken(RED, 0.22))
        ic.round_rect((83, 92, 115, 157), 9, ORANGE, outline=darken(ORANGE, 0.20))
    elif kind == "dress":
        ic.polygon([(101, 62), (139, 62), (170, 178), (70, 178)], PINK, outline=darken(PINK, 0.22))
        ic.round_rect((93, 55, 147, 91), 14, PINK, outline=darken(PINK, 0.22))
    elif kind == "jeans":
        ic.polygon([(83, 62), (122, 62), (116, 180), (76, 180)], BLUE, outline=darken(BLUE, 0.24))
        ic.polygon([(118, 62), (157, 62), (164, 180), (124, 180)], BLUE, outline=darken(BLUE, 0.24))
    elif kind == "glove" or kind == "mitten":
        ic.round_rect((80, 83, 146, 167), 27, PINK, outline=darken(PINK, 0.22))
        ic.ellipse((136, 116, 176, 154), PINK, outline=darken(PINK, 0.22))
    elif kind == "belt":
        ic.round_rect((54, 106, 186, 141), 8, BROWN, outline=darken(BROWN, 0.24))
        ic.round_rect((103, 99, 137, 148), 6, YELLOW, outline=darken(YELLOW, 0.22))
    elif kind == "coat":
        ic.round_rect((77, 59, 163, 178), 17, BLUE, outline=darken(BLUE, 0.25))
        ic.line([(120, 63), (120, 176)], darken(BLUE, 0.20), 5)
        ic.ellipse((108, 88, 117, 97), YELLOW)
        ic.ellipse((123, 112, 132, 121), YELLOW)
    else:
        ic.polygon([(83, 76), (105, 58), (120, 82), (135, 58), (157, 76), (167, 178), (73, 178)], TEAL, outline=darken(TEAL, 0.24))


def draw_shoe(ic: IconCanvas, kind: str = "sneaker") -> None:
    ic.shadow(w=126, h=20)
    color = BLUE if kind in {"sneaker", "pair", "size"} else BROWN if kind == "boot" else PINK if kind == "heel" else ORANGE
    if kind == "pair":
        draw_shoe_single(ic, 79, 122, color, 0.82)
        draw_shoe_single(ic, 124, 107, ORANGE, 0.82)
    elif kind == "lace":
        draw_shoe_single(ic, 69, 111, BLUE, 1.0)
        ic.line([(92, 118), (129, 98), (159, 125)], WHITE, 5)
    elif kind == "sole":
        draw_shoe_single(ic, 64, 112, GRAY, 1.0)
        ic.round_rect((70, 154, 177, 172), 7, DARK)
    elif kind == "heel":
        ic.polygon([(71, 139), (136, 103), (166, 142), (108, 160)], color, outline=darken(color, 0.22))
        ic.line([(157, 143), (176, 184)], DARK, 7)
    else:
        draw_shoe_single(ic, 64, 112, color, 1.0)


def draw_shoe_single(ic: IconCanvas, x: float, y: float, color: Color, scale: float) -> None:
    ic.round_rect((x, y, x + 109 * scale, y + 42 * scale), 15 * scale, color, outline=darken(color, 0.24))
    ic.polygon([(x + 8 * scale, y + 6 * scale), (x + 43 * scale, y - 34 * scale), (x + 79 * scale, y + 1 * scale), (x + 55 * scale, y + 19 * scale)], color, outline=darken(color, 0.24))
    ic.round_rect((x + 8 * scale, y + 35 * scale, x + 110 * scale, y + 49 * scale), 5 * scale, WHITE, outline=GRAY)


def draw_health(ic: IconCanvas, kind: str = "doctor") -> None:
    if kind == "doctor":
        draw_person(ic, TEAL, BROWN)
        ic.round_rect((95, 115, 145, 177), 12, WHITE, outline=GRAY)
        ic.line([(113, 139), (127, 139)], RED, 4)
        ic.line([(120, 132), (120, 146)], RED, 4)
    elif kind == "clinic":
        draw_building(ic, "building")
        ic.line([(105, 124), (135, 124)], RED, 7)
        ic.line([(120, 109), (120, 139)], RED, 7)
    elif kind == "pill":
        ic.shadow(w=100, h=18)
        ic.round_rect((65, 91, 175, 146), 27, RED, outline=darken(RED, 0.2))
        ic.round_rect((65, 91, 120, 146), 27, WHITE, outline=GRAY)
        ic.line([(120, 92), (120, 145)], GRAY, 4)
    elif kind == "water":
        draw_drop(ic, TEAL)
    elif kind == "sleep" or kind == "rest":
        draw_moon(ic)
    elif kind == "pulse":
        ic.shadow(w=111, h=19)
        ic.line([(60, 126), (91, 126), (106, 91), (124, 158), (141, 111), (158, 126), (183, 126)], RED, 8)
    else:
        ic.shadow(w=100, h=20)
        ic.ellipse((74, 67, 166, 159), TEAL, outline=darken(TEAL, 0.22))
        ic.line([(99, 113), (116, 135), (148, 93)], WHITE, 8)


def draw_body(ic: IconCanvas, kind: str = "hand") -> None:
    ic.shadow(w=108, h=20)
    if kind == "heart":
        ic.polygon([(120, 176), (70, 122), (76, 82), (112, 75), (120, 94), (128, 75), (164, 82), (170, 122)], RED, outline=darken(RED, 0.2))
    elif kind == "eye":
        ic.ellipse((55, 82, 185, 153), WHITE, outline=BLUE, width=4)
        ic.ellipse((96, 91, 144, 139), BLUE, outline=darken(BLUE, 0.20))
        ic.ellipse((113, 108, 127, 122), DARK)
    elif kind == "ear":
        ic.ellipse((86, 58, 158, 170), TAN, outline=darken(TAN, 0.18))
        ic.ellipse((105, 86, 141, 142), lighten(TAN, 0.08), outline=darken(TAN, 0.12))
    elif kind == "foot":
        ic.round_rect((69, 100, 166, 158), 24, TAN, outline=darken(TAN, 0.18))
        for x in [80, 99, 118, 137, 156]:
            ic.ellipse((x - 6, 83, x + 6, 97), TAN)
    elif kind == "smile":
        ic.ellipse((70, 59, 170, 159), YELLOW, outline=darken(YELLOW, 0.18))
        ic.ellipse((96, 95, 105, 104), DARK)
        ic.ellipse((135, 95, 144, 104), DARK)
        ic.line([(96, 119), (117, 137), (145, 119)], DARK, 5)
    elif kind == "finger":
        ic.round_rect((99, 56, 131, 166), 16, TAN, outline=darken(TAN, 0.18))
        ic.round_rect((81, 113, 113, 171), 16, TAN, outline=darken(TAN, 0.18))
        ic.round_rect((124, 116, 156, 171), 16, TAN, outline=darken(TAN, 0.18))
    else:
        ic.round_rect((79, 80, 161, 166), 26, TAN, outline=darken(TAN, 0.18))
        ic.line([(119, 84), (119, 52)], TAN, 19)
        ic.line([(88, 118), (55, 101)], TAN, 17)
        ic.line([(152, 118), (185, 101)], TAN, 17)


def draw_office_item(ic: IconCanvas, kind: str = "chair") -> None:
    ic.shadow(w=115, h=20)
    if kind == "chair":
        ic.round_rect((78, 63, 162, 130), 17, BLUE, outline=darken(BLUE, 0.23))
        ic.round_rect((73, 123, 167, 158), 14, BLUE, outline=darken(BLUE, 0.23))
        ic.line([(120, 158), (120, 184)], DARK, 6)
        ic.line([(94, 184), (146, 184)], DARK, 6)
    elif kind == "printer":
        ic.round_rect((67, 85, 173, 151), 12, GRAY, outline=darken(GRAY, 0.23))
        ic.round_rect((82, 55, 158, 91), 6, WHITE, outline=GRAY)
        ic.round_rect((82, 135, 158, 179), 6, WHITE, outline=GRAY)
    elif kind == "paper":
        draw_document(ic, WHITE, BLUE)
    elif kind == "folder":
        draw_document(ic, WHITE, ORANGE, folder=True)
    elif kind == "pen":
        ic.line([(66, 170), (158, 78)], BLUE, 13)
        ic.polygon([(158, 78), (181, 55), (190, 64), (167, 87)], GRAY)
        ic.polygon([(62, 174), (51, 194), (72, 183)], DARK)
    elif kind == "coffee":
        draw_cup(ic, BROWN, CREAM, steam=True)
    elif kind == "badge":
        ic.round_rect((72, 60, 168, 176), 13, WHITE, outline=GRAY)
        ic.ellipse((101, 79, 139, 117), BLUE)
        ic.round_rect((89, 132, 151, 146), 5, TEAL)
    else:
        ic.round_rect((58, 67, 182, 151), 12, DARK, outline=BLACK)
        ic.round_rect((72, 81, 168, 137), 7, BLUE)
        ic.round_rect((101, 151, 139, 176), 5, DARK)


def draw_tech(ic: IconCanvas, kind: str = "robot") -> None:
    ic.shadow(w=112, h=20)
    if kind == "robot":
        ic.round_rect((76, 70, 164, 154), 18, TEAL, outline=darken(TEAL, 0.25))
        ic.line([(120, 70), (120, 45)], DARK, 5)
        ic.ellipse((112, 35, 128, 51), YELLOW)
        ic.ellipse((96, 102, 108, 114), DARK)
        ic.ellipse((132, 102, 144, 114), DARK)
        ic.round_rect((98, 131, 142, 141), 4, WHITE)
    elif kind == "sensor":
        ic.ellipse((78, 71, 162, 155), PURPLE, outline=darken(PURPLE, 0.22))
        for r in [62, 83, 104]:
            ic.ellipse((120 - r / 2, 113 - r / 2, 120 + r / 2, 113 + r / 2), (0, 0, 0, 0), outline=TEAL, width=4)
    elif kind == "chip":
        ic.round_rect((78, 72, 162, 156), 12, DARK, outline=BLACK)
        ic.round_rect((98, 92, 142, 136), 8, TEAL)
        for x in [66, 174]:
            for y in [88, 108, 128, 148]:
                ic.line([(x, y), (78 if x < 120 else 162, y)], GRAY, 4)
    elif kind == "drone":
        ic.round_rect((96, 101, 144, 135), 9, TEAL, outline=darken(TEAL, 0.20))
        for x, y in [(65, 80), (175, 80), (65, 157), (175, 157)]:
            ic.line([(120, 118), (x, y)], DARK, 5)
            ic.ellipse((x - 21, y - 9, x + 21, y + 9), GRAY, outline=darken(GRAY, 0.20))
    elif kind == "laser":
        ic.round_rect((64, 119, 134, 148), 9, DARK, outline=BLACK)
        ic.polygon([(134, 124), (189, 91), (189, 176), (134, 143)], RED)
    elif kind == "server":
        for y in [62, 102, 142]:
            ic.round_rect((70, y, 170, y + 32), 8, DARK, outline=BLACK)
            ic.ellipse((88, y + 11, 99, y + 22), GREEN)
            ic.line([(112, y + 17), (153, y + 17)], GRAY, 4)
    elif kind == "battery":
        ic.round_rect((66, 89, 164, 149), 13, TEAL, outline=darken(TEAL, 0.24))
        ic.round_rect((164, 107, 183, 131), 5, TEAL, outline=darken(TEAL, 0.24))
        ic.polygon([(111, 98), (93, 127), (116, 125), (105, 146), (141, 111), (119, 114)], YELLOW)
    else:
        ic.round_rect((67, 71, 173, 154), 14, DARK, outline=BLACK)
        ic.line([(91, 101), (76, 116), (91, 131)], TEAL, 5)
        ic.line([(149, 101), (164, 116), (149, 131)], TEAL, 5)
        ic.line([(130, 96), (110, 136)], PURPLE, 5)


def draw_internet(ic: IconCanvas, kind: str = "site") -> None:
    ic.shadow(w=112, h=20)
    if kind == "mail":
        ic.round_rect((59, 83, 181, 158), 12, WHITE, outline=GRAY)
        ic.line([(64, 91), (120, 128), (176, 91)], BLUE, 5)
        ic.line([(64, 151), (105, 120)], BLUE, 4)
        ic.line([(176, 151), (135, 120)], BLUE, 4)
    elif kind == "cloud":
        draw_cloud(ic)
    elif kind == "chat":
        ic.round_rect((62, 75, 156, 137), 16, TEAL, outline=darken(TEAL, 0.20))
        ic.polygon([(95, 137), (82, 162), (122, 137)], TEAL)
        ic.round_rect((94, 104, 181, 163), 16, BLUE, outline=darken(BLUE, 0.20))
        ic.polygon([(150, 163), (165, 184), (128, 163)], BLUE)
    elif kind == "video":
        ic.round_rect((61, 78, 179, 155), 13, DARK, outline=BLACK)
        ic.polygon([(107, 94), (107, 139), (145, 116)], RED)
    elif kind == "search":
        ic.ellipse((69, 62, 145, 138), WHITE, outline=BLUE, width=7)
        ic.line([(137, 132), (179, 174)], BLUE, 12)
    elif kind == "link":
        ic.ellipse((64, 86, 128, 150), (0, 0, 0, 0), outline=BLUE, width=10)
        ic.ellipse((112, 86, 176, 150), (0, 0, 0, 0), outline=TEAL, width=10)
    else:
        ic.round_rect((60, 67, 180, 158), 13, WHITE, outline=GRAY)
        ic.round_rect((60, 67, 180, 92), 13, BLUE, outline=darken(BLUE, 0.18))
        ic.ellipse((77, 78, 86, 87), YELLOW)
        ic.ellipse((94, 78, 103, 87), ORANGE)
        ic.round_rect((78, 111, 162, 128), 5, TEAL)


def draw_phone(ic: IconCanvas, kind: str = "screen") -> None:
    ic.shadow(w=95, h=20)
    ic.round_rect((79, 45, 161, 185), 18, DARK, outline=BLACK)
    body_screen = BLUE if kind != "case" else PINK
    ic.round_rect((88, 62, 152, 164), 10, body_screen, outline=lighten(DARK, 0.18))
    ic.ellipse((113, 170, 127, 184), GRAY)
    if kind == "call":
        ic.line([(104, 127), (121, 143), (142, 111)], WHITE, 7)
    elif kind == "charge":
        ic.polygon([(119, 80), (101, 119), (123, 116), (113, 145), (143, 105), (123, 108)], YELLOW)
    elif kind == "message":
        ic.round_rect((99, 91, 142, 121), 8, WHITE)
        ic.polygon([(111, 121), (105, 135), (127, 121)], WHITE)
    elif kind == "camera":
        ic.ellipse((99, 91, 141, 133), DARK)
        ic.ellipse((110, 102, 130, 122), TEAL)
    elif kind == "contact":
        ic.ellipse((106, 83, 134, 111), WHITE)
        ic.round_rect((99, 118, 141, 145), 10, WHITE)
    elif kind == "app":
        for x in [98, 123]:
            for y in [82, 108, 134]:
                ic.round_rect((x, y, x + 18, y + 18), 5, WHITE)


def draw_camera_photo(ic: IconCanvas, kind: str = "camera") -> None:
    ic.shadow(w=112, h=20)
    if kind == "album":
        draw_book(ic, BLUE)
        ic.ellipse((97, 96, 143, 142), WHITE, outline=GRAY)
    elif kind == "portrait":
        draw_person(ic, BLUE, BROWN)
    elif kind == "frame":
        ic.round_rect((60, 61, 180, 166), 10, BROWN, outline=darken(BROWN, 0.22), width=5)
        ic.round_rect((77, 78, 163, 149), 6, CREAM, outline=darken(CREAM, 0.18))
        ic.ellipse((98, 91, 124, 117), BLUE)
        ic.polygon([(82, 148), (114, 112), (139, 148)], GREEN)
        ic.polygon([(116, 148), (148, 105), (163, 148)], TEAL)
    elif kind == "studio":
        ic.round_rect((72, 78, 151, 136), 11, DARK, outline=BLACK)
        ic.ellipse((97, 91, 135, 129), BLUE)
        ic.line([(151, 104), (192, 82)], DARK, 10)
        ic.line([(111, 136), (94, 181)], DARK, 6)
        ic.line([(111, 136), (143, 181)], DARK, 6)
    else:
        ic.round_rect((61, 82, 179, 154), 13, DARK, outline=BLACK)
        ic.round_rect((82, 66, 123, 87), 7, GRAY)
        ic.ellipse((94, 93, 146, 145), BLUE, outline=lighten(BLUE, 0.22))
        ic.ellipse((112, 111, 128, 127), DARK)
        if kind == "flash":
            ic.polygon([(164, 58), (150, 86), (169, 83), (158, 111), (190, 73), (171, 76)], YELLOW)
        elif kind == "focus":
            ic.ellipse((83, 82, 157, 156), (0, 0, 0, 0), outline=YELLOW, width=5)


def draw_cake(ic: IconCanvas, kind: str = "cake") -> None:
    ic.shadow(w=118, h=21)
    if kind == "candle":
        ic.round_rect((110, 73, 130, 164), 6, PINK, outline=darken(PINK, 0.20))
        ic.polygon([(120, 43), (107, 72), (133, 72)], YELLOW, outline=darken(YELLOW, 0.14))
    elif kind == "gift":
        draw_box(ic, PINK, ribbon=True)
    elif kind == "balloon":
        ic.ellipse((77, 52, 136, 125), RED, outline=darken(RED, 0.20))
        ic.ellipse((127, 61, 182, 130), BLUE, outline=darken(BLUE, 0.20))
        ic.line([(106, 124), (95, 181)], DARK, 3)
        ic.line([(154, 129), (143, 181)], DARK, 3)
    elif kind == "card":
        draw_ticket(ic, PINK)
    elif kind == "guest":
        draw_two_people(ic, PINK, BLUE)
    elif kind == "table":
        ic.ellipse((55, 92, 185, 148), RED, outline=darken(RED, 0.22))
        ic.round_rect((67, 121, 173, 160), 12, RED, outline=darken(RED, 0.22))
        ic.line([(88, 158), (78, 184)], BROWN, 6)
        ic.line([(152, 158), (162, 184)], BROWN, 6)
    else:
        ic.round_rect((68, 105, 172, 171), 13, PINK, outline=darken(PINK, 0.22))
        ic.round_rect((78, 77, 162, 116), 12, CREAM, outline=darken(CREAM, 0.18))
        ic.ellipse((91, 65, 113, 87), RED)
        ic.ellipse((127, 65, 149, 87), RED)
        ic.line([(96, 77), (96, 53)], YELLOW, 4)
        ic.line([(144, 77), (144, 53)], YELLOW, 4)


def draw_winter_summer(ic: IconCanvas, kind: str = "snow") -> None:
    if kind == "snow":
        ic.shadow(w=98, h=18)
        for angle in range(0, 180, 30):
            dx, dy = math.cos(math.radians(angle)) * 48, math.sin(math.radians(angle)) * 48
            ic.line([(120 - dx, 116 - dy), (120 + dx, 116 + dy)], BLUE, 5)
        ic.ellipse((112, 108, 128, 124), BLUE)
    elif kind == "ski":
        ic.shadow(w=128, h=18)
        ic.line([(62, 166), (180, 126)], BLUE, 8)
        ic.line([(68, 184), (186, 144)], TEAL, 8)
        ic.line([(112, 65), (128, 160)], BROWN, 5)
    elif kind == "ice":
        ic.shadow(w=105, h=19)
        ic.polygon([(82, 70), (139, 50), (169, 116), (130, 176), (72, 141)], TEAL, outline=darken(TEAL, 0.18))
        ic.line([(96, 82), (148, 119)], WHITE, 4)
    elif kind == "cocoa":
        draw_cup(ic, BROWN, RED, steam=True)
    elif kind == "sleigh":
        ic.shadow(w=126, h=20)
        ic.round_rect((70, 103, 158, 151), 13, RED, outline=darken(RED, 0.22))
        ic.line([(62, 169), (184, 169)], BROWN, 7)
        ic.line([(78, 151), (78, 169)], BROWN, 6)
        ic.line([(151, 151), (151, 169)], BROWN, 6)
    elif kind == "lake":
        ic.shadow(w=120, h=18)
        ic.ellipse((55, 94, 185, 166), TEAL, outline=darken(TEAL, 0.20))
        ic.line([(79, 123), (113, 115), (153, 130)], WHITE, 5)
    elif kind == "fan":
        ic.shadow(w=105, h=19)
        ic.ellipse((82, 65, 158, 141), WHITE, outline=BLUE, width=5)
        for pts in [[(120, 103), (144, 75), (151, 104)], [(120, 103), (91, 92), (105, 124)], [(120, 103), (128, 139), (101, 134)]]:
            ic.polygon(pts, TEAL, outline=darken(TEAL, 0.16))
        ic.line([(120, 141), (120, 181)], DARK, 6)
    elif kind == "berry":
        ic.shadow(w=100, h=19)
        for x, y in [(98, 118), (122, 103), (145, 125), (116, 139)]:
            ic.ellipse((x - 18, y - 18, x + 18, y + 18), RED, outline=darken(RED, 0.18))
        ic.ellipse((113, 81, 139, 105), GREEN)
    elif kind == "picnic":
        ic.shadow(w=120, h=20)
        ic.round_rect((62, 105, 178, 159), 12, RED, outline=darken(RED, 0.20))
        for x in [84, 124, 164]:
            ic.line([(x, 106), (x, 158)], WHITE, 4)
        for y in [122, 142]:
            ic.line([(64, y), (176, y)], WHITE, 4)
        ic.ellipse((91, 74, 126, 109), GREEN)
        ic.ellipse((126, 76, 159, 109), ORANGE)
    elif kind == "sandals":
        draw_shoe(ic, "sandal")
    elif kind == "hat":
        draw_clothing(ic, "hat")
    elif kind == "beach":
        draw_boat_sea(ic, "beach")
    else:
        draw_sun(ic)


def draw_dessert(ic: IconCanvas, kind: str = "cake") -> None:
    if kind == "cake":
        draw_cake(ic)
    elif kind == "cookie":
        ic.shadow(w=96, h=19)
        ic.ellipse((67, 62, 173, 168), BROWN, outline=darken(BROWN, 0.20))
        for x, y in [(94, 91), (131, 85), (113, 128), (145, 136)]:
            ic.ellipse((x - 7, y - 7, x + 7, y + 7), DARK)
    elif kind == "candy":
        ic.shadow(w=112, h=19)
        ic.round_rect((83, 88, 157, 151), 22, PINK, outline=darken(PINK, 0.22))
        ic.polygon([(83, 105), (52, 86), (61, 139), (83, 132)], PINK, outline=darken(PINK, 0.22))
        ic.polygon([(157, 105), (188, 86), (179, 139), (157, 132)], PINK, outline=darken(PINK, 0.22))
    elif kind == "pie":
        ic.ellipse((63, 89, 177, 163), TAN, outline=darken(TAN, 0.22))
        ic.polygon([(120, 97), (177, 127), (120, 163)], ORANGE, outline=darken(ORANGE, 0.18))
    elif kind == "cream":
        ic.shadow(w=94, h=18)
        for y, w in [(139, 80), (113, 63), (90, 45), (71, 27)]:
            ic.ellipse((120 - w / 2, y - 18, 120 + w / 2, y + 18), CREAM, outline=darken(CREAM, 0.10))
    elif kind == "icecream":
        ic.shadow(w=96, h=19)
        ic.polygon([(91, 117), (149, 117), (120, 188)], TAN, outline=darken(TAN, 0.22))
        ic.ellipse((78, 62, 138, 122), PINK, outline=darken(PINK, 0.18))
        ic.ellipse((105, 57, 166, 122), CREAM, outline=darken(CREAM, 0.14))
    elif kind == "waffle":
        ic.round_rect((70, 73, 170, 166), 13, TAN, outline=darken(TAN, 0.22))
        for x in [94, 120, 146]:
            ic.line([(x, 76), (x, 163)], darken(TAN, 0.15), 4)
        for y in [96, 121, 146]:
            ic.line([(73, y), (167, y)], darken(TAN, 0.15), 4)
    elif kind == "honey":
        ic.shadow(w=103, h=19)
        ic.round_rect((82, 82, 153, 172), 16, YELLOW, outline=darken(YELLOW, 0.22))
        ic.ellipse((82, 72, 153, 101), lighten(YELLOW, 0.18), outline=darken(YELLOW, 0.18))
        ic.line([(151, 77), (185, 52)], BROWN, 6)
        for y in [53, 61, 69]:
            ic.round_rect((174, y, 198, y + 7), 3, YELLOW, outline=darken(YELLOW, 0.16))
    else:
        ic.shadow(w=96, h=19)
        ic.ellipse((79, 67, 161, 152), YELLOW, outline=darken(YELLOW, 0.22))
        ic.line([(120, 151), (120, 181)], BROWN, 7)


def draw_drink(ic: IconCanvas, kind: str = "juice") -> None:
    if kind in {"tea", "coffee", "cocoa"}:
        draw_cup(ic, BROWN if kind != "tea" else ORANGE, CREAM, steam=True)
        return
    ic.shadow(w=98, h=20)
    color = ORANGE if kind in {"juice", "lemonade"} else WHITE if kind == "milk" else TEAL if kind in {"water", "smoothie"} else BROWN
    ic.polygon([(81, 58), (159, 58), (148, 178), (92, 178)], lighten(color, 0.18), outline=darken(color, 0.20))
    ic.ellipse((82, 53, 158, 77), WHITE, outline=GRAY)
    ic.ellipse((89, 69, 151, 88), color, outline=darken(color, 0.10))
    if kind == "lemonade":
        ic.ellipse((141, 64, 172, 95), YELLOW, outline=darken(YELLOW, 0.16))
        ic.line([(134, 60), (164, 33)], RED, 5)
    elif kind == "smoothie":
        for x, y, c in [(105, 106, PINK), (125, 122, BLUE), (137, 96, RED)]:
            ic.ellipse((x - 6, y - 6, x + 6, y + 6), c)


def draw_farm(ic: IconCanvas, kind: str = "barn") -> None:
    ic.shadow(w=124, h=21)
    if kind == "barn":
        ic.round_rect((66, 92, 174, 179), 8, RED, outline=darken(RED, 0.22))
        ic.polygon([(60, 94), (120, 48), (180, 94)], BROWN, outline=darken(BROWN, 0.22))
        ic.round_rect((99, 126, 141, 179), 6, BROWN)
        ic.line([(102, 130), (138, 176)], WHITE, 4)
        ic.line([(138, 130), (102, 176)], WHITE, 4)
    elif kind == "tractor":
        ic.round_rect((62, 109, 151, 148), 10, GREEN, outline=darken(GREEN, 0.24))
        ic.round_rect((110, 80, 153, 112), 8, GREEN, outline=darken(GREEN, 0.24))
        ic.ellipse((67, 136, 111, 180), DARK)
        ic.ellipse((136, 146, 166, 176), DARK)
        ic.ellipse((80, 149, 98, 167), GRAY)
    elif kind == "field":
        ic.ellipse((51, 91, 189, 175), GREEN, outline=darken(GREEN, 0.20))
        for x in [82, 112, 142, 168]:
            ic.line([(x, 103), (x - 27, 169)], YELLOW, 5)
    elif kind == "hay":
        ic.round_rect((70, 86, 170, 160), 14, YELLOW, outline=darken(YELLOW, 0.22))
        for y in [106, 128, 150]:
            ic.line([(76, y), (165, y)], darken(YELLOW, 0.15), 4)
    elif kind == "cow":
        ic.ellipse((62, 91, 171, 154), WHITE, outline=GRAY)
        ic.ellipse((132, 71, 178, 119), WHITE, outline=GRAY)
        ic.ellipse((82, 107, 105, 130), DARK)
        ic.ellipse((115, 93, 139, 117), DARK)
        ic.ellipse((149, 90, 157, 98), DARK)
    elif kind == "chicken":
        ic.ellipse((80, 82, 157, 158), CREAM, outline=darken(CREAM, 0.18))
        ic.ellipse((127, 63, 170, 108), CREAM, outline=darken(CREAM, 0.18))
        ic.polygon([(168, 86), (191, 96), (168, 106)], ORANGE)
        ic.polygon([(145, 61), (154, 42), (164, 63)], RED)
        ic.line([(105, 156), (95, 183)], ORANGE, 5)
        ic.line([(134, 156), (144, 183)], ORANGE, 5)
    elif kind == "grain":
        for x in [94, 120, 146]:
            ic.line([(x, 172), (x, 69)], GREEN, 5)
            for y in [87, 108, 129]:
                ic.ellipse((x - 17, y - 9, x, y + 9), YELLOW)
                ic.ellipse((x, y - 9, x + 17, y + 9), YELLOW)
    else:
        for x in [67, 116, 165]:
            ic.line([(x, 82), (x, 177)], BROWN, 9)
        for y in [111, 148]:
            ic.line([(57, y), (183, y)], BROWN, 9)


def draw_forest_item(ic: IconCanvas, kind: str = "pine") -> None:
    if kind == "pine":
        draw_tree(ic, "pine")
    elif kind == "mushroom":
        ic.shadow(w=99, h=19)
        ic.ellipse((65, 65, 175, 130), RED, outline=darken(RED, 0.20))
        for x, y in [(91, 87), (121, 76), (148, 100)]:
            ic.ellipse((x - 8, y - 8, x + 8, y + 8), WHITE)
        ic.round_rect((98, 111, 142, 179), 17, CREAM, outline=darken(CREAM, 0.18))
    elif kind == "fox":
        ic.shadow(w=108, h=20)
        ic.ellipse((75, 83, 165, 161), ORANGE, outline=darken(ORANGE, 0.20))
        ic.polygon([(82, 92), (96, 55), (114, 88)], ORANGE, outline=darken(ORANGE, 0.20))
        ic.polygon([(158, 92), (144, 55), (126, 88)], ORANGE, outline=darken(ORANGE, 0.20))
        ic.polygon([(96, 127), (120, 151), (144, 127)], WHITE)
        ic.ellipse((101, 105, 109, 113), DARK)
        ic.ellipse((131, 105, 139, 113), DARK)
    elif kind == "owl":
        ic.shadow(w=100, h=20)
        ic.ellipse((78, 61, 162, 166), BROWN, outline=darken(BROWN, 0.20))
        ic.ellipse((91, 86, 119, 118), YELLOW)
        ic.ellipse((121, 86, 149, 118), YELLOW)
        ic.ellipse((102, 97, 110, 105), DARK)
        ic.ellipse((132, 97, 140, 105), DARK)
        ic.polygon([(120, 111), (110, 128), (130, 128)], ORANGE)
    elif kind == "fern":
        ic.shadow(w=100, h=18)
        ic.line([(119, 178), (119, 66)], GREEN, 6)
        for y in [82, 101, 120, 139, 158]:
            ic.line([(119, y), (83, y - 18)], GREEN, 5)
            ic.line([(119, y), (155, y - 18)], GREEN, 5)
    elif kind == "stump":
        ic.round_rect((82, 91, 158, 173), 13, BROWN, outline=darken(BROWN, 0.22))
        ic.ellipse((79, 76, 161, 115), TAN, outline=darken(TAN, 0.20))
        ic.ellipse((102, 88, 138, 105), (0, 0, 0, 0), outline=BROWN, width=3)
    elif kind == "berry":
        draw_winter_summer(ic, "berry")
    else:
        draw_garden(ic, "path")


def draw_flower(ic: IconCanvas, kind: str = "rose") -> None:
    ic.shadow(w=100, h=19)
    if kind == "vase":
        ic.round_rect((88, 115, 152, 181), 16, BLUE, outline=darken(BLUE, 0.20))
        for x, c in [(95, PINK), (120, RED), (145, PURPLE)]:
            ic.line([(120, 120), (x, 73)], GREEN, 4)
            flower_head(ic, x, 68, c, 0.7)
        return
    if kind == "bouquet":
        for x, c in [(92, PINK), (120, RED), (148, PURPLE), (109, YELLOW), (136, ORANGE)]:
            flower_head(ic, x, 82 + (x % 3) * 6, c, 0.63)
        ic.line([(92, 119), (120, 178)], GREEN, 5)
        ic.line([(148, 119), (120, 178)], GREEN, 5)
        ic.round_rect((92, 137, 148, 177), 8, CREAM, outline=darken(CREAM, 0.18))
    elif kind == "petal":
        flower_head(ic, 120, 111, PINK, 1.15)
    elif kind == "stem":
        ic.line([(120, 181), (120, 73)], GREEN, 8)
        ic.ellipse((121, 113, 159, 138), GREEN, outline=darken(GREEN, 0.18))
        flower_head(ic, 120, 68, RED, 0.72)
    else:
        c = {"rose": RED, "tulip": PINK, "lily": WHITE, "peony": PURPLE}.get(kind, PINK)
        ic.line([(120, 181), (120, 97)], GREEN, 8)
        ic.ellipse((121, 126, 159, 151), GREEN, outline=darken(GREEN, 0.18))
        if kind == "tulip":
            ic.polygon([(90, 96), (101, 61), (120, 84), (139, 61), (150, 96), (120, 134)], c, outline=darken(c, 0.18))
        else:
            flower_head(ic, 120, 91, c, 1.0)


def flower_head(ic: IconCanvas, cx: float, cy: float, color: Color, scale: float = 1.0) -> None:
    for a in range(0, 360, 60):
        dx = math.cos(math.radians(a)) * 20 * scale
        dy = math.sin(math.radians(a)) * 20 * scale
        ic.ellipse((cx + dx - 14 * scale, cy + dy - 14 * scale, cx + dx + 14 * scale, cy + dy + 14 * scale), color, outline=darken(color, 0.16))
    ic.ellipse((cx - 12 * scale, cy - 12 * scale, cx + 12 * scale, cy + 12 * scale), YELLOW, outline=darken(YELLOW, 0.16))


def draw_insect(ic: IconCanvas, kind: str = "bee") -> None:
    ic.shadow(w=100, h=18)
    if kind == "butterfly":
        ic.ellipse((60, 65, 118, 130), PINK, outline=darken(PINK, 0.18))
        ic.ellipse((122, 65, 180, 130), PURPLE, outline=darken(PURPLE, 0.18))
        ic.ellipse((72, 123, 116, 174), ORANGE, outline=darken(ORANGE, 0.18))
        ic.ellipse((124, 123, 168, 174), BLUE, outline=darken(BLUE, 0.18))
        ic.round_rect((113, 83, 127, 166), 7, DARK)
    elif kind == "ant":
        for x in [84, 120, 156]:
            ic.ellipse((x - 22, 100, x + 22, 144), DARK)
        for x in [93, 120, 147]:
            ic.line([(x, 126), (x - 19, 160)], DARK, 4)
            ic.line([(x, 126), (x + 19, 160)], DARK, 4)
    elif kind == "beetle" or kind == "ladybug":
        color = RED if kind == "ladybug" else GREEN
        ic.ellipse((78, 70, 162, 166), color, outline=darken(color, 0.22))
        ic.line([(120, 74), (120, 164)], darken(color, 0.24), 5)
        if kind == "ladybug":
            for x, y in [(101, 101), (139, 101), (105, 133), (135, 133)]:
                ic.ellipse((x - 5, y - 5, x + 5, y + 5), DARK)
        ic.ellipse((93, 57, 147, 91), DARK)
    elif kind == "wing":
        ic.ellipse((64, 65, 126, 165), TEAL, outline=darken(TEAL, 0.18))
        ic.ellipse((114, 65, 176, 165), BLUE, outline=darken(BLUE, 0.18))
        ic.line([(120, 75), (120, 170)], DARK, 6)
    elif kind == "hive":
        for y, w in [(72, 46), (96, 68), (122, 86), (150, 102)]:
            ic.round_rect((120 - w / 2, y, 120 + w / 2, y + 25), 12, YELLOW, outline=darken(YELLOW, 0.18))
        ic.ellipse((108, 139, 132, 166), DARK)
    elif kind == "cocoon":
        ic.ellipse((84, 56, 156, 175), CREAM, outline=darken(CREAM, 0.18))
        for y in [83, 108, 133, 158]:
            ic.line([(91, y), (149, y - 12)], darken(CREAM, 0.14), 4)
    else:
        ic.ellipse((76, 92, 150, 145), YELLOW, outline=darken(YELLOW, 0.20))
        ic.ellipse((122, 86, 176, 138), YELLOW, outline=darken(YELLOW, 0.20))
        for x in [94, 114, 134]:
            ic.line([(x, 94), (x + 9, 142)], DARK, 5)
        ic.ellipse((84, 61, 126, 101), rgba((230, 245, 255), 175), outline=BLUE)
        ic.ellipse((120, 58, 162, 99), rgba((230, 245, 255), 175), outline=BLUE)


def draw_emotion(ic: IconCanvas, kind: str = "joy") -> None:
    color = YELLOW if kind in {"joy", "hope", "warmth"} else TEAL if kind in {"calm", "trust"} else PINK
    ic.shadow(w=101, h=20)
    ic.ellipse((67, 57, 173, 163), color, outline=darken(color, 0.18))
    if kind == "surprise":
        ic.ellipse((95, 96, 105, 107), DARK)
        ic.ellipse((135, 96, 145, 107), DARK)
        ic.ellipse((108, 121, 132, 148), DARK)
    elif kind == "calm":
        ic.line([(91, 103), (107, 98)], DARK, 4)
        ic.line([(133, 98), (149, 103)], DARK, 4)
        ic.line([(99, 130), (141, 130)], DARK, 4)
    elif kind == "interest":
        ic.ellipse((94, 96, 105, 108), DARK)
        ic.ellipse((136, 96, 147, 108), DARK)
        ic.line([(105, 132), (120, 140), (138, 132)], DARK, 4)
        ic.ellipse((148, 62, 171, 85), BLUE)
    elif kind == "pride":
        ic.star(120, 49, 18, 9, ORANGE)
        ic.ellipse((96, 101, 106, 111), DARK)
        ic.ellipse((134, 101, 144, 111), DARK)
        ic.line([(96, 126), (120, 143), (144, 126)], DARK, 5)
    else:
        ic.ellipse((96, 98, 106, 108), DARK)
        ic.ellipse((134, 98, 144, 108), DARK)
        ic.line([(96, 124), (118, 143), (145, 124)], DARK, 5)


def draw_hobby(ic: IconCanvas, kind: str = "drawing") -> None:
    if kind == "drawing":
        draw_paint(ic, "brush")
    elif kind == "knitting":
        ic.shadow(w=105, h=20)
        ic.ellipse((77, 92, 164, 170), PINK, outline=darken(PINK, 0.18))
        ic.line([(64, 74), (176, 185)], BROWN, 5)
        ic.line([(176, 74), (64, 185)], BROWN, 5)
        for x in [94, 116, 138]:
            ic.ellipse((x - 9, 118, x + 9, 136), lighten(PINK, 0.20))
    elif kind == "chess":
        ic.shadow(w=94, h=19)
        ic.ellipse((94, 60, 146, 111), DARK)
        ic.round_rect((96, 104, 144, 158), 11, DARK)
        ic.round_rect((75, 153, 165, 179), 7, DARK)
    elif kind == "puzzle":
        draw_toy(ic, "puzzle")
    elif kind == "dance":
        draw_person(ic, PINK, BROWN)
        ic.line([(84, 125), (55, 102)], PINK, 8)
        ic.line([(156, 125), (187, 101)], PINK, 8)
    elif kind == "baking":
        draw_cake(ic)
    elif kind == "reading":
        draw_book(ic, BLUE, open_book=True)
    else:
        draw_instrument(ic, "note")


def draw_camping(ic: IconCanvas, kind: str = "tent") -> None:
    ic.shadow(w=126, h=21)
    if kind == "tent":
        ic.polygon([(57, 172), (120, 62), (183, 172)], GREEN, outline=darken(GREEN, 0.24))
        ic.polygon([(120, 62), (183, 172), (120, 172)], TEAL, outline=darken(TEAL, 0.20))
        ic.polygon([(104, 172), (120, 119), (137, 172)], DARK)
    elif kind == "fire":
        for x, y in [(82, 156), (106, 169), (132, 169), (158, 156)]:
            ic.ellipse((x - 14, y - 11, x + 14, y + 11), BROWN, outline=darken(BROWN, 0.18))
        ic.polygon([(120, 61), (150, 121), (136, 167), (120, 179), (104, 167), (90, 121)], ORANGE, outline=darken(ORANGE, 0.18))
        ic.polygon([(120, 83), (137, 126), (128, 162), (120, 171), (112, 162), (103, 126)], YELLOW)
    elif kind == "map":
        draw_map_route(ic)
    elif kind == "compass":
        draw_map_route(ic, compass=True)
    elif kind == "backpack":
        ic.round_rect((75, 69, 165, 180), 20, ORANGE, outline=darken(ORANGE, 0.24))
        ic.round_rect((93, 49, 147, 83), 14, ORANGE, outline=darken(ORANGE, 0.24))
        ic.round_rect((92, 111, 148, 153), 11, lighten(ORANGE, 0.15), outline=darken(ORANGE, 0.18))
        ic.line([(75, 96), (52, 141)], DARK, 7)
        ic.line([(165, 96), (188, 141)], DARK, 7)
    elif kind == "lantern":
        ic.line([(98, 69), (120, 44), (142, 69)], DARK, 6)
        ic.round_rect((86, 73, 154, 164), 17, DARK, outline=BLACK)
        ic.round_rect((98, 91, 142, 147), 12, YELLOW, outline=darken(YELLOW, 0.18))
        ic.round_rect((80, 158, 160, 176), 7, DARK)
    elif kind == "trail":
        draw_garden(ic, "path")
    else:
        draw_cup(ic, BROWN, GREEN)


def draw_toy(ic: IconCanvas, kind: str = "blocks") -> None:
    ic.shadow(w=123, h=21)
    if kind == "doll":
        draw_person(ic, PINK, BROWN, "smile")
    elif kind == "blocks":
        for x, y, c in [(72, 121, RED), (114, 121, BLUE), (93, 80, YELLOW), (135, 80, GREEN)]:
            ic.round_rect((x, y, x + 43, y + 43), 7, c, outline=darken(c, 0.22))
    elif kind == "train":
        draw_vehicle(ic, "train", RED)
    elif kind == "puzzle":
        ic.round_rect((65, 75, 122, 132), 9, BLUE, outline=darken(BLUE, 0.2))
        ic.round_rect((118, 75, 175, 132), 9, ORANGE, outline=darken(ORANGE, 0.2))
        ic.round_rect((65, 128, 122, 185), 9, GREEN, outline=darken(GREEN, 0.2))
        ic.round_rect((118, 128, 175, 185), 9, PINK, outline=darken(PINK, 0.2))
        ic.ellipse((108, 99, 133, 124), ORANGE)
        ic.ellipse((108, 151, 133, 176), PINK)
    elif kind == "ball":
        draw_sport_ball(ic, "ball")
    elif kind == "robot":
        draw_tech(ic, "robot")
    elif kind == "kite":
        ic.polygon([(120, 49), (172, 103), (120, 158), (68, 103)], PINK, outline=darken(PINK, 0.22))
        ic.line([(120, 49), (120, 158)], WHITE, 4)
        ic.line([(68, 103), (172, 103)], WHITE, 4)
        ic.line([(120, 158), (95, 190), (126, 206)], DARK, 3)
    else:
        draw_vehicle(ic, "car", RED)


def draw_tool(ic: IconCanvas, kind: str = "hammer") -> None:
    ic.shadow(w=116, h=20)
    if kind == "hammer":
        ic.line([(83, 172), (143, 96)], BROWN, 13)
        ic.round_rect((115, 67, 181, 96), 7, GRAY, outline=darken(GRAY, 0.24))
        ic.round_rect((102, 79, 132, 110), 7, GRAY)
    elif kind == "saw":
        ic.polygon([(61, 151), (162, 70), (181, 91), (82, 171)], GRAY, outline=darken(GRAY, 0.24))
        teeth = [(73 + i * 12, 161 - i * 9) for i in range(7)]
        for x, y in teeth:
            ic.polygon([(x, y), (x + 8, y + 8), (x + 3, y - 5)], darken(GRAY, 0.12))
        ic.round_rect((53, 147, 86, 183), 10, BROWN, outline=darken(BROWN, 0.22))
    elif kind == "drill":
        ic.round_rect((62, 89, 143, 132), 11, ORANGE, outline=darken(ORANGE, 0.22))
        ic.polygon([(143, 99), (190, 111), (143, 123)], GRAY, outline=darken(GRAY, 0.22))
        ic.round_rect((92, 127, 126, 180), 8, ORANGE, outline=darken(ORANGE, 0.22))
    elif kind == "wrench":
        ic.line([(78, 174), (153, 99)], GRAY, 14)
        ic.ellipse((139, 67, 181, 109), (0, 0, 0, 0), outline=GRAY, width=12)
        ic.polygon([(165, 74), (189, 52), (190, 91)], (0, 0, 0, 0))
    elif kind == "nail":
        ic.line([(93, 175), (149, 63)], GRAY, 11)
        ic.line([(129, 60), (170, 80)], GRAY, 7)
    elif kind == "pliers":
        ic.line([(87, 176), (120, 121), (101, 67)], BLUE, 12)
        ic.line([(153, 176), (120, 121), (139, 67)], ORANGE, 12)
        ic.ellipse((110, 111, 130, 131), DARK)
    elif kind == "ruler":
        ic.round_rect((61, 103, 179, 136), 7, YELLOW, outline=darken(YELLOW, 0.22))
        for x in range(78, 166, 16):
            ic.line([(x, 104), (x, 119)], darken(YELLOW, 0.35), 3)
    else:
        ic.round_rect((63, 132, 177, 154), 7, TEAL, outline=darken(TEAL, 0.22))
        ic.ellipse((78, 91, 113, 126), TEAL, outline=darken(TEAL, 0.22))


def draw_business(ic: IconCanvas, token: str) -> None:
    if token == "product":
        draw_box(ic, ORANGE)
    elif token == "client":
        draw_person(ic, BLUE)
    elif token == "brand":
        draw_badge(ic, PURPLE, "star")
    elif token == "sale":
        draw_ticket(ic, RED)
    elif token == "team":
        draw_person(ic, TEAL, variant="group")
    elif token == "office":
        draw_building(ic, "building")
    elif token == "contract":
        draw_document(ic, WHITE, BLUE)
    else:
        ic.shadow(w=122, h=20)
        ic.round_rect((61, 92, 179, 174), 12, TEAL, outline=darken(TEAL, 0.24))
        ic.polygon([(56, 92), (184, 92), (168, 62), (72, 62)], ORANGE, outline=darken(ORANGE, 0.22))
        for x, c in [(82, RED), (112, YELLOW), (142, BLUE)]:
            ic.line([(x, 64), (x + 12, 92)], c, 8)
        ic.round_rect((101, 121, 139, 174), 7, DARK)


def draw_category_card(ic: IconCanvas, category: str, token: str) -> None:
    if category == "business":
        draw_business(ic, token)
    elif category == "cup":
        {"cup": lambda: draw_cup(ic, BROWN, CREAM, False), "mug": lambda: draw_cup(ic, BROWN, BLUE), "coffee": lambda: draw_cup(ic, BROWN, CREAM, steam=True), "drink": lambda: draw_drink(ic, "juice"), "tea": lambda: draw_cup(ic, ORANGE, CREAM, steam=True), "handle": lambda: draw_cup(ic, BROWN, ORANGE), "saucer": lambda: (draw_cup(ic, BROWN, CREAM), ic.ellipse((66, 162, 174, 189), WHITE, outline=GRAY)), "thermos": lambda: draw_cup(ic, BROWN, TEAL, thermos=True)}.get(token, lambda: draw_cup(ic))()
    elif category == "food":
        {"food": lambda: draw_plate(ic), "plate": lambda: draw_plate(ic), "lunch": lambda: draw_plate(ic, "lunch"), "canteen": lambda: draw_bowl(ic, ORANGE), "soup": lambda: draw_bowl(ic, ORANGE), "bread": lambda: draw_bread(ic), "fruit": lambda: draw_plate(ic, "fruit"), "salad": lambda: draw_plate(ic, "salad")}.get(token, lambda: draw_plate(ic))()
    elif category == "time":
        {"time": lambda: draw_clock(ic), "clock": lambda: draw_clock(ic), "alarm": lambda: draw_clock(ic, True), "minutes": lambda: draw_clock(ic), "day": lambda: draw_sun(ic), "night": lambda: draw_clock(ic, night=True), "week": lambda: draw_calendar(ic), "hour": lambda: draw_clock(ic)}.get(token, lambda: draw_clock(ic))()
    elif category == "travel":
        {"ticket": lambda: draw_ticket(ic, TEAL, True), "hotel": lambda: draw_hotel(ic), "route": lambda: draw_map_route(ic), "suitcase": lambda: draw_suitcase(ic), "train": lambda: draw_vehicle(ic, "train"), "plane": lambda: draw_vehicle(ic, "plane"), "map": lambda: draw_map_route(ic), "passport": lambda: draw_document(ic, BLUE, YELLOW)}.get(token, lambda: draw_map_route(ic))()
    elif category == "music":
        {"guitar": lambda: draw_instrument(ic, "guitar"), "piano": lambda: draw_instrument(ic, "piano"), "note": lambda: draw_instrument(ic, "note"), "rhythm": lambda: draw_instrument(ic, "rhythm"), "band": lambda: draw_instrument(ic, "band"), "concert": lambda: draw_instrument(ic, "concert"), "song": lambda: draw_instrument(ic, "song"), "melody": lambda: draw_instrument(ic, "melody")}.get(token, lambda: draw_instrument(ic))()
    elif category == "sport":
        {"ball": lambda: draw_sport_ball(ic), "coach": lambda: draw_person(ic, BLUE), "team": lambda: draw_person(ic, GREEN, variant="group"), "match": lambda: draw_sport_ball(ic), "field": lambda: draw_field(ic), "medal": lambda: draw_sport_ball(ic, "medal"), "race": lambda: draw_vehicle(ic, "car", RED), "player": lambda: draw_person(ic, ORANGE)}.get(token, lambda: draw_sport_ball(ic))()
    elif category == "nature":
        {"tree": lambda: draw_tree(ic), "river": lambda: draw_river(ic), "mountain": lambda: draw_mountain(ic), "stone": lambda: draw_badge(ic, GRAY, "round"), "leaf": lambda: draw_tree(ic, "leaf"), "cloud": lambda: draw_cloud(ic), "meadow": lambda: draw_meadow(ic), "sky": lambda: draw_cloud(ic)}.get(token, lambda: draw_tree(ic))()
    elif category == "weather":
        {"sun": lambda: draw_sun(ic), "rain": lambda: draw_cloud(ic, "rain"), "snow": lambda: draw_cloud(ic, "snow"), "wind": lambda: (ic.shadow(), ic.line([(60, 102), (139, 102), (162, 84)], BLUE, 9), ic.line([(75, 133), (160, 133), (180, 151)], TEAL, 8)), "storm": lambda: draw_cloud(ic, "storm"), "fog": lambda: draw_cloud(ic, "fog"), "frost": lambda: draw_winter_summer(ic, "snow"), "heat": lambda: draw_sun(ic, True)}.get(token, lambda: draw_cloud(ic))()
    elif category == "city":
        {"street": lambda: draw_vehicle(ic, "car"), "park": lambda: draw_tree(ic), "bridge": lambda: draw_building(ic, "bridge"), "square": lambda: draw_building(ic), "subway": lambda: draw_building(ic, "subway"), "cafe": lambda: draw_building(ic, "cafe"), "tower": lambda: draw_building(ic, "tower"), "traffic": lambda: (draw_vehicle(ic, "car", RED), ic.round_rect((102, 51, 138, 141), 10, DARK), ic.ellipse((112, 62, 128, 78), RED), ic.ellipse((112, 85, 128, 101), YELLOW), ic.ellipse((112, 108, 128, 124), GREEN))}.get(token, lambda: draw_building(ic))()
    elif category == "home":
        draw_home_item(ic, token)
    elif category == "kitchen":
        draw_utensil(ic, token)
    elif category == "school":
        {"class": lambda: draw_board(ic), "teacher": lambda: draw_person(ic, BLUE), "desk": lambda: draw_board(ic, "desk"), "board": lambda: draw_board(ic, "board"), "lesson": lambda: draw_book(ic, BLUE, open_book=True), "bell": lambda: draw_board(ic, "bell"), "notebook": lambda: draw_board(ic, "notebook"), "marker": lambda: draw_board(ic, "marker")}.get(token, lambda: draw_board(ic))()
    elif category == "books":
        {"novel": lambda: draw_book(ic, RED), "page": lambda: draw_document(ic), "cover": lambda: draw_book(ic, BROWN), "reader": lambda: draw_person(ic, BLUE, variant="reader"), "shelf": lambda: draw_stack_books(ic), "author": lambda: draw_person(ic, GREEN), "chapter": lambda: draw_book(ic, BLUE, open_book=True), "bookmark": lambda: draw_book(ic, BLUE)}.get(token, lambda: draw_book(ic))()
    elif category == "art":
        draw_paint(ic, token)
    elif category == "cinema":
        {"film": lambda: draw_film(ic), "actor": lambda: draw_person(ic, RED), "scene": lambda: draw_film(ic, "screen"), "screen": lambda: draw_film(ic, "screen"), "ticket": lambda: draw_ticket(ic, ORANGE), "popcorn": lambda: draw_film(ic, "popcorn"), "trailer": lambda: draw_film(ic), "camera": lambda: draw_film(ic, "camera")}.get(token, lambda: draw_film(ic))()
    elif category == "family":
        {"mother": lambda: draw_person(ic, PINK), "father": lambda: draw_person(ic, BLUE), "child": lambda: draw_person(ic, ORANGE), "sister": lambda: draw_person(ic, PINK), "brother": lambda: draw_person(ic, TEAL), "dinner": lambda: draw_plate(ic), "home": lambda: draw_home_item(ic, "door"), "album": lambda: draw_camera_photo(ic, "album")}.get(token, lambda: draw_two_people(ic))()
    elif category == "pets":
        draw_pet(ic, token)
    elif category == "garden":
        draw_garden(ic, token)
    elif category == "transport":
        {"bus": lambda: draw_vehicle(ic, "bus"), "taxi": lambda: draw_vehicle(ic, "taxi"), "wheel": lambda: draw_sport_ball(ic), "driver": lambda: draw_person(ic, BLUE), "route": lambda: draw_map_route(ic), "station": lambda: draw_building(ic, "subway"), "road": lambda: (ic.shadow(w=112, h=18), ic.polygon([(86, 183), (154, 183), (136, 55), (104, 55)], DARK), ic.line([(120, 67), (120, 173)], YELLOW, 5)), "stop": lambda: (ic.shadow(w=103, h=19), ic.regular_polygon(120, 116, 58, 8, RED, math.pi / 8))}.get(token, lambda: draw_vehicle(ic))()
    elif category == "sea":
        draw_boat_sea(ic, token)
    elif category == "space":
        draw_space(ic, token)
    elif category == "colors":
        colors = {"red": RED, "blue": BLUE, "green": GREEN, "yellow": YELLOW, "white": WHITE, "black": BLACK, "orange": ORANGE, "pink": PINK}
        draw_drop(ic, colors.get(token, BLUE))
    elif category == "shapes":
        draw_shape(ic, token)
    elif category == "clothes":
        draw_clothing(ic, token)
    elif category == "shoes":
        draw_shoe(ic, token)
    elif category == "health":
        draw_health(ic, token)
    elif category == "body":
        draw_body(ic, token)
    elif category == "work":
        {"task": lambda: draw_document(ic, WHITE, TEAL), "salary": lambda: draw_badge(ic, YELLOW, "round"), "shift": lambda: draw_clock(ic), "manager": lambda: draw_person(ic, BLUE), "report": lambda: draw_document(ic), "call": lambda: draw_phone(ic, "call"), "desk": lambda: draw_board(ic, "desk"), "break": lambda: draw_cup(ic, BROWN, CREAM, steam=True)}.get(token, lambda: draw_document(ic))()
    elif category == "office":
        draw_office_item(ic, token)
    elif category == "technology":
        draw_tech(ic, token)
    elif category == "internet":
        draw_internet(ic, token)
    elif category == "phone":
        draw_phone(ic, token)
    elif category == "photo":
        draw_camera_photo(ic, token)
    elif category == "holidays":
        {"music": lambda: draw_instrument(ic, "note")}.get(token, lambda: draw_cake(ic, token))()
    elif category == "winter":
        {"snow": lambda: draw_winter_summer(ic, "snow"), "ski": lambda: draw_winter_summer(ic, "ski"), "scarf": lambda: draw_clothing(ic, "scarf"), "ice": lambda: draw_winter_summer(ic, "ice"), "mitten": lambda: draw_clothing(ic, "mitten"), "pine": lambda: draw_tree(ic, "pine"), "cocoa": lambda: draw_winter_summer(ic, "cocoa"), "sleigh": lambda: draw_winter_summer(ic, "sleigh")}.get(token, lambda: draw_winter_summer(ic, "snow"))()
    elif category == "summer":
        draw_winter_summer(ic, token)
    elif category == "breakfast":
        {"toast": lambda: draw_bread(ic, True), "porridge": lambda: draw_bowl(ic, TAN, "berries"), "egg": lambda: draw_egg(ic), "jam": lambda: draw_drop(ic, RED), "cereal": lambda: draw_bowl(ic, CREAM, "cereal"), "yogurt": lambda: draw_bowl(ic, PINK, "berries"), "tea": lambda: draw_cup(ic, ORANGE, CREAM, steam=True), "butter": lambda: (ic.shadow(w=103, h=19), ic.round_rect((70, 99, 170, 151), 12, YELLOW, outline=darken(YELLOW, 0.22)), ic.ellipse((62, 138, 178, 170), WHITE, outline=GRAY))}.get(token, lambda: draw_bowl(ic))()
    elif category == "dessert":
        draw_dessert(ic, token)
    elif category == "drinks":
        draw_drink(ic, token)
    elif category == "farm":
        draw_farm(ic, token)
    elif category == "forest":
        draw_forest_item(ic, token)
    elif category == "flowers":
        draw_flower(ic, token)
    elif category == "insects":
        draw_insect(ic, token)
    elif category == "emotions":
        draw_emotion(ic, token)
    elif category == "friendship":
        {"friend": lambda: draw_two_people(ic), "chat": lambda: draw_internet(ic, "chat"), "walk": lambda: draw_person(ic, GREEN, variant="group"), "smile": lambda: draw_body(ic, "smile"), "help": lambda: draw_handshake(ic), "team": lambda: draw_person(ic, TEAL, variant="group"), "secret": lambda: (ic.shadow(w=100, h=19), ic.line([(95, 100), (95, 77), (145, 77), (145, 100)], DARK, 8), ic.round_rect((77, 96, 163, 163), 14, PURPLE, outline=darken(PURPLE, 0.22))), "memory": lambda: draw_camera_photo(ic, "album")}.get(token, lambda: draw_two_people(ic))()
    elif category == "hobbies":
        draw_hobby(ic, token)
    elif category == "camping":
        draw_camping(ic, token)
    elif category == "toys":
        draw_toy(ic, token)
    elif category == "tools":
        draw_tool(ic, token)
    else:
        colors = palette(category)
        draw_badge(ic, colors[int(hashlib.sha1(token.encode()).hexdigest()[:2], 16) % len(colors)])


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
                }
            )
    return cards


def generate_card(card: dict[str, str], out_dir: Path) -> dict[str, str]:
    card_id = card["id"]
    category, token = card_id.split("_", 1)
    icon = IconCanvas(card_id)
    draw_category_card(icon, category, token)
    destination = out_dir / f"{card_id}.png"
    icon.save(destination)
    return {"id": card_id, "category": category, "token": token, "status": "generated", "file": str(destination)}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", default="/tmp/codex_imagecard_raw_all")
    parser.add_argument("--clean", action="store_true")
    args = parser.parse_args()

    out_dir = Path(args.out_dir)
    if args.clean and out_dir.exists():
        shutil.rmtree(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    cards = load_cards()
    report = []
    for card in cards:
        try:
            report.append(generate_card(card, out_dir))
        except Exception as exc:  # noqa: BLE001 - batch report should include all failures
            report.append({"id": card["id"], "status": "error", "error": str(exc)})

    (out_dir / "procedural_generation_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    generated = sum(1 for item in report if item["status"] == "generated")
    errors = len(report) - generated
    print(f"generated={generated} errors={errors} out={out_dir}")
    return 0 if errors == 0 and generated == len(cards) else 1


if __name__ == "__main__":
    raise SystemExit(main())
