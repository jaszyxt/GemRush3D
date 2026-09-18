# HANDOFF — current state (update: v1.14.0 session, 2026-09-19)

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
- **Code: v1.14.0 (versionCode 23), committed on main** — **34 levels,
  11 packs**: Pack 11 "The Long Winter" (sunstone lantern + ice gates)
  shipped this session. Release-signed `Builds/GemRush3D.apk` (V2 cert
  `CN=Gem Rush 3D, O=PipStudio`, apksigner-verified) + `Builds/
  GemRush3D.exe` (check `GemRush3D_Data/Managed/GemRush.dll` mtime for
  freshness, not the stub exe).
- **Emulator (Pixel_7:5554)**: v1.14.0 installed + launch-verified (clean
  logcat). The emulator dies between sessions — just reboot the AVD and
  `adb install -r`.
- **Tablet (SM-X810 / R52W70BRE9E) + phone (SM-A366B / RRCY5008R7M) are
  still waiting for an update** (tablet last had v1.10.0, phone v1.10.1).
  `adb install -r Builds/GemRush3D.apk` on next USB; same key since v1.1.0,
  saves kept. Install to **every** `adb devices` entry.
- Tablet/phone were also due the **gamepad hardware pass** (Xbox +
  DualSense through the Windows exe) — still owed; virtual-pad rigs
  (`Assets/Editor/PadProbe.cs`, `PadProbe2.cs`, `WinterProbe.cs`) cover
  everything else.
- **Auto-install to the laptop (user request, 2026-09-19):** every build
  now ends with `DeployWindowsInstall()` in the build script — the fresh
  Windows player is copied to `%LOCALAPPDATA%\Programs\GemRush3D\` and
  the Desktop shortcut `Gem Rush 3D.lnk` is (re)written to launch it, so
  each update is playable the moment the build finishes. Runs
  automatically at the end of `BuildAndroid.Build()`; standalone via
  `GemRush/Install Windows Build (Local)`. Locked-file conflicts (game
  running) warn instead of failing the build. The old manual copy at
  `Desktop\GemRush3D\` is STALE (v1.13.0) — safe to delete; the shortcut
  now points at the auto-installed copy.

## Shipped in v1.14.0 — Pack 11 "The Long Winter" (this session)
- **New mechanic (one spec + one piece + one builder loop, per the bible):**
  - `LanternSpec`/`IceGateSpec` in LevelDefinition + `LongWinter` flag
    (snow tops, snowfall, pale sky, cool grading, `SoundMood.Winter`).
  - `Lantern.cs` — shrine; wake by walking into it → `Lantern.Lit` (static)
    and a sunstone orb hovers at Pip's shoulder. Lit survives death (no
    rebuild). Shrines sit at (2.5, 0.5, 3) — NOT on the spawn point: the
    probe caught v1 of the level self-lighting Pip at spawn.
  - `IceGate.cs` — translucent ice wall (solid collider, never harmful).
    Melts in 1.1 s while Pip (lit) is within 3.4 XZ / 3.5 y; stays melted
    (level-lifetime); leaves a slush remnant. `MeltedCount()` +
    `TriggerCrystalMap(world, portal)` — the pack signature: on victory
    every melted spot grows a crystal shard in a wave from the portal
    (wired in GameManager.OnReachGoal next to the garden bloom).
  - `MusicSynth.Winter` — Am7/Fmaj7/Cmaj7/Gsus2 at 3.4 s chords (13.6 s
    loop, gust-hosting moods keep 8.8 s — Winter hosts no gusts), sparse
    music-box twinkles. New SFX: `PlayLantern`/`PlayMelt`/`PlayCrystal`.
  - `Snowfall.cs` — one looping 160-particle flurry over the course
    corridor; snow-topped platforms + frosted props in LevelBuilder.
- **Levels 32-34** (`LevelPackEleven.cs`): First Snow (teach, 12 gems,
  zero hazards) → Frozen Fountains (develop: melt while hovering in an
  updraft, ferry carries Pip through a gate, 13 gems) → The Crystal
  Summit (combine: climb + napping guardian + heart + finale, 14 gems,
  milestone line). Winter palette per level.
- **Audit suite grew to 23 tests, all green**: `EveryIceGate_HasTheLightFirst`
  (shrine ahead of every gate on the route, gates on the course) and
  `WinterLevels_CarryTheFullKit` (winter ⇒ Winter mood + lantern + gates).
- **Play-verified**: `Assets/Editor/WinterProbe.cs` (`GemRush/Winter
  Probe/Run` in play mode) — 9/9: level loads, 2 gates + shrine built,
  lantern starts cold, wakes on contact, gate melts, melt persists,
  crystal map runs clean.
- **Version 1.14.0 / versionCode 23** in EnsureShaders.Build().

## Open items (prioritized)
1. **Install v1.14.0 on tablet + phone** (next USB; emulator already
   current).
2. **Gamepad hardware pass** (Xbox + DualSense via Windows exe).
3. **D11 string table** — last open directives item.
4. **Audio ADAPT queue** (RESEARCH.md): checkpoint cadence, parameterized
   MusicSynth intensity, gust haptic texture, milestone chime.
5. **Pack 12 — The Aurora Festival** (DESIGN.md Weather Atlas): aurora
   ribbons as ridable light-bridges; festival concert finale. Consider
   the research backlog's "dormant foreshadowing" (`Dormant` flag) when
   authoring — a dead bell/a sleeping aurora answering an earlier pack.
6. Also from DESIGN.md's compounding systems, still untouched: Daily Gem
   is live, but Pip's shelf / atlas screen / photo mode are future work.

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
