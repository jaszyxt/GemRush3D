#!/bin/sh
# Fails when a PROTECTED USER DIRECTIVE has been changed without the user
# asking for it. See docs/User-Directives.md — that file is law, and an
# agent's opinion ("looks inconsistent", "the audit said so", "cleaner")
# is NOT permission to break it.
#
# This guard is deliberately narrow: it checks only the parts of the
# directives that a script can actually prove, rather than pretending to
# police judgement. The rest lives as documented law in the file, which
# every agent is required to read.
#
# If this fails: do NOT edit the guard to make it pass. Either revert the
# change, or (if the user DID ask for it) update docs/User-Directives.md
# in the same commit recording the user's words.
set -e
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

FAILED=0
DIRECTIVES=docs/User-Directives.md

# ---------------------------------------------------------------- D-0
# The directives file itself must exist and still carry its headings.
if [ ! -f "$DIRECTIVES" ]; then
  echo "FAIL: $DIRECTIVES is missing. The user's protected directives"
  echo "      are law and must not be deleted. Restore the file."
  exit 1
fi

for heading in "## D-1." "## D-2." "## D-3." "## D-4." "## D-5."; do
  if ! grep -q "^$heading" "$DIRECTIVES"; then
    echo "FAIL: $DIRECTIVES no longer contains '$heading'. A protected"
    echo "      directive was removed. Restore it, or update the file"
    echo "      with the user's explicit instruction."
    FAILED=1
  fi
done

# ---------------------------------------------------------------- D-1
# Haptics mark EVENTS, not movement. The user explicitly named the jump.
# PlayerController's jump / skid / landing blocks must stay buzz-free:
# look for a Haptics.* call in the same statement neighbourhood as the
# movement verbs. The wind-ride texture is allowed and lives elsewhere.
check_no_haptic_near() {
  verb="$1"; label="$2"
  # Grab the 6 lines after any line containing the verb, and look for a
  # Haptics call among them.
  if grep -A6 "$verb" Assets/Scripts/PlayerController.cs \
      | grep -q "Haptics\."; then
    echo "FAIL (D-1): a Haptics.* call sits next to '$label' in"
    echo "      PlayerController.cs. The user's directive: haptics mark"
    echo "      EVENTS, never movement — the jump is explicitly excluded."
    echo "      Revert it, or update docs/User-Directives.md with the"
    echo "      user's explicit instruction."
    FAILED=1
  else
    echo "ok (D-1):   no haptics on $label"
  fi
}

check_no_haptic_near "PlayJump()" "the jump"
check_no_haptic_near "PlaySkid(" "the skid"
check_no_haptic_near "PlayLand(impactSpeed)" "the landing"

# A gem pickup must not buzz directly either: the buzz belongs to the
# streak milestone in AudioManager, not to every collected gem.
if grep -A6 "OnGemCollected" Assets/Scripts/Gem.cs | grep -q "Haptics\."; then
  echo "FAIL (D-1): Gem.cs fires a haptic on every gem pickup. The user's"
  echo "      directive reserves the buzz for the streak milestone."
  FAILED=1
else
  echo "ok (D-1):   no haptics on a single gem pickup"
fi

# ---------------------------------------------------------------- D-2
# The palette law: hazard red and reward gold are exclusive. A feature
# script must not hand-build those hues inline — that is how the families
# drifted apart the first time (seven copies of the gold literal).
if grep -rn "new Color(0.85f, 0.20f, 0.15f)" Assets/Scripts/ \
    --include=*.cs | grep -v "ArtLib.cs" | grep -q .; then
  echo "FAIL (D-2): an inline HazardRed literal appeared outside ArtLib.cs."
  echo "      Use ArtLib.HazardRed, or add a named constant to ArtLib."
  FAILED=1
else
  echo "ok (D-2):   no inline hazard-red literals"
fi

if grep -rn "new Color(1.00f, 0.84f, 0.25f)\|new Color(1f, 0.84f, 0.25f)" \
    Assets/Scripts/ --include=*.cs | grep -v "ArtLib.cs" | grep -q .; then
  echo "FAIL (D-2): an inline reward-gold literal appeared outside ArtLib.cs."
  echo "      Use ArtLib.Gold, or add a named constant to ArtLib."
  FAILED=1
else
  echo "ok (D-2):   no inline reward-gold literals"
fi

# ---------------------------------------------------------------- done
if [ "$FAILED" -ne 0 ]; then
  echo
  echo "A protected user directive was changed. See docs/User-Directives.md."
  echo "Do NOT weaken this guard. Revert the change, or record the user's"
  echo "explicit instruction in that file in the same commit."
  exit 1
fi

echo "user-directive check passed"
