#!/usr/bin/env bash
# Runs the pure-data EditMode tests WITHOUT Unity.
#
# Why this exists: the EditMode tests used to run only inside the editor,
# or on `v*` tags via GameCI — so a regression could sit on main
# undetected until release day. The level/geometry invariants are pure C#
# (Vector3 and Mathf are managed math, not native calls — verified: a
# test asserting Vector3.Distance returned exactly 5 outside Unity), so
# they compile against Unity's module assemblies and execute under Mono.
# tools/LevelAudit/run.sh already proved this mechanism for the
# reachability audit; this script points it at the test suite.
#
# Measured scope: 30 of the 75 EditMode tests run here — the whole
# LevelAuditTests suite. The rest need a live scene (UI hierarchy, rect
# transforms, instantiated GameObjects) or native-backed APIs
# (AudioClip, Resources) and stay on the GameCI tag path, which runs
# everything. This is a fast pre-flight, not a replacement.
#
# Usage:  bash tools/run-tests-headless.sh
#
# Two modes:
#   LOCAL  — Unity editor installed: uses its bundled Mono + its module
#            DLLs directly.
#   CI     — no Unity: uses the committed reference assemblies in
#            tools/headless/refs plus a system mono. Set CI_MONO_PREFIX
#            when the mono binaries are not on PATH.
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

OUT_DIR="$ROOT/tools/.headless-test"
mkdir -p "$OUT_DIR"

UNITY=""
if [ -n "$UNITY_EDITOR_DATA" ]; then
  UNITY="$UNITY_EDITOR_DATA"
else
  UNITY="$(ls -d "/c/Program Files/Unity/Hub/Editor/"*/Editor/Data 2>/dev/null | tail -1)"
fi

NUNIT="$ROOT/tools/headless/refs/nunit.framework.dll"
REF_DIR="$ROOT/tools/headless/refs"

if [ -n "$UNITY" ] && [ -d "$UNITY" ]; then
  MONO="$UNITY/MonoBleedingEdge/bin/mono.exe"
  CSC="$UNITY/MonoBleedingEdge/lib/mono/4.5/csc.exe"
  MGD="$UNITY/Managed/UnityEngine"
  MB="$UNITY/MonoBleedingEdge"
  NUNIT="$UNITY/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll"
  echo "mode: local (Unity at $UNITY)"
else
  # CI: no Unity. Mono comes from the system (apt in the workflow), and
  # the Unity assemblies come from the committed refs directory.
  #
  # The compiler is addressed as the managed csc.exe under Mono's 4.5
  # profile and run through `mono`, rather than the shell wrapper: Mono
  # ships both "csc" (a script) and "csc.exe" (the compiler), and Git
  # Bash resolves the script, which Windows then refuses to execute.
  MONO="${CI_MONO_PREFIX:-}mono"
  if command -v mono.exe >/dev/null 2>&1; then MONO="mono.exe"; fi
  MGD="$REF_DIR"
fi

# The compiler is always the managed csc.exe launched by mono — one code
# path for local and CI, so a difference between them cannot hide here.
if [ -n "$MB" ]; then
  CSC_DIR="$MB/lib/mono/4.5"
else
  # CI: mono lives at <prefix>/bin/mono, so the profile is a sibling of bin.
  CSC_DIR="$(dirname "$(command -v "$MONO")")/../lib/mono/4.5"
fi
# Normalize the ".." so a concatenation cannot produce the doubled
# ".../lib/mono/lib/mono/4.5" path, and so errors show a real path.
CSC_DIR="$(cd "$CSC_DIR" 2>/dev/null && pwd || echo "$CSC_DIR")"
# Mono names the compiler csc.exe on Windows and plain csc on Linux.
if [ -f "$CSC_DIR/csc.exe" ]; then
  CSC="$CSC_DIR/csc.exe"
elif [ -f "$CSC_DIR/csc" ]; then
  CSC="$CSC_DIR/csc"
