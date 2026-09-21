# RESEARCH.md — applied & curated findings (2026-09-17, deep-research session)

Four research tracks (game feel, level design grammar, mobile UX/accessibility,
adaptive audio/haptics) ran against the live codebase, filtered by the design
bible. This file records what was ADOPTED NOW, what's queued (ADAPT), and what
was deliberately SKIPPED — with sources. Backlog items are written so a future
session can pick them up without re-researching.

## ADOPTED THIS SESSION (all shipped in the "research pass" commit)

### Real bugs found by research, fixed
1. **Jump fired on finger-lift** — uGUI `Button.onClick` releases on pointer-UP,
   adding ~80 ms to the game's most-pressed input (XAG 109; Swink, *Game Feel*).
   TouchControls now uses an `IPointerDownHandler` — jumps register on press.
2. **Fly mode was effectively broken on touch** — nothing tracked "held", so
   Gloomfang rose only for the 0.15 s buffer window per tap. `JumpHeld` now
   feeds fly mode (and variable jump height).
3. **Interrupted touch bricked the joystick** — if the OS swallowed
   `TouchPhase.Ended` (call, notification shade), the finger id stayed claimed
   forever. `TouchControls.ResetInput()` runs on pause/focus loss.
4. **No auto-pause on interruption** — Android froze the frame but the game
   stayed `Playing`; resume dropped you mid-air. `GameManager.
   OnApplicationPause/Focus` now pauses and resets input (XAG 113; Hodent).
5. **Heart pickup shared the hazard's exact red** — same-hue pickup vs lethal
   hazard reads as one thing (worst for red-green colourblind players, ~1 in 12
   men). Hearts are now reward-gold (`ArtLib.HeartGold`), burst included.
6. **Haptic effects semantically inverted** — v1.9.1 mapped Light→EFFECT_CLICK,
   Medium→EFFECT_TICK; Android's scale is TICK < CLICK < HEAVY_CLICK. Remapped
   correctly; win now uses EFFECT_DOUBLE_CLICK (`Haptics.Fanfare`), death keeps
   HEAVY_CLICK (negative events strongest — AOSP guidance). 120 ms same-class
   retrigger cooldown prevents fused buzzes on gem trails.

### Feel & forgiveness wins (all research-backed)
7. **Gem magnetism** — gems within 2.2 m slide to Pip at 10 m/s: near-misses on
   the core verb count on fat mobile thumbs ("Juice it or lose it" school of
   input forgiveness).
8. **Variable jump height** — releasing a held jump cuts vertical velocity ×0.5
   once per jump (Celeste's forgiveness model, mild variant for a cozy touch
   audience). Never active in gusts/updrafts/flight; pad launches uncuttable.
9. **Terminal fall speed** (−28 m/s) — long drops no longer build unbounded
   speed; landing squash/dust thresholds read consistently.
10. **Landing overshoot spring** — squash decay is now an underdamped spring
    (stiffness 90, damping 12): landings rebound slightly past neutral instead
    of exponentially dying — classic follow-through.
11. **Camera vertical soft zone** — the camera holds its height during hops and
    only chases outside a ±2.5 m window (Itay Keren's GDC camera talk: SMW-era
    "platform snap"). Landing heights now read true; the horizon stays calm.
12. **Camera-shake setting** groundwork — `SaveSystem.ShakeOn` + early-out in
    `CameraFollow.Shake` (XAG 111 motion comfort; settings row UI is queued,
    see backlog).
13. **Left-handed touch layout** — `SaveSystem.LeftyOn` mirrors the joystick
    capture zone and jump anchor (XAG 116-lite; the two-action layout makes a
    full remap pointless).

### Audit suite grew with the game
14. Three new pure-data tests: `EveryCheckpoint_StandsOnAPlatformTop`
    (checkpoints previously had ZERO coverage), `EveryGustExit_HasALanding`
    (a gust that dumps you into the void is a death trap disguised as a ride),
    and echo bridges are now standable tops in `StandableTops` (a Bell Towers
    gem sits on a tone-solid bridge — legitimate). `LevelCount` floor raised to
    24. Suite: 14 tests over all 30 levels.

