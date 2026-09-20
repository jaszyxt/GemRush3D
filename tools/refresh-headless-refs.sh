#!/usr/bin/env bash
# Regenerates tools/headless/refs — the Unity assemblies that
# tools/run-tests-headless.sh compiles and runs against.
#
# Why these are committed rather than downloaded in CI: the tests must
# compile against real UnityEngine types (Vector3, Mathf, LevelDefinition
# fields) and Mono must resolve those assemblies at run time. Fetching
# them in CI would mean a 3.6 GB Unity editor installer per run; the
# subset actually needed is ~16 MB and changes only when the Unity
# version changes, which is exactly the kind of thing worth vendoring.
#
# Run this after upgrading the Unity editor version, then commit the
# result. Unity installs these under its Editor/Data folder; the package
# assemblies (uGUI, Input System, URP) come from Library/ScriptAssemblies
# and therefore need the project to have been opened at least once.
#
# Usage:  bash tools/refresh-headless-refs.sh
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

if [ -n "$UNITY_EDITOR_DATA" ]; then
  UNITY="$UNITY_EDITOR_DATA"
else
  UNITY="$(ls -d "/c/Program Files/Unity/Hub/Editor/"*/Editor/Data 2>/dev/null | tail -1)"
fi

if [ -z "$UNITY" ] || [ ! -d "$UNITY" ]; then
  echo "FAIL: no Unity editor install found (set UNITY_EDITOR_DATA to override)"
  exit 1
fi

MGD="$UNITY/Managed/UnityEngine"
NUNIT="$UNITY/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll"
DEST="$ROOT/tools/headless/refs"

echo "refreshing from $UNITY"
rm -rf "$DEST"
mkdir -p "$DEST"

count=0
for dll in "$MGD"/UnityEngine*.dll; do
  [ -f "$dll" ] || continue
  base="${dll##*/}"
  # Editor-only assemblies are not needed and would drag in UnityEditor.
  case "$base" in
    *Editor*) continue ;;
  esac
  cp -f "$dll" "$DEST/"
  count=$((count + 1))
done
echo "copied $count UnityEngine modules"

if [ -f "$NUNIT" ]; then
  cp -f "$NUNIT" "$DEST/"
  echo "copied nunit.framework.dll"
else
  echo "FAIL: nunit.framework.dll not found at $NUNIT"
  exit 1
fi

for name in UnityEngine.UI Unity.InputSystem \
            Unity.RenderPipelines.Universal.Runtime Unity.RenderPipelines.Core.Runtime; do
  src="$ROOT/Library/ScriptAssemblies/$name.dll"
  if [ -f "$src" ]; then
    cp -f "$src" "$DEST/"
    echo "copied $name.dll"
  else
    echo "WARN: $name.dll not in Library/ScriptAssemblies — open the project in Unity, then re-run"
  fi
done

echo
echo "wrote $(ls "$DEST" | wc -l) assemblies to tools/headless/refs ($(du -sh "$DEST" | cut -f1))"
echo "verify with: bash tools/run-tests-headless.sh"
