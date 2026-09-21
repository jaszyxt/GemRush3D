# Fun & Creativity Handover

**For the next agent taking the delight scope.** Written 2026-09-21 at
v1.28.0. Read this after `docs/User-Directives.md` (law) and in parallel
with `HANDOFF.md` (the chronological release log — this file is the
*role-scoped* view, not a replacement for it).

---

## 1. Who you are

You own **fun, delight and creativity** for Gem Rush 3D. Concretely:

- You decide what makes the game *feel* good — game feel, feedback, reward
  moments, reactive world, calm/rest, and the craft laws that govern them.
- You research the craft (competitor analysis, design literature), decide
  what to adopt, and **direct other agents** to build it.
- You verify your own work in-engine and lock it with tests.

**Not yours — hand these to their owners instead:**

| Area | Owner / doc |
|---|---|
| Audio mix, VO, synth design | `docs/Audio-Design.md` |
| Palette, art identity, visual bible | `docs/Art-Direction.md` |
| Level geometry, difficulty curve, pacing | `tools/LevelAudit`, difficulty owner |
| UI/UX ergonomics, touch targets | `docs/UIUX-Multiplatform-Directives.md` |
| Story canon, voice, cast | `docs/Story-Bible.md` |
| Release, builds, versioning | `HANDOFF.md` |

---

## 2. Read-first order

1. **`docs/User-Directives.md`** — the USER's protected decisions. It
   **outranks every other doc**, including this one. `tools/check-directives.sh`
   guards what a script can prove, and **CI fails if you break it**.
   Never weaken the guard to make a build pass.
2. **`docs/Fun-Creativity-Directives.md`** — *your* law. Vision, the three
   deliverables per pack, craft rules, the systems inventory, the future
   queue, the rejected list, source index. **Update it when you ship.**
3. **`docs/Feel-Log.md`** — the user's playtest verdicts, in their words,
   with reasons. This is the closest thing you have to actually feeling the
   game. **Never record a verdict the user did not give.**
4. **`HANDOFF.md`** — what each session shipped, environment wisdom,
   hard-won gotchas.
5. **`RESEARCH.md`** — research record (adopted / ADAPT / SKIPPED).

---

## 3. State of your scope (verified at v1.28.0)

All systems below exist in code and are wired. Anchors are current as of
this writing — re-verify with `git log --oneline -- <file>` before relying
on line numbers, since parallel sessions move fast.

### Game feel
| System | Where | Notes |
|---|---|---|
| Hit-stop on death | `GameManager` | 120 ms, unscaled restore, refuses when paused. Reviewed twice. |
| FOV kick / hold | `CameraFollow.FovKick/SetFovHold` | ShakeOn-gated, +10° cap (backdrop margin) |
| Skid dust, wind streaks | `PlayerController`, `WindStreaks.cs` | |
| Gust-ride haptics | `Haptics.StartRideTexture` | The one allowed *sustained* buzz (D-1) |

### Feedback & reward
| System | Where |
|---|---|
| Combo ladder (audio semitones) + crown at cap | `AudioManager.PlayPickup`, `ComboCrown.cs` |
| Milestone ring every 10th gem | `Fx.Ring` |
| HUD gem pulse + low-life heart glow | `UIManager` |
| Win confetti (+ petals in Two Suns) | `Fx.Confetti`, `UIManager.ShowWin` |
| Panel transitions | `UIManager` show/hide, `Tweener` |

### Reactive world / secrets
| System | Where |
|---|---|
| Idle Pip ladder (glance→wave→sit→sleep) | `PlayerController.UpdateIdleLife` |
| Pokeable flowers (position-hashed pentatonic) | `FlowerPoke.cs`, `Props.cs` |
| Gloomfang giggle-raindrop bloom | `Gloomfang.cs` |
| Checkpoint twirl | `Checkpoint.cs`, `PlayerController.Twirl` |
| Golden-gem signal (trail, glimmer, found outline, canon note) | `GoldenSignal.cs`, `GoldenGem.cs` |

