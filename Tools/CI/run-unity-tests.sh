#!/usr/bin/env bash

set -u
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

usage() {
  cat <<'USAGE'
Usage: Tools/CI/run-unity-tests.sh --mode EditMode|PlayMode [options]

Options:
  --results PATH       NUnit XML output (default: artifacts/tmp/<mode>-results.xml)
  --log PATH           Unity log output (default: artifacts/tmp/<mode>-unity.log)
  --test-filter NAME   Optional Unity test filter
  --category NAMES     Optional comma-separated NUnit categories
  --assembly NAMES     Optional comma-separated assembly names
  --project PATH       Unity project root (default: repository root)
  --unity PATH         Unity executable or .app (default: UNITY_EDITOR or ProjectVersion lookup)
  -h, --help           Show this help
USAGE
}

absolute_path() {
  case "$1" in
    /*) printf '%s\n' "$1" ;;
    *) printf '%s/%s\n' "$REPO_ROOT" "$1" ;;
  esac
}

resolve_unity() {
  local project="$1"
  local candidate="${UNITY_EDITOR:-}"
  local version

  if [[ -n "$UNITY_PATH" ]]; then
    candidate="$UNITY_PATH"
  fi
  if [[ -z "$candidate" ]]; then
    version="$(awk '/^m_EditorVersion:/{print $2; exit}' "$project/ProjectSettings/ProjectVersion.txt")"
    case "$(uname -s)" in
      Darwin) candidate="/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity" ;;
      Linux) candidate="/opt/unity/Editor/Unity" ;;
      *)
        echo "Unable to locate Unity automatically; pass --unity or set UNITY_EDITOR." >&2
        return 1
        ;;
    esac
  fi
  if [[ -d "$candidate" && "$candidate" == *.app ]]; then
    candidate="$candidate/Contents/MacOS/Unity"
  fi
  if [[ ! -x "$candidate" ]]; then
    echo "Unity executable is not runnable: $candidate" >&2
    return 1
  fi
  printf '%s\n' "$candidate"
}

MODE=""
RESULTS=""
LOG_FILE=""
TEST_FILTER=""
ASSEMBLY_NAMES=""
CATEGORY_NAMES=""
PROJECT_ROOT="$REPO_ROOT"
UNITY_PATH=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --mode|--results|--log|--test-filter|--category|--assembly|--project|--unity)
      [[ $# -ge 2 ]] || { echo "Missing value for $1" >&2; exit 64; }
      case "$1" in
        --mode) MODE="$2" ;;
        --results) RESULTS="$2" ;;
        --log) LOG_FILE="$2" ;;
        --test-filter) TEST_FILTER="$2" ;;
        --category) CATEGORY_NAMES="$2" ;;
        --assembly) ASSEMBLY_NAMES="$2" ;;
        --project) PROJECT_ROOT="$2" ;;
        --unity) UNITY_PATH="$2" ;;
      esac
      shift 2
      ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 64 ;;
  esac
done

if [[ "$MODE" != "EditMode" && "$MODE" != "PlayMode" ]]; then
  echo "--mode must be EditMode or PlayMode." >&2
  exit 64
fi
if [[ ! -d "$PROJECT_ROOT/Assets" || ! -f "$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt" ]]; then
  echo "Invalid Unity project root: $PROJECT_ROOT" >&2
  exit 64
fi
PROJECT_ROOT="$(cd "$PROJECT_ROOT" && pwd)"

MODE_SLUG="$(printf '%s' "$MODE" | tr '[:upper:]' '[:lower:]')"
RESULTS="$(absolute_path "${RESULTS:-artifacts/tmp/$MODE_SLUG-results.xml}")"
LOG_FILE="$(absolute_path "${LOG_FILE:-artifacts/tmp/$MODE_SLUG-unity.log}")"
UNITY_PATH="$(resolve_unity "$PROJECT_ROOT")" || exit 66

mkdir -p "$(dirname "$RESULTS")" "$(dirname "$LOG_FILE")"
rm -f "$RESULTS"

COMMAND=(
  "$UNITY_PATH"
  -batchmode
  -nographics
  -projectPath "$PROJECT_ROOT"
  -runTests
  -testPlatform "$MODE"
  -testResults "$RESULTS"
  -logFile "$LOG_FILE"
)
if [[ -n "$TEST_FILTER" ]]; then
  COMMAND+=( -testFilter "$TEST_FILTER" )
fi
if [[ -n "$CATEGORY_NAMES" ]]; then
  COMMAND+=( -testCategory "$CATEGORY_NAMES" )
fi
if [[ -n "$ASSEMBLY_NAMES" ]]; then
  COMMAND+=( -assemblyNames "$ASSEMBLY_NAMES" )
fi

echo "UNITY_TEST_START mode=$MODE results=$RESULTS log=$LOG_FILE"
"${COMMAND[@]}"
UNITY_STATUS=$?
if [[ $UNITY_STATUS -ne 0 ]]; then
  echo "UNITY_TEST_PROCESS_FAILED mode=$MODE exit=$UNITY_STATUS log=$LOG_FILE" >&2
  exit "$UNITY_STATUS"
fi

"${PYTHON:-python3}" "$SCRIPT_DIR/check-unity-test-results.py" "$RESULTS"