else
  echo "FAIL: no csc compiler found in $CSC_DIR"
  echo "      (install mono-devel, which provides it)"
  exit 1
fi

# Resolve mono so the availability check and the compile step agree.
MONO_BIN="$(command -v "$MONO" 2>/dev/null || echo "$MONO")"
if [ ! -e "$MONO_BIN" ]; then
  echo "SKIP: mono not found (install mono-devel, or set CI_MONO_PREFIX)"
  exit 0
fi

for f in "$MONO_BIN" "$CSC" "$NUNIT"; do
  if [ ! -e "$f" ] && ! command -v "$f" >/dev/null 2>&1; then
    echo "SKIP: missing $f"
    exit 0
  fi
done

OUT_DIR="$ROOT/tools/.headless-test"
mkdir -p "$OUT_DIR"

# Paths may need converting from Git Bash's /c/... form to native
# C:/... form, because on Windows Mono's csc is a native binary that
# cannot resolve the MSYS mount style. On Linux (CI) the paths are
# already correct, so this is a pass-through there.
winpath() {
  if command -v cygpath >/dev/null 2>&1; then
    cygpath -m "$1"
  else
    printf '%s' "$1"
  fi
}

# References go into a csc RESPONSE FILE, not a shell variable: the
# Unity install path contains a space ("C:/Program Files/..."), and
# shell word-splitting strips quotes on expansion, so an inline
# "$REFS" list always reaches csc as a broken path (CS0006). A
# response file is read by csc directly, one argument per line.
RSP="$OUT_DIR/refs.rsp"
: > "$RSP"
COUNT=0
# Two traps here, both hit for real while building this:
#  1. Piping `ls` through word-splitting breaks on the space in
#     "C:/Program Files" — a `for` over an unquoted glob preserves it.
#  2. The *Editor* exclusion must test the FILE NAME, not the full
#     path: the path itself contains ".../Hub/Editor/...", so matching
#     the whole string skipped every single assembly (COUNT=0).
for dll in "$MGD"/UnityEngine*.dll; do
  [ -f "$dll" ] || continue
  base="${dll##*/}"
  case "$base" in
    *Editor*) continue ;;
  esac
  printf -- '-r:"%s"\n' "$(winpath "$dll")" >> "$RSP"
  COUNT=$((COUNT + 1))
done
if [ "$COUNT" -eq 0 ]; then
  echo "FAIL: found no UnityEngine module assemblies under $MGD"
  exit 1
fi
printf -- '-r:"%s"\n' "$(winpath "$NUNIT")" >> "$RSP"

# Package-provided assemblies the game code references (uGUI, Input
# System, URP). Committed alongside the module refs so CI has them
# without a Unity install; locally they fall back to Library (which
# Unity writes when it compiles the project).
for name in UnityEngine.UI Unity.InputSystem Unity.RenderPipelines.Universal.Runtime \
            Unity.RenderPipelines.Core.Runtime; do
  pkg="$REF_DIR/$name.dll"
  [ -f "$pkg" ] || pkg="$ROOT/Library/ScriptAssemblies/$name.dll"
  if [ -f "$pkg" ]; then
    printf -- '-r:"%s"\n' "$(winpath "$pkg")" >> "$RSP"
  else
    echo "note: package assembly $name.dll not found (open the project in Unity once)"
  fi
done

echo "referencing $COUNT Unity modules"

