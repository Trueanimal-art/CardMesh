#!/usr/bin/env python3
"""Prepare the approved unified Variant H2 Unity sprites.

The generated source sheets stay in Design/ for traceability. This script only
performs deterministic crop, alpha-trim, resize, and padding operations.
"""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = (
    ROOT
    / "Design"
    / "MainMenuConcepts"
    / "2026-08-02"
    / "variant_h2_unified"
)
OUTPUT_DIR = ROOT / "Assets" / "Art" / "UI" / "MainMenuEmeraldVault"


def crop_to_canvas(
    source: Image.Image,
    crop_box: tuple[int, int, int, int],
    output_name: str,
    canvas_size: tuple[int, int],
    padding: int,
) -> None:
    sprite = source.crop(crop_box)
    alpha_bounds = sprite.getchannel("A").getbbox()
    if alpha_bounds is None:
        raise RuntimeError(f"{output_name}: crop contains no visible pixels")

    sprite = sprite.crop(alpha_bounds)
    max_width = canvas_size[0] - padding * 2
    max_height = canvas_size[1] - padding * 2
    scale = min(max_width / sprite.width, max_height / sprite.height)
    resized = sprite.resize(
        (round(sprite.width * scale), round(sprite.height * scale)),
        Image.Resampling.LANCZOS,
    )

    canvas = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    position = (
        (canvas_size[0] - resized.width) // 2,
        (canvas_size[1] - resized.height) // 2,
    )
    canvas.alpha_composite(resized, position)
    output_path = OUTPUT_DIR / output_name
    canvas.save(output_path, optimize=True)
    print(f"Wrote {output_path.relative_to(ROOT)} ({canvas.width}x{canvas.height})")


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    navigation = Image.open(SOURCE_DIR / "h2_navigation_alpha.png").convert("RGBA")
    top = Image.open(SOURCE_DIR / "h2_top_alpha.png").convert("RGBA")

    crop_to_canvas(
        navigation,
        (180, 300, 1510, 610),
        "h2_play_cta.png",
        (1024, 320),
        10,
    )
    crop_to_canvas(
        navigation,
        (130, 605, 455, 941),
        "h2_nav_levels.png",
        (512, 512),
        12,
    )
    crop_to_canvas(
        navigation,
        (475, 605, 810, 941),
        "h2_nav_collection.png",
        (512, 512),
        12,
    )
    crop_to_canvas(
        navigation,
        (840, 605, 1175, 941),
        "h2_nav_shop.png",
        (512, 512),
        12,
    )
    crop_to_canvas(
        navigation,
        (1195, 605, 1530, 941),
        "h2_nav_settings.png",
        (512, 512),
        12,
    )
    crop_to_canvas(
        top,
        (135, 0, 1520, 462),
        "h2_top_reward.png",
        (1024, 320),
        8,
    )
    crop_to_canvas(
        top,
        (135, 455, 1520, 931),
        "h2_top_chests.png",
        (1024, 320),
        8,
    )


if __name__ == "__main__":
    main()