## ADAPT — queued backlog (researched, ranked, not yet built)

> **Status note (2026-09-21):** this list is now largely SHIPPED. Kept
> verbatim below as the research record — the ticks show where each idea
> landed. It had gone stale enough to mislead a new session, which is its
> own hazard: treat this section as history, not as a to-do.
>
> DONE: combo pitch-ramp (`AudioManager.PlayPickup`), death/win ducking +
> tonic restart (`AudioManager.DuckFor` / `RestartMusicAtTonic`),
> gust→music phase-lock (`AudioManager.GetMusicPhase`), checkpoint cadence,
> parameterized music intensity (`AudioManager` melody crossfade), milestone
> chime + ring, skid dust, wind streaks, gust-ride haptics, HUD gem pulse,
> D11 string table (`Strings.cs`), weather remixes (`Remixes.cs`),
> golden-gem gate (`GoldenGem.cs`), forward-pass audit
> (`LevelReachability.cs` + `tools/LevelAudit`).
> OPEN: bell/echo/mirror index-integrity tests, `Dormant` foreshadowing,
> the optional `string Phase` twist field.

**Audio (highest-value queue; most files were mid-flight in Pack 9/10 work):**
- **Combo pitch-ramp on pickups** (XS): streak counter in
  `AudioManager.PlayPickup`; freq × `2^(min(streak,12)/12)`; the `noteCache`
  absorbs it. Mario 1-up chain / Sound Shapes pattern; fits "gem trails are
  songs" perfectly. Reset chain on death.
- **Silence + ducking on death/win; tonic restart on respawn** (S): duck music
  ~35% before PlayDie/PlayWin; restart pad at clip offset 0 on respawn so it
  lands on the tonic; the win arp becomes a true cadence in silence (Phillips'
  "contrast" principle; Mario's music-dropout-on-death is the genre grammar).
- **Gust→music phase-lock** (M): GustZone's private `t` accumulator drifts from
  the music loop forever. Drive gusts from `MusicSynth`'s musical phase (or
  `musicSource.time` — the clip is exactly 4 chords) so every gust onset lands
  on a chord boundary; add `SfxSynth.NoiseSwell` (filtered noise + chord-root
  sine) so gusts audibly "play the chord". This IS the Pack 8 signature moment
  in DESIGN.md.
- **Checkpoint cadence** (S): restart the pad at the dominant chord on
  checkpoint → resolves to tonic under the checkpoint arp.
- **Parameterized MusicSynth** (M–H, strategic): render 3 intensity variants
  per mood and crossfade (pragmatic) or an `OnAudioFilterRead` streaming driver
  with 4 gained voices (pad/sub/pulse/shimmer): menu/explore/near-death states,
  speed-following tempo (slow-and-dark for danger, not fast-and-frantic — cozy
  tone preservation).

**Feel deep cuts (S each):** haptic texture while riding gusts (30/50 ms
waveform, amp ~60, cancel on exit — state-haptic, latency-tolerant per Android
docs); streak ticks escalate one tier per 3-gem combo; speed streaks during
gust glides; turn-around skid dust; milestone chime every 10th chained gem.

**Level design grammar (for packs 11+; sources: kishōtenketsu analyses,
Celeste B-sides, Extra Credits on backtracking):**
- **The Twist (ten)**: a fourth beat beyond teach/develop/combine — reuse a
  mechanic so its MEANING flips without difficulty rising (e.g., a gust lane
  over a sleeping guardian's bed: waiting is safe but wakes him — arrive with
  the gust instead). Authoring convention; optionally a `string Phase` field.
- **Weather remixes** ("B-sides", best value/effort in the project): a
  `Remixed(LevelDefinition, Action<LevelDefinition>)` helper (~20 lines) turns
  shipped levels into new content (same geometry, new mood/weather/twist).
  Medals/ParTime recompute free; the audit suite playtests automatically.
