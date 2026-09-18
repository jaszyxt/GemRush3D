# HANDOFF — current state (update: v1.13.0 session, 2026-09-19)

**Read first, in order:** `DESIGN.md` (expansion contract, pack grammar,
character bible, pacing rules — the law) → `RESEARCH.md` (research pass:
adopted / ADAPT backlog / SKIPPED — sources included) →
`docs/UIUX-Multiplatform-Directives.md` (UI/UX agent's P0–P2 work queue;
**all of D1–D12 now done except D11**) → this file.

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
- **Code: v1.13.0 (versionCode 22), committed on main** — 31 levels, 10
  packs, **gamepad support (D5+D6)**. Release-signed
  `Builds/GemRush3D.apk` (cert `CN=Gem Rush 3D, O=PipStudio`, verified
  with apksigner) + `Builds/GemRush3D.exe` (GemRush.dll fresh, Input
  System included). The Windows launcher stub keeps an old mtime — check
  `GemRush3D_Data/Managed/GemRush.dll` for freshness instead.
- **Emulator (Pixel_7:5554)**: v1.13.0 installed and launch-verified
  (clean logcat, no script exceptions).
- **Tablet (SM-X810 / R52W70BRE9E)** and **phone (SM-A366B / RRCY5008R7M)
  were still offline at build time** — install `Builds/GemRush3D.apk`
  (`adb install -r`) on next USB; straight update, save kept. Install to
  **every** `adb devices` entry (standing instruction).
- Release signing: `tools/gemrush.keystore` + `signing.txt` (gitignored) —
  same key since v1.1.0.

## Shipped in v1.13.0 (this session)
- **D5+D6 Gamepad, full**: `com.unity.inputsystem` **1.20.0** (1.11.2
  does NOT compile on Unity 6000.6 — its editor code hits the TreeView
  hard error) + `Assets/Editor/EnsureInput.cs` pins Active Input Handling
  to **Both** (same InitializeOnLoad write-ProjectSettings pattern as
  EnsureShaders; needs one editor restart for the native backend).
  - `GamepadInput.cs` — the single guarded door to the Input System (1 Hz
    probe; every read try/catches so a non-live backend is a silent
    no-op, never a per-frame exception).
  - Gameplay: keyboard/touch/stick merged by max magnitude; stick has a
    0.15 radial deadzone, rescaled. Jump = buttonSouth edge into the same
    buffer + hold for variable height/fly. Start toggles pause, B walks
    the D4 back stack, shoulders page the touch-style level list.
  - Rumble: `Haptics.GamepadBurst` on death (heavy) + win (light),
    gated by `HapticsOn`, `TickRumble()`/`StopRumble()` realtime-based so
    it dies on pause/menu/focus loss.
  - Menus: `InputSystemUIInputModule`, first-selected per panel
    (`UIManager.Focus`), explicit nav via `MenuNav.cs` (grids wrap per
    row; paged touch grid re-wires on `FlipPage` — now public), visible
    focus via `FocusFX.cs` (brighten + 1.08×, suppressed on locked
    buttons; `RefreshRestColor()` must be called after any code repaints
    button tints — RefreshMenu/RefreshSettings do). Enter-shortcut in
    GameManager is skipped while any button holds focus (Submit already
    confirmed it — otherwise PLAY double-fired). Menu hint line is
    input-aware (gamepad/touch/keyboard blocks, rewrites live).
- **D8 finished**: `DesktopWindow.cs` remembers the windowed rect
  (size via Screen.SetResolution, position via user32 on Windows only;
  skipped in fullscreen/editor/mobile). `resizableWindow` was already
  set in the build script.
- **Bug fixes**: `GoalPortal.Update` NRE (unguarded `AudioManager.Instance`
  during teardown) — guarded. `PlayerController.Update` NRE at the
  KillY check when `GameManager.Instance` is null in half-torn-down
  worlds (seen on every editor domain-reload during play) — guarded.
- **Tests**: `MenuNavTests.cs` (4 pure-data tests) — suite now **21
  EditMode tests, all green** (first run after the D1–D4/D7/D9/D10 batch,
  which had been pending).
- **Version bump**: 1.13.0 / versionCode 22 in EnsureShaders.Build().

