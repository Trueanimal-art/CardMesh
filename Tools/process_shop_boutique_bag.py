#!/usr/bin/env python3
"""Normalize the approved Royal Boutique Bag source for the Main Menu glyph."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


CANVAS_SIZE = 512
TARGET_WIDTH = 320
TARGET_HEIGHT = 390
ALPHA_THRESHOLD = 8


def alpha_bounds(image: Image.Image) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > ALPHA_THRESHOLD else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise ValueError("The source image contains no visible pixels.")
    return bounds


def normalize(source_path: Path, output_path: Path) -> None:
    source = Image.open(source_path).convert("RGBA")
    subject = source.crop(alpha_bounds(source))
    subject = subject.resize(
        (TARGET_WIDTH, TARGET_HEIGHT),
        Image.Resampling.LANCZOS,
    )

    canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    x = (CANVAS_SIZE - TARGET_WIDTH) // 2
    y = (CANVAS_SIZE - TARGET_HEIGHT) // 2
    canvas.alpha_composite(subject, (x, y))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output_path, "PNG", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    normalize(args.input, args.output)


if __name__ == "__main__":
    main()
