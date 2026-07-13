#!/usr/bin/env python3
"""Validate a Unity Test Framework NUnit XML result and propagate failure."""

from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_count(root: ET.Element, name: str) -> int:
    value = root.get(name)
    if value is None:
        raise ValueError(f"missing '{name}' attribute on <test-run>")
    return int(value)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Return zero only when a non-empty Unity NUnit test run passed."
    )
    parser.add_argument("results", type=Path, help="Unity -testResults XML path")
    args = parser.parse_args()

    try:
        root = ET.parse(args.results).getroot()
        if root.tag != "test-run":
            raise ValueError(f"expected <test-run>, got <{root.tag}>")

        total = parse_count(root, "total")
        passed = parse_count(root, "passed")
        failed = parse_count(root, "failed")
        skipped = parse_count(root, "skipped")
        result = root.get("result", "Unknown")
    except (OSError, ET.ParseError, ValueError) as error:
        print(f"TEST_RESULTS_INVALID path={args.results} error={error}", file=sys.stderr)
        return 2

    print(
        f"TEST_RESULTS result={result} total={total} passed={passed} "
        f"failed={failed} skipped={skipped} path={args.results}"
    )
    if result != "Passed" or total <= 0 or failed != 0 or passed + skipped != total:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
