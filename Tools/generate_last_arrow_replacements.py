#!/usr/bin/env python3
"""Create full replacement PNGs for the final arrow-marked ImageCards.

These assets intentionally do not reuse the physically cropped source pixels.
They generate complete transparent 240x240 icons for the final small set that
remained visually broken after crop/refit passes.
"""

from __future__ import annotations

import argparse
import json
import math
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


TARGET_IDS = [
    "camping_backpack",
    "city_park",
    "clothes_glove",
    "dessert_waffle",
    "emotions_warmth",
    "garden_bench",
    "garden_soil",
    "sport_field",
    "tools_saw",
    "toys_train",
]

SIZE = 240


def rgba(color, alpha: int = 255):
    return (*color, alpha)


def mix(a, b, t: float):
    return tuple(round(a[i] * (1 - t) + b[i] * t) for i in range(4))


def gradient_fill(size: tuple[int, int], top, bottom):
    img = Image.new("RGBA", size)
    draw = ImageDraw.Draw(img)
    denom = max(1, size[1] - 1)
    for y in range(size[1]):
        draw.line((0, y, size[0], y), fill=mix(top, bottom, y / denom))
    return img


def alpha_bbox(img: Image.Image):
    return img.getchannel("A").getbbox()


def shadow(draw: ImageDraw.ImageDraw, cx: int = 120, cy: int = 177, w: int = 104, h: int = 18, a: int = 54):
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.ellipse((cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2), fill=(0, 0, 0, a))
    layer = layer.filter(ImageFilter.GaussianBlur(5))
    draw.bitmap((0, 0), layer)


def rounded_gradient(img: Image.Image, box, radius: int, top, bottom, outline=None, width: int = 2):
    mask = Image.new("L", (SIZE, SIZE), 0)
    ImageDraw.Draw(mask).rounded_rectangle(box, radius=radius, fill=255)
    grad = gradient_fill((box[2] - box[0], box[3] - box[1]), top, bottom)
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    layer.paste(grad, box[:2], mask.crop(box))
    img.alpha_composite(layer)
    if outline:
        ImageDraw.Draw(img).rounded_rectangle(box, radius=radius, outline=outline, width=width)


def ellipse_gradient(img: Image.Image, box, top, bottom, outline=None, width: int = 2):
    mask = Image.new("L", (SIZE, SIZE), 0)
    ImageDraw.Draw(mask).ellipse(box, fill=255)
    grad = gradient_fill((box[2] - box[0], box[3] - box[1]), top, bottom)
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    layer.paste(grad, box[:2], mask.crop(box))
    img.alpha_composite(layer)
    if outline:
        ImageDraw.Draw(img).ellipse(box, outline=outline, width=width)


def polygon_gradient(img: Image.Image, points, top, bottom, outline=None, width: int = 2):
    mask = Image.new("L", (SIZE, SIZE), 0)
    ImageDraw.Draw(mask).polygon(points, fill=255)
    bbox = mask.getbbox()
    if bbox is None:
        return
    grad = gradient_fill((bbox[2] - bbox[0], bbox[3] - bbox[1]), top, bottom)
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    layer.paste(grad, bbox[:2], mask.crop(bbox))
    img.alpha_composite(layer)
    if outline:
        d = ImageDraw.Draw(img)
        d.line(points + [points[0]], fill=outline, width=width, joint="curve")


def new_icon():
    return Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))


def draw_backpack():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 120, 181, 92, 18)
    rounded_gradient(img, (78, 76, 162, 174), 18, rgba((205, 113, 42)), rgba((139, 70, 31)), rgba((101, 55, 29)), 3)
    rounded_gradient(img, (92, 104, 148, 166), 10, rgba((238, 145, 56)), rgba((177, 83, 34)), rgba((104, 58, 32)), 2)
    rounded_gradient(img, (67, 94, 86, 160), 8, rgba((91, 70, 62)), rgba((48, 46, 50)), rgba((31, 30, 32)), 2)
    rounded_gradient(img, (154, 94, 173, 160), 8, rgba((91, 70, 62)), rgba((48, 46, 50)), rgba((31, 30, 32)), 2)
    rounded_gradient(img, (80, 50, 160, 83), 12, rgba((139, 185, 74)), rgba((88, 130, 48)), rgba((70, 88, 44)), 2)
    for x in (96, 120, 144):
        d.line((x, 52, x - 10, 82), fill=(95, 58, 35, 220), width=3)
    rounded_gradient(img, (152, 111, 171, 148), 7, rgba((57, 137, 221)), rgba((29, 73, 161)), rgba((16, 54, 119)), 2)
    d.line((100, 95, 140, 95), fill=(255, 200, 105, 230), width=4)
    d.arc((89, 111, 111, 145), 80, 280, fill=(245, 187, 87, 230), width=3)
    d.arc((129, 111, 151, 145), -100, 100, fill=(245, 187, 87, 230), width=3)
    return img


