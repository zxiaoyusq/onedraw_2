#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DOTNET_PATH="${DOTNET:-}"
UNITY_PATH=""
UPDATE=0
SKIP_UNITY=0
RESULTS_ROOT="$REPO_ROOT/artifacts/tmp/T250"

usage() {
  cat <<'USAGE'
Usage: Tools/CI/verify-config.sh [options]

Validates the workbook, runs ConfigExporter tests, rejects byte drift in the
managed JSON/hash/ConfigIds artifacts, and runs ConfigPipeline Unity tests.

Options:
  --update              Regenerate tracked artifacts before verification
  --skip-unity          Explicit partial run; never reports CONFIG_PIPELINE_PASS
  --dotnet PATH         dotnet executable (default: DOTNET, PATH, or Unity SDK)
  --unity PATH          Unity executable or .app forwarded to Unity test scripts
  --results-root PATH   Unity XML/log root (default: artifacts/tmp/T250)
  -h, --help            Show this help
USAGE
}

absolute_path() {
  case "$1" in
    /*) printf '%s\n' "$1" ;;
    *) printf '%s/%s\n' "$REPO_ROOT" "$1" ;;
  esac
}

resolve_dotnet() {
  local candidate="$DOTNET_PATH"
  local version
  if [[ -z "$candidate" ]] && command -v dotnet >/dev/null 2>&1; then
    candidate="$(command -v dotnet)"
  fi
  if [[ -z "$candidate" ]]; then
    version="$(awk '/^m_EditorVersion:/{print $2; exit}' "$REPO_ROOT/ProjectSettings/ProjectVersion.txt")"
    case "$(uname -s)" in
      Darwin)
        candidate="/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
        ;;
      Linux)
        candidate="/opt/unity/Editor/Data/Resources/Scripting/DotNetSdk/dotnet"
        ;;
    esac
  fi
  if [[ -z "$candidate" || ! -x "$candidate" ]]; then
    echo "Unable to locate a runnable .NET 8 SDK; pass --dotnet or set DOTNET." >&2
    return 1
  fi
  printf '%s\n' "$candidate"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --update) UPDATE=1; shift ;;
    --skip-unity) SKIP_UNITY=1; shift ;;
    --dotnet|--unity|--results-root)
      [[ $# -ge 2 ]] || { echo "Missing value for $1" >&2; exit 64; }
      case "$1" in
        --dotnet) DOTNET_PATH="$2" ;;
        --unity) UNITY_PATH="$2" ;;
        --results-root) RESULTS_ROOT="$(absolute_path "$2")" ;;
      esac
      shift 2
      ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 64 ;;
  esac
done

DOTNET_PATH="$(resolve_dotnet)" || exit 66
RESULTS_ROOT="$(absolute_path "$RESULTS_ROOT")"
mkdir -p "$RESULTS_ROOT"
TEMP_ROOT="$(mktemp -d "$RESULTS_ROOT/generated.XXXXXX")"
trap 'rm -rf "$TEMP_ROOT"' EXIT

WORKBOOK="$REPO_ROOT/Design/Config/GameConfig.xlsx"
SCHEMA="$REPO_ROOT/config/schema/gameplay.schema.json"
JSON_OUTPUT="$REPO_ROOT/Assets/_Game/Config/Generated/gameplay_config.json"
HASH_OUTPUT="$REPO_ROOT/Assets/_Game/Config/Generated/gameplay_config.hash"
IDS_OUTPUT="$REPO_ROOT/Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs"
TEST_PROJECT="$REPO_ROOT/Tools/ConfigExporter/Tests/ConfigExporter.Tests.csproj"
EXPORTER_PROJECT="$REPO_ROOT/Tools/ConfigExporter/ConfigExporter.csproj"
EXPORTER_DLL="$REPO_ROOT/Tools/ConfigExporter/bin/Debug/net8.0/OneStrokeDemon.ConfigExporter.dll"

echo "CONFIG_PIPELINE_START update=$UPDATE skipUnity=$SKIP_UNITY"
"$DOTNET_PATH" build "$EXPORTER_PROJECT" --nologo -p:UseAppHost=false

generate() {
  "$DOTNET_PATH" "$EXPORTER_DLL" generate \
    --input "$WORKBOOK" \
    --output "$1" \
    --hash-output "$2" \
    --ids-output "$3" \
    --schema "$SCHEMA" \
    --strict
}

if [[ $UPDATE -eq 1 ]]; then
  generate "$JSON_OUTPUT" "$HASH_OUTPUT" "$IDS_OUTPUT"
fi

TEMP_JSON="$TEMP_ROOT/gameplay_config.json"
TEMP_HASH="$TEMP_ROOT/gameplay_config.hash"
TEMP_IDS="$TEMP_ROOT/ConfigIds.g.cs"
generate "$TEMP_JSON" "$TEMP_HASH" "$TEMP_IDS"

set +e
"$DOTNET_PATH" "$EXPORTER_DLL" verify \
  --input "$WORKBOOK" \
  --output "$JSON_OUTPUT" \
  --hash-output "$HASH_OUTPUT" \
  --ids-output "$IDS_OUTPUT" \
  --schema "$SCHEMA" \
  --strict
VERIFY_STATUS=$?
set -e
if [[ $VERIFY_STATUS -ne 0 ]]; then
  for pair in \
    "$JSON_OUTPUT|$TEMP_JSON" \
    "$HASH_OUTPUT|$TEMP_HASH" \
    "$IDS_OUTPUT|$TEMP_IDS"
  do
    tracked="${pair%%|*}"
    expected="${pair#*|}"
    if [[ ! -f "$tracked" ]] || ! cmp -s "$tracked" "$expected"; then
      echo "CONFIG_GENERATED_DIFF artifact=$tracked" >&2
      diff -u --label "tracked:$tracked" --label "expected:$expected" "$tracked" "$expected" || true
    fi
  done
  exit "$VERIFY_STATUS"
fi

cmp "$JSON_OUTPUT" "$TEMP_JSON"
cmp "$HASH_OUTPUT" "$TEMP_HASH"
cmp "$IDS_OUTPUT" "$TEMP_IDS"
echo "CONFIG_GENERATED_DIFF_PASS artifacts=3"
"$DOTNET_PATH" test "$TEST_PROJECT" --nologo -p:UseAppHost=false

if [[ $SKIP_UNITY -eq 1 ]]; then
  echo "CONFIG_PIPELINE_PARTIAL_PASS unity=NOT_RUN reason=explicit_skip"
  exit 0
fi

UNITY_ARGUMENTS=(--project "$REPO_ROOT")
if [[ -n "$UNITY_PATH" ]]; then
  UNITY_ARGUMENTS=(--unity "$UNITY_PATH")
fi
"$SCRIPT_DIR/run-unity-tests.sh" \
  --mode EditMode \
  --category ConfigPipeline \
  --assembly OneStrokeDemon.Tests.EditMode \
  --results "$RESULTS_ROOT/editmode-results.xml" \
  --log "$RESULTS_ROOT/editmode-unity.log" \
  "${UNITY_ARGUMENTS[@]}"
"$SCRIPT_DIR/run-unity-tests.sh" \
  --mode PlayMode \
  --category ConfigPipeline \
  --assembly OneStrokeDemon.Tests.PlayMode \
  --results "$RESULTS_ROOT/playmode-results.xml" \
  --log "$RESULTS_ROOT/playmode-unity.log" \
  "${UNITY_ARGUMENTS[@]}"

echo "CONFIG_PIPELINE_PASS dotnet=PASS drift=PASS editmode=PASS playmode=PASS"
