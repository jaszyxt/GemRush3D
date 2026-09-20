# Audio Design — Gem Rush 3D

**Owner:** audio agent · **v1.0** · 2026-09-18
**Everything is synthesized at runtime. There are zero audio files, and
that is a feature** — new sounds are code, reviewed like code, and never
missing from a build.

## 1. Sonic identity

"A tiny warm hero in a big soft sky." Two timbre families carry the whole
game (all in `SfxSynth`):

- **Chime** — pure sines with shimmering, fast-dying upper partials
  (`ChimePartials`). Everything positive: gems, hearts, stars, bells,
  gifts, UI.
- **Breath** — one-pole low-passed noise with sweeping cutoff
  (`NoiseVoice`). Motion and weather: jumps, landings, wind, teleports,
  page turns.

Dark moments invert the palette: low hollow sines, dark filtered noise.
Positive events sit near **C-major pentatonic**, so anything the player
does lands in key with the score. Playing the game literally plays music.

## 2. The score (`MusicSynth`)

One pad loop per `SoundMood`, chosen per level by `LevelDefinition.Mood`
(`Auto` resolves from the realm flags: flight > dark > garden > mirror >
day). Spec per mood: four chords, chord tempo, voicing weights (octave
harmonic, sub warmth, detune width, glassy shimmer), optional motif.

| Mood | Realm | Character |
|---|---|---|
| Day | Packs 1–3 daylight | the home sound (C–Am–F–G) |
| Dark | Undercloud | low, sub-heavy + rumble bed |
| Sunset | Two Suns | golden Fmaj7–C–Am7–G6 |
| Garden | Sky Garden | Cadd9 voicing + music-box twinkles (matches melody gems) |
| Wind | Far Isles, Storm Chasers | open suspensions + wind bed |
| Bells | Bell Towers | low Am–G–F–Em + one soft bell per chord change + airy bed |
| Flight | Gloomfang's Day Off | wide-detuned wash, weightless |
| Mirror | Mirror Skies | modal sus2 chords, glassy shimmer |
| Menu | — | slower maj7 theme with an answering phrase |

**Load-bearing loop length:** gusts phase-lock to the music clock
(`AudioManager.GetMusicPhase`), so every mood that hosts gusts keeps four
2.2 s chords (the 8.8 s loop). Gust periods and active windows divide it,
so onsets land on chord boundaries and the swell "plays the chord".

Ambience beds (wind / high wind / rumble) live on the same wind channel
as gameplay wind; the mood sets a base level and updrafts pulse above it.

## 3. Event inventory (what answers the player)

| Moment | Sound | Notes |
|---|---|---|
| Jump | breathy puff + rising chirp | 3 baked pitch variants, randomized |
| Land | thump + dust | scales with impact; silent below 2.5 |
| Gem | chime | combo ladder: chained pickups climb semitones, resets on death |
| Melody gem | music-box note | Sky Garden; same scale as the Garden motif |
| Checkpoint | three rising chimes | |
| Heart | three notes over a low bed | "a hug in C" |
| Bounce pad | spring boing | fast rise, wobble settle |
| Death (hazard) | crack + dark drop | |
| Death (fall) | wind rush + distant poof | |
| Last life lost | two gentle notes | "careful now" — never punishing |
| Game over | low thud + minor swell | pad ducks under it |
| Level won | stab + run + sparkle | pad ducks; stars ding as they land; NEW! flourish |
| Game beaten | four swelling chords | head-silenced so it follows the fanfare |
| Sky Garden win | rising music-box run | under the fanfare, with the bloom wave |
| Bell | hum-heavy inharmonic strike | rings for the echo's length + tail |
| Echo bridge | rising chimes / falling answer | materialize vs. expire |
| Guardian wake | rising growl | sleeping spinners at half-speed; distance-faded |
| Gust | Nim's giggle, then chord-rooted swell | giggle 0.55 s before onset — the telegraph levels promise |
| Updraft | wind bed swells | heartbeat: re-asserted every frame inside |
| Goal portal | warm hum | proximity-faded; heard before seen |
| Mirror door | glass cluster + travel breath + arrival | |
| Gift (daily star) | three bell-toned notes | distinct from a win |
| UI | tiny tick everywhere; panels breathe; toggles blip up/down; pause dips, resume rises; epilogue pages turn | never louder than gameplay |

