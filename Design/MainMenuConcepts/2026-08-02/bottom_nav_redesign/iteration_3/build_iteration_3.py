from pathlib import Path
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont


HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
SOURCE = HERE / "sources" / "G_detached_play_four_tiles.png"
SOURCE_H = HERE / "agent_H" / "variant_H_transparent.png"
SOURCE_I = HERE / "agent_I" / "variant_I_corrected_transparent.png"
BACKGROUND = Path(
    "/Users/vnm/Documents/CardMesh/Assets/Art/Backgrounds/"
    "BG_MainMenu_Mobile_9x20.png"
)
FONT = Path(
    "/Users/vnm/Documents/CardMesh/"
    "Assets/TextMesh Pro/Examples & Extras/Fonts/Oswald-Bold.ttf"
)

WIDTH = 1080
HEIGHT = 760
GOLD = (236, 210, 139, 255)
GOLD_BRIGHT = (255, 233, 169, 255)


def crop_alpha(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise ValueError("Generated source has no visible content")
    return image.crop(bbox)


def background() -> Image.Image:
    source = Image.open(BACKGROUND).convert("RGB")
    crop_h = round(source.width * HEIGHT / WIDTH)
    top = max(0, source.height - crop_h)
    source = source.crop((0, top, source.width, top + crop_h))
    source = source.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)
    source = ImageEnhance.Brightness(source).enhance(0.47)
    source = ImageEnhance.Color(source).enhance(0.78)
    return source.convert("RGBA")


def draw_centered(
    image: Image.Image,
    center: tuple[int, int],
    text: str,
    font: ImageFont.FreeTypeFont,
    color: tuple[int, int, int, int],
    stroke: int = 2,
) -> None:
    draw = ImageDraw.Draw(image)
    bbox = draw.textbbox((0, 0), text, font=font, stroke_width=stroke)
    text_w = bbox[2] - bbox[0]
    text_h = bbox[3] - bbox[1]
    x, y = center
    draw.text(
        (x - text_w / 2, y - text_h / 2 - bbox[1]),
        text,
        font=font,
        fill=color,
        stroke_width=stroke,
        stroke_fill=(33, 20, 3, 245),
    )


def main() -> None:
    canvas = build_g()
    output = HERE / "G_detached_play_four_tiles_preview.png"
    canvas.convert("RGB").save(output, quality=96)

    option_h = build_h()
    option_h.convert("RGB").save(
        HERE / "H_linked_medallions_preview.png",
        quality=96,
    )
    option_i = build_i()
    option_i.convert("RGB").save(
        HERE / "I_emerald_capsules_preview.png",
        quality=96,
    )

    gap = 26
    sheet = Image.new(
        "RGB",
        (WIDTH, HEIGHT * 3 + gap * 2),
        (4, 31, 24),
    )
    for index, option in enumerate((canvas, option_h, option_i)):
        sheet.paste(option.convert("RGB"), (0, index * (HEIGHT + gap)))
    sheet.save(HERE / "GHI_finalists_comparison.png", quality=96)


def build_g() -> Image.Image:
    canvas = background()
    group = crop_alpha(Image.open(SOURCE))
    group_w = 1016
    group_h = round(group.height * group_w / group.width)
    group = group.resize((group_w, group_h), Image.Resampling.LANCZOS)
    left = (WIDTH - group_w) // 2
    top = 118

    shadow_alpha = group.getchannel("A").filter(ImageFilter.GaussianBlur(14))
    shadow = Image.new("RGBA", group.size, (0, 0, 0, 135))
    shadow.putalpha(shadow_alpha.point(lambda value: int(value * 0.54)))
    canvas.alpha_composite(shadow, (left, top + 10))
    canvas.alpha_composite(group, (left, top))

    play_font = ImageFont.truetype(FONT, 56)
    label_font = ImageFont.truetype(FONT, 32)
    draw_centered(
        canvas,
        (left + int(group_w * 0.600), top + int(group_h * 0.245)),
        "ИГРАТЬ",
        play_font,
        GOLD_BRIGHT,
        3,
    )

    label_y = top + group_h + 25
    for x, label in zip(
        (0.116, 0.377, 0.628, 0.884),
        ("УРОВНИ", "КОЛЛЕКЦИЯ", "МАГАЗИН", "НАСТРОЙКИ"),
    ):
        draw_centered(
            canvas,
            (left + int(group_w * x), label_y),
            label,
            label_font,
            GOLD,
        )

    draw = ImageDraw.Draw(canvas)
    draw.rounded_rectangle(
        (32, 24, 1048, 88),
        radius=18,
        fill=(2, 20, 15, 224),
        outline=(160, 130, 67, 220),
        width=2,
    )
    title_font = ImageFont.truetype(FONT, 31)
    draw.text(
        (58, 40),
        "G — ОТДЕЛЬНАЯ CTA + 4 КНОПКИ",
        font=title_font,
        fill=(245, 221, 158, 255),
    )
    return canvas


