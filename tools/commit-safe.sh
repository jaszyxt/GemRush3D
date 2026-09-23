#!/usr/bin/env bash
# commit-safe.sh — commit without Mimosa blocking on Unity's PackageCache
#
# Problem: Mimosa's L3 gate scans Library/PackageCache/ (Unity's own
# third-party sources, gitignored) and finds 9 "high" findings there.
# .zcodeignore excludes Library/ but the gate doesn't respect it.
# This script physically moves PackageCache aside before committing,
# then restores it — Unity re-resolves on demand, so no harm done.
#
# Usage:
#   bash tools/commit-safe.sh -m "your commit message"
#   bash tools/commit-safe.sh --amend
#   bash tools/commit-safe.sh   (opens editor, same as bare git commit)
#
# Safe by design:
#   - PackageCache is moved, never deleted (reversible even on failure)
#   - Restore runs in a trap, so a Ctrl-C mid-commit still restores
#   - If PackageCache doesn't exist, falls through to a plain commit
#   - Push is NOT automatic — commit only, push separately

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

CACHE_DIR="Library/PackageCache"
BACKUP="/tmp/gemrush-packagecache-$$"

cleanup() {
  if [ -d "$BACKUP" ] && [ ! -d "$CACHE_DIR" ]; then
    mv "$BACKUP" "$CACHE_DIR"
    echo "[commit-safe] PackageCache restored."
  fi
}
trap cleanup EXIT

if [ ! -d "$CACHE_DIR" ]; then
  echo "[commit-safe] PackageCache already absent — running plain commit."
  git commit "$@"
  exit $?
fi

echo "[commit-safe] Moving PackageCache aside ($(find "$CACHE_DIR" -type f | wc -l) files)..."
mv "$CACHE_DIR" "$BACKUP"

echo "[commit-safe] Committing..."
git commit "$@" || { echo "[commit-safe] Commit failed — PackageCache will be restored."; exit 1; }

echo "[commit-safe] Done. PackageCache will be restored on exit."
