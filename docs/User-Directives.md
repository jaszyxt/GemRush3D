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

## Enforcement

`tools/check-directives.sh` (run by CI on every push and pull request)
guards the mechanically-checkable parts of this file:

1. **D-2** — no inline colour literals for hazards/rewards outside ArtLib.
2. **D-1** — no `Haptics.*` call inside the jump / skid / ordinary-landing
   code paths in `PlayerController.cs`.
3. This file must not be deleted, and the directive headings must remain.

A failing guard means someone changed a protected decision without the
user's explicit instruction. **Do not edit the guard to make it pass.**
Either revert the change, or — if the user did ask for it — update this
file in the same commit with the user's words, as described above.