def composite_group(
    source: Path,
    top: int,
    group_w: int = 1016,
) -> tuple[Image.Image, int, int, int, int]:
    canvas = background()
    group = crop_alpha(Image.open(source))
    group_h = round(group.height * group_w / group.width)
    group = group.resize((group_w, group_h), Image.Resampling.LANCZOS)
    left = (WIDTH - group_w) // 2

    shadow_alpha = group.getchannel("A").filter(ImageFilter.GaussianBlur(14))
    shadow = Image.new("RGBA", group.size, (0, 0, 0, 135))
    shadow.putalpha(shadow_alpha.point(lambda value: int(value * 0.54)))
    canvas.alpha_composite(shadow, (left, top + 10))
    canvas.alpha_composite(group, (left, top))
    return canvas, left, top, group_w, group_h


def title(canvas: Image.Image, text: str) -> None:
    draw = ImageDraw.Draw(canvas)
    draw.rounded_rectangle(
        (32, 24, 1048, 88),
        radius=18,
        fill=(2, 20, 15, 224),
        outline=(160, 130, 67, 220),
        width=2,
    )
    title_font = ImageFont.truetype(FONT, 31)
    draw.text(
        (58, 40),
        text,
        font=title_font,
        fill=(245, 221, 158, 255),
    )


def build_h() -> Image.Image:
    canvas, left, top, group_w, group_h = composite_group(SOURCE_H, 106)
    play_font = ImageFont.truetype(FONT, 56)
    label_font = ImageFont.truetype(FONT, 32)
    draw_centered(
        canvas,
        (left + int(group_w * 0.635), top + int(group_h * 0.205)),
        "ИГРАТЬ",
        play_font,
        GOLD_BRIGHT,
        3,
    )
    label_y = top + group_h + 24
    for x, label in zip(
        (0.144, 0.380, 0.617, 0.853),
        ("УРОВНИ", "КОЛЛЕКЦИЯ", "МАГАЗИН", "НАСТРОЙКИ"),
    ):
        draw_centered(
            canvas,
            (left + int(group_w * x), label_y),
            label,
            label_font,
            GOLD,
        )
    title(canvas, "H — СВЯЗАННЫЕ МЕДАЛЬОНЫ")
    return canvas


def build_i() -> Image.Image:
    canvas, left, top, group_w, group_h = composite_group(SOURCE_I, 104)
    play_font = ImageFont.truetype(FONT, 54)
    label_font = ImageFont.truetype(FONT, 30)
    draw_centered(
        canvas,
        (left + int(group_w * 0.625), top + int(group_h * 0.230)),
        "ИГРАТЬ",
        play_font,
        GOLD_BRIGHT,
        3,
    )
    for x, label in zip(
        (0.160, 0.410, 0.660, 0.905),
        ("УРОВНИ", "КОЛЛЕКЦИЯ", "МАГАЗИН", "НАСТРОЙКИ"),
    ):
        draw_centered(
            canvas,
            (left + int(group_w * x), top + int(group_h * 0.830)),
            label,
            label_font,
            GOLD,
        )
    title(canvas, "I — ИЗУМРУДНЫЕ КАПСУЛЫ")
    return canvas


if __name__ == "__main__":
    main()
