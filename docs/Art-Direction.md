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
| `Gold` | (1.00, 0.84, 0.25) | **every reward**: hearts, bells, stars, daily gift, UI accents | the reward gold family |
| `Air` | (0.65, 0.92, 1.00) | updrafts, mirror panes, Gloomfang's spark | air/mirror substance, always faded |
| `CheckpointOff/On` | grey → green | checkpoint state | the only state-change color pair in the world |
| `CloudWhite` | (0.97, 0.98, 1.00) | clouds | always alpha-faded (0.3–0.45) |

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
- **Gamepad focus style (reserved for D5, art-owned spec):** selected
  button brightens ~25% toward white and scales to 1.06 (same spring as
  the press dip); exactly one focus highlight visible per panel; the
  focus ring never uses hue alone (scale + brightness both change).
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

## 10. Change log

- **2026-09-19 (art audit v1)** — consolidated seven gold literals into
  `ArtLib.Gold` and four air-blues into `ArtLib.Air`; HUD Lives counter
  moved to gold (heart identity); win stars are now painted five-point
  stars with an audio-synced landing pop; fixed the mirrored level-name
  sign; replaced EchoBridge's Built-in Standard material with the URP
  palette material (device-crash risk); clamped `SafeArea` against
  oversized safe-area reports; iOS haptics branch (D12); quotes/README
  counts reconciled with the real 28 levels / 10 packs.
