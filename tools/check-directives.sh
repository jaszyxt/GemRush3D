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

for heading in "## D-1." "## D-2." "## D-3." "## D-4." "## D-5." "## D-6." "## D-7." "## D-8."; do
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

# ---------------------------------------------------------------- D-7
# The Evergreen Law (story leads, build follows): story text describes a
# game that keeps growing, so it must not state its own size. Precedent:
# a menu quote claimed "twelve levels" when the game had forty.
#
# Both checks below are pure fixed-pattern greps over repository files.
# Every expansion is quoted and no value is ever passed to a command
# interpreter: nothing here is assembled from input.

# Spelled-out level/pack counts anywhere inside a quoted story string.
# Case-INSENSITIVE on purpose: the historical drift line was
# "Twelve levels" (capitalised), and an earlier case-sensitive version of
# this check missed both that shape and a mid-sentence count. Verified by
# injection - both a capitalised and a mid-sentence violation fail it.
COUNT_RE='"[^"]*\b(twelve|fifteen|twenty|twenty-five|thirty|thirty-five|forty|forty-five|fifty) (levels|packs)\b'
counts="$(grep -inE "$COUNT_RE" Assets/Scripts/Story.cs \
    Assets/Scripts/LevelLibrary.cs Assets/Scripts/LevelPackBSides.cs \
    Assets/Scripts/LevelPackTwo.cs Assets/Scripts/LevelPackThree.cs \
    Assets/Scripts/LevelPackFour.cs Assets/Scripts/LevelPackFive.cs \
    Assets/Scripts/LevelPackSix.cs Assets/Scripts/LevelPackSeven.cs \
    Assets/Scripts/LevelPackEight.cs Assets/Scripts/LevelPackNine.cs \
    Assets/Scripts/LevelPackTen.cs Assets/Scripts/LevelPackEleven.cs \
    Assets/Scripts/LevelPackTwelve.cs Assets/Scripts/LevelPackThirteen.cs \
    2>/dev/null || true)"
if [ -n "$counts" ]; then
  echo "FAIL (D-7): evergreen story text states a level or pack COUNT."
  echo "      The game keeps growing, so the count will become a lie."
  echo "      Say 'the realm keeps growing' rather than how many levels"
  echo "      it has. See docs/Story-Bible.md (Evergreen Law)."
  printf '%s\n' "$counts" | sed 's/^/      /'
  FAILED=1
else
  echo "ok (D-7):   no level counts in story prose"
fi

# Lives stated in evergreen story prose. Same class as the level count:
# a menu quote shipped "Three lives" long after the game moved to five
# lives (GameManager.StartingLives), and nothing caught it. The Story
# Bible pre-registered the exception - "if lives change, that quote
# changes with them" - so this guard is that promise, enforced.
#
# The check reads the REAL number out of GameManager rather than banning
# lives-counts outright: a count that agrees with the constant is correct
# copy (and the honest way to say "five lives"), while a count that
# disagrees is drift. That is the actual rule; banning the phrase would
# have failed on the corrected text and taught everyone to ignore the
# guard.
LIVES_RE='"[^"]*\b(one|two|three|four|five|six|seven|eight|nine|ten) lives\b'
STARTING_LIVES="$(grep -oE 'StartingLives = [0-9]+' \
    Assets/Scripts/GameManager.cs 2>/dev/null | grep -oE '[0-9]+' \
    | head -1 || true)"
if [ -z "$STARTING_LIVES" ]; then
  echo "FAIL (D-7): could not read StartingLives from GameManager.cs."
  echo "      The lives guard cannot verify story copy against a constant"
  echo "      it cannot find - fix the pattern or the constant's name."
  FAILED=1
else
  # Local echo of a word number, so the comparison needs no tooling.
  WORD_TO_NUM() {
    case "$1" in
      one|One) echo 1 ;; two|Two) echo 2 ;; three|Three) echo 3 ;;
      four|Four) echo 4 ;; five|Five) echo 5 ;; six|Six) echo 6 ;;
      seven|Seven) echo 7 ;; eight|Eight) echo 8 ;; nine|Nine) echo 9 ;;
      ten|Ten) echo 10 ;; *) echo "" ;;
    esac
  }
  lives_bad=""
  lives_ok=0
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    word="$(printf '%s' "$hit" | grep -ioE '\b(one|two|three|four|five|six|seven|eight|nine|ten) lives\b' | grep -oiE '^[a-z]+' | head -1)"
    num="$(WORD_TO_NUM "$word")"
    if [ "$num" != "$STARTING_LIVES" ]; then
      lives_bad="${lives_bad}${hit}
