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
| Streak crown at octave cap | `ComboCrown`, `AudioManager.PlayPickup` |
| HUD gem-counter pulse + heart low-life glow | `UIManager.RefreshHud` |
| Win confetti (3-star) + petal mix (Two Suns) | `Fx.Confetti`, `UIManager.ShowWin` |
| Panel fade/scale transitions (0.2 s) | `UIManager` show/hide, `Tweener` |
| FOV kick (bounce pads +8°, wind rides +5°, ShakeOn-gated, +10° cap) | `CameraFollow.FovKick` |
| Skid dust, wind speed streaks, gust ride haptics | `PlayerController`, `WindStreaks`, `Haptics` |
| Idle Pip ladder (glance 6s → wave 12s → sit+yawn 20s → sleep 35s under Gloomfang's shade) | `PlayerController.IdleLife` |
| Pokeable flowers (pentatonic chime per flower) | `FlowerPoke`/`Props` |
| Gloomfang giggle-raindrop bloom (10 s cooldown) | `Gloomfang`, `Fx` |
| Golden-gem signal (mote trail, breathing glimmer, found outline, canon note) | `GoldenSignal`, `GoldenGem`, `Strings.GoldenNote` |
| The Rest (slow win-drift, unrewarded, ShakeOn-gated) | `RestBeat` |

## Future queue (researched, ranked, mine to green-light)

Shipped from this queue: photo mode + share cards, ghost runs, Pip's shelf,
parameterized music intensity, golden-gem remix gate, the golden signal, and
The Rest. What remains:

1. **Secrets with a taught signal, applied beyond the golden** — the signal
   law now has a working implementation (`GoldenSignal`); the next use is
   teaching the *wonder* beats (prism gates, if the content door reopens) the
   same way. Diegetic cue first, no UI marker, never missable-in-a-punishing
   sense.
2. **More micro-rest** — The Rest covers the win moment. Research (Slowdowns /
   Stasis / Stillness) also favours **in-level** calm: an optional perch off
   the route where the camera eases and the ambient mix opens. Zero reward
   attached, or it stops being rest.
3. **The guardian Games (Pack 14)** — see-saws exist (The Homecoming shipped
   them); the carousel set-piece + level content are canonical in
   `docs/Movement-Two-Story.md`. Shelved with the rest of the content pause.

## Rejected (do not re-litigate)

Jump wind-up (fights the jump buffer), camera lookahead (wobble; revisit only if
long glides feel directionless), mid-level mood flips (doesn't fit the level
data model — do moods at level granularity), difficulty selectors, streak guilt,
scarcity of any kind. Delight here is abundant, unhurried, and kind.

**Also rejected, now that the research is in:** rewarding the rest beat
(attaching a reward converts rest into a task — Cook's law: an activity must be
satisfying in itself, and "when the reward outweighs its gentle momentary
pleasure, the activity becomes extrinsic and loses its cozy appeal"); quantified
friendship/relationship scores (the "transactional kindness" anti-pattern);
day/streak notifications of any kind.

## Source index

"The Making of ASTRO BOT" (Nicolas Doucet, GDC 2025) · "Juice It or Lose It"
(Jonasson & Purho, 2012) · "The Art of Screenshake" (Nijman/Vlambeer) ·
Nintendo's kishōtenketsu level structure (Hayashida, GDC 2015) · "Cozy Games"
(Daniel Cook, Lostgarden) · "Designing for Coziness" (Game Developer) ·
Sakurai's Creating Games: "Being Kind to Beginners" · Designing Game Feel: A
Survey (arXiv 2025) · Team ASOBI idle-animation showcases (community catalogs).

Added in the 2026-09-21 research pass ("The Kindness Layer"):

- **Nicole Lazzaro, "4 Keys 2 Fun"** — empirical emotion taxonomy (facial
  coding over ~100M player experiences). Bestsellers engage 3 of the 4 keys per
  session; and *wonder is an emotion adults feel very rarely*, which makes it
  disproportionately valuable to a game that can deliver it. Source for the
  wonder-first framing of any future prism-gate work.
- **Secret/discovery craft (Rumbral's collectible design; Outer Wilds
  environmental-storytelling scholarship; Super Mario Odyssey's collectible
  softness)** — teach the *signal* early; diegetic cues, never UI markers;
  collectibles carry narrative weight rather than filler; hide them in optional
  space; previously-found collectibles leave a faded outline and nothing is
  ever permanently missable. Source for `GoldenSignal`.
- **Reflective-play research (ACM CHI 2024 "A Design Framework for Reflective
  Play"; thatgamecompany's GDC 2025 "It's Okay to Slow Down"; Attention
  Restoration theory applied to games; the HAW Hamburg cozy-games taxonomy)** —
  name the patterns Slowdowns, Stasis and Stillness, cite Animal Crossing's
  bench and *Flower*'s measurable stress reduction. Source for `RestBeat`.
- **Cozy market/design analysis (KOCCA trend data; Wanderstop and Dorfromantik
  design interviews)** — "cozy" in Steam descriptions rose from 0.4% (2022) to
  3.1% (2025); the demand driver is **autonomy**, not "healing"; and the
  recurring anti-pattern to avoid is friendship/relationship systems that
  become *transactional* (quantified kindness points). Source for the
  "rewarding the rest is rejected" rule above.
- **Adaptive-music literature (Celeste FMOD analysis theses; DMuSe layer/intensity
  documentation)** — vertical layering, stingers, and quantized transitions;
  already implemented, retained as the reference for future music states.

