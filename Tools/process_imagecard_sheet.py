#!/usr/bin/env python3
from __future__ import annotations

import argparse
import math
import struct
import uuid
import zlib
from pathlib import Path
from typing import Iterable


PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


def read_png_rgba(path: Path) -> tuple[int, int, list[tuple[int, int, int, int]]]:
    data = path.read_bytes()
    if data[:8] != PNG_SIGNATURE:
        raise ValueError(f"{path} is not a PNG file")

    offset = 8
    width = height = bit_depth = color_type = None
    idat_parts: list[bytes] = []
    while offset < len(data):
        length = struct.unpack(">I", data[offset : offset + 4])[0]
        chunk_type = data[offset + 4 : offset + 8]
        chunk_data = data[offset + 8 : offset + 8 + length]
        offset += length + 12
        if chunk_type == b"IHDR":
            width, height, bit_depth, color_type, _, _, _ = struct.unpack(">IIBBBBB", chunk_data)
        elif chunk_type == b"IDAT":
            idat_parts.append(chunk_data)
        elif chunk_type == b"IEND":
            break

    if width is None or height is None or bit_depth != 8 or color_type not in (2, 6):
        raise ValueError(f"{path} must be an 8-bit RGB/RGBA PNG")

    channels = 4 if color_type == 6 else 3
    stride = width * channels
    raw = zlib.decompress(b"".join(idat_parts))
    previous = bytearray(stride)
    cursor = 0
    pixels: list[tuple[int, int, int, int]] = []
    for _ in range(height):
        filter_type = raw[cursor]
        cursor += 1
        row = bytearray(raw[cursor : cursor + stride])
        cursor += stride
        unfilter(row, previous, channels, filter_type)
        for x in range(width):
            index = x * channels
            red = row[index]
            green = row[index + 1]
            blue = row[index + 2]
            alpha = row[index + 3] if channels == 4 else 255
            pixels.append((red, green, blue, alpha))
        previous = row

    return width, height, pixels


def write_png_rgba(path: Path, width: int, height: int, pixels: list[tuple[int, int, int, int]]) -> None:
    raw_rows = bytearray()
    for y in range(height):
        raw_rows.append(0)
        for x in range(width):
            raw_rows.extend(pixels[(y * width) + x])

    def chunk(chunk_type: bytes, payload: bytes) -> bytes:
        crc = zlib.crc32(chunk_type + payload) & 0xFFFFFFFF
        return struct.pack(">I", len(payload)) + chunk_type + payload + struct.pack(">I", crc)

    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(
        PNG_SIGNATURE
        + chunk(b"IHDR", ihdr)
        + chunk(b"IDAT", zlib.compress(bytes(raw_rows), 9))
        + chunk(b"IEND", b"")
    )


def unfilter(row: bytearray, previous: bytearray, bpp: int, filter_type: int) -> None:
    for index in range(len(row)):
        left = row[index - bpp] if index >= bpp else 0
        up = previous[index]
        up_left = previous[index - bpp] if index >= bpp else 0
        if filter_type == 0:
            value = row[index]
        elif filter_type == 1:
            value = row[index] + left
        elif filter_type == 2:
            value = row[index] + up
        elif filter_type == 3:
            value = row[index] + ((left + up) // 2)
        elif filter_type == 4:
            value = row[index] + paeth(left, up, up_left)
        else:
            raise ValueError(f"Unsupported PNG filter type {filter_type}")
        row[index] = value & 0xFF


def paeth(left: int, up: int, up_left: int) -> int:
    estimate = left + up - up_left
    left_distance = abs(estimate - left)
    up_distance = abs(estimate - up)
    up_left_distance = abs(estimate - up_left)
    if left_distance <= up_distance and left_distance <= up_left_distance:
        return left
    if up_distance <= up_left_distance:
        return up
    return up_left


def crop_grid_cell(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    rows: int,
    columns: int,
    cell_index: int,
    gutter: int,
) -> tuple[int, int, list[tuple[int, int, int, int]]]:
    column = cell_index % columns
    row = cell_index // columns
    cell_w = width // columns
    cell_h = height // rows
    x0 = (column * cell_w) + gutter
    y0 = (row * cell_h) + gutter
    x1 = min(width, ((column + 1) * cell_w) - gutter)
    y1 = min(height, ((row + 1) * cell_h) - gutter)
    out_w = max(1, x1 - x0)
    out_h = max(1, y1 - y0)
    out = []
    for y in range(y0, y1):
        for x in range(x0, x1):
            out.append(pixels[(y * width) + x])
    return out_w, out_h, out


def remove_chroma_key(
    pixels: list[tuple[int, int, int, int]],
    key_color: tuple[int, int, int],
    tolerance: int,
) -> list[tuple[int, int, int, int]]:
    key_red, key_green, key_blue = key_color
    output = []
    for red, green, blue, alpha in pixels:
        key_distance = abs(red - key_red) + abs(green - key_green) + abs(blue - key_blue)
        if key_color == (0, 255, 0):
            is_key = key_distance <= tolerance or (green > 175 and red < 105 and blue < 120)
        elif key_color == (255, 0, 255):
            is_key = key_distance <= tolerance or (red > 175 and blue > 175 and green < 125)
        else:
            is_key = key_distance <= tolerance
        if is_key:
            output.append((0, 0, 0, 0))
        else:
            output.append((red, green, blue, alpha))
    return output


def parse_hex_color(value: str) -> tuple[int, int, int]:
    text = value.strip().lstrip("#")
    if len(text) != 6:
        raise ValueError(f"Invalid key color: {value}")
    return int(text[0:2], 16), int(text[2:4], 16), int(text[4:6], 16)


def trim_to_alpha(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    padding: int,
) -> tuple[int, int, list[tuple[int, int, int, int]]]:
    min_x = width
    min_y = height
    max_x = -1
    max_y = -1
    for y in range(height):
        for x in range(width):
            if pixels[(y * width) + x][3] > 8:
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)

    if max_x < min_x or max_y < min_y:
        return width, height, pixels

    min_x = max(0, min_x - padding)
    min_y = max(0, min_y - padding)
    max_x = min(width - 1, max_x + padding)
    max_y = min(height - 1, max_y + padding)
    out_w = max_x - min_x + 1
    out_h = max_y - min_y + 1
    out = []
    for y in range(min_y, max_y + 1):
        for x in range(min_x, max_x + 1):
            out.append(pixels[(y * width) + x])
    return out_w, out_h, out


