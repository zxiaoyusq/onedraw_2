#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PYTHON_BIN="${PYTHON:-python3}"
TEMP_ROOT="$(mktemp -d)"
trap 'rm -rf "$TEMP_ROOT"' EXIT

"$PYTHON_BIN" "$SCRIPT_DIR/check-unity-test-results.py" "$SCRIPT_DIR/Tests/fixtures/passed.xml"
if "$PYTHON_BIN" "$SCRIPT_DIR/check-unity-test-results.py" "$SCRIPT_DIR/Tests/fixtures/failed.xml"; then
  echo "Failed NUnit fixture unexpectedly returned zero." >&2
  exit 1
fi

if "$SCRIPT_DIR/run-unity-tests.sh" --mode Invalid; then
  echo "Invalid test mode unexpectedly returned zero." >&2
  exit 1
fi
if "$SCRIPT_DIR/run-unity-tests.sh" --results; then
  echo "Missing test argument value unexpectedly returned zero." >&2
  exit 1
fi
if "$SCRIPT_DIR/build-web.sh" --unknown; then
  echo "Unknown Web build argument unexpectedly returned zero." >&2
  exit 1
fi
if "$SCRIPT_DIR/build-web.sh" --output; then
  echo "Missing build argument value unexpectedly returned zero." >&2
  exit 1
fi

"$SCRIPT_DIR/new-task-evidence.sh" T999 --output-root "$TEMP_ROOT"
test -s "$TEMP_ROOT/T999/verification.md"
test -s "$TEMP_ROOT/T999/change-whitelist.md"
test -s "$TEMP_ROOT/T999/baseline-status.txt"
test -s "$TEMP_ROOT/T999/baseline-commit.txt"
if "$SCRIPT_DIR/new-task-evidence.sh" T999 --output-root "$TEMP_ROOT"; then
  echo "Evidence overwrite guard unexpectedly returned zero." >&2
  exit 1
fi

echo "HARNESS_SMOKE_PASS"
