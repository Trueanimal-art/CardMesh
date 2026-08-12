#!/usr/bin/env python3
"""Trim and normalize generated Emerald Vault UI assets for Unity."""

from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Art/UI/MainMenuEmeraldVault"
ATLAS = Path("/tmp/cardmesh_emerald_icon_atlas_alpha.png")


def alpha_bounds(image: Image.Image, padding: int = 0) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError("Image has no visible alpha content")
    left, top, right, bottom = bounds
    return (
        max(0, left - padding),
        max(0, top - padding),
        min(image.width, right + padding),
        min(image.height, bottom + padding),
    )


def normalize(source: Path, target: Path, canvas: tuple[int, int], padding: int) -> None:
    image = Image.open(source).convert("RGBA")
    image = image.crop(alpha_bounds(image, padding=4))
    usable = (canvas[0] - 2 * padding, canvas[1] - 2 * padding)
    image.thumbnail(usable, Image.Resampling.LANCZOS)
    result = Image.new("RGBA", canvas, (0, 0, 0, 0))
    offset = ((canvas[0] - image.width) // 2, (canvas[1] - image.height) // 2)
    result.alpha_composite(image, offset)
    result.save(target, optimize=True)


def slice_icon_atlas() -> None:
    atlas = Image.open(ATLAS).convert("RGBA")
    names = (
        "icon_reward.png",
        "icon_chests.png",
        "icon_levels.png",
        "icon_collection.png",
        "icon_shop.png",
        "icon_settings.png",
        "icon_play.png",
        "badge_diamond.png",
    )

    cell_width = atlas.width / 4
    cell_height = atlas.height / 2
    gutter_inset = 12

    for index, name in enumerate(names):
        column = index % 4
        row = index // 4
        left = round(column * cell_width) + gutter_inset
        top = round(row * cell_height) + gutter_inset
        right = round((column + 1) * cell_width) - gutter_inset
        bottom = round((row + 1) * cell_height) - gutter_inset
        cell = atlas.crop((left, top, right, bottom))
        cell = cell.crop(alpha_bounds(cell, padding=4))
        cell.thumbnail((432, 432), Image.Resampling.LANCZOS)
        result = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        offset = ((512 - cell.width) // 2, (512 - cell.height) // 2)
        result.alpha_composite(cell, offset)
        result.save(OUT / name, optimize=True)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    normalize(OUT / "top_activity_frame.png", OUT / "top_activity_frame.png", (1024, 384), 8)
    normalize(OUT / "bottom_rail.png", OUT / "bottom_rail.png", (2048, 384), 8)
    normalize(OUT / "play_crest.png", OUT / "play_crest.png", (1024, 1024), 12)
    slice_icon_atlas()


if __name__ == "__main__":
    main()
