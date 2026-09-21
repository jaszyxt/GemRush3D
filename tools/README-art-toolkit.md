# Art & verification toolkit

Tools I built to substitute measurement for judgement where judgement is
unreliable — my eyes on a still frame, my guess about phone performance,
my memory of what a constant used to be.

**Rule for all of them:** a tool that reports is not a tool that decides.
The two that gate CI are conservative on purpose, because a gate that
cries wolf gets switched off, and a switched-off gate is worse than none.

| Tool | What it does | Runs where |
|---|---|---|
| `tools/check-directives.sh` | **GATES.** Fails when a protected user directive is broken (haptics on movement, inline palette literals, the directives file itself). | CI + local |
| `Assets/Tests/EditMode/ContrastAuditTests.cs` | **GATES + reports.** Legibility floors (hazard, goal, greyscale separation, palette families) plus a full object-vs-sky matrix. | CI headless + editor |
| `tools/check-motion.sh` | Reports. Inventories motion constants outside the shared helpers; **fails** only on a genuinely duplicated spring. | local |
| `Assets/Editor/VisualBaselineProbe.cs` | Reports. Captures fixed realm/UI scenes to a baseline and diffs later runs. | editor play mode |
| `Assets/Editor/BudgetProbe.cs` | Reports. Per-realm particle counts, emitters, renderers, frame timing. | editor play mode |
| `docs/Feel-Log.md` | Records the user's playtest verdicts and their reasons. | prose |

## Quick start

```sh
# The fast, no-Unity, no-license gate (also what CI runs):
bash tools/run-tests-headless.sh      # 39 tests incl. the contrast gates
sh tools/check-directives.sh          # protected user directives
sh tools/check-motion.sh              # motion-constant inventory

# In the editor, in play mode, from the GemRush menu:
#   GemRush/Visual Baseline/Capture     (write the baseline)
#   GemRush/Visual Baseline/Compare     (diff against it)
#   GemRush/Visual Budget/Record        (per-realm numbers)
```

## Why each one exists

**`check-directives.sh`** — I got a user directive wrong once (I added
haptics to every hop *after* writing that haptics should be reserved for
events). A rule in a document did not stop me; a failing check would
have. It is narrow on purpose: it polices what a script can prove, and
the rest stays documented law.

**`ContrastAuditTests.cs`** — I did the contrast math by hand during the
readability pass. Hand math does not survive new content. Two things it
caught immediately: my own luminance formula was wrong (naive channel
average gave 2.29:1 where the true WCAG figure is 3.37:1 — a false
alarm), and the object-vs-sky matrix is now regenerated on every run
instead of reconstructed from memory.

**`check-motion.sh`** — the motion study found six hover-bob frequencies
and three hand-rolled copies of one pop curve. Reading for that once does
not prevent it recurring. On its first run it immediately found the pop
curve still duplicated in two files, which I then consolidated.

**`VisualBaselineProbe.cs`** — my weakest verification habit was looking
at one screenshot, alone, once. A baseline plus a numeric diff means a
change to a realm nobody thought to check announces itself. It reports
"this moved", never "this is worse" — the second is a human call.

**`BudgetProbe.cs`** — I cannot feel a phone's frame rate. I cannot even
honestly estimate it from a desktop editor. What I *can* do is record the
same numbers every release and watch them move.

## Deliberate limitations

- **No beauty judgement anywhere.** Every tool here measures separation,
  count, or change. None of them can tell you a thing is *good*.
- **No device truth.** Frame timing in the editor is the editor's. The
  budget report is for comparing runs, not for declaring a phone safe.
- **Thresholds carry the reason for their value.** Where a floor looks
  arbitrary, the comment says what it was measured from and what it is
  protecting. Changing one means updating its reason.
