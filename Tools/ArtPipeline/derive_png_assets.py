#!/usr/bin/env python3
"""Create deterministic PNG crops/copies used by the T630 prototype set."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from PIL import Image


def parse_box(value: Any) -> tuple[int, int, int, int]:
    if not isinstance(value, list) or len(value) != 4:
        raise ValueError("crop must be [left, top, right, bottom]")
    box = tuple(int(item) for item in value)
    if box[2] <= box[0] or box[3] <= box[1]:
        raise ValueError(f"invalid crop box: {box}")
    return box


def trim_alpha(image: Image.Image, padding: int) -> Image.Image:
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("source contains no visible pixels")
    left = max(0, bounds[0] - padding)
    top = max(0, bounds[1] - padding)
    right = min(image.width, bounds[2] + padding)
    bottom = min(image.height, bounds[3] + padding)
    return image.crop((left, top, right, bottom))


def fit_within(image: Image.Image, size: list[int]) -> Image.Image:
    if len(size) != 2 or int(size[0]) <= 0 or int(size[1]) <= 0:
        raise ValueError("fitWithin must be [positiveWidth, positiveHeight]")
    result = image.copy()
    result.thumbnail((int(size[0]), int(size[1])), Image.Resampling.LANCZOS)
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("spec", type=Path)
    args = parser.parse_args()

    spec_path = args.spec.resolve()
    root = spec_path.parent
    document = json.loads(spec_path.read_text(encoding="utf-8"))
    jobs = document.get("jobs")
    if not isinstance(jobs, list) or not jobs:
        raise ValueError("spec must contain a non-empty jobs array")

    for job in jobs:
        source = (root / job["source"]).resolve()
        output = (root / job["output"]).resolve()
        image = Image.open(source).convert("RGBA")
        if "crop" in job:
            image = image.crop(parse_box(job["crop"]))
        if job.get("trimAlpha", False):
            image = trim_alpha(image, int(job.get("padding", 0)))
        if "fitWithin" in job:
            image = fit_within(image, job["fitWithin"])
        output.parent.mkdir(parents=True, exist_ok=True)
        image.save(output, format="PNG", optimize=True)


if __name__ == "__main__":
    main()
