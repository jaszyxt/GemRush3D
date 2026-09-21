#!/bin/sh
# Motion-consistency linter.
#
# The motion study found the same ideas re-implemented with different
# numbers: six hover-bob frequencies, three hand-rolled copies of one pop
# curve, eight separate exp-smoothing rates, and Pip's spring constants
# copy-pasted into a flower. None of it was broken; all of it was drift,
# and drift is invisible until someone retunes one copy.
#
# This tool does NOT fail the build. Motion has legitimate variety — a
# big gem and a small heart should not travel the same distance, and some
# curves genuinely are one-offs. Failing on any literal would push people
# to hide the numbers, which is worse than the drift. Instead it prints
# an inventory of every motion constant OUTSIDE the shared helpers, so a
# reviewer can see whether a new one was justified or just copied.
#
# Usage:  sh tools/check-motion.sh
#   Warnings only; always exits 0 unless a genuine duplicate is found.
set -e
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

SRC=Assets/Scripts
DUPES=0

echo "=== Motion constant inventory (outside the shared helpers) ==="
echo

# --- Hover bob: collectibles should breathe at one rate -------------
echo "-- hover/bob rates (ArtLib.HoverBobRate is the shared one) --"
grep -rn "Mathf.Sin(Time.time \* [0-9]" $SRC --include=*.cs \
  | sed 's/:.*Mathf.Sin(Time.time \* /  rate /' \
  | sed 's/).*//' \
  | sort -t' ' -k3 -n | head -20
echo

# --- Shared spring -------------------------------------------------
echo "-- spring constants (Tweener.SpringStiffness/Damping are shared) --"
grep -rn "\-90f\|12f \*" $SRC --include=*.cs | grep -i "vel" | grep -v "Tweener.cs" || true
echo

# --- Exp smoothing --------------------------------------------------
echo "-- exp-smoothing rates: 1 - Exp(-<rate> * dt) --"
grep -rno "Exp(-[0-9.]*f \* \(Time\.\|dt\|Time\.unscaled\)" $SRC --include=*.cs \
  | sed 's/.*Exp(-/  rate /' | sed 's/f .*//' | sort -n | uniq -c | sort -rn | head -15
echo

# --- Pop curves -----------------------------------------------------
echo "-- hand-rolled pop curves: Sin(t*PI) * <amp> --"
grep -rn "Mathf.Sin(t \* Mathf.PI) \* [0-9.]*f" $SRC --include=*.cs || true
echo

# --- The genuine duplicate check -----------------------------------
# Identical floating-point constants repeated verbatim across files, for
# the SAME idea, is the thing worth failing on. Pip's spring was the
# historical case; it is now shared, so this should stay quiet.
echo "-- duplicate spring implementations (should be none) --"
if grep -rln "90f \* (squash\|90f \* bounce\|90f \* (bounce" $SRC --include=*.cs \
    | grep -q .; then
  echo "FAIL: a spring is still hand-implemented outside Tweener.StepSpring:"
  grep -rln "90f \* (squash\|90f \* bounce\|90f \* (bounce" $SRC --include=*.cs
  DUPES=1
else
  echo "ok: no hand-rolled k=90/c=12 springs outside Tweener"
fi
echo

if [ "$DUPES" -ne 0 ]; then
  echo "Motion duplication found. Route the copy through the shared helper"
  echo "(Tweener.StepSpring / ArtLib.HoverBobRate) or document why it differs."
  exit 1
fi

echo "motion inventory complete (warnings above are informational)"
