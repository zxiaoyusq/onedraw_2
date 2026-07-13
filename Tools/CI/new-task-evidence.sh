#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

usage() {
  cat <<'USAGE'
Usage: Tools/CI/new-task-evidence.sh TASK-ID [--output-root PATH]

Creates verification.md, change-whitelist.md and immutable Git baseline records.
Existing task evidence is never overwritten.
USAGE
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi
[[ $# -gt 0 ]] || { usage >&2; exit 64; }
TASK_ID="$1"
shift
OUTPUT_ROOT="$REPO_ROOT/artifacts/evals"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --output-root)
      [[ $# -ge 2 ]] || { echo "Missing value for $1" >&2; exit 64; }
      OUTPUT_ROOT="$2"
      shift 2
      ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 64 ;;
  esac
done

if [[ ! "$TASK_ID" =~ ^T[0-9]{3}$ ]]; then
  echo "TASK-ID must match T followed by three digits." >&2
  exit 64
fi
case "$OUTPUT_ROOT" in
  /*) ;;
  *) OUTPUT_ROOT="$REPO_ROOT/$OUTPUT_ROOT" ;;
esac
TASK_DIR="$OUTPUT_ROOT/$TASK_ID"
if [[ -e "$TASK_DIR" ]]; then
  echo "Refusing to overwrite existing evidence: $TASK_DIR" >&2
  exit 73
fi

BASELINE_STATUS="$(git -C "$REPO_ROOT" status --short --branch)"
BASELINE_COMMIT="$(git -C "$REPO_ROOT" rev-parse HEAD)"
mkdir -p "$TASK_DIR"
sed "s/TASK-ID/$TASK_ID/g" "$REPO_ROOT/templates/verification.md" > "$TASK_DIR/verification.md"
sed "s/TASK-ID/$TASK_ID/g" "$REPO_ROOT/templates/change-whitelist.md" > "$TASK_DIR/change-whitelist.md"
printf '%s\n' "$BASELINE_STATUS" > "$TASK_DIR/baseline-status.txt"
printf '%s\n' "$BASELINE_COMMIT" > "$TASK_DIR/baseline-commit.txt"
echo "TASK_EVIDENCE_CREATED task=$TASK_ID path=$TASK_DIR baseline=$BASELINE_COMMIT"