- **Golden-gem gate**: one hidden golden gem per level (Daily Gem plumbing)
  unlocks the remix layer — Celeste's cassette model.
- **Dormant foreshadowing**: `bool Dormant` on BellSpec/GustSpec — a dead bell
  in Pack 8's nest answers Pack 9's echo (kishōtenketsu across packs).
- **Forward-pass audit** (60–80 lines): BFS over standable tops asserting
  route existence (subsumes orphan-island checks; prevents future backtracking
  pain). Also: pack-difficulty tripwire (`string Pack` field + max-spinner/gap
  monotonicity), sightline-from-rest-isle test, rise-gentleness test.
- Bell/EchoBridge/MirrorDoor index-integrity tests once those spec types
  settle (post-Pack-9 commit).

**UX (S each, next settings pass — UIManager was mid-flight):** settings rows
for Shake/Lefty; pause button 58→90 units, top-right corner; font floor
18–21→22+ units (toast 24→28); level-select rows are 22–28 dp tall — 3-per-row
regroup; safe-area inset for HUD/controls; HUD gem-counter pulse as the visual
partner of the pickup chime. **Skip list (with reasons):** full remapping,
colourblind LUT filters (fix contrast at the source instead), screen-reader
support, difficulty selectors, font-size slider.

## SKIPPED (researched, rejected — do not re-litigate)
- True jump wind-up (anticipation delay) — fights the jump buffer; input
  latency is the enemy of this game's identity (Sakurai's own trade-off note).
- Camera lookahead — ship the soft zone first; Keren flags extrapolation
  wobble on jumps; revisit only if long glides feel directionless.
- ~~Hit-stop on death~~ — **NO LONGER SKIPPED: SHIPPED.** It was deferred
  only because it touches `Time.timeScale` (pause interplay) while that file
  was mid-edit. It now lives in `GameManager` with exactly the guard design
  proposed here (≤150 ms, unscaled-time restore, never stacks with Escape
  pause) and was independently reviewed: `TickHitStop()` runs before every
  state gate, so the freeze can never outlive a pause or a respawn.
- Wonder-seed mid-level state flips — per-zone mood doesn't fit the per-level
  data model; defer until LevelDefinition grows zones.

## ROUND 2 — tooling & technology (same day, second pass)

Four tracks researched against verified project state (headless test
execution, Unity dev tooling, shipping/ops infrastructure, plus a
reconnaissance pass over the repo and a friction audit). Headline
finding, worth stating plainly: **the biggest wins were not new tools,
they were wiring up what the project already had** — and three of the
findings were latent bugs, not missing software.

### ADOPTED — verified working, CI green

1. **Version drift fixed at the root.** The version was hand-edited in
   five places and HAD ALREADY DRIFTED (code 1.27.0, HANDOFF 1.19.0,
   Steam-Deploy 1.22.1). `GemRush.version` at the repo root is now the single
   source; `EnsureShaders` reads it and *derives* the Android
   versionCode (major*10000 + minor*100 + patch, so 1.27.0 -> 12700,
   safely above the old hand-held 43 and monotonic by construction).
   `tools/check-version.sh` fails CI when a doc disagrees; proven to
   catch injected drift and pass when correct.

2. **30 of the 75 EditMode tests now run on EVERY push, with no Unity
   and no license.** The enabling discovery: UnityEngine's math is
   *managed* code, so a test asserting `Vector3.Distance` returns
   exactly 5 outside Unity. `tools/run-tests-headless.sh` compiles the
   level-audit suite against Unity module assemblies and runs it under
   Mono (~15 s); `tools/headless/HeadlessTests.csproj` does the same via
   the .NET SDK in CI, which is where it now runs. The 16 MB of
   reference assemblies are committed so CI needs no 3.6 GB Unity
   install. Honest scope: the other 45 tests need a live scene (UI
   hierarchy, RectTransform, instantiated GameObjects) or native APIs
   (AudioClip, Resources) and stay on the GameCI tag path, which still
   runs everything.

