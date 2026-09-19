# Fun & Creativity Directives

*Standing law from the fun & creativity office. Every agent building content for
Gem Rush 3D inherits this file. It extends `DESIGN.md` and `docs/Story-Bible.md`;
on story matters the Story Bible still wins, on delight matters this file wins.*

## The vision

**Make mastery visible. Make the sky alive. Ship the wonder.**

Research ground truth (see source index at the bottom): players feel delight when
(1) every action gets disproportionate, multi-sense feedback — "juice";
(2) the world reacts to *them*, even when nothing is required — Astro Bot's
"everything responds" principle; (3) mastery is *visible*, not just audible
(a rising pitch nobody can see is half a reward); and (4) surprise rides on top
of safety, never instead of it — cozy pillars: curiosity replaces pressure.

Gem Rush already teaches perfectly (kishōtenketsu, one new idea per pack, stated
as a feeling). Our delight budget goes to feedback and reactive life, never to
difficulty.

## The three deliverables every pack must ship

1. **One reactive-world moment** — something in the level responds to the player
   that didn't have to (flowers poke-chime, Gloomfang giggles, a guardian naps).
2. **One visible-mastery beat** — skill the player develops must become *seen*
   (combo streak counter, crown, milestone shockwave), not only heard.
3. **One secret** — a small optional discovery with zero punishment for missing
   it (Daily Gem law). Never FOMO. Never guilt. A gift that waits.

## Delight craft rules

- **Feedback is disproportionate.** A pickup is a sound *and* a burst *and* a
  popup *and* a HUD pulse. Every verb Pip can do deserves at least three senses.
- **The palette law is also a delight law.** Red stays hazard-only; gold is for
  every reward; new celebration colors come from `ArtLib` families, never inline.
- **Comfort settings are sacred.** Any new screen motion respects
  `SaveSystem.ShakeOn` if it moves the camera; haptics respect `HapticsOn`;
  nothing flashes above ~1.8 Hz (photosensitivity audit, UIUX directives).
- **Phone budget is sacred.** One-shot bursts stay under ~60 particles and
  auto-destroy; ambient systems stay under ~160. If Snowfall can do it in 160,
  confetti can do it in 60.
- **Time punctuation has a guard.** Hit-stop style freezes: ≤150 ms, unscaled-
  time restore, never stack with Escape pause (RESEARCH.md design).
- **Text is the last resort.** If a moment needs a sentence, redesign the moment
  (levels teach themselves; delight moments especially).
- **Evergreen law applies to joy, too.** No digits, statuses or level numbers in
  celebration strings; the completion screen is a chapter break, never a finale.

## Delight systems inventory (where the juice lives)

Built and shipped — do not rebuild, extend instead:

| System | Where |
|---|---|
| Hit-stop on death (120 ms, pause-guarded) | `GameManager.OnPlayerDied` |
| Combo ladder (audio semitones) + visible streak | `AudioManager.PlayPickup`, `UIManager` HUD |
| Milestone shockwave every 10th chained gem | `Fx.Ring`, gem pickup path |
| Streak crown at octave cap | `PlayerController` crown rig |
| HUD gem-counter pulse + heart low-life glow | `UIManager.RefreshHud` |
| Win confetti (3-star) + petal mix (Two Suns) | `Fx.Confetti`, `GameManager.OnReachGoal` |
| Panel fade/scale transitions (0.2 s) | `UIManager.ShowPanel`, `Tweener` |
| FOV kick (bounce pads +8°, wind rides +5°) | `CameraFollow.FovKick` |
| Skid dust, wind speed streaks, gust ride haptics | `PlayerController`, `GustZone`, `Haptics` |
| Idle Pip ladder (glance → wave → yawn → nap under Gloomfang's shade) | `PlayerController.IdleLife` |
| Pokeable flowers (pentatonic chime per flower) | `Props`/`FlowerPoke` |
| Gloomfang giggle-raindrop bloom (10 s cooldown) | `Gloomfang`, `Fx` |

## Future queue (researched, ranked, mine to green-light)

1. **Photo mode + share cards** — the nap-under-the-cloud moment deserves a
   camera; also serves accessibility (pause the world, look around).
2. **Ghost runs** — race your best-time self, translucent and friendly.
3. **Pip's shelf** — a home hub that displays earned things (stars, gifts, the
   crown). One-way menu changes at movement milestones is the model.
4. **Parameterized music intensity** — explore/near-death crossfade, slow-and-
   dark for danger (cozy tone: slow, never frantic). RESEARCH.md strategic item.
5. **Golden-gem remix gate** — one hidden golden gem per level unlocks B-side
   remixes (Celeste cassette model; Daily Gem plumbing exists).
6. **Pack 14 The Guardian Games** — see-saws + Red Nine's carousel, canonical in
   `docs/Movement-Two-Story.md`. Guard rule one is spin. Rule two: take turns.

## Rejected (do not re-litigate)

Jump wind-up (fights the jump buffer), camera lookahead (wobble; revisit only if
long glides feel directionless), mid-level mood flips (doesn't fit the level
data model — do moods at level granularity), difficulty selectors, streak guilt,
scarcity of any kind. Delight here is abundant, unhurried, and kind.

## Source index

"The Making of ASTRO BOT" (Nicolas Doucet, GDC 2025) · "Juice It or Lose It"
(Jonasson & Purho, 2012) · "The Art of Screenshake" (Nijman/Vlambeer) ·
Nintendo's kishōtenketsu level structure (Hayashida, GDC 2015) · "Cozy Games"
(Daniel Cook, Lostgarden) · "Designing for Coziness" (Game Developer) ·
Sakurai's Creating Games: "Being Kind to Beginners" · Designing Game Feel: A
Survey (arXiv 2025) · Team ASOBI idle-animation showcases (community catalogs).
