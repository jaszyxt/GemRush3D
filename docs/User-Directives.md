# User Directives — Protected Rules

**These are the USER's standing decisions. They are not suggestions, not
defaults, and not open to reinterpretation by any agent.**

An agent (human or AI) **MUST NOT** change anything listed in this file
unless the user has **explicitly asked for that specific change in
writing** in the current session. "It seemed inconsistent", "the audit
recommended it", "it would be cleaner", "the plan implied it", or "it
looks like a bug" are **NOT** permission. They are reasons to *ask*.

If a rule here conflicts with a research finding, an audit, a plan, or
another doc — **this file wins, and the other doc is wrong.**

**How to change a protected rule:** the user states the change, the
change is made, and this file is updated in the same commit with the
user's words as the reason. Nothing else unlocks it.

Added: 2026-09-21.

---

## D-1. Haptics mark EVENTS, never movement

**Source: user, 2026-09-21** — *"I don't want haptics on every move by pip
esp the jump because it's excessive buzzing on my hands. I want it when
it's really needed."*

**Rule:** vibration is reserved for moments that carry information the
player would otherwise miss. Pip simply *moving* never buzzes on its own.

**Silent (do not add haptics here):**
- Jumping — **explicitly called out by the user**
- Walking, running, skidding, landing from a normal hop
- Collecting a single gem (dozens per level; a chain would rattle the phone)

**May buzz:**
- A streak milestone (the every-10th-gem moment that already rings and chimes)
- A firm/hard landing only (never a walk-off hop)
- Checkpoint reached, spare heart collected, lantern woken
- Death, level won, whole game finished

**The one allowed sustained buzz:** the wind-ride texture — it is a
*state* ("you are being carried"), not a repeated event.

**Test before adding any new haptic:** does this tell the player
something they would otherwise miss? If it only confirms "you pressed a
button", it does not get a haptic.

**Why it matters:** the user plays on a phone held in their hands. A buzz
that fires on every hop is not feedback, it is noise — and it trains the
player to ignore the buzz that *matters*.

---

## D-2. The palette law is law

**Source: user, standing** — colour families may not be reassigned.

- `HazardRed` = **only** spinner arms. Nothing else, ever.
- `Gold` = **every** reward (hearts, bells, stars, daily gift, lantern).
  Gold is never decoration and never a neutral prop colour.
- New colours go into `ArtLib` as named constants — **never** inline
  literals in a feature script.
- Red must never mark a pickup; gold must never mark a danger.

**Do not "fix" a contrast problem by changing a family's hue.** The
families are load-bearing for colour-blind players (hazard red and reward
gold separate at 3.37:1 in greyscale). Fix contrast with *silhouette*,
*emission*, or *layout* instead.

---

## D-3. User edits are authoritative

**Source: HANDOFF working agreement, user SOP.**

The user edits files in parallel sessions. If the user's version of a
file differs from what an agent remembers or expects:

- **Re-read the file before writing.** Never write from memory.
- The **user's version wins**. Do not "restore" what an agent thinks was
  there, and do not re-apply a change the user has removed.
- If an agent's change and the user's change collide, the user's stays and
  the agent's is dropped.

---

## D-4. Movement, feel and difficulty numbers are the user's

**Source: user, standing (see `DESIGN.md`, `HANDOFF.md` pacing law).**

Do not retune these to taste: Pip's speed/jump/gravity, coyote time,
the difficulty curve and pacing between packs, lives count, or the
placement rules for hearts and checkpoints.

An agent may *report* that something feels off, with evidence. An agent
may not change it. Difficulty never rises between packs; a new mechanic
is introduced alone before it is combined.

---

## D-5. A reported bug is a CLASS, not an instance

**Source: user, standing (stated 2026-09-21 after two reports).**

When the user reports a bug, finding and fixing *that spot* is not the job
done. Every report is treated as the visible instance of a pattern, and the
whole game is swept for siblings before the work is called complete.

Required shape of the response:

1. **Name the class.** What general defect is this an instance of? (A reward
   hidden where the player will not go? A mechanic trusting another system's
   clock? A renderer with no mesh? A trigger too small to hit? A placement
   score with no rule against the exit?)
