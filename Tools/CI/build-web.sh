#!/usr/bin/env bash

set -u
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

usage() {
  cat <<'USAGE'
Usage: Tools/CI/build-web.sh [options]

Options:
  --output PATH      WebGL output directory (default: Builds/WebGL)
  --log PATH         Unity log output (default: artifacts/tmp/web-build-unity.log)
  --development      Enable Development build option
  --smoke            Include the T100 Web runtime smoke probe
  --project PATH     Unity project root (default: repository root)
  --unity PATH       Unity executable or .app (default: UNITY_EDITOR or ProjectVersion lookup)
  -h, --help         Show this help

This is the standard Web build entry. Running it performs a build; T040 only validates the entry.
USAGE
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
      *) echo "Unable to locate Unity; pass --unity or set UNITY_EDITOR." >&2; return 1 ;;
    esac
  fi
  if [[ -d "$candidate" && "$candidate" == *.app ]]; then
    candidate="$candidate/Contents/MacOS/Unity"
  fi
  [[ -x "$candidate" ]] || { echo "Unity executable is not runnable: $candidate" >&2; return 1; }
  printf '%s\n' "$candidate"
}

OUTPUT="Builds/WebGL"
LOG_FILE="artifacts/tmp/web-build-unity.log"
PROJECT_ROOT="$REPO_ROOT"
UNITY_PATH=""
DEVELOPMENT=0
SMOKE=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --output|--log|--project|--unity)
      [[ $# -ge 2 ]] || { echo "Missing value for $1" >&2; exit 64; }
      case "$1" in
        --output) OUTPUT="$2" ;;
        --log) LOG_FILE="$2" ;;
        --project) PROJECT_ROOT="$2" ;;
        --unity) UNITY_PATH="$2" ;;
      esac
      shift 2
      ;;
    --development) DEVELOPMENT=1; shift ;;
    --smoke) SMOKE=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 64 ;;
  esac
done

if [[ -z "$OUTPUT" || ! -d "$PROJECT_ROOT/Assets" ]]; then
  echo "A valid --output and Unity project are required." >&2
  exit 64
fi
PROJECT_ROOT="$(cd "$PROJECT_ROOT" && pwd)"
case "$LOG_FILE" in
  /*) ;;
  *) LOG_FILE="$REPO_ROOT/$LOG_FILE" ;;
esac
mkdir -p "$(dirname "$LOG_FILE")"
UNITY_PATH="$(resolve_unity "$PROJECT_ROOT")" || exit 66

COMMAND=(
  "$UNITY_PATH"
  -batchmode
  -nographics
  -quit
  -projectPath "$PROJECT_ROOT"
  -executeMethod OneStrokeDemon.Editor.Build.WebBuildEntry.BuildFromCommandLine
  -buildOutput "$OUTPUT"
  -logFile "$LOG_FILE"
)
if [[ $DEVELOPMENT -eq 1 ]]; then
  COMMAND+=( -developmentBuild )
fi
if [[ $SMOKE -eq 1 ]]; then
  COMMAND+=( -webSmoke )
fi

echo "UNITY_WEB_BUILD_START output=$OUTPUT log=$LOG_FILE development=$DEVELOPMENT smoke=$SMOKE"
"${COMMAND[@]}"
STATUS=$?
if [[ $STATUS -ne 0 ]]; then
  echo "UNITY_WEB_BUILD_FAILED exit=$STATUS log=$LOG_FILE" >&2
  exit "$STATUS"
fi

case "$OUTPUT" in
  /*) OUTPUT_ABSOLUTE="$OUTPUT" ;;
  *) OUTPUT_ABSOLUTE="$PROJECT_ROOT/$OUTPUT" ;;
esac
if [[ ! -f "$OUTPUT_ABSOLUTE/index.html" ]]; then
  echo "UNITY_WEB_BUILD_OUTPUT_MISSING $OUTPUT_ABSOLUTE/index.html" >&2
  exit 1
fi
echo "UNITY_WEB_BUILD_PASS output=$OUTPUT_ABSOLUTE"