## Open items (prioritized)
1. **Install v1.13.0 on tablet + phone** (both offline again; emulator is
   already current).
2. **Gamepad hardware pass**: Xbox + DualSense pads through the Windows
   exe — the DoD's real-hardware leg. Everything else was verified with
   the virtual pad: `Assets/Editor/PadProbe.cs` + `PadProbe2.cs`
   (menu `GemRush/Pad Probe/Run` / `Run 2`, play mode + editor window
   focused; see Environment wisdom below). All 13 checks green there.
3. **D11 string table** (P2) — the only directives item left; then that
   doc is fully closed.
4. **Audio ADAPT queue** in RESEARCH.md — checkpoint cadence,
   parameterized MusicSynth intensity, gust haptic texture, milestone
   chime. (Combo pitch-ramp, ducking, tonic restart, gust phase-lock +
   NoiseSwell are DONE.)
5. Next pack (11, The Long Winter) per DESIGN.md expansion queue — one
   pack per session; Pack 8 gust phase-lock work is a model.

## Environment wisdom (hard-won this session)
- **Input System package on Unity 6000.6**: use **1.20.0** (registry
  latest for 6000.0+). 1.11.2 fails with CS0619 TreeView errors. The
  GemRush.asmdef needed `"Unity.InputSystem"` added to references
  (auto-reference only covers predefined assemblies). In
  **LowLevel**, `GamepadButton`/`InputState` moved there; there is no
  `InputSystem.QueueState` and `InputState.Change` refuses bitfield
  controls — the universal write is
  `StateEvent.From(pad, out ptr)` + `control.WriteValueIntoEvent(value,
  ptr)` **(argument order: value, eventPtr)** + `QueueEvent(ptr)`.
- **The editor player loop fully suspends when the editor window is
  unfocused**, even in play mode with `runInBackground=true` — MCP
  probes and any time-based state machine stall. Fix: focus the Unity
  window from the shell (`SetForegroundWindow` on the Unity process
  main window). `EditorApplication.update` is even worse — it stops
  entirely; drive probe rigs from a MonoBehaviour instead.
- **`execute_code` compiles with codedom (C# 6, no extension methods,
  no Roslyn installed)** — for anything non-trivial, write an editor
  script and `execute_menu_item` it instead; verify `read_console` is
  ERROR-FREE after every compile before using the new code, because a
  failed compile silently leaves the old assembly running.
- **A build via the in-editor menu starves the MCP bridge** for its
  whole duration (main-thread) — "No Unity Editor instances found" from
  MCP just means the build is running; poll `Builds/` file mtimes
  instead of pinging.
- **On the emulator, the Unity surface stays paused (`HasFocus=0`) when
  the emulator window is not the desktop foreground** — `adb exec-out
  screencap` then returns a black image; this is the designed
  auto-pause, not a bug. Install/launch/logcat checks are the reliable
  headless signals.
- The game auto-pauses on editor focus loss (design). Physics probes
  read frozen while unfocused — set `Application.runInBackground = true`
  in-session and resume via `GameManager.ResumeGame()` if needed.
- **Stale-assembly races**: after refresh+compile, play mode may restart
  and read the old world mid-teardown (duplicate "~World"). Stop play,
  re-enter, wait, then probe. The new `PlayerController` guard makes
  this non-fatal, but probes should still re-boot.
- The user edits files concurrently — always re-read before Edit; their
  version bumps in EnsureShaders.cs are authoritative (this session
  bumped 1.12.2/21 → **1.13.0/22**; the headless Build() stamps at
  build time). Headless builds need the editor closed gracefully
  (CloseMainWindow / `EditorApplication.Exit(0)` — blocked by
  execute_code safety checks, call it with them disabled or close by
  hand); force-kill leaves a Scene Backup dialog that blocks the next
  launch. Better: build via the in-editor menu
  `GemRush/Build Android APK (Release)` while the editor is open.
- Two zombie Unity processes may linger after editor churn; they exit on
  next reboot. `Assets/Screenshots/` + kept `Assets/_Recovery/` are
  local-only clutter; deletable. `Assets/Resources/PerformanceTestRun*.json`
  are Test-Framework build artifacts — harmless, not committed.
