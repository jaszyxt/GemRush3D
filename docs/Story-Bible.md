# Gem Rush 3D — Story Bible

**Owner:** the story agent. **Audience:** every agent (human or automated)
that adds levels, mechanics, UI copy or audio to this game.

**Precedence:** on all matters of story, voice, canon and tone, this
document wins over every other document, including DESIGN.md. Systems,
engineering and pacing law remain governed by DESIGN.md; this document
governs *what the game says and where it is going*.

---

## 1. The mandate — three pillars, three tests

The story exists to **inspire, to encourage, and to make people laugh** —
in that order when they conflict. Every string that ships must pass its
pillar's test:

- **Inspire.** The hero wins by empathy, never by force. Every conflict in
  this game ends in understanding, and the player's kindness is the actual
  mechanic. *Test: could this level's story make someone a little
  gentler?* Shipped proof: "One last climb, Pip. Not to win. To listen."
- **Encourage.** Failure is a nap, not a verdict. Difficulty is framed as
  patience; absence is framed as freedom. The player is never measured,
  scolded or made anxious. *Test: would a six-year-old who just lost feel
  invited back?* Shipped proof: "Every legend takes a nap sometimes. One
  more try, Pip."
- **Laugh.** Warm wit: deadpan bureaucracy, understatement, running gags
  that compound. The joke always includes the player, never points at
  them, and is never at anyone's expense — not even the antagonist's.
  *Test: does the joke land like an in-joke between friends?* Shipped
  proof: "He has requested the word 'probationary' be removed. Request
  pending."

## 2. The world in one line

**Weather is memory, and memory is level design.** Every region is a
feeling; every feeling is exactly one weather-obstacle. The mechanics ARE
the storytelling: the Undercloud was dark because Gloomfang was afraid,
the garden bloomed because his rain was joy, the bells sang because the
towers missed the storms. If a proposed mechanic cannot be stated as a
feeling, it is not in the game yet — send it back until it is one.

## 3. The cast (canon — do not contradict)