def draw_city_park():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 120, 182, 118, 18)
    polygon_gradient(img, [(65, 171), (99, 145), (174, 147), (187, 172), (151, 187), (82, 185)], rgba((105, 185, 83)), rgba((67, 130, 55)), rgba((52, 94, 49)), 2)
    rounded_gradient(img, (102, 126, 158, 151), 7, rgba((185, 115, 54)), rgba((129, 70, 35)), rgba((90, 56, 35)), 2)
    for x in (111, 128, 145):
        d.line((x, 128, x - 4, 150), fill=(244, 184, 97, 230), width=3)
    d.line((98, 153, 161, 153), fill=(86, 51, 31, 255), width=5)
    d.line((108, 153, 101, 169), fill=(95, 54, 31, 255), width=5)
    d.line((151, 153, 158, 169), fill=(95, 54, 31, 255), width=5)
    rounded_gradient(img, (82, 99, 98, 164), 6, rgba((96, 65, 39)), rgba((70, 45, 28)), rgba((54, 35, 22)), 2)
    ellipse_gradient(img, (64, 64, 121, 116), rgba((134, 217, 74)), rgba((63, 148, 45)), rgba((49, 107, 42)), 2)
    ellipse_gradient(img, (94, 53, 161, 118), rgba((142, 225, 78)), rgba((72, 156, 45)), rgba((50, 112, 39)), 2)
    ellipse_gradient(img, (125, 78, 177, 124), rgba((131, 207, 73)), rgba((70, 145, 48)), rgba((50, 101, 44)), 2)
    return img


def draw_glove():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 121, 181, 70, 15)
    rounded_gradient(img, (82, 70, 156, 156), 18, rgba((46, 96, 183)), rgba((25, 53, 133)), rgba((15, 36, 100)), 3)
    for i, x in enumerate((86, 104, 122, 140)):
        rounded_gradient(img, (x, 51 + i % 2 * 4, x + 20, 94), 9, rgba((53, 111, 204)), rgba((25, 59, 145)), rgba((16, 42, 106)), 2)
    rounded_gradient(img, (73, 103, 94, 147), 10, rgba((52, 102, 192)), rgba((25, 58, 139)), rgba((17, 39, 101)), 2)
    rounded_gradient(img, (82, 154, 157, 176), 5, rgba((238, 210, 165)), rgba((181, 107, 70)), rgba((123, 65, 45)), 2)
    d.line((91, 162, 150, 162), fill=(177, 50, 40, 220), width=4)
    d.line((91, 170, 150, 170), fill=(177, 50, 40, 220), width=3)
    cx, cy = 119, 114
    for angle in range(0, 360, 45):
        x = cx + math.cos(math.radians(angle)) * 24
        y = cy + math.sin(math.radians(angle)) * 24
        d.line((cx, cy, x, y), fill=(245, 245, 236, 230), width=3)
    ellipse_gradient(img, (113, 108, 125, 120), rgba((255, 255, 245)), rgba((230, 230, 215)))
    return img


def draw_waffle():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 120, 179, 93, 16)
    polygon_gradient(img, [(76, 91), (145, 68), (174, 134), (101, 163)], rgba((255, 184, 45)), rgba((205, 94, 18)), rgba((159, 84, 20)), 3)
    for p1, p2 in [((95, 87), (124, 153)), ((118, 79), (148, 144)), ((144, 77), (169, 129)), ((84, 112), (159, 86)), ((94, 136), (169, 108))]:
        d.line((*p1, *p2), fill=(166, 86, 25, 210), width=7)
        d.line((*p1, *p2), fill=(255, 205, 72, 170), width=2)
    rounded_gradient(img, (103, 98, 139, 130), 7, rgba((255, 224, 84)), rgba((240, 143, 22)), rgba((185, 93, 21)), 2)
    return img


