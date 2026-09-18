# Gem Rush 3D — Design Bible

> Story, voice, canon and tone are governed by **docs/Story-Bible.md** —
> on story matters, that document wins over this one.

The north star: the game **gradually develops** — each pack adds exactly one
new idea, teaches it gently, then combines it with everything before it. It
becomes **more interesting and creative** without ever becoming meaner, and
stays **coherent and consistent in character**: same warm voice, same soft
shapes, same rules everywhere.

## The engagement loop (why players stay)

- **Seconds**: juice — squash & stretch, landing dust, gem sparkle, bounce
  pads, wind that carries you. The game should feel good to *touch*.
- **Minutes**: drip-feed novelty — a checkpoint every 30–60s with a
  one-line story beat; music that follows the sky's mood; one new verb per
  pack, never more.
- **Session**: the unlock ladder — after every level the *next* thing is
  already in sight. Medals (gold/silver/bronze, auto-derived) give cleared
  levels a reason to be replayed.
- **The whole game**: the story arc. Gloomfang is not an obstacle, he is the
  plot. Saving him is the reward for playing well; playing as him is the
  reward for finishing.

## The pack grammar (how content grows)

Every pack = 3 levels + exactly **one new mechanic or twist**, taught in the
Nintendo rhythm: **introduce → develop → combine**.

| Pack | Name | New idea | Combines with |
|---|---|---|---|
| 1 | The Storm | movers, spinners, elevators, checkpoints | — |
| 2 | The Rematch | narrow bridges, guardian variety | pack 1 verbs |
| 3 | The Undercloud | descent + DarkRealm mood + hearts | everything, harder |
| 4 | The Two Suns | bounce pads (launch) | story celebration |
| 5 | The Far Isles | updraft columns (wind as platform) | pads, guardians |
| 6 | bonus | play as Gloomfang (flight) | all verbs, no deaths |
| 7+ | (planned) | see roadmap | everything |

Rule: a new pack's hardest moment must not exceed the previous pack's
hardest moment. Difficulty comes from **combining** known verbs in new
geometries, never from cruelty.

## Character bible (consistency)

- **Voice**: warm, wry, family-friendly. The narrator likes everyone,
  including the antagonist. Hazards are "guardians"; death is "taking a
  nap". Gloomfang is lonely weather, never a villain.
- **Shapes**: soft primitives only. Rounds for friends (Pip, gems, clouds),
  red arms for hazards. No spikes, no jagged silhouettes.
- **Palette is data**: every level sets sky/fog/ambient; the music follows
  the same mood (warm pads in daylight, low pads in the dark).
- **Zero imported assets, zero manual editor setup.** If a feature needs a
  file shipped or a checkbox ticked, redesign it.
- **Systems over screens**: content flows through LevelDefinition + the
  builder. A new mechanic = one spec class + one piece component + one
  builder loop. Never bespoke level scripting.

## Pacing rules (non-negotiable)

1. Gaps of 3–5 units, rises of ~1 unit per hop — unless a mechanic exists
   to make it easy (lift, wind, pad).
2. A checkpoint before every hard section, plus a story beat on it.
3. New mechanics are introduced with **no hazard nearby**, then combined
   with hazards only after a full level of practice.
4. A spare life (heart) appears in any level longer or tougher than the
   one before it.
5. All difficulty is opt-in beyond "finish the level": medals, gem
   perfection and star chasing are for players who want more.

## Engineering queue (supports the expansion contract)

1. **Save-key stability**: key saves by level name, not index, with a
   one-time migration — inserting a level must never shift anyone's stars.
2. **Fx pooling**: burst particles are instantiated/destroyed per event;
   pool them for steady frame times on low-end phones.
3. **Headless level audit**: an executeMethod that validates every level's
   geometry (gap widths, gem reachability, spinner/checkpoint overlaps)
   so content can grow fast without playtesting everything.
4. **Engagement rewards**: star-milestone cosmetics (trail colors at
   15/30/45 stars) + session recap on the menu.
5. **Ghost runs**: race your own best-time recording, pip-shaped.
6. **Gloomfang reactions**: he bounces and sparks when you collect near
   him; tiny, alive.

## Retention & community plan (research-backed, 2026-09)

Research across retention science, premium mobile design (Alto's Odyssey,
Monument Valley), cozy community growth (Stardew Valley, A Short Hike) and
viral community engines (Fall Guys). The benchmarks: top-tier mobile is
D1 45%+, D7 20%+, D30 8%+. The diagnostic: weak D1 = onboarding problem,
weak D7 = habit problem, weak D30 = depth problem.

**Our diagnosis.** D1 is strong (instant play, zero setup). D7 is the risk:
after 15 levels there is no *reason to return tomorrow*. D30 needs depth:
collectibles, mastery, identity.

**Copy (what the winners do right):**
1. **Cozy habit, not FOMO** (Alto/Snowman): retention through atmosphere,
   flow and session flexibility — short or long sessions both work. No
   energy meters, no punishing streak loss. This is already our character;
   build the *gentle* version of the daily hook.
2. **Free content cadence** (Stardew Valley): constant free updates gave
   41M+ players recurring return-and-share moments. Our expansion contract
   IS this — keep packs shipping.
3. **Shareable moments + amplify them** (Fall Guys): growth engine =
   moments players want to show, then devs re-sharing community content.
4. **Photo mode / shareable scorecards**: in-game photography exists to be
   posted. Our skies are the asset.
5. **Recurring hooks**: daily challenges, world records, speedruns —
   mastery ladders for the engaged core.
6. **Dev transparency** (ustwo publishing numbers; Snowman interviews):
   openness generates press and goodwill. DESIGN.md and postmortems public
   when we get there.