3. **The silent level-truncation bug is gone.** `LevelLibrary` used to
   pre-size its array from a hand-summed 13-term length expression —
   add a pack, forget the term, and levels VANISH from the game with no
   error anywhere. It now appends to a List, which cannot lose content,
   and a new test asserts the atlas region table tiles the level list
   exactly (proven to fail on a wrong Count).

4. **Multi-device deploy with verification.**
   `tools/deploy.sh` installs to every connected device and compares
   each one's installed versionCode against the APK's — automating the
   check that once caught a package-identity bug by hand. Verified live:
   it found a stale emulator (code 25), installed, and confirmed 43.

5. **Code-quality gate.** `.editorconfig` + Microsoft.Unity.Analyzers
   (pinned) ride the compile CI already ran, so findings cost no extra
   pipeline time and need no Unity asset-label setup. `dotnet format
   --verify-no-changes` is the new gate; its first run found 13 real
   formatting defects (delegate spacing, a statement joined onto a
   ternary line). Rules that fight Unity idioms are explicitly
   disabled, and line-ending enforcement was dropped after it produced
   hundreds of no-value errors.

### VERIFIED, one worry eliminated
- **Target SDK is already 36.** `AndroidTargetSdkVersion: 0` means
  "automatic", so the real value was unknown — and Google requires API
  36 for new apps from 31 Aug 2026. `aapt` confirms the built APK
  targets 36, so the deadline is NOT a risk. No action needed.

### SKIPPED (researched, rejected — do not re-litigate)
- **Luban / Datra / Tiled / Blender level authoring** — every one
  requires imported assets or a heavyweight schema ecosystem; this
  project's levels are already clean C# data and the existing
  `tools/LevelAudit` is the right shape. A generator would add a
  dependency to solve a problem the audit suite already solves.
- **Maestro** — drives the accessibility tree, and Unity renders to one
  GL/Vulkan surface exposing none, so it cannot see the game at all.
- **AltTester** — GPL-3.0 plus a 2026 move to EUR 75/month for the tier
  that matters, for capability the 14 in-editor probes already cover.
- **release-please / semantic-release** — solve version
  *determination*; this project needed version *collapsing*. A ~30-line
  custom fix beat both.
- **Burst / Unity.Mathematics** — documented scheduling overhead that
  can make small workloads slower; textbook premature optimisation for
  tens of objects.
- **Roslyn source generators** for the duplicated probe rigs — the
  generator route needs a separate project, an external build and asset
  labelling; a plain shared-base-class refactor is strictly better and
  cheaper.
- **Unity Diagnostics / Firebase / Sentry crash reporting** — deferred
  by the owner's choice this round. Diagnostics remains the
  recommended first step (no SDK, no manifest, works on sideloads).
- **Play package-name change** (`com.DefaultCompany.GemRush3D`) — a
  real decision, deliberately left to the owner: it is permanent once
  published but costs one save to change today.

## Source index (key)
Celeste & Forgiveness (Maddy Thorson) · Itay Keren "Scroll Back" GDC 2015 ·
"Juice it or lose it" (Jonasson & Purho) · Sakurai's Creating Games episodes ·
Xbox Accessibility Guidelines (XAG 102/103/106/109/111/113/115/116/117) ·
Game Accessibility Guidelines · WCAG 2.2 (1.4.11, 2.5.5/2.5.8) · Material/
Apple HIG target sizes · Machado 2009 CVD simulation (run locally on ArtLib) ·
Hoober 2013 grip research · Celia Hodent (The Gamer's Brain) · Android haptics
UX docs (TICK<CLICK<HEAVY_CLICK, ≥1.4× distinguishability, sentiment rules) ·
Winifred Phillips GDC talks (vertical layering, contrast, diegetics) ·
Adaptive music history (iMUSE, Spore, Ape Out, Rez, Space Invaders) ·
Eurogamer/MCV kishōtenketsu (Hayashida) · Celeste wiki B-Sides · SMU Guildhall
retraversal thesis.