"
    else
      lives_ok=$((lives_ok + 1))
    fi
  done <<EOF
$(grep -ihE "$LIVES_RE" Assets/Scripts/Story.cs Assets/Scripts/Strings.cs 2>/dev/null || true)
EOF
  if [ -n "$lives_bad" ]; then
    echo "FAIL (D-7): story prose states a number of LIVES that is wrong."
    echo "      The game gives $STARTING_LIVES (GameManager.StartingLives)."
    echo "      A count that disagrees with the constant is drift - the copy"
    echo "      must follow the design, not the other way round."
    printf '%s' "$lives_bad" | sed 's/^/      /'
    FAILED=1
  else
    echo "ok (D-7):   story lives counts agree with StartingLives=$STARTING_LIVES"
  fi
fi

# The probationary gag was RESOLVED at level 40 (the badge is official).
# Reintroducing it as current status reverses the ending.
#
# This matches only CURRENT-STATUS phrasing: a line saying he IS
# probationary, or asking for the word to be removed. It deliberately does
# NOT match mere mentions of the word, because canon-sanctioned lines must
# be able to ACKNOWLEDGE the resolution - the shipped milestone reads "the
# word 'probationary' is gone", which is the ending working, not a
# violation. An earlier draft banned every mention and so failed on
# correct text; a guard that cries wolf on correct copy gets deleted.
PROBATION_IS_RE='weather support \(probationary\)|probation[^"]*(be removed|pending|continues|remains|still on)'
PROBATION_ASK_RE='request[^"]*remove[^"]*probation|remove[^"]*the word .probationary'
probation="$(grep -inE "$PROBATION_IS_RE|$PROBATION_ASK_RE" \
    Assets/Scripts/Story.cs Assets/Scripts/Strings.cs \
    Assets/Scripts/LevelPackTwo.cs Assets/Scripts/LevelPackThree.cs \
    Assets/Scripts/LevelPackFour.cs Assets/Scripts/LevelPackFive.cs \
    Assets/Scripts/LevelPackSix.cs Assets/Scripts/LevelPackSeven.cs \
    Assets/Scripts/LevelPackEight.cs Assets/Scripts/LevelPackNine.cs \
    Assets/Scripts/LevelPackTen.cs Assets/Scripts/LevelPackEleven.cs \
    Assets/Scripts/LevelPackTwelve.cs Assets/Scripts/LevelPackThirteen.cs \
    2>/dev/null || true)"
if [ -n "$probation" ]; then
  echo "FAIL (D-7): a shipped string reinstates the probationary status."
  echo "      The badge review concluded at the end of Movement Two -"
  echo "      Gloomfang is NOT probationary. The gag paid off and is"
  echo "      retired; lines may note that it ENDED, but none may assert"
  echo "      it is current or still pending. See docs/Story-Bible.md"
  echo "      (Canon amendments)."
  printf '%s\n' "$probation" | sed 's/^/      /'
  FAILED=1
else
  echo "ok (D-7):   the probationary status is not reinstated"
fi

# ---------------------------------------------------------------- D-10
# Every gust rides the same wind: gust call sites carry GEOMETRY only
# (position, size, direction). The wind numbers live once in GustZone's
# constants; a wind literal at a call site means someone is tuning one
# gust apart from the rest.
wind_literals="$(grep -H -E 'Gusts\.Add|GustSpec\(' \
    Assets/Scripts/LevelPack*.cs Assets/Scripts/LevelLibrary.cs 2>/dev/null |
    grep -E ', ?4\.4f|, ?2\.2f|, ?8f\)|, ?3f\)' || true)"
if [ -n "$wind_literals" ]; then
  echo "FAIL (D-10): a gust call site carries wind numbers. All gusts"
  echo "      share one wind (GustZone.DefaultPeriod/DefaultActiveTime/"
  echo "      DefaultStrength/DefaultLift); level data places lanes only."
  echo "      Tune the wind by changing GustZone's constants, which"
  echo "      changes EVERY gust together — never one lane."
  printf '%s\n' "$wind_literals" | sed 's/^/      /'
  FAILED=1
else
  echo "ok (D-10):  gust call sites carry geometry only"
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
