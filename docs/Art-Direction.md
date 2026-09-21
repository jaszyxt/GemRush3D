# Art Direction — Gem Rush 3D

**Owner:** art agent · **v1.0** · 2026-09-19
**The whole visual identity lives in code.** There are no textures, no
models, no prefabs, no imported anything — every surface is a primitive
with an `ArtLib` material, every sprite is painted at boot, every light
is a value in a script. That is a feature: the art style is *reviewed
like code and can never go missing from a build*. This document is the
single source of truth for what the game should look like and why.

---

## 1. Visual identity

**"A toy sky in warm light."** The realm is a cozy diorama floating in a
friendly atmosphere, menaced by weather that is sad rather than scary.

Five pillars — every visual decision is tested against them:

1. **Readable first.** Gameplay truth (hazard, reward, path, goal) must
   be legible from across the level, on a phone in sunlight. Style
   never outranks reading the course.
2. **One warm palette.** Everything shares the `ArtLib` palette; realm
   identity comes from *sky/fog/sun/grading*, never from recoloring
   gameplay objects. A gem is the same pink in every realm.
3. **Soft shapes, soft light.** Spheres over boxes for anything alive or
   friendly (clouds, trees, characters), rounded 9-slice UI, soft
   shadows, gentle bloom. Hazards are the only hard-edged reds.
4. **Motion is meaning.** Things that move tell the truth: hazards sweep
   and glow brighter when awake, rewards bob and sparkle, air shimmers
   and drifts. Nothing decorative moves fast.
5. **Juice follows physics.** Squash, stretch, overshoot and settle —
   the same spring grammar on Pip's body, the win-star pop and the
   button press dip. One motion language everywhere.

## 2. The palette (ArtLib is law)

All colors are defined once in `ArtLib.cs`. **Never inline a color
literal in a new script — reference `ArtLib`.** (The audit of 2026-09-19
removed seven copy-pasted golds and four air-blues; keep it that way.)

### Core identity colors

| Color | Value | Role | Rules |
|---|---|---|---|
| `Grass` | (0.36, 0.72, 0.34) | platform tops, props | the world's "ground truth" green |
| `Dirt` | (0.50, 0.37, 0.25) | platform bodies, mover undersides | always under grass, never beside it |
| `Stone` | (0.58, 0.60, 0.64) | neutral structure: posts, frames, pads | the "does nothing" color |
| `MoverOrange` | (0.90, 0.55, 0.18) | moving platform tops | kinetic = warm; slight emission (0.15) |
| `ElevatorBlue` | (0.28, 0.62, 0.88) | vertical movers | kinetic = saturated; never used decoratively |
| `HazardRed` | (0.85, 0.20, 0.15) | **spinner arms only** | see the exclusivity rule below |
| `GemPink` | (0.98, 0.30, 0.75) | gems, bounce pads, start flag | the "playful reward" family |
| `PortalCyan` | (0.20, 0.90, 0.95) | the goal portal (+ win burst) | one object owns one hue |
| `Gold` | (1.00, 0.84, 0.25) | **every reward**: hearts, bells, stars, daily gift, lantern sunstone, see-saw edge stripes, UI accents | the reward gold family |
| `Air` | (0.65, 0.92, 1.00) | updrafts, mirror panes, echo bridges, Gloomfang's spark | air/mirror substance, always faded |
| `CheckpointOff/On` | grey → green | checkpoint state | the only state-change color pair in the world |
| `CloudWhite` | (0.97, 0.98, 1.00) | clouds | always alpha-faded (0.3–0.45) |
| `Snow` / `Rain` | (0.93,0.95,0.99) / (0.72,0.82,0.95) | winter snowfall vs. rain | deliberately distinct — a flurry must never read as a shower |
| `Dust` | (0.90, 0.90, 0.90) | jump / land / skid kick-up puffs | warm-shifted off white so dust never reads as a passing glow |
| `IceBlue` / `FrostedLeaf` / `FrostedRock` | pale glacial family | The Long Winter: ice gates, frost props | "frozen" reads one way everywhere |
| `Trunk` / `Leaf` / `Rock` / `Bud` / `Wood` | prop families | platform dressing, garden buds, see-saw planks | `LongWinter` swaps leaf/rock for the frosted pair |
| `Aurora` (4-color array) | green→cyan→violet→pink | Aurora Festival ribbons, sky bands, finale | shimmer drifts through it **in order**, everywhere |
| `Pastels` (5-color array) | flower heads | platform flowers, poke flowers, bloom wave | one garden, one petal palette |