2. **Sweep every level and every system for the class** — not just the level
   in the report, and not just the file that was named.
3. **Fix all instances found**, and state plainly which areas were checked
   and came back clean, so "clean" is a recorded finding rather than silence.
4. **Add the assertion** that fails if the class returns, where the class is
   mechanically checkable.

**Also distrust the model first.** Every bug reported from play so far was
invisible to the agent's own instruments — an invisible bridge, an uncrossing
gust, a reward behind the exit. The instruments model geometry; the failures
were rendering, a clock and placement scoring. When a report contradicts an
audit that says "all clear", the *instrument* is the first suspect.

Precedent: the golden gem (hidden at the exit) led to finding `DailyGem` had
the same defect plus a worse one (it could silently place nothing at all).

---

## D-6. Little difficulty. Evolving variety instead.

**Source: user, standing (stated 2026-09-21). Verbatim:** *"i dont want the
game to be very difficult. i want it to be interesting and evolving. i
don't like the spinners so fast! i want the game to be fun with little
difficulty only."*

This is the governing intent for every difficulty decision in this project,
and it outranks any "the curve should rise" instinct an agent may hold.

**The rule.**

- **Difficulty stays LOW and roughly FLAT for the whole game.** It is not a
  ramp to be climbed. A player who reaches the last region should not be
  meeting harder play than the first region — they should be meeting
  *newer* play.
- **What evolves is VARIETY, not challenge.** Interest comes from new
  mechanics, new combinations, new realms, new sights — never from tighter
  timing, faster hazards, wider gaps or narrower platforms.
- **Nothing may get harder than what came before it.** `DESIGN.md` already
  says it: *"a new pack's hardest moment must not exceed the previous
  pack's hardest moment."* That rule is now load-bearing law, not a
  guideline. The historical spike (140°/s spinners in level 5, the game
  maximum, in the teaching arc) is exactly the violation this forbids.
- **Spinner speed ceiling: 90°/s, and lower is better.** Fast spinning arms
  are the single mechanic the user named as unfun. Prefer 60–75. The
  sleeping-guardian mechanic (idle 12°/s, waking only when approached) is
  the preferred shape for any guardian facing a young player.
- **Comfort beats spectacle.** When a choice exists between a more
  thrilling moment and a kinder one, choose kinder. Deaths already cost
  nothing (D-4, 5 lives, checkpoint respawn); do not reintroduce stakes via
  hazard speed, hazard count, or narrower geometry.

**What this means when building a new level or pack.** Add the new idea
(mechanic, realm, prop, ride) and keep the pressure where it already is.
A new pack that introduces three new mechanics at 60°/s is *better* by this
directive than one that introduces none at 140°/s.

**What it does not forbid.** Optional mastery for players who want it —
medals, gem perfection, star chasing — because `DESIGN.md` keeps all of
that opt-in beyond "finish the level". Difficulty may exist for those who
seek it; it must never be required to progress, and it must never be
imposed on a child who is just trying to reach the portal.

**Enforcement:** the hazard instrument (`tools/LevelAudit/run.sh --hazards`)
measures every spinner's read window against child reaction research, and
`LevelAuditTests` asserts it. A spinner over 90°/s, or a level harder than
its predecessor, should be treated as a bug.

---

## D-7. The story leads. The build follows it.

**Source: user, 2026-09-21** — *"i want you to dictate the direction of the
game by your story and the agent developers will follow thru"*, and on
scope, *"you own the stories. you decide the best approach"*, and
*"yes, the game will follow your story. collaborate with other agents."*

The story is not flavour applied after the fact. It is the **delivery
vehicle for the whole expansion contract**: each region is a feeling, each
feeling is one weather-obstacle. Levels, mechanics, props and audio are
built *to serve* the story — so story text and canon are protected here,
with the same force as D-1 and D-2.

**Canon is closed. These may not be changed by an agent:**

- **Gloomfang's redemption is permanent** (end of pack 3). He is never
  re-villainized, never an "evil storm" again, and his loneliness is never
  played for mockery.
- **The badge story is RESOLVED.** The review that Movement Two was built
  around concluded at level 40 (The Homecoming): the Sky-Keeper crossed
  one word off the badge. He is **not** probationary. The old
  "request pending" gag **paid off and is retired** — do not restart it,
  and do not replace it with a new version of the same joke.
