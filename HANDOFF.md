# HANDOFF — current state (update: v1.19.0 session, 2026-09-19)

**Read first, in order:** `DESIGN.md` (expansion contract, pack grammar,
character bible, pacing rules — the law) → `RESEARCH.md` (research pass:
adopted / ADAPT backlog / SKIPPED — sources included) →
`docs/UIUX-Multiplatform-Directives.md` (UI/UX work queue; **all done
except D11**) → this file.

## Working agreement with the user (SOP — do not regress)
1. **Plan → research → solution → THEN simulate.** No trial-and-error in
   the editor; one clean verification cycle per change.
2. **Agile increments**: one milestone per session, DoD = compiles clean →
   play-verified → dual build (APK + Windows exe) → signature-verified →
   README true.
3. **Pacing is law**: difficulty never rises between packs; new mechanics
   are introduced alone, combined later; hearts before hard stretches.
4. The user edits files in parallel sessions — **re-read before writing**;
   their version bumps and edits are authoritative.
5. Parallel subagents work by strict file ownership (they shipped the
   UI/UX batch successfully); give each agent exclusive files.

## Current shipped state
- **Code: v1.19.0 (versionCode 28), committed on main** — 40 levels,
  13 packs + B-Sides. Release-signed
  `Builds/GemRush3D.apk` (V2 cert `CN=Gem Rush 3D, O=PipStudio`) +
  `Builds/GemRush3D.exe` (check `GemRush3D_Data/Managed/GemRush.dll`
  mtime for freshness, not the stub exe).