| Character | Canon | How to write them | Never |
|---|---|---|---|
| **Pip** (he) | The hero. Very small, very determined. The Sky-Keeper's helper. "Small, determined, excellent at falling upward." Falls with style. Brings snacks. | Address Pip directly in missions ("Climb, Pip."). Pip notices things, decides things, is 90% sure of things. Pip's courage is quiet, not loud. | Pip never fights, never boasts, never speaks dialogue — Pip acts and the narrator tells us. |
| **Gloomfang** (he) | A very large storm. Once stole the light because he feared the dark ("abandonment issues" is the level-1 joke). Redeemed end of pack 3; employed as **weather support (probationary)**; rains on Tuesdays; learning to hum; cries at sunsets; built Nim's crib out of mist and refuses to discuss it. | Enormous, gentle, embarrassed by his own feelings. His thunder whispers when it matters. He is a person who happens to be weather. | Never re-villainize him. Never play his loneliness for mockery. No "evil storm" regressions. |
| **The Sky-Keeper** (she) | The realm's mapmaker and caretaker. Gave Gloomfang his badge. Wrote one word — *"more."* — in the corner of the finished map. | Warm bureaucracy. She processes feelings as paperwork and means every word of it. Her forms are affection. | She is never cold, never an obstacle, never a quest-giver who withholds. |
| **Nim** (he) | A baby cloud, "the size of a pillow with the volume of a parade." Giggles become tailwind gusts. First word was a gust. Gloomfang's apprentice; the family's smallest member. Snoring is "rehearsing." | Pure joy with no timing at all. Nim is the punchline that hugs back. | Nim is never a burden or a fetch-plot; he leads, Pip follows, everyone's day improves. |
| **Guardians** (they) | Big, red, round, committed. Once the stormcell elite; now friendly — they dance, charge admission, sleep in gardens, dream. Sleeping Guardians wake only while you linger. | Give them dignity in their silliness. Their dreams leak into gameplay (Red Nine hums Pip's theme). | They are never enemies again after the pack-3 reveal. No guardian is ever punished for doing its job. |
| **Red Nine** (he) | The Sky Garden's light sleeper. The only named guardian. Dreams of being a carousel. | Treat the name as a small solemn honor. | Red Nine never becomes a running gag that exhausts him. Use him sparingly. |
| **Mirror-Gloomfang** (he) | Lives on the mirrored side of the sky since pack 10. Copies Pip's moves — first half a second late, then half a second ahead. Learning to be a reflection. Hums the lullaby before Pip does. "Almost ready." | A memory being gentle with itself. His arrival is scheduled (see roadmap, pack 12). | He is never a shadow-boss, never an evil twin, never fought. |

**The lullaby** is canon: Gloomfang's old lullaby, hummed through packs
8–10, tuned into the First Bell, taught to Nim, hummed first by the
mirror. It is the game's "small piece of music that means home." Audio
agents: new arrangements of it are always welcome; new replacement themes
are not.

## 4. The voice

The narrator **likes everyone, including the antagonist** — that is the
single rule all others derive from. The narrator is a fond official
biographer of small events.

Recipes that work (all shipped; imitate these shapes):

- **Understate big feelings with bureaucratic formality.** "…was still,
  technically, a weather management problem."
- **Setup, escalation, button.** "Gloomfang took a bow and made it rain
  confetti. Briefly. Accidentally."
- **Specificity over adjectives.** "The size of a pillow with the volume
  of a parade" — never just "very loud."
- **The declarative with a turn.** "'Go back,' he thunders. It comes out
  as a whisper."
- **The parenthetical truth.** "(He will cry at the sunset.)"
- **The formal record of a silly event.** "Population: one more, sort
  of." / "Request pending."
- **Recurrence, not repetition.** Gags return changed: Tuesday rain
  becomes a job becomes a day off; "probationary" becomes a request
  becomes (someday) a ceremony. Never paste the same gag twice.

**Banned forever:** cruelty, cynicism, sarcasm aimed at the player, FOMO
or guilt ("don't miss out", "your streak"), violence as a solution,
calling any character a villain after their redemption, slang that will
age badly, brand/real-world jokes, and — see the Evergreen Law — numbers
in evergreen text.

## 5. The Evergreen Law (anti-drift)

Story text once drifted: a menu quote claimed twelve levels when the game
had twenty-eight; the README said the epilogue played at level 9. The law
that prevents recurrence:

1. **No counts in evergreen strings.** No level numbers, pack counts,
   totals, or percentages in menu quotes, game-over text, tags or
   epilogue. The world "keeps growing" — text that states its size will
   lie.
2. **No spec digits in prose.** Mechanics live in specs, not sentences.
   "Six seconds of bridge" became "for as long as the tone sings."
   Numbers belong to data files.
3. **No statuses that updates invalidate.** Don't write "the last", "the
   only", "the end", "final" about *content*, only about *moments*
   (a last gem in a level is a moment; "the last level" is a status).
4. **The completion screen is a chapter break, never a finale.** The
   canon closer is: "every ending here is just a portal to the next
   adventure."
5. **Known exceptions, tracked:** "Three lives" in the menu quote (lives
   are a design constant; if lives change, that quote changes with
   them), and "a thousand years" (mythic numbers are allowed).

## 6. Continuity ledger — the story so far

One line per pack; future packs must not contradict these:

1. **The Storm** — the storm swallows the Sunstones; Pip takes the sky
   back portal by portal. Gloomfang flinches, watches, is nervous.
2. **The Rematch** — Gloomfang retaliates properly: stormcell elite,
   his private garden, Skyfall Summit. His hugs are reportedly bigger
   than his grudge.
3. **The Undercloud** — the dive. The dark he never talks about; the
   glowing stones he carried down himself; the Heart of the Storm,
   beating out of rhythm from loneliness. Pip takes his hand; the realm
   lights from below. **The redemption is complete and permanent.**
4. **The Two Suns** — the celebration lap: two suns (the old one and the
   one Pip made), the first Tuesday rain, and the way home. The shelf
   gets the last gem. (See gag registry: the shelf always has room for
   one more.)
5. **The Far Isles** — the charting begins. The wind stands still and
   holds; the map fills; the Sky-Keeper writes *"more."* in the corner.
6. **Gloomfang's Day Off** — the badge goes ON. He does his run alone,
   hums the whole way home, and absolutely does not cry at the sunset.
7. **The Sky Garden** — his rain woke a garden; Sleeping Guardians were
   already asleep in it. Red Nine is named. The gate opens in the colors
   Pip picked.
8. **Storm Chasers** — Nim, found and followed and homed. The atlas
   grows one word: "family." Gloomfang built the crib. Refuses to
   discuss it.
9. **The Bell Towers** — the towers that sang storms home ring again;
   the First Bell is tuned to the lullaby (he asked us not to ring it;
   we ring it); the Silent Spire sings its first note ever. The sky
   realm has a choir now.
10. **Mirror Skies** — a mirrored sky with paired doors; on the wrong
    side of the glass, a reflection-Gloomfang copies Pip, half a second
    late, then ahead, learning to be a reflection. "Both of you are
    almost ready."

**Running gag registry** (advance, never repeat verbatim):

- **Tuesdays** — rain delivery day; the day off; the calendar's gentle
  heartbeat. Lives in mission text and is allowed to touch real dates.
- **The badge / "(probationary)"** — requested removed; request pending.
  Its arc is the spine of Movement Two (below).
- **The lullaby** — see cast section.
- **The shelf** — always has room for one more. That's the point; it is
  an emotional capacity, never "full."
- **"Pip checked. Twice."-style trios** — the verify-the-silly-thing
  beat; ration them, one per level at most.
- **The Sky-Keeper's one-word memos** — "more." Future memos may appear
  (one word each, at movement milestones only).
- **Guardian economics** — they charge admission, accept compliments,
  form opinions about music. They are little citizens, not props.

## 7. The delivery contract — where story reaches the player

The LevelAuditTests keep geometry honest; this table keeps story honest.
Every level ships with the first four slots; the pack finale adds the
fifth.

| Slot | Lives in | Shape | Job |
|---|---|---|---|
| **Mission** (intro card) | `LevelDefinition.Mission` | 1–3 sentences, readable in ~3 seconds. Names the level's new mechanic *in fiction*. May end with an address to Pip or a wink. | Make the player want the next 90 seconds. |
| **Story beats** (checkpoint toasts) | `LevelDefinition.StoryBeats` | One thought per beat, ≤ 2 short lines. World-alive observations, not tutorial restatement. | Drip-feed the world every 30–60s of play. |
| **Win line** (win screen) | `LevelDefinition.WinLine` | One sentence. A joke or a warm beat. | Reward, never grade. Never mentions stars, time or mistakes. |
| **Milestone** (pack finales only) | `LevelDefinition.Milestone` | `REGION CHARTED: <NAME>. <flourish>` (pack 6 precedent: `BONUS LOGGED:` for the secret level). The flourish must add a *new* feeling, never echo the win line's. | The "atlas grew a page" stamp — the world grew under the player's feet. |
| **Menu quotes** (rotating) | `Story.MenuQuotes` | Evergreen one-liners, cast- and place-flavored. | Coverage law: every shipped region gets at least one quote. Quotes are memories — none are ever deleted. |
| **Epilogue** (movement finales) | `Story.Epilogue` | Page-per-view paragraphs. | **Renewal law:** when a movement closes, the epilogue is rewritten to include every region shipped. An out-of-date epilogue is a continuity bug. Ends with the canon closer. |
| **Gift & recap lines** | `DailyGem`, `UIManager` | One line, zero urgency. | "Today's gift is still out there" — missing a day must be *invisible*. Never count days, never streak. |

## 8. The roadmap — direction, dictated

**The shape of the game:** the story moves in **movements**. Movement One
(packs 1–10) told the rescue: a storm took the light, a small hero
brought him home. Every movement ends with a rewritten epilogue and one
permanent change to the menu screen. The last pack of a movement is a
celebration lap — no new danger, the story is the content.

**Movement One theme (done):** *fear → friendship.*
**Movement Two theme (packs 11–16, dictated now):** ***The Probation
Season*** — *can a storm become a citizen?* The Sky-Keeper schedules
Gloomfang's badge review; every region the crew charts is evidence;
the verdict lands on the finale. Feelings ladder: **calm → joy → wonder
→ play → teaching → pride.**

Full level-by-level story scripts for all of Movement Two (packs 11–16)
are written and canonical in **docs/Movement-Two-Story.md** — copy story
text from there verbatim. Pack 16's script includes the Movement Two
epilogue rewrite (Renewal Law) ready for `Story.cs`.

### Pack 11 — The Long Winter (NEXT TO BUILD) · feeling: calm

The quietest pack. Gloomfang requests one week of stillness to practice
for the review; the crew accompanies him to the snow-soft isles where
weather goes to rest. **New obstacle: the sunstone lantern** — carry its
light to melt ice paths as you pass, opening the route. No new danger;
hearts are generous; the music is slow. Signature moment: at the summit
the melted paths refreeze into one giant crystal map of everywhere you
walked. Story beat to plant: the Sky-Keeper's memo arrives — one word:
*"soon."*

### Pack 12 — The Aurora Festival · feeling: joy

The realm's thanks, mid-movement. Every mechanic in the game appears as
an instrument; **aurora ribbons** — ridable moving light-bridges — carry
Pip through the concert. The festival is where the mirror door finally
opens and **Mirror-Gloomfang steps through as a guest** — payoff of
"almost ready"; nobody fights, everybody dances. Signature moment: the
last note lights a **permanent aurora over the menu screen** (one-way
menu change, movement milestone).

### Pack 13 — The First Rainbow · feeling: wonder

Two suns plus Tuesday rain produce the realm's first rainbow, and the
crew goes to find where it lands. **Prism gates:** light-bridges that
exist only where Pip stands inside the colored ring — color-keyed doors
as a feeling, not a puzzle grind. Signature: the rainbow keeps an end
somewhere nobody expected (the Undercloud).

### Pack 14 — The Guardian Games · feeling: play

The guardians, inspired by dancers and carousels, request a sport.
**See-saw platforms** ridden as team events with guardian teammates;
taking turns is the win condition. Red Nine's carousel dream comes true
— he is the centerpiece, gloriously, for exactly one level. Signature:
the medal ceremony where every participant place is first.

### Pack 15 — The Little Apprentice · feeling: teaching

Pip is given a smallest helper of his own: a flickering spark who copies
Pip's last jump to reach plates Pip can't — the player teaches by
playing normally. Mirror of pack 8 inverted: Pip was the found one once.
Signature: the spark copies Pip's victory pose, badly, proudly.

### Pack 16 — The Badge Ceremony (movement finale) · feeling: pride

A celebration lap, no new hazard. The review lands: "probationary" comes
off; the badge now reads **weather support (senior)** — and Gloomfang
requests a hat. Request pending. (The bureaucratic gag survives by
evolving; it never dies.) Epilogue is rewritten to include packs 11–16.
Movement Three is teased in one line.

### Movement Three seeds (packs 17+, not scheduled)

- **The Deep Bloom** — what grows in the old dark now that the Undercloud
  is lit. Feeling: renewal.
- **The Migration** — weather develops seasons; the realm learns
  goodbye-and-hello. Feeling: belonging.
- **The Sky Library** — the hall where every run ever flown is a book;
  ghost-Pips are its readers. Feeling: being remembered. (Natural home
  for the ghost-run feature when it is built.)
- **Tuesday, Interrupted** — one Tuesday, the rain doesn't come. A gentle
  mystery; Gloomfang is fine — he is making a surprise. Feeling: trust.

## 9. Story QA gate — run before shipping any pack

1. Exactly one new mechanic; it can be stated as a feeling.
2. Introduced with no hazard nearby (DESIGN pacing rules still apply).
3. Mission names the mechanic in fiction, in ≤ 3 sentences, no digits.
4. ≥ 1 story beat per checkpoint-adjacent stretch; beats observe the
   world, they do not explain controls.
5. Win line rewards; never grades; never repeats a beat.
6. Pack's final level carries a **Milestone** with a fresh flourish.
7. ≥ 1 new menu quote referencing the new region.
8. Named characters use canon pronouns; nobody is re-villainized.
9. No count/status words that the next pack will invalidate (Evergreen
   Law).
10. Hardest moment ≤ previous pack's hardest moment.
11. Running gags advanced, not pasted.
12. If the movement closes: Epilogue rewritten; menu receives its
    permanent change; celebration lap contains no new danger.

## 10. Where the machinery lives

| What | Where |
|---|---|
| Epilogue, menu quotes | `Assets/Scripts/Story.cs` |
| Mission / WinLine / StoryBeats / Milestone per level | the `LevelPack*.cs` files and `LevelLibrary.cs` |
| The Milestone field | `LevelDefinition.Milestone` (win-screen banner) |
| Win banner, game-over line, recap, gift copy | `Assets/Scripts/UIManager.cs`, `Assets/Scripts/DailyGem.cs` |
| Geometry/metadata audit (the other half of the QA gate) | `Assets/Tests/EditMode/LevelAuditTests.cs` |

*Story is the delivery vehicle for the whole expansion contract. When in
doubt, build the feeling, and the obstacle will introduce itself.*