def draw_blanket():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 120, 180, 96, 16)
    rounded_gradient(img, (73, 85, 158, 165), 18, rgba((248, 123, 92)), rgba((201, 70, 49)), rgba((156, 51, 42)), 2)
    for y in (101, 119, 137):
        d.arc((72, y, 158, y + 44), 180, 360, fill=(255, 184, 160, 160), width=3)
    for x in (91, 116, 141):
        d.line((x, 88, x - 8, 161), fill=(255, 188, 160, 150), width=3)
    rounded_gradient(img, (138, 89, 176, 164), 15, rgba((240, 112, 85)), rgba((190, 63, 47)), rgba((142, 44, 35)), 2)
    return img


def draw_bench():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 120, 181, 105, 18)
    for x in (84, 156):
        rounded_gradient(img, (x, 121, x + 13, 170), 5, rgba((153, 84, 37)), rgba((91, 48, 26)), rgba((68, 36, 21)), 2)
    rounded_gradient(img, (75, 84, 165, 103), 7, rgba((202, 126, 62)), rgba((137, 72, 36)), rgba((92, 50, 30)), 2)
    rounded_gradient(img, (70, 116, 170, 139), 8, rgba((218, 151, 78)), rgba((151, 81, 38)), rgba((95, 53, 31)), 2)
    for x in range(84, 158, 15):
        rounded_gradient(img, (x, 88, x + 8, 138), 4, rgba((236, 176, 104)), rgba((145, 76, 36)), rgba((93, 50, 29)), 1)
    d.arc((84, 56, 156, 115), 190, 350, fill=(145, 76, 36, 255), width=8)
    d.line((78, 139, 166, 139), fill=(82, 45, 28, 255), width=5)
    return img


def draw_soil():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 120, 181, 99, 17)
    ellipse_gradient(img, (72, 99, 169, 171), rgba((124, 76, 45)), rgba((72, 38, 25)), rgba((54, 29, 20)), 2)
    for box in [(82, 90, 111, 120), (103, 83, 136, 118), (129, 93, 159, 127), (77, 124, 112, 158), (111, 118, 151, 159), (138, 130, 170, 163), (92, 147, 126, 176)]:
        ellipse_gradient(img, box, rgba((137, 82, 48)), rgba((71, 38, 26)), rgba((54, 29, 20)), 1)
    return img


def draw_field():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 120, 181, 106, 17)
    polygon_gradient(img, [(70, 93), (163, 75), (178, 146), (88, 170)], rgba((123, 197, 51)), rgba((67, 135, 42)), rgba((48, 85, 35)), 3)
    polygon_gradient(img, [(88, 170), (178, 146), (179, 164), (93, 189)], rgba((125, 78, 38)), rgba((75, 45, 28)), rgba((54, 34, 23)), 2)
    d.line((118, 84, 132, 158), fill=(238, 245, 210, 230), width=3)
    d.line((76, 118, 173, 98), fill=(238, 245, 210, 230), width=3)
    d.line((84, 144, 177, 125), fill=(238, 245, 210, 230), width=3)
    d.arc((89, 105, 137, 145), 270, 90, fill=(238, 245, 210, 230), width=3)
    d.arc((124, 94, 168, 132), 90, 270, fill=(238, 245, 210, 230), width=3)
    d.ellipse((118, 113, 130, 125), outline=(238, 245, 210, 230), width=3)
    return img


def draw_saw():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 120, 181, 72, 14)
    blade = [(93, 55), (145, 84), (137, 151), (111, 144)]
    polygon_gradient(img, blade, rgba((235, 235, 238)), rgba((178, 181, 186)), rgba((120, 125, 134)), 2)
    teeth = []
    for i in range(7):
        y = 82 + i * 9
        teeth.extend([(140, y), (151, y + 4), (139, y + 8)])
    d.line(teeth, fill=(128, 133, 142, 255), width=2)
    rounded_gradient(img, (78, 138, 123, 181), 10, rgba((35, 106, 213)), rgba((16, 61, 151)), rgba((12, 43, 106)), 3)
    rounded_gradient(img, (89, 151, 111, 169), 7, rgba((246, 210, 74)), rgba((223, 147, 35)), rgba((155, 83, 22)), 2)
    d.line((111, 139, 124, 151), fill=(236, 187, 57, 255), width=5)
    return img


