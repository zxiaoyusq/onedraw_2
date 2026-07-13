#!/usr/bin/env bash

set -u
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

usage() {
  cat <<'USAGE'
Usage: Tools/CI/build-wechat.sh [options]

Options:
  --output PATH      WXSDK output directory (default: Builds/WeChat/T120)
  --log PATH         Unity log output (default: artifacts/tmp/T120-wechat-unity.log)
  --development      Enable Development build option
  --project PATH     Unity project root (default: repository root)
  --unity PATH       Unity executable or .app (default: UNITY_EDITOR or ProjectVersion lookup)
  -h, --help         Show this help

The build uses an empty AppID and restores SDK metadata and ProjectSettings after Unity exits.
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

OUTPUT="Builds/WeChat/T120"
LOG_FILE="artifacts/tmp/T120-wechat-unity.log"
PROJECT_ROOT="$REPO_ROOT"
UNITY_PATH=""
DEVELOPMENT=0

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
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 64 ;;
  esac
done

if [[ -z "$OUTPUT" || ! -d "$PROJECT_ROOT/Assets" ]]; then
  echo "A valid --output and Unity project are required." >&2
  exit 64
fi
PROJECT_ROOT="$(cd "$PROJECT_ROOT" && pwd)"
if [[ -e "$PROJECT_ROOT/Library/UnityLockfile" ]]; then
  echo "Unity Editor is open for this project; close it before a batch conversion." >&2
  exit 73
fi
case "$LOG_FILE" in
  /*) ;;
  *) LOG_FILE="$REPO_ROOT/$LOG_FILE" ;;
esac
mkdir -p "$(dirname "$LOG_FILE")" "$REPO_ROOT/artifacts/tmp"
UNITY_PATH="$(resolve_unity "$PROJECT_ROOT")" || exit 66

BACKUP_DIR="$(mktemp -d "$REPO_ROOT/artifacts/tmp/T120-settings-backup.XXXXXX")"
BACKUP_ARCHIVE="$BACKUP_DIR/settings.tar"
GENERATED_ASSET_ROOTS=(
  "Assets/Resources"
  "Assets/Resources.meta"
  "Assets/WX-WASM-SDK-V2"
  "Assets/WX-WASM-SDK-V2.meta"
  "Assets/WebGLTemplates"
  "Assets/WebGLTemplates.meta"
  "Assets/_Recovery"
  "Assets/_Recovery.meta"
  "TextToolDatas"
)
(
  cd "$PROJECT_ROOT" || exit 1
  PRESERVE_PATHS=(
    "ProjectSettings"
    "Packages/com.qq.weixin.minigame/Editor/MiniGameConfig.asset"
    "Assets/Settings/UniversalRP.asset"
    "Assets/UniversalRenderPipelineGlobalSettings.asset"
  )
  META_FILES=()
  while IFS= read -r file; do
    META_FILES+=("$file")
  done < <(find Packages/com.qq.weixin.minigame -name '*.meta' -type f -print | sort)
  PRESERVE_PATHS+=("${META_FILES[@]}")
  for path in "${GENERATED_ASSET_ROOTS[@]}"; do
    if [[ -e "$path" ]]; then
      PRESERVE_PATHS+=("$path")
    fi
  done
  tar -cf "$BACKUP_ARCHIVE" "${PRESERVE_PATHS[@]}"
) || exit 74

restore_settings() {
  if [[ -f "$BACKUP_ARCHIVE" ]]; then
    for path in "${GENERATED_ASSET_ROOTS[@]}"; do
      rm -rf "$PROJECT_ROOT/$path"
    done
    if ! tar -xf "$BACKUP_ARCHIVE" -C "$PROJECT_ROOT"; then
      echo "Failed to restore protected Unity settings; backup retained at $BACKUP_ARCHIVE" >&2
      return 1
    fi
  fi
  rm -rf "$BACKUP_DIR"
}
trap restore_settings EXIT INT TERM

COMMAND=(
  "$UNITY_PATH"
  -batchmode
  -nographics
  -quit
  -projectPath "$PROJECT_ROOT"
  -executeMethod OneStrokeDemon.Editor.Build.WechatBuildEntry.BuildFromCommandLine
  -buildOutput "$OUTPUT"
  -logFile "$LOG_FILE"
)
if [[ $DEVELOPMENT -eq 1 ]]; then
  COMMAND+=( -developmentBuild )
fi

echo "UNITY_WECHAT_BUILD_START output=$OUTPUT log=$LOG_FILE development=$DEVELOPMENT appid=EMPTY"
"${COMMAND[@]}"
STATUS=$?
RESTORE_STATUS=0
restore_settings || RESTORE_STATUS=$?
trap - EXIT INT TERM
if [[ $RESTORE_STATUS -ne 0 ]]; then
  echo "UNITY_WECHAT_BUILD_RESTORE_FAILED exit=$RESTORE_STATUS backup=$BACKUP_ARCHIVE" >&2
  exit 74
fi
if [[ $STATUS -ne 0 ]]; then
  echo "UNITY_WECHAT_BUILD_FAILED exit=$STATUS log=$LOG_FILE" >&2
  exit "$STATUS"
fi

case "$OUTPUT" in
  /*) OUTPUT_ABSOLUTE="$OUTPUT" ;;
  *) OUTPUT_ABSOLUTE="$PROJECT_ROOT/$OUTPUT" ;;
esac
for required in minigame/game.js minigame/game.json minigame/project.config.json minigame/unity-namespace.js; do
  if [[ ! -s "$OUTPUT_ABSOLUTE/$required" ]]; then
    echo "UNITY_WECHAT_BUILD_OUTPUT_MISSING $OUTPUT_ABSOLUTE/$required" >&2
    exit 1
  fi
done
echo "UNITY_WECHAT_BUILD_PASS output=$OUTPUT_ABSOLUTE appid=EMPTY"