**Exclusivity rules (accessibility, non-negotiable):**

- **Red means danger, nowhere else.** `HazardRed` appears only on
  spinner arms (and the death burst). Hearts, bells and stars are gold —
  never red — so pickups and dangers never share a hue for red-green
  colourblind players.
- **Gold means the game gives you something.** Hearts, echo bells, win
  stars, the daily gift, the HUD lives counter, menu titles and the
  DAILY level button all share `Gold`.
- **Never signal state by hue alone.** Hazards also move and shine;
  the DAILY button also says "DAILY"; checkpoints change shape context
  (post + disc) as well as color.

### Realm skies (palette-as-data)

Levels set `SkyColor` / `FogColor` (fog always matches the sky — there
is never a horizon seam). The camera clear, the Backdrop gradient, the
parallax silhouettes and the fog all derive from those two colors, so
each pack recolors the *entire atmosphere* for free while gameplay
colors stay constant:

| Realm (packs) | Sky | Character |
|---|---|---|
| Day (1–2) | default blue | the home sky |
| Ascent approach (3·1) | deep blue | height reads in the sky first |
| The Undercloud (3·2–3, `DarkRealm`) | near-black indigo | dim blue sun, 0.013 fog, −12 sat / +10 contrast grading |
| The Two Suns (4·1, 6) | peach-gold | sunset celebration lap |
| Sky Garden (7) | mint-green | Gloomfang's rain woke it |
| Far Isles (5) / Storm Chasers (8) | pale aerated blues | "thin air" — winds live here |
| Bell Towers (9) | dusk amber → lilac → violet | evening climbs; the last level is the darkest day realm |
| Mirror Skies (10) | glassy blue-greys | slightly wrong on purpose; MirrorGloomfang is 55% alpha |
| The Long Winter (11, `LongWinter`) | near-white glacial | props swap to frost variants; falling snow; lantern gold is the only warmth |
| Aurora Festival (12) | dusk violet | the aurora (green/cyan/violet/pink) is the only saturated thing; bands persist over the menu once unlocked |
| The Homecoming (13) | default day | deliberately home — no new sky, just wood and gold |
| B-Sides | nightfall deeps | remix skies of earlier levels, hours later in the day |

`PostFx` grades per realm: daylight gets +4 saturation/+4 contrast, the
Undercloud −12/+10. Bloom is global (intensity 0.85, threshold 0.95,
warm tint) so **only emissive things glow** — if it blooms it matters.

## 3. Materials & lighting

- **One material factory**: `ArtLib.Solid(color, emission)` — URP/Lit,
  smoothness 0.35 always. Emission is the only "material" variation and
  it is *semantic*: it marks things that are awake, charged, or magical
  (gems 1.6, portal 1.4, bells 0.8, hearts 0.6, arms 0.25→1.0 as a
  guardian wakes).
- **Transparency** only through `ArtLib.SetFade` (surface type, blend,
  ZWrite, queue — the whole recipe, or it renders opaque). Echo bridges
  prove the pattern: alpha 0.06 = "whisper of a hint", 0.8 = solid.
- **One sun**, directional, soft shadows, per-realm color/intensity
  (day: warm 1.9 @ 48°; Undercloud: blue 0.95 @ 64°) + Trilight ambient
  tuned per realm. `GameBootstrap` owns defaults; `LevelBuilder.
  ApplyAtmosphere` retunes per level. No other realtime lights, ever —
  URP honours one directional anyway.
- **Anti-aliasing 4×, always on.** At this poly count it is the
  cheapest quality win on every target.
- **Determinism is an art rule**: props, clouds and buds seed from the
  level name (`System.Random`, never `UnityEngine.Random`) so a level
  looks *authored* — identical every run, different per level.

## 4. Characters

- **Pip** — orange capsule, white eyes with tracking pupils, brown
  nose-cube. Readability at any distance comes from the saturated
  orange against green/blue; expression comes only from pupil gaze.
  Squash & stretch: stretch ≤ 0.25 rising, squash ≤ 0.30 landing,
  underdamped spring (past-neutral overshoot), idle breathing sine.