**Improve (our premium, gentle versions, in build order):**
1. **The Daily Gem** (D7 habit, zero FOMO): each day, one level glows on
   the menu with a single hidden golden gem. Find it → Gloomfang's Gift
   counter ticks up. Miss a day? Nothing bad happens. Ever.
2. **Photo mode + share card** (community engine): HUD button hides UI,
   slow orbit camera, then the Android share sheet with a framed scorecard
   ("Skyfall Summit — GOLD — 1:12 — 13/14 gems").
3. **Ghost Pip** (D30 mastery): record best run, race your translucent
   self. Speedrun ladder without any server.
4. **Session recap** (return motivation): menu greets: "Last visit: 2
   medals, 6 gems. The guardians noticed."
5. **Completion screen**: total gems/stars/medals as a "realm atlas"
   percentage — collection engine, Animal Crossing-lite.
6. **Cosmetic identity**: star-milestone trail colors for Pip (15/30/45
   stars) — identity without paywalls.

**Deliberately not doing** (premium character): energy meters, loss-based
streaks, ads, pop-ups, push-notification guilt. Research shows those move
short-term metrics and kill the word-of-mouth engine cozy games depend on:
recommendation to non-gamers.

## The expansion contract (the user's directive)

**The storyline keeps expanding. Every finished pack is a milestone that
unlocks a new, interesting obstacle.** Each new obstacle follows the gentle
grammar: introduced alone, developed with known verbs, combined last. The
story is the delivery vehicle: every pack is another Sky-Keeper errand with
Gloomfang along, so the world grows *because* the story grows.

Planned expansion queue (one pack per session, order may shuffle):

| Pack | Story beat | New obstacle | Feel |
|---|---|---|---|
| 7 | The Sky Garden blooms under Tuesday rain | **Sleeping Guardians** — spinners that wake (accelerate) only while you linger; flow rewarded | garden-soft |
| 8 | Pip chases a rogue gust across the map | **Tailwind gusts** — telegraphed wind pulses that push and carry you | open-air |
| 9 | The bell towers that once guided storms | **Echo bells** — ring them to reveal hidden gem trails and light the path | reverent |
| 10 | A mirrored sky appears over the Far Isles | **Mirror doors** — paired portals that hop you across gaps | magical |
| 11+ | The atlas keeps growing; Gloomfang's badge comes off probation | rotations, see-saws, rainbows, and whatever the story asks for | — |

Milestone framing in-game: after each pack, the chart updates (toast/epilogue
line) so the player *feels* the world growing under their feet.

## The Weather Atlas — content masterplan

**Creative thesis: weather is memory, and memory is level design.** The
Undercloud was dark because Gloomfang was afraid; his rain bloomed the
garden because he was happy. Every future region is a *feeling*, and each
feeling is one new weather-obstacle. This keeps expansion infinite,
creative, and perfectly in character — the mechanics ARE the storytelling.

**Pack 7 — The Sky Garden** (feeling: care)
Rain that grows things. New obstacle: **Sleeping Guardians** — spinners that
wake and accelerate only while you linger, so flow is rewarded and standing
still is safe but boring. Signature moment: gems you collected are flowers;
at the gate, the whole garden blooms in the colors you picked.

**Pack 8 — Storm Chasers** (feeling: play)
A runaway baby cloud (Gloomfang and Pip's stray find) leads Pip on a chase.
New obstacle: **tailwind gusts**, telegraphed by drifting petals, that push
Pip in long glides. Signature moment: the gusts pulse in time with the
music — the level is playing the song with you.

**Pack 9 — The Bell Towers** (feeling: wonder)
Towers that once sang storms home. New obstacle: **echo bells** — ringing
one makes hidden bridges glow solid while the tone rings out. Signature
moment: ring the towers in the right order and they play a melody — and the
tower stays permanently brighter where the song has passed.

**Pack 10 — Mirror Skies** (feeling: memory)
A mirrored sky appears over the Far Isles. New obstacle: **mirror doors** —
paired portals that hop you across impossible gaps. Signature moment: on
the far side, a mirror-Gloomfang copies your every move — it is his
childhood memory, played gently, not fought.

**Pack 11 — The Long Winter** (feeling: calm)
The quietest pack. Snow-soft platforms, slow music, no new danger. New
obstacle: **the sunstone lantern** — carry its light to melt ice paths as
you pass, opening the route. Signature moment: at the summit, all the
melted paths refreeze into one giant crystal map of everywhere you walked.

**Pack 12 — The Aurora Festival** (feeling: joy)
The realm's thanks. **Aurora ribbons** — ridable moving light-bridges.
Signature moment: the festival concert finale where every mechanic in the
game appears as an instrument, and the last note lights a permanent aurora
over the menu screen forever.

## Compounding content systems (depth between packs)

- **Melody gems**: gem trails play real notes in order (SfxSynth); full
  trails complete songs. Collecting IS composing.
- **Pip's shelf / home hub**: the start island slowly fills with trophies,
  photos, season decorations — the world remembers you.
- **The Atlas screen**: the map that fills in as packs complete, with medal
  stamps per region — the meta-collection.
- **A living calendar**: real dates matter, gently — Tuesdays bring
  Gloomfang's rain to any level, anniversary weeks bring confetti skies.
  The world is on a clock without ever demanding anything.
- **Old levels, new weather**: cleared levels can reappear under a new
  mood (rain, dusk, aurora) as remix challenges — near-infinite content
  from existing geometry.
- **Photo postcards**: photo mode + framed scorecards, made to be shared
  (the Fall Guys growth engine, in our cozy voice).