def draw_train():
    img = new_icon()
    d = ImageDraw.Draw(img)
    shadow(d, 120, 181, 112, 18)
    rounded_gradient(img, (69, 101, 169, 163), 13, rgba((44, 143, 226)), rgba((16, 82, 177)), rgba((11, 57, 131)), 3)
    rounded_gradient(img, (96, 77, 151, 110), 10, rgba((238, 65, 42)), rgba((175, 36, 28)), rgba((125, 28, 24)), 2)
    rounded_gradient(img, (68, 132, 98, 164), 7, rgba((238, 58, 38)), rgba((173, 35, 28)), rgba((122, 27, 23)), 2)
    rounded_gradient(img, (78, 82, 105, 110), 8, rgba((249, 194, 50)), rgba((219, 124, 24)), rgba((157, 76, 20)), 2)
    rounded_gradient(img, (82, 113, 110, 135), 4, rgba((252, 236, 130)), rgba((239, 160, 41)), rgba((160, 87, 24)), 2)
    rounded_gradient(img, (124, 116, 153, 139), 4, rgba((252, 236, 130)), rgba((239, 160, 41)), rgba((160, 87, 24)), 2)
    for x in (87, 122, 156):
        ellipse_gradient(img, (x - 11, 153, x + 11, 175), rgba((65, 69, 78)), rgba((18, 21, 27)), rgba((10, 10, 12)), 2)
        ellipse_gradient(img, (x - 5, 159, x + 5, 169), rgba((244, 196, 51)), rgba((210, 118, 24)))
    d.line((75, 154, 170, 154), fill=(245, 184, 49, 255), width=4)
    return img


DRAWERS = {
    "camping_backpack": draw_backpack,
    "city_park": draw_city_park,
    "clothes_glove": draw_glove,
    "dessert_waffle": draw_waffle,
    "emotions_warmth": draw_blanket,
    "garden_bench": draw_bench,
    "garden_soil": draw_soil,
    "sport_field": draw_field,
    "tools_saw": draw_saw,
    "toys_train": draw_train,
}


def fit_canvas(img: Image.Image) -> tuple[Image.Image, dict[str, object]]:
    bbox = alpha_bbox(img)
    if bbox is None:
        raise ValueError("empty generated icon")
    cropped = img.crop(bbox)
    max_side = 144
    scale = min(max_side / max(cropped.size), 1.0)
    resized = cropped.resize((round(cropped.width * scale), round(cropped.height * scale)), Image.Resampling.LANCZOS)
    out = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    out.alpha_composite(resized, ((SIZE - resized.width) // 2, (SIZE - resized.height) // 2))
    final = alpha_bbox(out)
    if final is None:
        raise ValueError("empty final icon")
    l, t, r, b = final
    return out, {"bbox": list(final), "margins": [l, t, SIZE - r, SIZE - b], "maxSide": max(r - l, b - t)}


def composite_preview(img: Image.Image, tile: int = 148) -> Image.Image:
    bg = Image.new("RGBA", img.size, (246, 244, 235, 255))
    bg.alpha_composite(img)
    return bg.convert("RGB").resize((tile, tile), Image.Resampling.NEAREST)


def make_contact_sheet(items, output: Path):
    tile = 158
    label = 210
    header = 42
    sheet = Image.new("RGB", (label + tile * 2, header + tile * len(items)), (31, 34, 37))
    d = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("Arial.ttf", 13)
    except OSError:
        font = ImageFont.load_default()
    d.text((label, 12), "before", fill=(230, 230, 230), font=font)
    d.text((label + tile, 12), "replacement", fill=(230, 230, 230), font=font)
    for i, (card_id, before, after) in enumerate(items):
        y = header + i * tile
        d.text((8, y + 10), card_id, fill=(230, 230, 230), font=font)
        sheet.paste(composite_preview(before), (label + 5, y + 5))
        sheet.paste(composite_preview(after), (label + tile + 5, y + 5))
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default="Assets/Resources/ImageCards/Premium")
    parser.add_argument("--out-dir", default="Tools/ImageCardAudit/last_arrow_replacements")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    root = Path(args.root)
    out_dir = Path(args.out_dir)
    sliced = out_dir / "sliced"
    sliced.mkdir(parents=True, exist_ok=True)
    items = []
    report = []
    for card_id in TARGET_IDS:
        before_path = root / f"{card_id}.png"
        before = Image.open(before_path).convert("RGBA")
        generated, item_report = fit_canvas(DRAWERS[card_id]())
        generated.save(sliced / f"{card_id}.png")
        if args.apply:
            backup = out_dir / "before_apply"
            backup.mkdir(parents=True, exist_ok=True)
            shutil.copy2(before_path, backup / before_path.name)
            generated.save(before_path)
        items.append((card_id, before, generated))
        report.append({"id": card_id, "file": str(before_path), **item_report})
    make_contact_sheet(items, out_dir / "before_after.png")
    (out_dir / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"generated={len(items)} apply={args.apply}")
    print(out_dir / "before_after.png")
    print(out_dir / "report.json")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