def resize_fit_canvas(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    canvas_size: int,
    max_object_size: int,
) -> tuple[int, int, list[tuple[int, int, int, int]]]:
    scale = min(max_object_size / max(1, width), max_object_size / max(1, height), 1.0)
    scaled_w = max(1, round(width * scale))
    scaled_h = max(1, round(height * scale))
    scaled = resize_bilinear(width, height, pixels, scaled_w, scaled_h)
    canvas = [(0, 0, 0, 0) for _ in range(canvas_size * canvas_size)]
    x0 = (canvas_size - scaled_w) // 2
    y0 = (canvas_size - scaled_h) // 2
    for y in range(scaled_h):
        for x in range(scaled_w):
            canvas[((y0 + y) * canvas_size) + x0 + x] = scaled[(y * scaled_w) + x]
    return canvas_size, canvas_size, canvas


def resize_bilinear(
    width: int,
    height: int,
    pixels: list[tuple[int, int, int, int]],
    target_w: int,
    target_h: int,
) -> list[tuple[int, int, int, int]]:
    if width == target_w and height == target_h:
        return list(pixels)

    output: list[tuple[int, int, int, int]] = []
    for y in range(target_h):
        source_y = (y + 0.5) * height / target_h - 0.5
        y0 = max(0, math.floor(source_y))
        y1 = min(height - 1, y0 + 1)
        fy = source_y - y0
        for x in range(target_w):
            source_x = (x + 0.5) * width / target_w - 0.5
            x0 = max(0, math.floor(source_x))
            x1 = min(width - 1, x0 + 1)
            fx = source_x - x0
            c00 = pixels[(y0 * width) + x0]
            c10 = pixels[(y0 * width) + x1]
            c01 = pixels[(y1 * width) + x0]
            c11 = pixels[(y1 * width) + x1]
            output.append(tuple(round(lerp(lerp(c00[i], c10[i], fx), lerp(c01[i], c11[i], fx), fy)) for i in range(4)))
    return output


def lerp(left: float, right: float, amount: float) -> float:
    return left + ((right - left) * amount)