### Calm / rest
| System | Where |
|---|---|
| The Rest (slow unrewarded win drift) | `RestBeat.cs` |
| Perches (bench on each level's calmest landing; **Pip sits on arrival**) | `Perch.cs` + `PlayerController` sit blend |

### Tests
`Assets/Tests/EditMode/DelightAuditTests.cs` — 13 tests covering ghost-run
data, golden-gem data, perch placement invariants, `Fx` particle clamps, and
a cozy-law copy scan. **This is a data/law net, not a feel net** — hit-stop,
the crown, the idle ladder and the perch's *sit* have no automated coverage
because they need a live scene. Verify those by hand.

---

## 4. The rules you must obey (short form)

From `docs/User-Directives.md` — read the file, this is only an index:

- **D-1 Haptics mark EVENTS, never movement.** Jump/skid/ordinary landing/
  single gem are silent. Streak milestone, firm landing, checkpoint, heart,
  lantern, death, win may buzz. Wind-ride is the one allowed sustained
  state. Test: *does this tell the player something they'd otherwise miss?*
- **D-2 Palette law.** `HazardRed` = spinner arms only. `Gold` = rewards
  only. New colours go in `ArtLib` — never inline. Fix contrast with
  silhouette/emission/layout, never by re-hueing a family.
- **D-3 User edits are authoritative.** Re-read before every write. The
  user's version wins; never "restore" what they removed.
- **D-4 Movement/feel/difficulty numbers are the user's.** You may report
  with evidence; you may not retune.
- **D-5 A reported bug is a CLASS, not an instance.** Name the class, sweep
  every level and system, fix all instances, add the assertion. **Distrust
  your own instruments first** — every play-reported bug so far was
  invisible to the agent's instruments.
- **D-6 Little difficulty; evolving variety instead.** Spinner ceiling
  90°/s (prefer 60–75). Nothing may be harder than what preceded it.

Also from your own craft law (`Fun-Creativity-Directives.md`): no reward
attached to rest, **no checkbox interactions** (no sit prompt on the bench —
explicitly banned with a reason), no streak guilt, no FOMO, evergreen copy
(no digits in celebration strings).

---

## 5. Environment: how to actually work here

**Unity is open in an editor and driven through MCP.** This is the working
loop — do not fight it:

1. `mcp__unityMCP__refresh_unity` (compile: request, force) →
2. `mcp__unityMCP__read_console` filtered to `error CS` → expect 0 →
3. `mcp__unityMCP__run_tests` (EditMode) → poll `get_test_job` →
4. `mcp__unityMCP__manage_editor` play → verify by **measurement**
   (`execute_code` + reflection) → stop →
5. Commit only when green.

**Hard-won gotchas:**
- **Play Mode blocks EditMode tests.** Stop play before running the suite.
- `execute_code` uses **C# 6 (CodeDom)** — no local functions, no
  expression-bodied members, no `=>` in your snippets. Use plain locals and
  reflection.
- **`GameBootstrap.Player` is briefly null during a level rebuild**, so
  readings taken mid-`PlayLevel` are transients, not bugs. Let a build
  settle, then re-poll.
- **`TeleportTo` sets `lastGroundedTime`**, so `grounded` stays true briefly
  after a teleport — you cannot produce an airborne state that way. Test
  movement guards by invoking the private predicate directly (that is how
  `Perch.IsSettled` was verified).
- **"Particle Velocity curves must all be in the same mode" is
  PRE-EXISTING** — the untouched baseline emits it. Do not chase it.
- **Screenshots**: `manage_camera` with `include_image: true`. Images over
  ~200KB are saved to an artifact path you then `Read`. Camera-path captures
  EXCLUDE Screen Space - Overlay UI — use the default (no camera arg) to see
  UI.
- **Verify by looking, not only by reasoning.** Two of this scope's real
  bugs (a bench under a spinner's sweep; a sit that never fired) were found
  by measuring and by looking at a captured frame, not by reading code.
- A **`tools/run-tests-headless.sh`** path exists (no Unity/licence needed);
  the editor suite is authoritative.

**Parallel sessions share this repo.** Before editing, `git status` and
check mtimes. If a file is mid-flight by someone else, wait or work in a
different file. Working tree is often dirty — do not assume clean.

---

## 6. Recently closed — do NOT re-raise

These were investigated and resolved. Re-opening them wastes a session:

- **Haptics on movement** — removed (`69b1cbb`); D-1 now governs. Only the
  wind-ride texture remains in `PlayerController`, which is explicitly allowed.
- **The perch "place to sit" gap** — the sit is now real (`fe866f5`); Pip
  eases into the pose on arrival. **Do not add a prompt** — banned with a
  reason in the directives doc.
- **`Fx.Burst` missing particle clamp** — fixed with the other two
  one-shots; the test that had been silently passing is now sensitive
  (proven by temporarily removing the clamp).
- **Golden gem hidden at/behind the exit** — fixed (placement now enforces
  a minimum portal distance); `DailyGem` had the same class of defect plus a
  worse one, both swept (`5f0c789`).
- **Hit-stop vs pause interplay** — reviewed twice, verdict PASS.
- **Docs claiming shipped work was unbuilt** — corrected.

---

## 7. What to do next (honest state)

**Your queue is nearly empty of buildable work.** Both remaining items are
gated on the content pause (40 levels is a user decision):

1. **Signal law beyond the golden gem** — apply the taught-signal pattern
   (diegetic cue, no UI marker) to the *wonder* beats (prism gates) **if the
   content door reopens**.
2. **The Guardian Games (Pack 14)** — see-saws exist; the carousel
   set-piece is canonical in `docs/Movement-Two-Story.md`. Shelved.

**The highest-value work available right now is not more features — it is
evidence.** `docs/Feel-Log.md` has **five open questions waiting on a
playtest verdict** (HUD scrim strength, checkpoint haptic weight, hazard
legibility against the violet Bell Towers sky, the photo shutter sound,
snow ambience). Those are judgement calls made without the user's senses.

So: **ask the user to play and give verdicts before building anything new.**
Record each verdict in the Feel-Log with their words and the reason. A
verdict that generalises becomes a directive; one that is local stays as a
note so it is not "fixed" back later.

If the user wants building instead, the tools to check first are:
- `tools/check-motion.sh` (motion linter)
- `tools/check-directives.sh` (protected rules — CI runs it)
- `tools/LevelAudit/run.sh --hazards` (spinner comfort instrument)
- `Assets/Editor/BudgetProbe.cs`, `VisualBaselineProbe.cs` (new this session)

---

## 8. How to hand off cleanly when you are done

1. Update `docs/Fun-Creativity-Directives.md` — the inventory table for
   anything new, the queue for anything shipped, the rejected list for
   anything you ruled out **with the reason**.
2. Update `docs/Feel-Log.md` if the user gave any verdicts.
3. Update `HANDOFF.md` with a dated session entry.
4. Update this file's §3 table and §6 (recently closed).
5. Commit per coherent unit of work, verified green first. Report real
   numbers, and say plainly what you did **not** verify.
