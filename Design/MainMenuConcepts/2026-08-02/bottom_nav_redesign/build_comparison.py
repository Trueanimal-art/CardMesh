from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parent
SOURCES = ROOT / "sources"
BACKGROUND = Path("/Users/vnm/Documents/CardMesh/Assets/Art/Backgrounds/BG_MainMenu_Mobile_9x20.png")
FONT = Path(
    "/Users/vnm/Documents/CardMesh/"
    "Assets/TextMesh Pro/Examples & Extras/Fonts/Oswald-Bold.ttf"
)

CANVAS_W = 1080
CARD_H = 650
GOLD = (236, 210, 139, 255)
GOLD_BRIGHT = (255, 231, 158, 255)
EMERALD = (4, 31, 24, 255)


def crop_alpha(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    bbox = rgba.getchannel("A").getbbox()
    if bbox is None:
        raise ValueError("Generated UI source has no visible alpha content")
    return rgba.crop(bbox)


def centered_text(
    layer: Image.Image,
    xy: tuple[int, int],
    text: str,
    font: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int, int],
    stroke_width: int = 2,
) -> None:
    draw = ImageDraw.Draw(layer)
    x, y = xy
    box = draw.textbbox(
        (0, 0),
        text,
        font=font,
        stroke_width=stroke_width,
    )
    width = box[2] - box[0]
    height = box[3] - box[1]
    draw.text(
        (x - width / 2, y - height / 2 - box[1]),
        text,
        font=font,
        fill=fill,
        stroke_width=stroke_width,
        stroke_fill=(36, 22, 4, 235),
    )


def build_background() -> Image.Image:
    source = Image.open(BACKGROUND).convert("RGB")
    source_ratio = source.width / source.height
    target_ratio = CANVAS_W / CARD_H
    if source_ratio > target_ratio:
        crop_w = round(source.height * target_ratio)
        left = (source.width - crop_w) // 2
        source = source.crop((left, 0, left + crop_w, source.height))
    else:
        crop_h = round(source.width / target_ratio)
        top = max(0, source.height - crop_h)
        source = source.crop((0, top, source.width, top + crop_h))
    source = source.resize((CANVAS_W, CARD_H), Image.Resampling.LANCZOS)
    source = ImageEnhance.Brightness(source).enhance(0.46)
    source = ImageEnhance.Color(source).enhance(0.75)
    return source.convert("RGBA")


def add_panel(
    background: Image.Image,
    source_name: str,
    panel_width: int,
    top: int,
) -> tuple[Image.Image, tuple[int, int, int, int]]:
    panel = crop_alpha(Image.open(SOURCES / source_name))
    new_h = round(panel.height * panel_width / panel.width)
    panel = panel.resize((panel_width, new_h), Image.Resampling.LANCZOS)
    left = (CANVAS_W - panel_width) // 2

    shadow = Image.new("RGBA", background.size, (0, 0, 0, 0))
    shadow_alpha = panel.getchannel("A").filter(ImageFilter.GaussianBlur(14))
    shadow_patch = Image.new("RGBA", panel.size, (0, 0, 0, 145))
    shadow_patch.putalpha(shadow_alpha.point(lambda value: int(value * 0.52)))
    shadow.alpha_composite(shadow_patch, (left, top + 10))
    background = Image.alpha_composite(background, shadow)
    background.alpha_composite(panel, (left, top))
    return background, (left, top, panel_width, new_h)


def make_option_a() -> Image.Image:
    image = build_background()
    image, (left, top, width, height) = add_panel(
        image, "A_royal_command_bar.png", 1036, 122
    )
    label_font = ImageFont.truetype(FONT, 30)
    play_font = ImageFont.truetype(FONT, 48)
    for x, text in zip(
        (0.105, 0.290, 0.500, 0.730, 0.905),
        ("УРОВНИ", "КОЛЛЕКЦИЯ", "ИГРАТЬ", "МАГАЗИН", "НАСТРОЙКИ"),
    ):
        font = play_font if text == "ИГРАТЬ" else label_font
        y = top + int(height * (0.710 if text == "ИГРАТЬ" else 0.805))
        centered_text(image, (left + int(width * x), y), text, font, GOLD_BRIGHT)
    return image


def make_option_b() -> Image.Image:
    image = build_background()
    image, (left, top, width, height) = add_panel(
        image, "B_illuminated_card_dock.png", 1036, 132
    )
    label_font = ImageFont.truetype(FONT, 30)
    play_font = ImageFont.truetype(FONT, 48)
    for x, text in zip(
        (0.105, 0.285, 0.500, 0.730, 0.905),
        ("УРОВНИ", "КОЛЛЕКЦИЯ", "ИГРАТЬ", "МАГАЗИН", "НАСТРОЙКИ"),
    ):
        font = play_font if text == "ИГРАТЬ" else label_font
        y = top + int(height * (0.735 if text == "ИГРАТЬ" else 0.830))
        centered_text(image, (left + int(width * x), y), text, font, GOLD_BRIGHT)
    return image


def make_option_c() -> Image.Image:
    image = build_background()
    image, (left, top, width, height) = add_panel(
        image, "C_cardmesh_command_deck.png", 1036, 72
    )
    label_font = ImageFont.truetype(FONT, 34)
    play_font = ImageFont.truetype(FONT, 52)
    centered_text(
        image,
        (left + int(width * 0.598), top + int(height * 0.205)),
        "ИГРАТЬ",
        play_font,
        GOLD_BRIGHT,
        stroke_width=3,
    )
    for x, text in zip(
        (0.125, 0.375, 0.625, 0.875),
        ("УРОВНИ", "КОЛЛЕКЦИЯ", "МАГАЗИН", "НАСТРОЙКИ"),
    ):
        centered_text(
            image,
            (left + int(width * x), top + int(height * 0.900)),
            text,
            label_font,
            GOLD,
        )
    return image


def add_option_header(image: Image.Image, letter: str, title: str) -> Image.Image:
    draw = ImageDraw.Draw(image)
    letter_font = ImageFont.truetype(FONT, 46)
    title_font = ImageFont.truetype(FONT, 31)
    draw.rounded_rectangle(
        (30, 24, 1048, 94),
        radius=20,
        fill=(2, 20, 15, 225),
        outline=(160, 130, 67, 220),
        width=2,
    )
    draw.ellipse((48, 34, 102, 88), fill=(151, 113, 40, 255))
    centered_text(image, (75, 61), letter, letter_font, (255, 240, 187, 255), 1)
    draw.text((124, 43), title, font=title_font, fill=(245, 221, 158, 255))
    return image


def main() -> None:
    options = [
        add_option_header(make_option_a(), "A", "КОМАНДНЫЙ ГЕРБ"),
        add_option_header(make_option_b(), "B", "КАРТОЧНЫЙ ДОК"),
        add_option_header(make_option_c(), "C", "КОМАНДНАЯ ПАНЕЛЬ"),
    ]

    for name, option in zip(
        (
            "A_royal_command_bar_preview.png",
            "B_illuminated_card_dock_preview.png",
            "C_cardmesh_command_deck_preview.png",
        ),
        options,
    ):
        option.convert("RGB").save(ROOT / name, quality=95)

    gap = 26
    sheet = Image.new(
        "RGB",
        (CANVAS_W, CARD_H * len(options) + gap * (len(options) - 1)),
        EMERALD[:3],
    )
    y = 0
    for option in options:
        sheet.paste(option.convert("RGB"), (0, y))
        y += CARD_H + gap
    sheet.save(ROOT / "ABC_bottom_navigation_comparison.png", quality=96)


if __name__ == "__main__":
    main()
