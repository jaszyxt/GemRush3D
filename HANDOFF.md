# HANDOFF — current state (update: v1.12.2 session, 2026-09-19)

**Read first, in order:** `DESIGN.md` (expansion contract, pack grammar,
character bible, pacing rules — the law) → `RESEARCH.md` (research pass:
adopted / ADAPT backlog / SKIPPED — sources included) →
`docs/UIUX-Multiplatform-Directives.md` (UI/UX agent's P0–P2 work queue,
D1–D12 with DoDs; D1–D4 + D8 shipped, D5–D12 open) → this file.

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
- **Code: v1.12.2 (versionCode 20), committed `9bc11c5` on main** — 31
  levels, 10 packs. Windows exe refreshed in
  `AppData\Local\Programs\GemRush3D\` + Desktop shortcut; emulator has it.
- **Tablet (SM-X810 / R52W70BRE9E)**: last installed **v1.10.0** — needs
  `Builds/GemRush3D.apk` (v1.12.2) on next USB; straight update, save kept.
- **Phone (SM-A366B / RRCY5008R7M)**: last installed **v1.10.1** — same,
  update on next connection. Old phone RFCW40396ZN (A34) is retired/offline.
- Release signing: `tools/gemrush.keystore` + `signing.txt` (gitignored) —
  same key since v1.1.0, so updates install over each other, save kept.

## Shipped in v1.10.0 → v1.12.2 (this stretch)
- **Pack 9 Bell Towers** (23): echo bells solidify hidden bridges while
  the tone rings (Bell/BellRig/EchoBridge + SfxSynth.BellTone).
- **Pack 10 Mirror Skies** (26–28): paired mirror doors (anti-ping-pong
  exit geometry: exit offset 1.7 units past the twin's trigger) +
  translucent mirror-Gloomfang (`Gloomfang.Create(..., mirror: true)`).
- **B-Sides** (29–31): `Remixes.Remixed(base, mutate)` — same geometry,
  new mood/twist; night versions of Gust Alley, First Blooms, The Ascent
  (+ the `LevelLibrary.TheAscent()` internal accessor for init-order safety).
- **Fixes from player reports** (root causes in git log): updraft
  midpoint-trap (rim-fade removed; wind = constant lift, ballistic
  pop-out), fly-forever after wind (windLift resets every physics step),
  props now keep clear of spinner sweeps, level grid auto-fits any count.

## Open items (prioritized)
1. **Install v1.12.2 on tablet + phone** (both offline at ship time; A36
   got v1.10.1, tablet v1.10.0 — `-r` update, saves kept).
2. **D5–D12 from the directives doc** — next: gamepad (Input System,
   Both handling; ship move+jump together), Settings-from-Pause (D9),
   canvas split (D7), text floors (D10).
3. **Audio ADAPT queue** in RESEARCH.md — checkpoint cadence,
   parameterized MusicSynth intensity, gust haptic texture, milestone
   chime. NOTE: combo pitch-ramp, ducking, tonic restart, gust
   phase-lock + NoiseSwell are DONE (AudioManager/SfxSynth/GustZone).
4. **Strip `[GustDebug]`/`[DoorDebug]` logs if any reappear** before
   release builds (search Scripts/ for "Debug]").
5. Run the EditMode audit suite in-editor once after big script batches
   (14 tests; CI csc green as of v1.12.2).

## Environment wisdom (hard-won)
- The **game auto-pauses on editor focus loss** (research-backed design).
  Physics probes read frozen while the editor is unfocused — this is NOT
  a bug. Set `Application.runInBackground = true` in-session, resume via
  `GameManager.ResumeGame()`, and expect focus transitions to re-pause.
- **Stale-assembly races**: after a refresh+compile, play mode may restart
  and probes read the old world mid-teardown (duplicate "~World",
  leftover gems). Re-enter play, wait, then probe.
- **The user edits files concurrently** — always re-read before Edit;
  their version bumps in EnsureShaders.cs are authoritative (v1.12.2/19
  at this writing; the headless Build() stamps version at build time).
- Headless builds **fail with exit 1 if the editor holds the project** —
  close it gracefully first (CloseMainWindow; force-kill leaves a Scene
  Backup dialog that blocks the next launch). Better: build via the
  in-editor menu `GemRush/Build Android APK (Release)` (includes the
  Windows exe pass) while the editor is open.
- Multiple Android devices: install to **every** `adb devices` entry
  ("any android device" is the user's standing instruction).

## Environment debt (harmless, known)
- Two zombie-ish Unity processes can linger after editor churn; they exit
  on next reboot. `Assets/Screenshots/` + the kept `Assets/_Recovery/`
  scene backup (from a force-kill) are local-only clutter; deletable.
- tools/*.png, device screenshots in tools/ are untracked scratch — fine
  per .gitignore.
