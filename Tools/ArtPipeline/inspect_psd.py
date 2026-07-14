#!/usr/bin/env python3
"""Inspect a PSD and write a deterministic JSON layer manifest.

This tool intentionally does not copy the source PSD into the Unity project. It is
used during authoring to decide which layers are safe to export as runtime PNGs.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any

from psd_tools import PSDImage


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def layer_record(layer: Any, index_path: tuple[int, ...]) -> dict[str, Any]:
    left, top, right, bottom = layer.bbox
    record: dict[str, Any] = {
        "indexPath": ".".join(str(index) for index in index_path),
        "name": layer.name,
        "kind": layer.kind,
        "visible": layer.is_visible(),
        "opacity": layer.opacity,
        "blendMode": str(layer.blend_mode),
        "bbox": [left, top, right, bottom],
        "width": right - left,
        "height": bottom - top,
        "isGroup": layer.is_group(),
    }
    if layer.is_group():
        record["children"] = [
            layer_record(child, (*index_path, child_index))
            for child_index, child in enumerate(layer)
        ]
    return record


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("--manifest", required=True, type=Path)
    args = parser.parse_args()

    source = args.source.resolve()
    manifest = args.manifest.resolve()
    psd = PSDImage.open(source)
    output = {
        "sourceFile": source.name,
        "sourceSha256": sha256(source),
        "canvas": {"width": psd.width, "height": psd.height},
        "colorMode": str(psd.color_mode),
        "depth": psd.depth,
        "layers": [
            layer_record(layer, (index,)) for index, layer in enumerate(psd)
        ],
    }

    manifest.parent.mkdir(parents=True, exist_ok=True)
    manifest.write_text(
        json.dumps(output, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