- **Gloomfang** — vapor cloud of overlapping spheres at 78% alpha,
  opaque eyes + gold badge (the badge is the character's reward history
  made visible). View contract: anchored to the camera's right edge,
  never between camera and Pip. The mirror twin is the same body at 55%
  alpha. Playable Gloomfang uses the same `BuildBody` — they are twins
  by construction.
- **Guardians (spinners)** — stone post + red arm. The arm's emission
  tracks wakefulness (0.25 dozing → 1.0 awake): a sleeping guardian is
  *visibly* dim before it is audibly awake.
- **Nim** (Storm Chasers) — never shown directly; she is petals,
  giggles and a gust that arrives on the chord. Absence is her design.

## 5. VFX grammar

- **`Fx.Burst(pos, eventColor × 1.5–1.8, count)`** — every pickup,
  death, landing and teleport bursts in *the event's own color*. The
  burst is a chord, not a firework: it confirms which system just
  spoke. New events pick the palette color of what caused them.
- **Floating text** (`Fx.Popup`) is pale gold, rises 1.7u, dies in 0.9s.
- **Air is drawn faded and moving**: updraft wisps drift up and wrap,
  gust streaks sweep and brighten while blowing, petals drift always
  (the telegraph), clouds sway at 0.35/−0.25 so the sky has parallax
  against itself.
- **Motion grammar** — one spring: Pip's squash, the win-star
  slam-settle (scale 1.6→1.0 over 0.28s, timed to its ding), the
  button press dip (0.9→1.0 over 0.16s). If a new animation eases
  differently, make it argue.
- **Photosensitivity (audited 2026-09-19, see UI/UX doc appendix):**
  no effect exceeds ~1.8 Hz modulation, nothing flashes full-screen,
  bloom pulses are slow sines. Passes the ≤3 flashes/sec rule with
  margin. Keep new effects under it.

## 6. UI aesthetics

- **Panels**: dark navy-black scrims (0,0,0.05, ~0.55–0.8); mood-tinted
  only for emotional screens (win = green-black, game over = red-black).
- **HUD band**: the four readouts have no panel — they sit on the world —
  so a painted top-edge gradient scrim (`Fx.TopScrimSprite`, anchors
  0.90→1.0, non-raycast) darkens the band beneath them. It reads as lens
  falloff rather than a UI bar. Text keeps its outline on top of it.
  **Do not remove the scrim**: without it the winter and sunset realms
  drop white text to ~1.6:1 and the gold lives counter to ~1.02:1.
- **Buttons**: the rounded 9-slice circle sprite, `onColor` green fill
  (0.16,0.55,0.32 — chosen so white bold text holds ≈4:1), white bold
  labels, press-dip feedback, one universal tick sound. Toggle-off is
  `offColor` rust; locked is grey. **Do not darken fills** — brighten
  text instead (XAG 102).
- **Star iconography**: win stars are `Fx.StarSprite()` — a painted
  five-point star, white so the UI tints it: `Gold` earned, `starDim`
  unearned. Stars start dim and *land one by one*, each golding at the
  exact frame its ding plays. The reward moment is audio-led, visual
  second — keep them coupled.
- **Typography**: one font (LegacyRuntime.ttf, Arial fallback), one
  black outline (2,−2 @ 85%) on every text. Hierarchy by size/weight
  only; titles bold + gold, story italic + pale blue, HUD plain white.
- **Gamepad focus style (implemented in `FocusFX` per D5):** selected
  button brightens ~30% toward white and scales to 1.08; exactly one
  focus highlight visible per panel; the focus ring never uses hue alone
  (scale + brightness both change); non-interactable (locked) buttons
  take selection with no highlight — selection rests without pretending
  they are alive.
- Every screen parents under `SafeRoot` (`SafeArea`). The component
  clamps reported insets to 0..1 — some editors/devices misreport
  safe area larger than the screen, and unclamped anchors stretch the
  whole UI off-screen (found and fixed 2026-09-19).

## 7. Screens, orientations, quality tiers

- **Landscape-locked** (`LandscapeLeft`): the composition, camera
  framing (aspect-aware dolly-back to 1.5× on narrow screens) and UI
  bands are designed for landscape. Portrait is not a supported
  orientation; in a portrait window, match-height scaling correctly
  center-crops instead of squashing.
- **Quality has one axis: shadows** (Settings). Art must survive
  shadow-off — nothing load-bearing may depend on contact shadows.
  Post-processing, AA and bloom are always-on identity, not quality
  knobs. `SafeArea` + aspect camera + match-height UI carry device
  variance; there are no LODs because draw calls are counted in
  hundreds of primitives, not thousands of meshes.