- **Laptop auto-install worked unattended this build**: the deploy step
  copied v1.15.0 to `%LOCALAPPDATA%\Programs\GemRush3D\` by itself and
  retargeted the Desktop shortcut. Byte-compare verified. This is the
  expected post-build state for every future update.
- **Emulator (Pixel_7:5554)**: v1.15.0 installed. The emulator dies
  between sessions — reboot the AVD, wait for `sys.boot_completed=1`,
  then `adb install -r`.
- **Tablet (SM-X810 / R52W70BRE9E) + phone (SM-A366B / RRCY5008R7M) are
  still waiting for an update** (tablet last had v1.10.0, phone v1.10.1).
  `adb install -r Builds/GemRush3D.apk` on next USB; same key since
  v1.1.0, saves kept. Install to **every** `adb devices` entry.
- Tablet/phone were also due the **gamepad hardware pass** (Xbox +
  DualSense through the Windows exe) — still owed; virtual-pad rigs
  (`Assets/Editor/PadProbe.cs`, `PadProbe2.cs`, `WinterProbe.cs`,
  `AuroraProbe.cs`) cover everything else.

## Shipped in v1.15.0 — Pack 12 "The Aurora Festival" (this session)
- **New mechanic:** `AuroraRibbonSpec` + `AuroraRibbon.cs` — flowing
  light-bridges. **Subclasses MovingPlatform** (Velocity setter made
  protected), so PlayerController.TryRide's carry works unchanged; the
  path adds a perpendicular sway (tapered to zero at the path ends so
  boarding is reliable) on top of the eased travel. Visuals: translucent
  slab + underglow with a slow hue drift through the aurora palette.
- **`SoundMood.Festival`** — bright C-G-Am-F pad with high bell sparkles;
  `AuroraFestival` flag → dusk sky/fog per level + `AuroraBand.cs`
  decorative aurora strips overhead (idempotent "AuroraBands" child).
- **The concert finale** (level 37): one lap through every mechanic in
  the atlas (movers, sleeping guardian, updraft, gust, bell + echo
  bridge, mirror door, lantern + gate, ribbons x2). `PlayConcert()` — a
  baked bell-choir cadence — plays under the win fanfare; clearing it
  sets `SaveSystem.AuroraUnlocked`, and `GameManager.ShowMenu` spawns
  the **permanent aurora over the menu** forever.
- **Milestone chime** (RESEARCH feel backlog): every 10th chained gem
  plays a tiny bright triad (`PlayMilestoneChime`, cached clip 9500).
- **Levels 35-37** (`LevelPackTwelve.cs`): Festival Lights (teach,
  12 gems) → Ribbon Dance (develop: sway + climbing ribbon + napping
  guardian + heart, 13 gems) → The Festival Finale (combine, 16 gems,
  2 hearts, milestone line, AuroraUnlock).
- **Audit suite 25/25**: `StandableTops` now samples ribbon paths
  (start/mid/end, sway folded into extents); `NoOrphanIslands` accepts
  ribbon ends as neighbor justification; new tests
  `FestivalLevels_CarryTheRide` + `OnlyTheFestivalFinale_UnlocksTheMenuAurora`.
- **Play-verified**: `Assets/Editor/AuroraProbe.cs` (`GemRush/Aurora
  Probe/Run`) — 8/8 incl. the carry check (ribbon flowed 10.1 u, player
  drift 0.4 u) and menu-aurora spawn after the unlock flag.
- **Version 1.15.0 / versionCode 24** in EnsureShaders.Build().

## Player directives (2026-09-19, latest — these govern)
1. **The laptop install IS the verification target.** The emulator is
   retired from the loop (user: "so lagging"); the user plays the
   installed app (`%LOCALAPPDATA%\Programs\GemRush3D\`, Desktop
   shortcut). Verify there, not on the emulator.
2. **Content pauses at 40 levels.** Pack 13 closed the era at exactly
   40; from now on sessions focus on IMPROVING the game (feel, systems,
   polish), not new levels — unless the player reopens the door.
3. A parallel session landed the "Delight Pass" (hit-stop, combo crown,
   streak visuals, win confetti, FOV kicks, skid dust, gust haptics,
   panel transitions — see `docs/Fun-Creativity-Directives.md`). It
   touches `Time.timeScale` near pause logic — worth a focused review
   in the improvement era.

## Fun & creativity session (2026-09-19, after the Delight Pass)
- **Hit-stop vs pause review: PASSED, item closed.** `TickHitStop()` is
  the first call in `GameManager.Update()` (before any state gate), so
  the 120 ms freeze always releases on the realtime clock; every
  transition calls `EndHitStop()` (flag-clear only — each transition
  sets its own timeScale), and `BeginHitStop()` refuses to engage
  unless `timeScale == 1`. Pause can neither inherit nor strand the
  freeze; budget is under the 150 ms cap.
- **"Secret Life" wave landed** (commit 2c54c25): idle Pip ladder
  (glance → wave → sit → sleep; Gloomfang drifts over as a shade),
  pokeable pentatonic flowers (`FlowerPoke`, 40/level cap, position-
  hashed notes — a given flower always sings the same note), Gloomfang
  giggle-raindrop that blooms a flower (10 s cooldown), checkpoint
  twirl. All comfort-gated, phone-budgeted, and inside the delight law
  in `docs/Fun-Creativity-Directives.md` (its "future queue" is the
  creativity backlog: photo mode, ghost runs, Pip's shelf, golden-gem
  remix gate).
- **The Movement-Two rainbow pack (prism gates) is consciously
  SHELVED, not forgotten** — the content pause at 40 chose The
  Homecoming as the era's close. If the player ever reopens the door,
  the prism-gate design brief lives in `docs/Movement-Two-Story.md`
  §Pack 13 and is fully compatible with the delight systems.
- Suite 27/27 on the combined tree (Delight Pass + Secret Life +
  Homecoming + D11 string-table work in flight); play-smoke clean.

## Shipped in v1.18.0 — Improvement pass (this session)
- **Hit-stop vs pause review**: the Delight Pass hit-stop state machine
  is CORRECTLY implemented — verified every interleave (Escape during
  freeze, focus-loss auto-pause, natural expiry on GameOver). No fix
  needed; guard design held.
- **Checkpoint cadence** (audio ADAPT queue): `AudioManager.
  RestartMusicAtDominant()` steps the pad to the loop's last chord on
  every checkpoint — it resolves home to the tonic under the chime.
- **D11 string table DONE — the UI/UX directives doc is now 100%
  closed**: `Assets/Scripts/Strings.cs` holds every user-facing string
  (constants + composed methods); UIManager, TouchControls (JUMP) and
  the lantern toast reference it. DoD grep clean. Note: medal names
  stay in `LevelDefinition.MedalFor` (exempt); region names are data in
  `LevelLibrary.Regions`.
- Improvement backlog for future sessions: parameterized MusicSynth
  intensity (menu/explore/near-death layering — the last big audio
  item), photo mode, ghost runs, Pip's shelf visuals, atlas polish.

## Shipped in v1.19.0 — Adaptive music intensity (this session)
- **The last big audio ADAPT item is DONE**: parameterized music
  intensity. Every mood now renders TWO synced layers — the full loop
  (chords + motif) and a "bed" layer (chords only, identical length and
  envelopes). `AudioManager` plays both sample-locked; the melody layer
  crossfades OUT over 2 s when the music "holds its breath" (Pip on his
  LAST life, or game over) and back IN when a heart returns. The chord
  bed never stops, so the loop stays seamless; ducking (death/win sting)
  applies to both layers; tonic/dominant restarts apply to both.
- Menu/won/complete keep the melody (celebration states). GetMusicPhase
  and the gust phase-lock are unaffected (musicSource.time keeps
  advancing even when faded).
- **Play-verified**: `Assets/Editor/MusicProbe.cs` (`GemRush/Music
  Probe/Run`) — 7/7: melody up at full lives, layers in sync
  (drift 0.000 s), melody drops at last life (vol 0.00 vs bed 0.55),
  bed keeps singing, melody returns on heart.
- Memory note: two loops per mood are synthesized lazily and cached
  (each 8.8–13.6 s mono — negligible).

## Shipped in v1.20.0 — Photo mode (this session)
- **PHOTO on the pause menu** (between RESTART and the bottom row): pauses
  the run (timeScale 0), hides the pause panel, and orbits a slow,
  height-breathing camera around Pip on the UNSCALED clock (`PhotoMode.cs`
  disables `CameraFollow`, restores + SnapToTarget on exit).
- Small bottom capture bar: CAPTURE / OPEN FOLDER / DONE. CAPTURE hides
  the bar for one frame, grabs the composited view via
  `ScreenCapture.CaptureScreenshotAsTexture()` (synchronous — the
  deferred CaptureScreenshot API silently failed to write), encodes PNG,
  and writes to `Pictures\GemRush3D\gemrush_<timestamp>.png` at 2x.
  Status line reports the path; OPEN FOLDER opens it in Explorer.
- B/Escape/BACK closes photo mode back into the pause menu (back-stack
  updated); Enter shortcut suppressed while the bar is up.
- **Play-verified**: `Assets/Editor/PhotoProbe.cs` (`GemRush/Photo
  Probe/Run`) — 5/5: opens, camera yields, PNG lands on disk, exit
  restores pause + follow camera. Probe deletes its test capture.
- **Parallel-session hazard (new)**: an active parallel session
  overwrote `Assets/Editor/PhotoProbe.cs` with a stale copy of
  UIManager.cs (2044 lines) mid-session; also shipped broken
  `VoiceLinesExporter.cs` + `VoiceOver.text` (compile blockers) which I
  minimal-fixed (VoiceEntry.text field added; forceToMono lowercase).
  If files go missing/stale again: check `git status` + re-verify
  affected files before compiling, and commit early.

## Shipped in v1.21.0 — Photo share card (this session)
- **CAPTURE now frames the scorecard INTO the shot**: a gold-framed card
  (level name, 3-star row, live time + medal preview + gem count, and the
  GEM RUSH 3D footer) is raised for the capture frame — every photo
  leaves with its story on it. Card lives top-center over the orbiting
  world; hidden the rest of the time.
- **Capture hardened**: deferred ScreenCapture.CaptureScreenshot
  silently failed to write (probe-verified); replaced with synchronous
  `ScreenCapture.CaptureScreenshotAsTexture()` -> EncodeToPNG ->
  WriteAllBytes. Works identically in editor and player, timeScale 0 ok.
- **git PUSH added to the milestone loop** (player directive): after
  each commit, push to origin/main. 15 commits were backlogged before
  the first push; now synced.
- Parallel-session note: their VoiceLines session shipped with two
  compile blockers (VoiceEntry.text missing, ForceToMono capitalization)
  — minimal-fixed in place. A stale-buffer write also clobbered
  PhotoProbe.cs with a copy of UIManager.cs (2044 lines); rewritten.
  If a file seems reverted/corrupted, check for parallel clobbering
  before debugging your own code.
- **Play-verified**: PhotoProbe 5/5 incl. real PNG on disk; share card
  visually verified (gold stars, framed card over the live world).

## Open items (prioritized)
1. **Install v1.17.0 on tablet + phone** (next USB; laptop + emulator
   superseded — laptop is primary).
2. **Gamepad hardware pass** (Xbox + DualSense via Windows exe).
3. **D11 string table** — last open directives item.
4. **Audio ADAPT queue** (RESEARCH.md): checkpoint cadence, parameterized
   MusicSynth intensity, gust haptic texture. (Milestone chime: DONE.)
5. **CONTENT PAUSED AT 40** (player directive). Improvement backlog for
   future sessions, in no fixed order: parameterized MusicSynth
   intensity (checkpoint cadence + gust haptic texture: DONE), D11
   string table, photo mode, ghost runs, Pip's shelf visuals, and the
   atlas screen's per-region polish (medal stamps art, region flavor
   lines). Creativity backlog: `docs/Fun-Creativity-Directives.md`
   future queue.

## Environment wisdom (hard-won, cumulative)
- **Unity editor launch race**: if old Unity processes are zombie-ing,
  a new launch exits immediately (ExitDontLaunchBugReporter, code 0)
  while the project lock is held. Kill/let them die, launch once. If
  MCP says "No Unity Editor instances found", the editor is closed or
  busy — check `tasklist` before assuming.
- **Build via in-editor menu starves the MCP bridge** (main-thread):
  "No instances found" mid-build just means it's working. Poll
  `Builds/` mtimes. Incremental Android builds after a code-only change
  can finish in ~90 s with warm caches.
- **The editor player loop suspends when the editor window is
  unfocused**, even in play mode with runInBackground=true — probe rigs
  stall until you focus the Unity window (PowerShell
  SetForegroundWindow on the Unity process). Drive probes from a
  MonoBehaviour, not `EditorApplication.update` (it stops entirely).
- **`execute_code` = codedom (C# 6, no extension methods, no Roslyn)**:
  write an editor script + `execute_menu_item` instead; ALWAYS
  re-check console for `error CS` after each compile — a failed compile
  silently leaves the OLD assembly running (cost: one full debug loop
  this session).
- **Emulator Unity surface stays black in `adb screencap` while the
  window lacks desktop focus** (`HasFocus=0` in logcat) — that's the
  designed auto-pause, not a crash. Trust install + logcat.
- **The emulator dies between sessions**; reboot the AVD
  (`emulator -avd Pixel_7`), wait for `sys.boot_completed=1`, install.
- Input System on Unity 6000.6 = **1.20.0** (1.11.2 CS0619 TreeView
  errors); GemRush.asmdef references `Unity.InputSystem`;
  `GamepadButton`/`InputState` live in `LowLevel`;
  `WriteValueIntoEvent(value, ptr)` argument order; `InputState.Change`
  refuses bitfield controls.
- The game auto-pauses on editor focus loss (design). Stale-assembly
  races after refresh+compile: stop play, re-enter, wait. The
  `PlayerController`/`GoalPortal` null guards make teardown non-fatal.
- The user edits files concurrently — re-read before Edit; their
  version bumps in EnsureShaders.cs are authoritative (now
  **1.14.0/23**). Headless builds need the editor closed gracefully;
  the in-editor menu `GemRush/Build Android APK (Release)` (APK + exe)
  is the better path while the editor is open.
