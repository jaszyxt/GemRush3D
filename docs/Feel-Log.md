# Feel Log — the user's playtest verdicts

**Purpose.** I cannot feel the game. I can measure a contrast ratio, count
particles, and verify that a spring settles — but I cannot tell you whether
a jump *responds* well, whether a haptic is too much in the hand, or
whether a colour is charming. Those come from the user playing it.

Every entry below records a **verdict and its reason**. The reason is the
valuable half: "the jump buzz feels excessive" became a standing rule
(D-1) that now guards every future feature. Ten entries of this teach me
more about this game's intended feel than any amount of reading the code.

**How to use it:**
- The user says something feels wrong (or right) → it goes here with their
  words and the reason.
- A verdict that generalises becomes a directive in
  `docs/User-Directives.md` and gets a CI guard if one is possible.
- A verdict that is local (this level, this one sound) stays here as a
  note so it is not "fixed" back later.
- **Never** record a verdict here that the user did not give. A guess in
  this file is worse than an empty one.

Format: date · what was judged · the user's verdict · the reason ·
followed up how.

---

## 2026-09-21 · Haptics on movement

**Verdict:** too much. *"I don't want haptics on every move by pip esp the
jump because it's excessive buzzing on my hands. I want it when it's
really needed."*

**Reason:** the phone is held in the hands, so a buzz on every hop is not
feedback, it is noise — and it devalues the buzz that matters.

**Followed up:** became directive **D-1** in `docs/User-Directives.md`,
with a CI guard (`tools/check-directives.sh`) that fails if a haptic
reappears on the jump, skid, landing or per-gem pickup.

---

## 2026-09-21 · The star trail changing colour between devices

**Verdict:** reported as a bug — gold on the phone, green on the laptop.

**Reason (mine, corrected):** not a bug. The trail is a progress reward
(gold → pink → green as stars are earned) read from per-device saves. The
laptop had simply earned fewer stars.

**Followed up:** the real defects were legibility and staleness, both
fixed (the trail announces each tier once, and re-tints live when a
milestone is crossed). **Lesson worth keeping:** a reward that changes
silently will be read as a bug. Any future state change that is not
self-explaining needs to announce itself.

---

## 2026-09-21 · HUD legibility

**Verdict:** (raised by me from measurement, not by the user playing)
the HUD had no background and the gold Lives counter sat at ~1.02:1
against the winter sky.

**Reason:** captured in-engine and confirmed unreadable.

**Followed up:** added the top-band scrim. **Still awaiting the user's
verdict on strength** — 55% opacity was my choice, not theirs. This entry
stays open until they look at it on a phone.

---

## Open questions waiting on a playtest

These are judgement calls I made without the user's senses. Each one
should come back here with a verdict:

1. **HUD scrim strength** — 55% opacity. Too dark? Too subtle?
2. **Checkpoint and milestone haptics** — set to Medium (the rung that was
   previously dead code). Is the phone's buzz the right weight, or should
   these drop to Light?
3. **Hazard vs the violet Bell Towers sky** — measures 1.08:1 raw, which
   is borderline; the arm is emissive and I verified similar cases read
   fine, but this specific realm was never eyeballed. Does a spinner read
   clearly there?
4. **The photo-mode shutter sound** — a new two-part click, synthesized by
   me with no audio judgement. Good, or distracting?
5. **Snow ambience** — a high, still hush. Peaceful, or eerie?
