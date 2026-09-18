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
- Hit-stop on death — valuable but touches `Time.timeScale` (pause interplay)
  in a file the main session was actively editing; queued with a guard design
  (≤150 ms, unscaled-time restore, never stacks with Escape pause).
- Wonder-seed mid-level state flips — per-zone mood doesn't fit the per-level
  data model; defer until LevelDefinition grows zones.

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