def write_unity_sprite_meta(path: Path) -> None:
    meta_path = path.with_name(path.name + ".meta")
    if meta_path.exists():
        return

    guid = uuid.uuid4().hex
    sprite_id = uuid.uuid4().hex
    meta_path.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "TextureImporter:\n"
        "  internalIDToNameTable: []\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 13\n"
        "  mipmaps:\n"
        "    mipMapMode: 0\n"
        "    enableMipMap: 0\n"
        "    sRGBTexture: 1\n"
        "    linearTexture: 0\n"
        "    fadeOut: 0\n"
        "    borderMipMap: 0\n"
        "    mipMapsPreserveCoverage: 0\n"
        "    alphaTestReferenceValue: 0.5\n"
        "    mipMapFadeDistanceStart: 1\n"
        "    mipMapFadeDistanceEnd: 3\n"
        "  bumpmap:\n"
        "    convertToNormalMap: 0\n"
        "    externalNormalMap: 0\n"
        "    heightScale: 0.25\n"
        "    normalMapFilter: 0\n"
        "    flipGreenChannel: 0\n"
        "  isReadable: 0\n"
        "  streamingMipmaps: 0\n"
        "  textureFormat: 1\n"
        "  maxTextureSize: 1024\n"
        "  textureSettings:\n"
        "    serializedVersion: 2\n"
        "    filterMode: 1\n"
        "    aniso: 1\n"
        "    mipBias: 0\n"
        "    wrapU: 1\n"
        "    wrapV: 1\n"
        "    wrapW: 1\n"
        "  nPOTScale: 0\n"
        "  lightmap: 0\n"
        "  compressionQuality: 100\n"
        "  spriteMode: 1\n"
        "  spriteExtrude: 1\n"
        "  spriteMeshType: 0\n"
        "  alignment: 0\n"
        "  spritePivot: {x: 0.5, y: 0.5}\n"
        "  spritePixelsToUnits: 100\n"
        "  spriteBorder: {x: 0, y: 0, z: 0, w: 0}\n"
        "  spriteGenerateFallbackPhysicsShape: 0\n"
        "  alphaUsage: 1\n"
        "  alphaIsTransparency: 1\n"
        "  spriteTessellationDetail: -1\n"
        "  textureType: 8\n"
        "  textureShape: 1\n"
        "  singleChannelComponent: 0\n"
        "  flipbookRows: 1\n"
        "  flipbookColumns: 1\n"
        "  maxTextureSizeSet: 0\n"
        "  compressionQualitySet: 0\n"
        "  textureFormatSet: 0\n"
        "  ignorePngGamma: 0\n"
        "  applyGammaDecoding: 0\n"
        "  swizzle: 50462976\n"
        "  platformSettings:\n"
        "  - serializedVersion: 3\n"
        "    buildTarget: DefaultTexturePlatform\n"
        "    maxTextureSize: 1024\n"
        "    resizeAlgorithm: 0\n"
        "    textureFormat: -1\n"
        "    textureCompression: 0\n"
        "    compressionQuality: 100\n"
        "    crunchedCompression: 0\n"
        "    allowsAlphaSplitting: 0\n"
        "    overridden: 0\n"
        "  spriteSheet:\n"
        "    serializedVersion: 2\n"
        "    sprites: []\n"
        "    outline: []\n"
        "    physicsShape: []\n"
        "    bones: []\n"
        f"    spriteID: {sprite_id}\n"
        "    internalID: 0\n"
        "    vertices: []\n"
        "    indices: \n"
        "    edges: []\n"
        "    weights: []\n"
        "    secondaryTextures: []\n"
        "    nameFileIdTable: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def process_sheet(args: argparse.Namespace) -> int:
    source = Path(args.source)
    names = args.names
    expected = args.rows * args.columns
    if len(names) != expected:
        raise SystemExit(f"Expected {expected} names, got {len(names)}")

    width, height, pixels = read_png_rgba(source)
    out_dir = Path(args.out_dir)
    for index, name in enumerate(names):
        cell_w, cell_h, cell_pixels = crop_grid_cell(width, height, pixels, args.rows, args.columns, index, args.gutter)
        alpha_pixels = remove_chroma_key(cell_pixels, parse_hex_color(args.key_color), args.tolerance)
        trim_w, trim_h, trim_pixels = trim_to_alpha(cell_w, cell_h, alpha_pixels, args.padding)
        final_w, final_h, final_pixels = resize_fit_canvas(trim_w, trim_h, trim_pixels, args.canvas_size, args.max_object_size)
        out_path = out_dir / f"{name}.png"
        write_png_rgba(out_path, final_w, final_h, final_pixels)
        write_unity_sprite_meta(out_path)
        print(f"wrote {out_path} {final_w}x{final_h}")

    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Convert a chroma-key ImageCard sheet into project PNG sprites.")
    parser.add_argument("--source", required=True)
    parser.add_argument("--out-dir", default="Assets/Resources/ImageCards/Premium")
    parser.add_argument("--rows", type=int, default=2)
    parser.add_argument("--columns", type=int, default=2)
    parser.add_argument("--names", nargs="+", required=True)
    parser.add_argument("--gutter", type=int, default=28)
    parser.add_argument("--padding", type=int, default=28)
    parser.add_argument("--tolerance", type=int, default=74)
    parser.add_argument("--key-color", default="#00ff00")
    parser.add_argument("--canvas-size", type=int, default=240)
    parser.add_argument("--max-object-size", type=int, default=214)
    parser.set_defaults(func=process_sheet)
    return parser


def main(argv: Iterable[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
