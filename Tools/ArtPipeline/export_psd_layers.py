#!/usr/bin/env python3
"""Export explicitly selected pixel layers from a PSD as transparent PNGs."""

from __future__ import annotations

import argparse
import math
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw
from psd_tools import PSDImage


def find_layer(psd: PSDImage, index_path: str) -> Any:
    current: Any = psd
    for segment in index_path.split("."):
        current = current[int(segment)]
    return current


def parse_layer(value: str) -> tuple[str, str]:
    if "=" not in value:
        raise argparse.ArgumentTypeError("layer must use INDEX_PATH=OUTPUT_NAME")
    index_path, output_name = value.split("=", 1)
    if not index_path or not output_name:
        raise argparse.ArgumentTypeError("layer must use INDEX_PATH=OUTPUT_NAME")
    return index_path, output_name


def make_contact_sheet(
    exports: list[tuple[str, str, Image.Image]], output_path: Path
) -> None:
    columns = 4
    cell_width = 360
    cell_height = 300
    rows = math.ceil(len(exports) / columns)
    sheet = Image.new("RGB", (columns * cell_width, rows * cell_height), "#20242c")
    draw = ImageDraw.Draw(sheet)

    for index, (index_path, layer_name, image) in enumerate(exports):
        column = index % columns
        row = index // columns
        origin_x = column * cell_width
        origin_y = row * cell_height
        preview = image.copy()
        preview.thumbnail((cell_width - 24, cell_height - 54), Image.Resampling.LANCZOS)
        preview_x = origin_x + (cell_width - preview.width) // 2
        preview_y = origin_y + 36 + (cell_height - 42 - preview.height) // 2
        checker = Image.new("RGBA", preview.size, "#596170")
        checker.alpha_composite(preview)
        sheet.paste(checker.convert("RGB"), (preview_x, preview_y))
        draw.text(
            (origin_x + 10, origin_y + 8),
            f"{index_path}  {layer_name}",
            fill="#ffffff",
        )

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path, format="PNG", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--layer", action="append", type=parse_layer, required=True)
    parser.add_argument("--contact-sheet", type=Path)
    args = parser.parse_args()

    psd = PSDImage.open(args.source.resolve())
    output_dir = args.output_dir.resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    exports: list[tuple[str, str, Image.Image]] = []

    for index_path, output_name in args.layer:
        layer = find_layer(psd, index_path)
        if layer.is_group() or layer.kind != "pixel":
            raise ValueError(
                f"{index_path} ({layer.name}) is {layer.kind}; only pixel layers are safe"
            )
        image = layer.topil()
        if image is None:
            raise ValueError(f"{index_path} ({layer.name}) has no raster pixels")
        image = image.convert("RGBA")
        output_path = output_dir / f"{output_name}.png"
        output_path.parent.mkdir(parents=True, exist_ok=True)
        image.save(output_path, format="PNG", optimize=True)
        exports.append((index_path, layer.name, image))

    if args.contact_sheet:
        make_contact_sheet(exports, args.contact_sheet.resolve())


if __name__ == "__main__":
    main()