- **The icon** (`EnsureShaders.PaintIcon`) is the identity compressed:
  Pip orange on grass green under a blue sky with two pink gems. New
  marketing surfaces should borrow its exact four colors.

## 8. Technical constraints (why the art looks like this)

1. **Zero imported assets** — new visuals are code or they don't ship.
2. **Pinned shaders**: every `Shader.Find` name must be in
   `EnsureShaders.RequiredShaders`, or it strips from device builds.
   Never reintroduce a Built-in shader lookup (`EchoBridge` used to
   build a `"Standard"` material — broken under URP, crashed device
   builds; fixed 2026-09-19). When in doubt, go through `ArtLib.Solid`.
3. **IL2CPP stripping**: destroy primitives' colliders by concrete type
   (`Props.DecorPrimitive` shows the pattern) or SphereCollider can be
   stripped out from under decor on device.
4. **Solid-color sky, not a skybox** — guaranteed identical on every
   GPU (a procedural skybox rendered dark on some devices).
5. **TextMesh reads from −Z** — the spawn sign must stay unrotated
   (rotating it 180° mirrored every level name on screen; fixed
   2026-09-19).

## 9. Adding a new mechanic — the art checklist

- [ ] Colors from `ArtLib` (ask: is it reward-gold, playful-pink,
      air-blue, kinetic-orange, neutral-stone? If none fit, add ONE
      constant to ArtLib with a comment saying why).
- [ ] Emission only if it's awake/charged/magical; bloom follows it.
- [ ] Telegraph before danger (petals → gust, giggle → blow, glow →
      wake); reward answered by a burst in its own color.
- [ ] Motion uses the shared spring (`Tweener`); one overshoot grammar.
- [ ] Collider-free decor via `ArtLib.Decor*` / `Props.DecorPrimitive`.
- [ ] Deterministic scatter: seed from the level name, `System.Random`.
- [ ] Stays clear of the play corridor and spinner sweep zones.
- [ ] Compile-checked against `tools/stubs/UnityStubs.cs` (add stubs
      for any new Unity API you touch).

- **UIs are measured, not eyeballed.** Layout claims in this doc carry
  numbers: the win stars sit at anchor y 0.525 so their bottom edge
  clears the stats text top by ~25 units at every aspect (verified
  in-engine; at the old 0.5 the centre star overlapped the "Level N"
  line). When you move a reward icon, re-measure the clearance.

## 10. Change log

- **2026-09-20 (art pass v4 — readability, haptics, motion, AV)** — study-driven
  from three audits (motion/animation, audio-visual coupling, readability).
  *(a)* **HUD readability (P1, verified visually):** the HUD panel was
  `alpha 0` — the four readouts sat on bare sky behind a 2 px outline. On
  the winter realm white text measured ~1.6:1 and the gold `Lives`
  counter ~1.02:1; a capture showed the counter effectively vanishing.
  Added `Fx.TopScrimSprite()` — a painted top-edge gradient, non-raycast,
  behind every readout — and verified the band reads cleanly on the
  worst-case realm. *(b)* **Haptics:** gem pickup (the core verb) had NO
  haptic and `Haptics.Medium()` was dead code. Now: `Light` on gem / jump
  / skid, impact-scaled `Light`/`Medium` on landing, `Medium` on
  checkpoint / heart / lantern (the rung between a tick and a death
  thud), `Fanfare` on completing the whole game — not just one level.
  *(c)* **Motion:** two real pause leaks fixed — the checkpoint twirl kept
  spinning behind the pause menu (verified: twirl clock holds at 0 through
  a pause) and `RestBeat` orbited the camera behind the win-screen menus;
  Pip's squash spring and `FlowerPoke`'s bounce now step one shared
  `Tweener.StepSpring` instead of hand-copied constants; one
  `ArtLib.HoverBobRate` replaces six hover frequencies that drifted out of
  phase; `FocusFX` tweens instead of snapping (it was the most-triggered
  motion and the only un-tweened one); three first-frame pops (crown, ice
  shard, raindrop flower) now set their entry pose before activation.
  *(d)* **Audio-visual:** photo mode has its own `Shutter` voice (it was
  reusing a win-screen star ding); crossing a star line now *sounds* like
  a milestone, not an ordinary gem; Winter gained a `Snow` ambience bed
  (the one weather system with none). *(e)* **Contrast:** the ice gate
  measured ~1.0:1 against the winter sky it lives in — fixed with an
  opaque frost RIM (silhouette, not hue; the palette family is correct)
  that melts with the pane. Verified NOT problems after in-engine checks:
  the portal (bloom-backed), and the whole `Air` family against wind-realm
  teal. *(f)* **Docs:** corrected the `GustZone` class comment (it still
  claimed a music phase-lock the Festival-Finale fix deliberately
  removed), the stale `OnTriggerStay` comment, and two `Audio-Design.md`
  mix figures that disagreed with the code.

