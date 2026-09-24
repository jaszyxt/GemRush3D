# Movement Two — Canon Record & Craft Checklist

**Owner:** the story agent. **Status:** Movement Two is **shipped and
complete** (packs 11–13, levels 32–40).

**Working with other agents:** the story leads and the build follows it.
If a mechanic, level or prop needs a story line, that is a **feature
request with a reason attached** and it takes priority over cosmetic work
— the mechanic IS the storytelling. Do not edit story text (missions, win
lines, beats, milestones, menu quotes, the epilogue) without the story
owner's say-so; if a build needs a line changed, ask, because the line may
be load-bearing canon. The protected decisions live in
`docs/User-Directives.md` **D-7** (which wins over every story doc).

> ⚠️ **A previous revision of this file contained a 46-level speculative
> script (packs 11–16: Long Winter, Aurora Festival, First Rainbow,
> Guardian Games, Little Apprentice, Badge Ceremony) written before the
> build agents shipped their own packs. That script is retired. Do not
> paste it over shipped text.** The built packs are the canon; see §1.

This file does two jobs:
1. **Records what actually shipped** (§1) so no future agent re-invents it.
2. **Carries the craft rules** (§3–§4) learned while reviewing, so the next
   pack's text is right the first time.

---

## 1. Shipped canon — Movement Two (packs 11–13)

The real Movement Two is shorter than originally pitched, and it is
finished. Its shape: **stillness → celebration → home.** It opens with
Gloomfang asking the realm for one quiet week and closes with the realm
walking him home; the badge review resolves at the very end, in a
homecoming rather than a ceremony.

### Pack 11 — The Long Winter (feeling: calm) — levels 32–34

`Assets/Scripts/LevelPackEleven.cs` · new mechanic: **the sunstone lantern**
(wake it; its warm light travels with Pip and melts **ice gates** in the
path). No new danger — the ice never hurts, it only waits.

- **32. First Snow** — wake the lantern at the shore; learn that standing
  with the light melts a gate. *"Wake it, walk with its light, and let the
  ice remember it is water."*
- **33. Frozen Fountains** — the updraft columns froze; one gate sits
  inside a wind column, one gates a gem high up, and the ferry must be
  melted out of mid-crossing.
- **34. The Crystal Summit** — the finale. At the portal every melted path
  refreezes at once into an enormous crystal map of the whole walk, with a
  small orange shape walking in the middle of it. **Milestone:** *"REGION
  CHARTED: THE LONG WINTER. The atlas shines a little warmer now."*

### Pack 12 — The Aurora Festival (feeling: joy) — levels 35–37

`Assets/Scripts/LevelPackTwelve.cs` · new mechanic: **aurora ribbons**
(flowing bridges of light that sway as they carry Pip). The realm's
thank-you, thrown because the atlas is charted.

- **35. Festival Lights** — step on when the glow reaches your shore; two
  gentle crossings, no hazards. *"The realm has been carrying you all
  game; tonight, it carries you again, for fun."*
- **36. Ribbon Dance** — the ribbons swing and one climbs as it crosses;
  a guardian sleeps in the meadow below with one arm over the music.
- **37. The Festival Finale** — the concert built out of the whole atlas:
  movers keeping the beat, a guardian on percussion (asleep, on tempo),
  wind, a bell, a door from the mirror, the winter's lantern-light. The
  last note ends the festival sky-wide. **Milestone:** *"REGION CHARTED:
  THE AURORA FESTIVAL."*

### Pack 13 — The Homecoming (feeling: pride) — levels 38–40

`Assets/Scripts/LevelPackThirteen.cs` · new mechanic: **see-saw planks**
(tip an end down and the treats on the shelves beneath rise to meet you).
The atlas is full; the Sky-Keeper sends Pip home the long way — and
**the badge story resolves here.**

- **38. The Long Way Home** — the scenic route past the see-saw meadows
  the old keepers built for exactly this kind of stroll.
- **39. Tipping Points** — planks in every size and temperament; a
  contrarian one tips sideways to be difficult.
- **40. Coming Home** — the last level. Sunset lap through everything;
  the road ends at the little island, the shelf with one open spot, and a
  storm practicing "welcome home" without crying. **Milestone:** *"REGION
  CHARTED: THE HOMECOMING. Gloomfang's badge is official — the word
  'probationary' is gone."*

**Canon consequence — the review is RESOLVED.** The badge review that
Movement Two was built around no longer needs a dedicated pack. It
concluded at level 40: *"The Sky-Keeper, watching from the hall, took out
a pen and crossed out one word on a badge."* Any future text that treats
Gloomfang's employment as probationary is **wrong** and must be changed.