- **Nim is family** (Gloomfang's found baby cloud). No character — and
  especially not Nim — may be characterized negatively in any doc.
- **The lullaby is the realm's song.** New arrangements are welcome;
  replacing it is not.
- **Characters' pronouns are canon** (Pip he, Gloomfang he, the
  Sky-Keeper she, Nim he, Red Nine he). See the cast table in
  `docs/Story-Bible.md` §3.

**The Evergreen Law (anti-drift).** Story strings describe a game that
keeps growing, so they may not state its size:

- **No counts** in evergreen text — no level totals, pack counts,
  percentages. (Precedent: a menu quote claimed "twelve levels" when the
  game had forty; the README said the epilogue played at level 9.)
- **No spec digits in prose.** Mechanics live in specs, not sentences
  ("for as long as the tone sings", not "six seconds of bridge").
- **No statuses that the next pack invalidates** — never "the last", "the
  final", "the end", "the only" about *content*. Moments may be final;
  the game may not be.
- **The completion screen is a chapter break, never a finale.**

**What this means for other agents.**

- Do not edit story text, missions, win lines, beats, milestones, menu
  quotes or the epilogue without the story owner's say-so. If a build
  needs a line changed, **ask** — the line may be load-bearing canon.
- Story is a **handoff partner, not a blocker**. If the story needs a
  mechanic to exist (a lantern to carry, a bell to ring), that is a
  feature request *with a reason attached*, and it takes priority over
  cosmetic work — the mechanic IS the storytelling.
- Every pack's story text is validated against the **craft checklist** in
  `docs/Movement-Two-Story.md` §3 and the QA gate in
  `docs/Story-Bible.md` §9 before it ships.

**Why it matters:** story text is the one artifact a player reads
literally, and it is the thing that dates fastest. Every drift found in
the 2026-09 review was a string that described a game that no longer
existed — a menu quote, a README line, a test message, and an epilogue
that contradicted the ending the player had just watched.

**Enforcement:** `tools/check-directives.sh` fails the build when
evergreen story text states a level count, or when the retired
probationary gag is reintroduced as current status.

---

## Enforcement

`tools/check-directives.sh` (run by CI on every push and pull request)
guards the mechanically-checkable parts of this file:

1. **D-2** — no inline colour literals for hazards/rewards outside ArtLib.
2. **D-1** — no `Haptics.*` call inside the jump / skid / ordinary-landing
   code paths in `PlayerController.cs`.
3. **D-7** — no level counts in evergreen story text, no lives count
   that disagrees with `GameManager.StartingLives`, and no reinstated
   "probationary" as Gloomfang's current status.
4. This file must not be deleted, and the directive headings must remain.

A failing guard means someone changed a protected decision without the
user's explicit instruction. **Do not edit the guard to make it pass.**
Either revert the change, or — if the user did ask for it — update this
file in the same commit with the user's words, as described above.

---

## D-8. Print only what the user will read

**Source: user, 2026-09-21** — *"you printed a lot. why?"* … *"don't do
that again, you can do that in the background. please print only what i
will read."*

**Rule:** when reporting to the user, print only what they will actually
read. Reasoning, exploration, intermediate findings, tool narration and
session summaries belong in the working (background) process, not in the
reply. Long recaps of already-completed work are not a status report —
they are noise the user has to scroll past.

**What to print:**
- the outcome first, in one or two sentences
- what changed, and whether it is verified
- anything genuinely blocking, and the decision needed
- anything that would change what the user does next

**What not to print:** history they already know, files they did not ask
about, restatements of the plan, exhaustive inventories, or a summary of
a long session when a short answer was asked for.

**Why it matters:** the user reads the replies. Volume costs them
attention and buries the one line that needed a decision. A short reply
that answers the question beats a complete record every time.

---

## D-9. Movements are 40 levels each

**Source: user, 2026-09-24** — *"each chapter should have 40 levels"*

Every movement = 40 levels. Movement One is the shipped game (levels 1–40).
Future movements each get 40 levels — no more, no fewer. This is the
creative constraint that shapes all future story and content work.
