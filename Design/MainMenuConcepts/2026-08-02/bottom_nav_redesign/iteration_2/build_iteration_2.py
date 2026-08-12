from pathlib import Path
import sys

from PIL import Image, ImageFilter, ImageFont


HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
SOURCES = HERE / "sources"
sys.path.insert(0, str(ROOT))

from build_comparison import (  # noqa: E402
    CARD_H,
    CANVAS_W,
    EMERALD,
    FONT,
    GOLD,
    GOLD_BRIGHT,
    add_option_header,
    build_background,
    centered_text,
    crop_alpha,
)


def make_option_d() -> Image.Image:
    image = build_background()
    image, (left, top, width, height) = add_panel_from_iteration(
        image, "D_integrated_command_bridge.png", 1036, 112
    )
    label_font = ImageFont.truetype(FONT, 34)
    play_font = ImageFont.truetype(FONT, 52)
    centered_text(
        image,
        (left + int(width * 0.615), top + int(height * 0.335)),
        "ИГРАТЬ",
        play_font,
        GOLD_BRIGHT,
        3,
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


def make_option_e() -> Image.Image:
    image = build_background()
    image, (left, top, width, height) = add_panel_from_iteration(
        image, "E_five_cell_reliquary.png", 1036, 184
    )
    label_font = ImageFont.truetype(FONT, 31)
    play_font = ImageFont.truetype(FONT, 44)
    for x, text in zip(
        (0.100, 0.300, 0.500, 0.700, 0.900),
        ("УРОВНИ", "КОЛЛЕКЦИЯ", "ИГРАТЬ", "МАГАЗИН", "НАСТРОЙКИ"),
    ):
        centered_text(
            image,
            (left + int(width * x), top + int(height * 0.835)),
            text,
            play_font if text == "ИГРАТЬ" else label_font,
            GOLD_BRIGHT if text == "ИГРАТЬ" else GOLD,
            3 if text == "ИГРАТЬ" else 2,
        )
    return image


def make_option_f() -> Image.Image:
    image = build_background()
    image, (left, top, width, height) = add_panel_from_iteration(
        image, "F_inverted_command_deck.png", 1036, 154
    )
    label_font = ImageFont.truetype(FONT, 33)
    play_font = ImageFont.truetype(FONT, 52)
    for x, text in zip(
        (0.125, 0.375, 0.625, 0.875),
        ("УРОВНИ", "КОЛЛЕКЦИЯ", "МАГАЗИН", "НАСТРОЙКИ"),
    ):
        centered_text(
            image,
            (left + int(width * x), top + int(height * 0.525)),
            text,
            label_font,
            GOLD,
        )
    centered_text(
        image,
        (left + int(width * 0.610), top + int(height * 0.835)),
        "ИГРАТЬ",
        play_font,
        GOLD_BRIGHT,
        3,
    )
    return image


def add_panel_from_iteration(
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


def save_sheet(
    output: Path,
    items: list[Image.Image],
    gap: int = 26,
) -> None:
    sheet = Image.new(
        "RGB",
        (CANVAS_W, CARD_H * len(items) + gap * (len(items) - 1)),
        EMERALD[:3],
    )
    y = 0
    for item in items:
        sheet.paste(item.convert("RGB"), (0, y))
        y += CARD_H + gap
    sheet.save(output, quality=96)


def main() -> None:
    new_options = [
        add_option_header(make_option_d(), "D", "ВСТРОЕННЫЙ МОСТ"),
        add_option_header(make_option_e(), "E", "ПЯТЬ ЯЧЕЕК"),
        add_option_header(make_option_f(), "F", "НИЖНЯЯ КОМАНДА"),
    ]
    names = (
        "D_integrated_command_bridge_preview.png",
        "E_five_cell_reliquary_preview.png",
        "F_inverted_command_deck_preview.png",
    )
    for name, option in zip(names, new_options):
        option.convert("RGB").save(HERE / name, quality=95)

    save_sheet(HERE / "DEF_new_variants_comparison.png", new_options)

    baseline_c = Image.open(ROOT / "C_cardmesh_command_deck_preview.png").convert("RGBA")
    save_sheet(
        HERE / "CDEF_finalists_comparison.png",
        [baseline_c, *new_options],
    )


if __name__ == "__main__":
    main()
