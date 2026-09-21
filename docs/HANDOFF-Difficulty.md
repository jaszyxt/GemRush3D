# HANDOFF — Playability, Reachability & Difficulty

**For a fresh session picking up these scopes.** Read `docs/User-Directives.md`
FIRST (it is law and outranks everything, including this file), then this.

Written 2026-09-21 at v1.28.1. Every command below was **run and verified green
on this tree** before being written down — if one fails, the tree moved, not the
doc.

---

## 1. What these scopes are

1. **Game expert — playability/reachability.** Every level completable:
   spawn→portal, no impossible crossings, no unreachable collectibles.
2. **Difficulty curve.** The game is kind and readable for a 7-year-old —
   measured, never guessed.

**Not mine** (hand off, don't fold in): docs, art, audio, story, UI,
Steam/deploy.

**The governing intent is D-6: little difficulty, evolving variety.** The game
does *not* get harder as it goes. It gets *more interesting*. Never propose
making this game harder to fix a curve-shape problem — that error is recorded in
D-6 because it was made once.

---

## 2. The instruments (all in `tools/LevelAudit/`)

One command builds and runs the standalone audit — no Unity, no license:

```bash
sh tools/LevelAudit/run.sh              # reachability, all 40 levels
sh tools/LevelAudit/run.sh --margins    # tight jumps, FORCED vs optional
sh tools/LevelAudit/run.sh --curve      # the whole difficulty ramp
sh tools/LevelAudit/run.sh --retrace    # worst walk-back after a death
sh tools/LevelAudit/run.sh --hazards    # spinner windows vs child reaction
sh tools/LevelAudit/verify_tests.sh     # syntax-gate the test file
```

**Verified output on this tree (all green):**
- reachability: `ALL LEVELS REACHABLE`
- hazards: `0 hazard(s) outside the child budget`
- retrace: `worst in game: 77u at The Festival Finale; 0 level(s) over the bound`

### What each measures

| Instrument | File | Measures |
|---|---|---|
| `LevelReachability.cs` | mirror of live physics | spawn→portal BFS over standable surfaces; jump **margin** = 1 − gap/max-jump-for-that-rise; `Hop.Optional` via **remove-edge re-search** (a real bypass, not "has another neighbour") |
| `HazardTiming.cs` | `Spinner.Create` geometry | per-spinner revolution period, arm-pass ms, passes/crossing, **clear window**, platform **exposure** |
| `--retrace` | level data | longest stretch between respawn points (spawn/checkpoints/portal) |

### Trusted constants
Jump 9.5, run 8, gravity 9.81 → ~4.6 rise, ~15.5 flat range.
Child response budget **450 ms** (7-year-old RT research).
Spinner ceiling **90 deg/s** (D-6). Levels are **1-based** in UI, 0-based in code
(L25 = index 24 = The Silent Spire).

---

## 3. Tests

`Assets/Tests/EditMode/LevelAuditTests.cs` — **39 tests** (suite total 96 across
6 files; re-count with
`grep -rc '\[Test\]' Assets/Tests/EditMode/*.cs` before quoting — numbers drift
as parallel sessions add suites).

Headless (no Unity): `bash tools/run-tests-headless.sh`.
Editor full suite is authoritative; CI runs the headless set on every push.

The difficulty/playability assertions, and why each exists:

| Test | Locks |
|---|---|
| `Spawn_ReachesThePortal_OnEveryLevel` | whole-course completability |
| `EveryGem_IsPhysicallyCollectable` | no gem buried in geometry |
| `NoRouteJump_IsNearTheMaximumJump` | no pixel-perfect jump (2% floor) |
| `NoRouteJump_IsEverForcedWithoutAWayAround` | no trap (15% floor on FORCED hops only) |
| `TeachingRegions_AreNotThePunishingOnes` | early regions never the hardest |
| `Retrace_IsShortEnoughToForgiveADeath` | 45 s retrace bound |
| `NoSpinner_IsFasterThanTheComfortCeiling` | D-6's 90 deg/s cap |
| `EverySpinner_IsReadableByAChild` | 450 ms window + exposure ceiling |
| `Golden_IsNotHiddenOnTheWayOut` | rewards never parked at the exit |

**Rule: fix the LEVEL, not the threshold.** If an assertion fails, the content
is wrong. The one exception is documented in the test itself when the bound is
deliberately set to current reality.

---

## 4. Bugs found this era, and the method that found them

**The method (now D-5 law): a reported bug is a CLASS, not an instance.**
Name the class → sweep every level and system → fix all instances → record what
came back **clean** → add the assertion.

| Bug | Class | Fixed |
|---|---|---|
| Echo bridges invisible (renderer, no mesh) | rendering | real cube child, mesh on/off with state |
| Bell silent after game-over replay | rebuild race | `BellRig.Create` retires the doomed rig |
| L25 gust crossing impossible | **clock** — gust read the MUSIC phase; Festival's 13.6 s loop isn't a multiple of the 4.4 s period, so the wind blew once then never | own clock + musical offset |
| Golden gem missed on a perfect run | **placement scoring** — 29/37 within 12 u of the portal, L25 3.6 u *behind* the exit | 15 u portal rule |
| `DailyGem` same class + could silently place nothing | placement + absence | portal rule + never-vanish fallback |
| L7 two gems buried 8.1 u under their deck | placement | restored to grab height |
| 11 spinners over 90 deg/s (max 140 in L5) | difficulty spike | capped per D-6 |

**Distrust your own model first.** Every play-reported bug was invisible to the
instruments, because the instruments model geometry and the failures were
rendering, clocks and placement scoring. When a report contradicts an audit
saying "all clear", **the instrument is the first suspect**.

---

## 5. What the curve actually looks like (measured)

| Region | Spinner speeds | Shape |
|---|---|---|
| I–II (L1–5) | 60 → 75 → 90 → 90 | gentle, correct |
| III–V (L7–15) | 75–90 | gentle |
| VII–XIV (L16–40) | 60–90, 21 sleepy guardians | **gentler still — and that is intended** |

Distribution now: **17 × 60, 17 × 75, 13 × 90 — nothing faster.** Spinner
cap applied; the inversion is the design, not a defect (D-6).

---

## 6. Environment notes that save real time

- **Unity's bundled `csc` wrapper has a hardcoded build-machine path** — invoke
  `mono.exe` on `lib/mono/4.5/csc.exe` directly. Never reference Unity's
  `NetStandard/ref` facade with mono's mscorlib (CS0518 everywhere); use mono's
  own `Facades/netstandard.dll`.
- **The editor's player loop suspends when unfocused** — refocus via PowerShell
  `SetForegroundWindow` before expecting a recompile.
- **Parallel sessions commit into the same repo.** Check `git log` and
  `git status` before editing; re-read a file before every Edit; expect your
  work to be folded into someone else's commit. Untracked/dirty files you did
  not touch are theirs — leave them.
- **You cannot play the game.** Ask for a screenshot or a report; that is how
  every real bug here was found.

---

## 7. Open items in these scopes

1. **Nothing blocking.** All instruments green, all tests green, 40/40
   completable.
2. **The variable jump height is a single hard 50% cut** on release (binary
   full-or-half gamut rather than graded). Flagged, not changed — alters core
   feel and needs the user's play verdict.
3. **Optional mastery vs D-6**: medals and gem perfection stay opt-in per
   `DESIGN.md`. If a future change makes any of it required to reach the
   portal, that violates D-6.
4. **A fresh build is owed** whenever the user last played — everything above
   ships in code but they play a built artifact.

---

## 8. Definition of done for this scope

- `sh tools/LevelAudit/run.sh` + `--hazards` + `--retrace` all green.
- `verify_tests.sh` clean; the touched assertions still pass.
- New rules locked with an assertion where mechanically checkable.
- Committed **and pushed** (`git push` after every commit — user directive).
- Anything found outside these scopes handed off, not folded in.