- **2026-09-19 (art pass v3 — lens hygiene, win spacing, trail legibility)**
  — *(a)* **Lens:** new `CameraFollow.ResetLens()`, called by
  `SnapToTarget()` and `PhotoMode.Begin()`. The FOV kick/hold animates on
  the unscaled clock (deliberately, so it reads through hit-stop), which
  meant a wind-ride hold or bounce-pad kick stayed frozen into every
  photo-mode postcard and settled visibly on exit. Verified in-engine:
  with a hold freshly armed, entering photo mode leaves the lens at the
  neutral 60° baseline and exiting restores the follow with no residual.
  *(b)* **Win screen:** stars moved 0.5 → 0.525 (verified 25 units of
  clearance above the stats line, was overlapping); the star pop tween is
  token-guarded like the gem pulse so a stale pop cannot overwrite a
  re-shown panel. *(c)* **The reported gold/green trail:** not a rendering
  bug — `StarTrail` is a progress reward whose tier comes from per-device
  saves (gold < 15 stars, festival pink 15–29, aurora green 30+), so a
  phone and a laptop legitimately differ. The real defects were that the
  tier was illegible and could go stale: `StarTrail` is now a live
  component that re-tints `startColor` **and** the lifetime gradient in
  place when a milestone is crossed, and each tier announces itself once
  per device through the story toast (`Strings.TrailTierLine`, tracked by
  `SaveSystem.TrailTierAnnounced`). *(d)* **Palette:** `ArtLib.Dust` and
  `ArtLib.Rain` added; StarTrail tiers, GustZone streaks, Snowfall flakes,
  Rainfall drops and ConfettiSky's palette all reference ArtLib now.
  *(e)* **Stub rig:** filled eight long-standing gaps in
  `tools/stubs/UnityStubs.cs` (`Mathf.Approximately`/`Sign`,
  `Vector3.Scale/Min/Max`, `Renderer.enabled`/`shadowCastingMode`,
  `ParticleSystemRenderer.renderMode`, `Application.persistentDataPath`,
  `SystemInfo`, the `YieldInstruction` family, `PlayerPrefs`
  single-argument getters, `Transform.Rotate` with a `Space`) — the
  offline syntax gate now covers the whole game again instead of failing
  on scripts added since it last passed.

- **2026-09-19 (art audit v2 — post-content-era sweep)** — audited all
  content since v1 (packs 11–13, B-Sides, delight pass, Secret Life,
  photo mode, gamepad support): shader pinning, fade recipes, emission
  semantics, photosensitivity cadence and particle budgets all PASS.
  Consolidated the duplicated aurora palette (AuroraRibbon +
  AuroraBand) into `ArtLib.Aurora`, the duplicated petal pastels
  (Props + bloom wave) into `ArtLib.Pastels`, and the prop literals
  (trunk/leaf/rock/bud, see-saw wood) into named ArtLib constants —
  zero inline gameplay-color literals remain outside ArtLib. Echo
  bridges now use `Air` (their old literal was a near-duplicate).
  Bible updated: Long Winter / Aurora / Homecoming / B-Sides realms,
  extended palette table, FocusFX implementation note.
- **2026-09-19 (art audit v1)** — consolidated seven gold literals into
  `ArtLib.Gold` and four air-blues into `ArtLib.Air`; HUD Lives counter
  moved to gold (heart identity); win stars are now painted five-point
  stars with an audio-synced landing pop; fixed the mirrored level-name
  sign; replaced EchoBridge's Built-in Standard material with the URP
  palette material (device-crash risk); clamped `SafeArea` against
  oversized safe-area reports; iOS haptics branch (D12); quotes/README
  counts reconciled with the real 28 levels / 10 packs.