Distance rule: one-shot world sounds scale by `AudioManager.Falloff`
(quadratic within a per-event range) so far-off things stay quiet.

## 4. Channels & mixing

| Channel | Content | Level |
|---|---|---|
| `source` | all one-shots | per-clip synth volumes (UI ≈ 0.2, gameplay ≈ 0.3–0.5) |
| `musicSource` | mood pad | 0.55 master × 0.13 synth; ducks to 35% on death/win, restores 2.5 s |
| `windSource` | mood bed + updraft pulse | bed 0.09–0.5 by mood, pulse +0.75, decays 2.2/s |
| `humSource` | portal proximity | ≤ 0.16, fades 1.4/s |

Everything is gated by `SaveSystem.SoundOn` (music and ambience are part
of "Sound"). All fades are Update-driven on unscaled time — no
coroutines, pause-safe by design.

## 5. How to add a sound

1. Build the voice in `SfxSynth` from `Voice`/`NoiseVoice` (add a named
   timbre if it's a new material), expose it, and synthesize it in
   `AudioManager.Awake` if short, or lazily into `noteCache` if long.
   `FinalizeClip` peak-guards everything — always go through it.
2. Keep positive sounds in C-major pentatonic (C D E G A).
3. Expose a `PlayXxx` wrapper; gate through `PlayIfOn`.
4. For world events, scale by `Falloff(position, range)` and rate-limit
   (edge-trigger, like `GustZone.wasTelegraph`) so it can't machine-gun.
5. Hook the call site; run the offline check
   (`tools/stubs/UnityStubs.cs` must cover any new Unity API you use).

## 6. Known boundaries

- Playback is 2D by design (the camera follows the player everywhere).
- Clips are mono; width comes from detune, not pan.
- One shared SFX source: overlapping PlayOneShots sum — the peak guard
  and quiet synth levels keep stacking from clipping.

## 7. Voice over (chapter added by the VO pass)

The realm has a **narrator**: ~185 lines (40 missions, 40 win lines, 81
story beats, 13 milestones, 4 epilogue pages, 13 menu quotes, ~4,300
words) are read aloud by a warm storyteller voice. Full design, setup
and regeneration live in `tools/voice/README.md`; the essentials an
audio owner must know:

- **Pipeline**: C# text → `GemRush/Voice/Export Voice Lines` →
  `Assets/Resources/Voice/manifest.json` (id, cast, FNV-1a text hash) →
  `python tools/voice/generate.py` (Kokoro-82M, Apache-2.0) → committed
  OGGs at 24 kHz mono, −16 LUFS. Voice is the project's first file-based
  asset class, kept "code-first" by the regenerating pipeline.
- **The hash contract** is the load-bearing rule: `VoiceOver.Play(id,
  text)` refuses to play a line whose on-screen text does not hash to
  the value recorded at generation time. Editing prose never ships stale
  or contradictory audio — the line just falls back to text-only.
- **Mixing**: the voice is its own channel (`VoiceOver`, source priority
  0, loudness pre-normalized so no runtime volume rides). While it
  speaks, `AudioManager.DuckFor(line + 1.2 s, 0.16)` holds the score
  down; the existing sting duck (0.35 / 2.5 s) is untouched.
- **Timing**: mission cards and story toasts keep their own timers but
  refill while their line plays (`holdIntroForVoice` / `holdToastForVoice`
  in `UIManager.Update`) — text stays until the voice finishes.
- **One voice, one source**: newest line interrupts; pause, level change
  (`BuildWorld` stops voice + unloads clips) and the `VoiceOn`/`SoundOn`
  gates silence it immediately.
- **Casting**: narrator = `af_heart` @ 0.95×. **Phase-2 pilot live:** the
  13 menu quotes now use the `gloomfang` cast (am_michael @ 0.92×,
  pitched 0.89, bass-shelved) — Gloomfang mutters his own lines. Marked
  pilot: the listen pass decides whether it stays. The manifest's cast
  field controls per-line casting; the exporter and bootstrap both
  emit quotes as `gloomfang`, locked by `Manifest_Quotes_UseGloomfangCast`.
- **Fatigue pass (2026-09-20):** repeated sounds never play one static
  take. Land has 3 baked pitch variants; jump adds volume jitter
  (±1 dB) on top of its variants; UI clicks jitter ±1.5 dB; melody
  notes get music-box humanization (±1 dB). Variant pitch sets are
  locked by `RepeatedSounds_HaveBakedVariation`. Levels unchanged
  (verified via measure_mix.py).
- **Bootstrap restore:** the offline ManifestBootstrap now preserves
  file/duration from the prior manifest (a re-export had zeroed 172
  durations — caught by `Manifest_EveryLine_HashMatchesText` in CI
  shape; durations were ffprobe-restored). `Resources_HasNoOrphanClips`
  keeps unreferenced OGGs out of the build.
- **Casting (original):** narrator = `af_heart` @ 0.95×. `cast.py` pre-tunes a
  Gloomfang voice (deeper, bass-shelved) for phase 2 quotes, and the
  generator is deliberately engine-agnostic — upgrading to
  Qwen3-TTS/Chatterbox later is one function swap, not a redesign.
- **Why not cloud/free-tier TTS**: ElevenLabs' free tier and Edge-TTS
  are non-commercial / ToS-gray for a published game; Kokoro and
  Qwen3-TTS are Apache-2.0, Chatterbox MIT. CC0 voice libraries cannot
  voice custom prose.

## 8b. Gap pass (2026-09-24) — sounds that were missing entirely

An audit swept every interactive moment against its audio (the inverse of
the calibration pass, which fixed sounds that existed). Eleven silences
were found; six were judged worth filling, all reusing existing DSP
voices — no new pipelines, no size growth:

| Moment | Was | Now |
|---|---|---|
| **SeeSaw** (Homecoming's signature object) | completely silent | low wooden groan as Pip's weight tips it, lighter settle as it springs level — both edge-triggered, distance-faded |
| **Player skid** | dust puff, no sound | scrape under the puff; the 0.25 s cooldown and 6 u/s gate were already built |
| **Level unlock** | `SaveSystem.UnlockLevel` fired silently | two bright notes placed *after* the star cluster, via the UI's existing delayed-timer pattern |
| **Menu / Atlas page flips** | navigated silently | reuse the existing `PlayPageTurn` clip (previously used only by the epilogue) |
| **Shelf trophy** | visual twinkle only | one keepsake chime per arrival (not per trophy) |
| **Star-trail tier** | written announcement, no sting | rising three-note figure, one-shot per tier |

**Deliberately still silent** (recorded so they are decisions, not
oversights): running footsteps (a step loop at 8 u/s would dominate the
mix), wall bumps, jump-cut on release, the idle-ladder rungs (the ladder
is authored wordless), the Aurora Ribbon ride (it is a mover, and movers
are silent by convention), **`RestBeat`** (its whole design is "nothing
is asked of them, nothing is timed" — sound would contradict the beat),
**`GoldenSignal`'s hint layer**, and — most notably — **Winter's ambience
bed**: `SoundMood.Winter` has no bed while every other realm does. The
code documents it as "the quietest pack", so the silence under the pad
is intent; a snow bed would contradict it. Left as-is.

On `GoldenSignal` specifically (decided 2026-09-24): the golden gem's
*pickup* plays `PlayGift`, so the discovery beat is voiced. The hint —
the glimmer, trail and found-outline that lead you to it — stays
wordless on purpose. A hidden objective should be *noticed*, not
announced; attaching a cue to a persistent visual turns a diegetic clue
into a UI marker with a bell on it. It is also the wrong shape for
sound: unlike a trophy or a tier (one-shot progression beats), a hint
loops for as long as you are near an undiscovered gem, so any cue would
either become wallpaper or fire too rarely to help. If playtesting ever
shows players genuinely missing the gems, the fix is a stronger
*visual* (faster glimmer, brighter trail) before it is a chime.

**Defect caught by measurement:** the first SeeSaw creak was numerically
correct and perceptually gone — a two-pole low-pass at ~120–300 Hz throws
away almost all noise energy, landing it 13 dB under every other world
sound. Gains raised; `EveryVoice_SitsInTheAudibleWorldBand` now fails any
cue measuring below −35 dBFS, so a future silent-by-accident voice is a
test failure rather than a shipped absence.

## 8. Audit log

**Audit v1 — 2026-09-19 (post-generation, against v1.22.0)**

- **Hook integrity**: all 7 `VoiceOver.Play` call sites, keep-alive
  fields, `DuckFor`, `VoiceOn` and manager wiring verified present in
  HEAD after two intervening feature commits — no clobbering.
- **Text drift**: manifest re-collected from current code; 185/185
  hashes identical to the shipped manifest — every clip still matches
  its prose (B-Sides aliases included).
- **Clip DSP** (all 185): uniform Vorbis/24 kHz/mono; zero clipping,
  zero DC offset, no dead air > 1 s lead / 1.5 s trail; speech rate
  1.3–4.2 words/s on every line (no truncated or garbled output).
- **Loudness**: −16 LUFS ±0.7 across spot checks (mission, epilogue,
  quote, beat, win) — on the mobile narration target.
- **Runtime path (in-editor probe)**: manifest parses via the exact
  `JsonUtility` path; 185/185 clips resolve via `Resources.Load`;
  loaded length 1470.7 s matches the manifest (no import truncation);
  importer settings confirmed (mono/24 kHz).
- **Hash chain in-editor**: `VoiceOver.Hash` recomputation matches
  stored hashes for all 185 entries; 13 alias links resolve; the
  mission-1 play gate passes.
- **Known-benign**: Unity logs "Warnings during import of AudioClip"
  for ffmpeg-produced Vorbis files — the muxer's vendor tag in the
  Vorbis ident header is format-mandated and cannot be stripped. Clips
  import fully and load cleanly; no action possible or needed.

## 9. Mix calibration v2 (2026-09-19, measured)

`tools/audio/measure_mix.py` replicates the runtime DSP in Python and
prints the game's true loudness map (peak / gated RMS / ITU-R BS.1770
LUFS / spectral centroid per clip at play level). Run it after any
synth change; judge against the table, not by ear at 2 am.

**Measured before calibration** (grounded in industry norms: mobile
≈ −18 LUFS integrated, priority hierarchy voice > key SFX > music >
ambience, 6–12 dB separation for clarity moments):

1. **The score masked the game.** Music measured −15.6..−18.5 LUFS —
   as loud as or louder than the pickups/checkpoints it should sit
   under (0 dB separation; norms say 6–12 dB).
2. **All noise was hiss, not breath.** Single-pole filtered noise left
   spectral centroids at 5.2–6.7 kHz on the gust/fall/mirror family —
   "static", the hidden half of the annoying-gust complaint.
3. **Bells sustained at −16 LUFS for up to 9 s**, the loudest world
   sound, with a shrill 8.93× inharmonic top.
4. **Panel/intro/page whooshes were mute** (−41..−43 RMS, ~12 dB under
   the UI click) — designed moments that phone speakers cannot play.
5. **Narration at −16 LUFS** was on the mobile ceiling rather than in
   the world.

**Applied calibration (device-level targets):** music bed −27..−30
LUFS (`MusicVolume` 0.55 → 0.26); key SFX unchanged at −17..−21
(8–10 dB separation); bells −19 (0.42 → 0.30 + softened 5.42×/8.93×
partials); narration −20.4 (VoiceOver source 0.62) with music ducked
~12 dB under it (DuckFor fraction 0.3); UI whooshes raised into
audibility (−31..−33); NoiseVoice and NoiseSwell upgraded to cascaded
two-pole filters (−12 dB/oct) with ~+4 dB gain compensation — gust
centroid 6715 → 3194 Hz, fall death 5217 → 2379, land 3634 → 1090
(now reads as a thump, not static).