## 2. Retired material (unbuilt — reference only)

The following was written before the build agents shipped. **None of it is
in the game.** It may be mined for future packs, but only after the Story
Bible's roadmap is updated to place it — and never pasted over shipped
text.

| Retired script | What it was | Salvageable idea |
|---|---|---|
| The First Rainbow (prism gates) | rainbow ends in the Undercloud, where the light was kept | strong; a natural Movement Two region |
| The Guardian Games (sport) | taking turns as the win condition; Red Nine's carousel | Red Nine's carousel is lovely; the "no losers" framing is on-theme |
| The Little Apprentice (flicker) | Pip teaches a copying spark | strong teaching-feeling pack; the flicker is a good recurring character |
| The Badge Ceremony (pack 16) | a dedicated ceremony pack with the hat gag | **superseded** — the verdict already happened in pack 13. Do not build. |
| Pack 11/12 alternate level names | (my "Hushfall", "The Invitation", "The Concert", etc.) | discard; shipped names are canon |

## 3. Craft checklist — run before shipping any pack's text

These are the rules that reviewing Movement Two proved necessary. Each
one cites the failure that produced it.

1. **Mission cards: 30–42 words, hard ceiling 45** — for NEW text. The
   intro card shows for ~3.5 seconds. Reviewing the retired script found
   nine missions at 47–51 words. Long ones all failed the same way:
   scenery clause + instruction clause + direct-address tag. Cut one of
   the three.

   **Grandfathered exceptions — do not "fix" these.** Seven shipped
   missions in packs 3, 6, 7, 8, 9 and 10 run 46–56 words and were
   deliberately left alone (reviewed and decided, not overlooked). Two
   are protected set-pieces: pack 3's *"One last climb, Pip. Not to win.
   To listen."* (the redemption climax) and pack 6's Gloomfang's Day Off.
   The budget governs new writing; it is not a licence to retro-edit the
   game's best lines down to a number.
2. **A mission teaches ONE new mechanic.** Riding something already
   introduced (a mover, a ferry) does not count as new. A mission naming
   three unrelated hazards reads as three new mechanics. (Failure found:
   a finale mission naming nine nouns and zero verbs.)
3. **Name the new mechanic in fiction, never in spec digits.** "for as
   long as the tone sings", not "six seconds of bridge."
4. **No counts, no statuses, no "final".** See Story-Bible §5 (Evergreen
   Law). This is the rule whose violation produced the whole
   reconciliation.
5. **Vary the sentence furniture.** Structures found overused across one
   movement:
   - **"Not X, but Y"** (negate a quality, then replace it) — 9 uses.
     Two consecutive levels using it read as copy-paste.
   - **"Even X. Especially X."** — 4 uses. Works twice; a third is a tic.
   - **`[The|A] memo arrive[s|d], [adj] and official: <payload>`** — 3
     near-identical deliveries. Vary the sentence, keep the payload.
   - **em-dash "introduce a noun, then reveal its poetic meaning"** — 6
     uses, all in the same half of the movement.
   A payload (the memo words, the running gag) may repeat; the *sentence
   delivering it* may not.
6. **Win lines reward; they never grade.** No stars, no times, no
   mistakes, no "finally."
7. **Beats observe the world; they do not explain controls.** The
   in-game bell hint is a UI string, not a story beat.
8. **Don't spoil your own climax in the mission card.** A mission may
   promise the moment; it may not narrate it. (Failure found: a pack-12
   mission revealing the mirror crossing before the player sees it, and a
   win line that resolved the badge verdict four packs early.)
9. **One-line misread test.** Read every beat aloud imagining a tired
   child who has just died three times. If a line can attach to *their*
   failure, rewrite it. (Failure found: "watched you **fall** with
   style," where "fall" is the player's death verb.)
10. **Never characterise a beloved character negatively in the ledger.**
    The ledger is read by agents who will write from it. Affection only.

## 4. Where the story lives

| What | Where |
|---|---|
| Epilogue, menu quotes | `Assets/Scripts/Story.cs` |
| **All UI strings** (menu, HUD, atlas, prompts, golden notes) | `Assets/Scripts/Strings.cs` — the sanctioned home; no English literals in layout code |
| Mission / WinLine / StoryBeats / Milestone per level | `LevelPack*.cs` + `LevelLibrary.cs` |
| Region names + atlas grouping | `LevelLibrary.Regions` |
| Geometry & metadata audit | `Assets/Tests/EditMode/LevelAuditTests.cs` |
| Story law: pillars, voice, canon, roadmap | `docs/Story-Bible.md` |
