#!/bin/sh
# Fails when a doc cites a version that disagrees with GemRush.version.
#
# The version is hand-written in exactly one place (GemRush.version at the
# repo root); bundleVersion and the Android versionCode derive from it. Docs
# are prose, so they can't derive — this guard makes drift loud instead
# of silent. It exists because it already happened: the code sat at
# 1.27.0 while HANDOFF.md advertised 1.19.0 and Steam-Deploy.md 1.22.1.
#
# A doc may legitimately describe history ("v1.26.1 hotfix"), so only
# the version a doc presents as CURRENT is checked. Each entry below
# names the file and a grep pattern whose capture group is the current
# version claim.
set -e
# Resolve the repo root from this script's own location, whether invoked
# as "tools/check-version.sh" or by absolute path.
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

if [ ! -f GemRush.version ]; then
  echo "FAIL: GemRush.version file missing at repo root"
  exit 1
fi
VERSION="$(tr -d ' \t\r\n' < GemRush.version)"
if [ -z "$VERSION" ]; then
  echo "FAIL: GemRush.version is empty"
  exit 1
fi
echo "GemRush.version = $VERSION"

# Format check: exactly major.minor.patch, digits only.
case "$VERSION" in
  *[!0-9.]*|.*|*.)
    echo "FAIL: GemRush.version '$VERSION' is not plain major.minor.patch"
    exit 1
    ;;
esac
DOTS="$(printf '%s' "$VERSION" | tr -cd '.' | wc -c)"
if [ "$DOTS" -ne 2 ]; then
  echo "FAIL: GemRush.version '$VERSION' must have exactly two dots"
  exit 1
fi

FAILED=0

# check <file> <regex-with-one-capture-group> <human-label>
check() {
  file="$1"; pattern="$2"; label="$3"
  if [ ! -f "$file" ]; then
    echo "skip: $file not present"
    return 0
  fi
  found="$(grep -oE "$pattern" "$file" | head -1 | sed -E "s/$pattern/\1/" || true)"
  if [ -z "$found" ]; then
    echo "note: $file has no '$label' line to check"
    return 0
  fi
  if [ "$found" != "$VERSION" ]; then
    echo "FAIL: $file claims $label $found but GemRush.version is $VERSION"
    FAILED=1
  else
    echo "ok:   $file $label $found"
  fi
}

# HANDOFF's header names the session it belongs to.
check HANDOFF.md '^# HANDOFF — .*v([0-9]+\.[0-9]+\.[0-9]+)' 'current version'

# README advertises the shipped build.
check README.md 'bundleVersion = "([0-9]+\.[0-9]+\.[0-9]+)"' 'bundleVersion'

if [ "$FAILED" -ne 0 ]; then
  echo
  echo "Version drift detected. Fix the doc, or bump GemRush.version deliberately."
  echo "Do NOT edit the version in EnsureShaders.cs or ProjectSettings — it derives."
  exit 1
fi

echo "version check passed"