# Sources go in their own response file for the same path reasons.
SRC_RSP="$OUT_DIR/sources.rsp"
: > "$SRC_RSP"
for cs in "$ROOT"/Assets/Scripts/*.cs; do
  printf '"%s"\n' "$(winpath "$cs")" >> "$SRC_RSP"
done
printf '"%s"\n' "$(winpath "$ROOT/Assets/Tests/EditMode/LevelAuditTests.cs")" >> "$SRC_RSP"
echo "compiling $(wc -l < "$SRC_RSP") source files"

# The test sources we run headlessly. LevelAuditTests is geometry and
# level-data only — no scene objects — which is why it is the first
# suite to make the crossing.

echo "== compiling game scripts + LevelAuditTests against Unity module DLLs =="
rm -f "$OUT_DIR/tests.dll"
# mscorlib/netstandard come from whichever Mono we resolved: Unity's
# bundled Mono (MB) locally, or the system Mono's 4.5 profile in CI.
# csc.exe lives in the mono 4.5 profile, alongside the base class
# libraries — so the profile directory is simply CSC_DIR.
BASE_LIBS="$CSC_DIR"
# Compile output is captured rather than piped: a pipe makes the exit
# status come from grep, and `set -e` would then treat a clean compile
# as a failure (or, worse, hide a real one).
COMPILE_LOG="$OUT_DIR/compile.log"
set +e
"$MONO_BIN" "$CSC" -nologo -target:library -out:"$(winpath "$OUT_DIR/tests.dll")" \
  -r:"$(winpath "$BASE_LIBS/mscorlib.dll")" \
  -r:"$(winpath "$BASE_LIBS/Facades/netstandard.dll")" \
  @"$(winpath "$RSP")" \
  @"$(winpath "$SRC_RSP")" > "$COMPILE_LOG" 2>&1
COMPILE_STATUS=$?
set -e

if [ "$COMPILE_STATUS" -ne 0 ] || [ ! -f "$OUT_DIR/tests.dll" ]; then
  echo "compile errors:"
  grep -E "error CS" "$COMPILE_LOG" | sort -u | head -20
  echo
  echo "FAIL: the headless test assembly did not compile. Either a test"
  echo "      now touches engine features that cannot run outside Unity,"
  echo "      or a real compile error was introduced. The GameCI tag"
  echo "      build still runs the full suite as the authoritative gate."
  exit 1
fi
echo "compiled cleanly ($(grep -cE 'warning CS' "$COMPILE_LOG" || true) warnings)"

echo "== building the runner =="
rm -f "$OUT_DIR/runner.exe"
"$MONO_BIN" "$CSC" -nologo -target:exe -out:"$(winpath "$OUT_DIR/runner.exe")" \
  -r:"$(winpath "$BASE_LIBS/mscorlib.dll")" \
  -r:"$(winpath "$NUNIT")" \
  "$(winpath "$ROOT/tools/headless/HeadlessTestRunner.cs")" 2>&1 | grep -E "error CS" | head -10 || true

if [ ! -f "$OUT_DIR/runner.exe" ]; then
  echo "FAIL: could not build the headless runner"
  exit 1
fi

echo "== running tests =="
# Copy every referenced Unity assembly next to the test assembly: Mono
# resolves dependencies from the app base directory, and the tests fail
# with "Could not load file or assembly 'UnityEngine.CoreModule'" when
# the assembly only exists elsewhere.
cp -f "$NUNIT" "$OUT_DIR/nunit.framework.dll"
for dll in "$MGD"/UnityEngine*.dll; do
  [ -f "$dll" ] || continue
  base="${dll##*/}"
  case "$base" in
    *Editor*) continue ;;
  esac
  cp -f "$dll" "$OUT_DIR/" 2>/dev/null || true
done
# Package assemblies too, minus the ones that are editor-only.
for name in UnityEngine.UI Unity.InputSystem Unity.RenderPipelines.Universal.Runtime \
            Unity.RenderPipelines.Core.Runtime; do
  pkg="$REF_DIR/$name.dll"
  [ -f "$pkg" ] || pkg="$ROOT/Library/ScriptAssemblies/$name.dll"
  [ -f "$pkg" ] && cp -f "$pkg" "$OUT_DIR/" 2>/dev/null || true
done

cd "$OUT_DIR"
"$MONO_BIN" "$OUT_DIR/runner.exe" "$OUT_DIR/tests.dll"
