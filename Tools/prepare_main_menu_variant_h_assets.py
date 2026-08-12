#!/usr/bin/env python3
"""Extract the approved Variant H controls into production Unity sprites."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = (
    ROOT
    / "Design/MainMenuConcepts/2026-08-02/bottom_nav_redesign/"
    "iteration_3/agent_H/variant_H_transparent.png"
)
OUTPUT = ROOT / "Assets/Art/UI/MainMenuEmeraldVault"


def visible_crop(
    source: Image.Image,
    region: tuple[int, int, int, int],
    padding: int = 8,
) -> Image.Image:
    region_image = source.crop(region)
    bounds = region_image.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"No visible pixels in region {region}")

    left, top, right, bottom = bounds
    left = max(0, left - padding)
    top = max(0, top - padding)
    right = min(region_image.width, right + padding)
    bottom = min(region_image.height, bottom + padding)
    return region_image.crop((left, top, right, bottom))


def normalize(
    image: Image.Image,
    canvas: tuple[int, int],
    padding: int,
) -> Image.Image:
    usable = (canvas[0] - padding * 2, canvas[1] - padding * 2)
    image.thumbnail(usable, Image.Resampling.LANCZOS)
    result = Image.new("RGBA", canvas, (0, 0, 0, 0))
    offset = (
        (canvas[0] - image.width) // 2,
        (canvas[1] - image.height) // 2,
    )
    result.alpha_composite(image, offset)
    return result


def main() -> None:
    sheet = Image.open(SOURCE).convert("RGBA")
    OUTPUT.mkdir(parents=True, exist_ok=True)

    split_y = round(sheet.height * 0.49)
    play = visible_crop(sheet, (0, 0, sheet.width, split_y), padding=10)
    normalize(play, (1024, 320), padding=20).save(
        OUTPUT / "h_play_cta.png",
        optimize=True,
    )

    names = (
        "h_nav_levels.png",
        "h_nav_collection.png",
        "h_nav_shop.png",
        "h_nav_settings.png",
    )
    for index, name in enumerate(names):
        left = round(sheet.width * index / 4)
        right = round(sheet.width * (index + 1) / 4)
        medallion = visible_crop(
            sheet,
            (left, split_y, right, sheet.height),
            padding=10,
        )
        normalize(medallion, (512, 512), padding=20).save(
            OUTPUT / name,
            optimize=True,
        )


if __name__ == "__main__":
    main()
