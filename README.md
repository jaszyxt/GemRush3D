# Gem Rush 3D

A tiny 3D platformer built with Unity — and with **zero manual editor setup**:
the whole game (levels, character, UI, audio, icon, effects) is constructed
from code. Even the app icon is painted procedurally at build time.

![Genre](https://img.shields.io/badge/genre-3D%20platformer-green)

## Release build (already configured)

`Builds/GemRush3D.apk` is a **release-signed** build (v1.30.3, IL2CPP,
arm64). Signing uses `tools/gemrush.keystore`; its password lives in
`tools/signing.txt`. Both are git-ignored — back them up somewhere safe:
all updates to a published game must be signed with the same key.

To rebuild headlessly (close the Unity editor first):

```
Unity.exe -batchmode -quit -nographics -projectPath <path-to-GemRush3D> ^
  -executeMethod GemRush.EditorTools.BuildAndroid.Build -logFile build-log.txt
```

The build script automatically sets the product name, version, adaptive app
icon and release signing (including the `useCustomKeystore` flag — without
it Unity silently falls back to debug signing).

## Play it

1. Open **Unity Hub** and install **Unity 6 LTS** if you haven't (any 6000.x;
   this project targets `6000.6.0f1`).
2. **Projects → Add → select this `GemRush3D` folder** → open it.
3. Double-click `Assets/Scenes/Game.unity`, then press **Play**.

That's it. The scene file is intentionally empty — a bootstrap script builds
everything at runtime, so there is nothing to wire up by hand.

## Controls

| Action | Keys | Gamepad |
|---|---|---|
| Move | `W A S D` or Arrow Keys | Left Stick / D-Pad |
| Jump | `Space` | A / Cross (hold to fly as Gloomfang) |
| Pause / Resume | `Esc` (or Android back) | Start |
| Menus | Arrows + `Enter`, mouse | D-Pad/Stick choose · A confirm · B back · shoulders flip level pages |
| Close Settings / quit dialog | `Esc` (or Android back) | B |
| Quit | from the menu: `Esc`, then confirm on desktop (Android back never quits) | B, then confirm |

The menu hint line follows the last-used device: plug a gamepad in and the
instructions switch to the pad layout (rumble on death/win obeys the Haptics
setting).

## The game

The storm scattered the sky realm's **Sunstones**. You are **Pip**, the
Sky-Keeper's little helper: hop the islands, reclaim the gems, light each
portal — and chase the storm. Forty levels in thirteen packs and B-Sides, rated 1–3 stars
each:

**Pack One — The Storm**
1. **First Steps** — the tutorial trail: one mover, one spinner, one
   checkpoint. 10 gems.
2. **Spinner Gauntlet** — two spinners (the second faster), a fast mover,
   narrow bridge islands. 12 gems.
3. **The Ascent** — a vertical climb chained together with elevators, up to
   the summit arena. 14 gems.

**Pack Two — The Rematch** (unlocks as you clear levels)
4. **Stormcell Steps** — riding fast horizontal movers across a live storm
   front, two spinner arenas. 11 gems.
5. **Gloomfang's Garden** — a three-guardian spinner gauntlet over narrow
   4×4 bridge islands, with elevator rides between tiers. 12 gems.
6. **Skyfall Summit** — the rematch: a long vertical climb chaining three
   lifts with tight hop chains, summit in the clouds. 13 gems.

**Pack Three — The Undercloud** (the storm's secret)
7. **The Long Fall** — the realm's first *descent*: fall down through the
   cloud floor, dodging a spinner shelf at the bottom. 12 gems + a spare
   life.
8. **The Undercloud** — the dark beneath the sky: indigo fog, dim sun,
   two fast guardian arenas and a speed ferry. 14 gems.
9. **Heart of the Storm** — the finale: a long climb to the bottom of
   everything, two guardians, and a very apologetic storm. 16 gems + a
   spare life.

**Pack Four — The Two Suns** (post-story celebration lap)
10. **Twinlight Terrace** — a golden-hour victory lap: one tame guardian,
    two bounce pads, one ferry ride into the sunset. 14 gems.
11. **Raindance Revels** — Gloomfang's first official Tuesday rain:
    dancing guardians, an elevator, a confetti ferry and a bounce pad.
    15 gems.
12. **Pip's Homecoming** — the first course, revisited: same islands, same
    gentle guardian, one last walk to the shelf by the window. 11 gems.

**Pack Five — The Far Isles** (charting the unknown, on standing winds)
13. **Tailwind Point** — first contact with the updrafts: stand in the
    wind and it carries you. 13 gems.
14. **Monsoon Mosaic** — Gloomfang's weather-support audition: dancing
    guardians, an updraft and his (now inspected) rain. 14 gems + heart.
15. **The Last Blank Space** — the farthest point on the map: two
    guardians, two updrafts, and the corner where the atlas ends.
    15 gems + heart.

**Pack Six — Gloomfang's Day Off** (secret, unlocks after level 15)
16. **Gloomfang's Day Off** — play *as* the storm himself: no gravity,
    hold jump to rise, deliver the last Sunstones through a sunset run.

**Pack Seven — The Sky Garden** (Gloomfang's rain woke it)
17. **First Blooms** — meet the **sleeping guardians**: they spin only
    while you linger nearby, so keep moving and they keep dreaming.
18. **Petal Drift** — petals ride the wind; a lift, a spare life and
    livelier dreams.
19. **The Blooming Gate** — three guardians, one of them a light sleeper.
    Touch the portal and the whole garden **blooms** in a wave.

**Pack Eight — Storm Chasers** (a runaway baby cloud named Nim)
20. **Gust Alley** — the gaps are too wide to jump; wait for Nim's giggle
    and the **tailwind gust** carries you across.
21. **Where Nim Laughs** — gust lanes cross the sleeping garden. + heart.
22. **The Baby's Home** — the longest wind lanes in the game, and a nest
    of clouds with a very small snore in it. + heart.

**Pack Nine — The Bell Towers** (they once sang storms home)
23. **The First Bell** — ring an **echo bell** and hidden bridges turn
    solid for as long as the tone sings.
24. **Chorus in the Clouds** — three bells, three bridges, and guardians
    who do not appreciate bell music. + heart.
25. **The Silent Spire** — the tallest tower, the quietest bell, and the
    one note no one has ever heard. + heart.

**Pack Ten — Mirror Skies** (a mirrored sky over the Far Isles)
26. **Mirror Lake** — walk into a **mirror door**, walk out of its twin
    across the water. The way in is never the way out. 12 gems.
27. **Twin Towers** — one door exits five floors up: a staircase made of
    light, guarded by someone who never noticed it. 14 gems + heart.
28. **The Mirror Meadow** — three door pairs over the meadow, and a
    translucent Gloomfang drifting on the wrong side of the glass,
    copying your every move. 15 gems + heart.

**The B-Sides** (remixed night versions of your favorites, unlocked late)
29. **Gust Alley — Nightfall** — the same alley after dark, with a night
    safety isle and Nim's snores in the lanes. 15 gems.
30. **The Garden That Dreams** — the sleeping garden, now with wind lanes
    that sway the beds in time. + heart.
31. **The Ascent — Nightfall** — climb the beacon route in the dark, the
    way the first keepers did. From the summit: every portal you ever
    lit, still burning.

**Pack Eleven — The Long Winter** (the quietest pack: snow that never melts)
32. **First Snow** — wake the **sunstone lantern** and its warm light
    travels with Pip, melting the frozen gates in his path. The ice
    never hurts; it only waits.
33. **Frozen Fountains** — the updraft columns hum under the ice. Melt a
    door while hovering, ride the ferry straight through another.
    13 gems.
34. **The Crystal Summit** — one last climb past a napping guardian to
    the winter's rooftop, where every melted path refreezes into a
    crystal map of the whole walk. 14 gems + heart.

**Pack Twelve — The Aurora Festival** (the realm's thanks — feeling: joy)
35. **Festival Lights** — the aurora lays itself across the gaps as
    **ribbons of light**: walk on when the glow reaches your shore and
    ride the sway. 12 gems.
36. **Ribbon Dance** — the ribbons learn to swing, and one climbs while
    it crosses. A guardian naps through the whole concert. 13 gems +
    heart.
37. **The Festival Finale** — the concert: one lap through every mechanic
    in the atlas, played as instruments. Clearing it lights a **permanent
    aurora over the menu**. 16 gems + 2 hearts.


**Pack Thirteen — The Homecoming** (the last chart: the long way back)
38. **The Long Way Home** — **see-saw planks** tip under Pip's weight;
    stand on an end and the treat shelves below tip up to meet you.
    12 gems.
39. **Tipping Points** — longer planks, one that tips sideways out of
    pure spite, and a guardian complaining about the creaking in its
    sleep. 13 gems + heart.
40. **Coming Home** — the last stretch, walked at sunset, ending at a
    small island with a shelf. Gloomfang's badge stops saying
    "probationary". 14 gems + 2 hearts.

Stars: **3** = all gems, **2** = half, **1** = finished. Best times and stars
are saved on the device; clearing a level unlocks the next (the B-Side
remixes are optional extras, opened by finding a level's hidden golden
gem — they never block progress). Touching a red
spinner arm (or falling) costs a life; you carry 5 per attempt — **heart
pickups** in the tougher levels grant one back (up to 8). **Bounce pads**
launch Pip sky-high; gems at their apex are yours if you dare. **Updraft
columns** in the Far Isles let Pip float to places jumps can't reach.
**Sleeping guardians** in the Sky Garden only wake when you linger.
**Echo bells** in the Bell Towers turn hidden bridges solid while their
tone sings. **Tailwind gusts** in Storm Chasers carry Pip across gaps
too wide to jump. **Mirror doors** in the Mirror Skies hop Pip between
paired points.
The menu has level select (locked levels unlock as you clear; touch
devices page the grid and open it on the newest level), settings for
sound, screen shake, haptics, shadows, left-handed touch controls, text
size — and fullscreen on desktop (reachable from the pause menu too, so
nothing needs quitting mid-run) — and every level starts with a short
mission card. Checkpoints flash one-line **story beats** as you pass them — every level
has them — clearing the last level of a pack stamps a milestone banner on
the win screen (the atlas grows a page), finishing the last level plays a
four-page **epilogue**, and
the menu rotates Gloomfang flavor quotes. Pause anytime with the HUD button or Esc.
Esc/back always walks the navigation stack: it closes dialogs and settings
first, then pauses/resumes, and from the desktop title menu it asks before
quitting — it never hard-quits. Every screen keeps clear of phone notches,
punch-hole cameras and gesture bars, and touch buttons hold a ~48 dp
minimum size.

## The sound of the realm (all synthesized at runtime)

Every sound — effects, score and ambience — is synthesized from raw
samples when the game boots; there are zero audio files. Two timbre
families carry the whole game: **chimes** (struck-crystal partials) for
everything positive, and **breath** (filtered noise) for motion and
weather — so the game sounds like one place. Positive events sit near
C-major pentatonic, which means playing the game literally plays music:

- **Per-realm moods** — each pack has its own pad loop: warm Day, low
  Dark (Undercloud), golden Sunset (Two Suns), twinkling Garden (Sky
  Garden, whose melody gems play the same scale), airy Wind (Far Isles,
  Storm Chasers), bell-toned Bells (Bell Towers), weightless Flight
  (Gloomfang's Day Off) and a glassy Mirror Skies. The menu hums its own
  gentle theme. Some moods carry an ambience bed — wind in the wind
  realms, a deep rumble in the Undercloud.
- **Responsive world audio** — landings thud in proportion to impact,
  falls whoosh (hazards crack), updrafts swell the wind while you're in
  them, the goal portal hums louder as you approach, echo bridges chime
  as they take shape and sigh as they fade, sleeping guardians growl
  awake, and Nim **giggles** half a second before each gust — the
  telegraph the levels promise. Gust onsets land on chord boundaries:
  the wind audibly plays the chord it is phase-locked to.
- **Reward grammar** — chained gem pickups climb a semitone ladder (a
  gem run becomes a riff), win screens ding each star as it lands,
  records get a flourish, the full-game ending swells four chords, and
  the last life lost plays a gentle "careful now" cue before the
  game-over sting.
- **Polite UI** — every button answers with the same tiny tick; panels
  breathe open and closed; settings toggles blip up (on) or down (off);
  epilogue pages turn quietly. UI never speaks louder than gameplay.

## The voice of the realm (narrated by Kokoro)

The realm's story is **read aloud**: every mission briefing, checkpoint
story beat, win line, milestone, the four-page epilogue and the menu's
flavor quotes (~185 lines) are narrated by a warm storyteller voice. The
voice is not a recording — it is **generated** by the open-source
[Kokoro-82M](https://huggingface.co/spaces/hexgrad/Kokoro-TTS) model
(Apache-2.0) from the game's own writing, loudness-normalized to mobile
standard, and committed alongside the pipeline that can regenerate every
line. The project's "zero imported assets" rule survives intact: voice
clips are build artifacts of code and text, reproducible with two
commands (see `tools/voice/README.md`).

Design rules of the narration:

- **Voice never contradicts the page.** Every line is hashed; if the
  writing changes before regeneration, that line simply plays as
  text-only — stale audio can never ship.
- **The score yields.** While the narrator speaks, the music ducks and
  breathes back after; mission cards and story toasts hold on screen
  until their line finishes.
- **One voice at a time.** The newest line wins; pause, level change or
  the Voice setting stops it instantly. Voice rides under the master
  Sound toggle (`SaveSystem.VoiceOn`).
- **Casting is data.** The narrator is Kokoro's `af_heart` at 0.95×;
  the menu's flavor quotes are read by **Gloomfang himself** — a deeper,
  warmer voice (`tools/voice/cast.py`) — and swapping the whole engine
  (e.g. Qwen3-TTS) only means reimplementing one Python function.
- **Nothing fatigues.** Sounds you hear hundreds of times (jumps,
  landings, buttons, melody notes) carry baked pitch variants and
  micro-jitter, so no two playbacks are identical.

## How it's built (code-first Unity)

| Script | Role |
|---|---|
| `GameBootstrap` | Entry point (`[RuntimeInitializeOnLoadMethod]`): lighting, managers, 60 FPS target, world for the current level |
| `GameManager` | State machine (menu/playing/paused/won/game over/complete), level progression, score, timer, lives |
| `LevelDefinition` | Pure data for a level: platforms, movers, spinners, gems, checkpoints, bounce pads, hearts, portal, mission text, story beats, atmosphere (sky/fog/sun) |
| `LevelLibrary` | Pack One levels in play order — add a level by adding a method + one line |
| `LevelPackTwo` … `LevelPackEleven` | Packs Two through Eleven (Rematch, Undercloud, Two Suns, Far Isles, Day Off, Sky Garden, Storm Chasers, Bell Towers, Mirror Skies, Long Winter), same pattern |
| `Story` | The completion epilogue and the menu's rotating flavor quotes |
| `BouncePad` / `HeartPickup` | Launch pad and extra-life pickup, both data-driven |
| `Updraft` / `GustZone` | Standing-wind columns and phase-locked tailwind gusts that carry Pip |
| `Bell` / `EchoBridge` / `MirrorDoor` | Echo bells with their solid-while-ringing bridges, and the paired mirror doors |
| `Gloomfang` | The storm himself, tagging along as weather support on post-story levels |
| `LevelBuilder` | Generic builder: turns any `LevelDefinition` into GameObjects |
| `SaveSystem` | Versioned `PlayerPrefs` wrapper: best times, stars, unlocked level, settings |
| `PlayerController` | Rigidbody movement, camera-relative input, coyote time + jump buffering, moving-platform carrying, hazard/fall death |
| `CameraFollow` | Smooth third-person follow camera |
| `MovingPlatform` / `Spinner` / `Gem` / `Checkpoint` / `GoalPortal` / `HazardMarker` | The interactive pieces |
| `TouchControls` | Floating virtual joystick + JUMP button, auto-created on touch devices |
| `SafeArea` | Fits the UI root to the device safe area (notches, punch-holes, rounded corners, gesture bars) |
| `UIManager` | Menu with paged level select on touch, mission intro cards, HUD (change-cached, split canvases), pause with settings, win/game-over/completion screens — all built in code |
| `SfxSynth` / `MusicSynth` / `AudioManager` | The whole game's audio, synthesized at runtime: a chime/breath DSP toolbox, per-realm mood loops and ambience beds, and the channels that play them; respects the sound setting |
| `VoiceOver` / `tools/voice/` | Narrated story: plays generated voice lines against the on-screen text (hash-verified), ducks the score; the pipeline regenerates every clip from code — see `tools/voice/README.md` |
| `Haptics` | Short vibration pulses (jump/gem/checkpoint/death/win), settings-aware; amplitude control on Android, system buzz on iOS |
| `Fx` / `ArtLib` | Particle bursts, shared procedural sprites, material palette |

Game feel: squash & stretch on jump/land, landing dust, camera shake on
death, pulsing gem glow, 60 FPS target.

There are no prefabs, no imported assets and no Asset Store dependencies —
only built-in packages plus a few UPM packages (see `Packages/manifest.json`).

> **Render pipeline: URP.** The game runs on the Universal Render Pipeline
> (BiRP is feature-deprecated from Unity 6.5). The pipeline asset and
> renderer data in `Assets/Settings/` are **generated by code**
> (`Assets/Editor/EnsureUrp.cs`) — regenerate by calling
> `GemRush.EditorTools.EnsureUrp.Create()` if they ever go missing.
> Bloom/vignette/per-realm color grading are built at runtime in
> `PostFx.cs` (a code-created VolumeProfile — still zero asset files).
> The old embedded `com.unity.postprocessing` package was removed.

## Tweak it

- **Movement feel** — `PlayerController`: `moveSpeed`, `jumpVelocity`,
  `coyoteTime`, `jumpBuffer`, `airControl`.
- **Difficulty** — lives in `GameManager.PlayLevel` (`Lives = 3`), spinner
  speeds in `LevelLibrary`, mover speed via each `MoverSpec` period.

### Adding a level

1. Write a method in `LevelLibrary` that fills a `LevelDefinition`
   (the existing three show the pattern). Design guide: gaps of 3–5 units
   and rises of ~1 unit per hop are comfortable; put a checkpoint before
   every hard section.
2. Add it to the `Levels` array. Unlocks, stars, saves and the menu's level
   row adapt automatically.

## Play it on Android

One-time setup, in two places:

**1. Add the Android build tools to the editor (Unity Hub):**
- Hub → **Installs** → find **6000.6.0f1** → gear icon → **Add modules**
- Check **Android Build Support** and leave its two sub-items checked
  (**OpenJDK**, **Android SDK & NDK Tools**) → install (~4–5 GB)

**2. Build the APK (Unity editor):**
1. `File → Build Profiles` → **Android** → **Switch Platform** (takes a few
   minutes to reimport)
2. With `Assets/Scenes/Game.unity` open, click **Add Open Scenes** so the
   scene list isn't empty
3. Either **Build And Run** with the phone plugged in (fastest way to play),
   or **Build** to produce a `GemRush3D.apk` you can copy to the phone and tap
   to install (allow "install unknown apps" when Android asks)

The game is touch-ready: a floating joystick on the left half of the screen
and a JUMP button on the right appear automatically on touch devices, the
menu has a tappable START, the game locks to landscape and keeps the screen
awake. Desktop controls keep working unchanged.

## Make a standalone build

The headless build script produces **both** targets in one pass: the
signed Android APK and a portable Windows build at `Builds/GemRush3D.exe`
(no installer needed — double-click to play, files sit beside it in
`Builds/GemRush3D_Data/`). `Builds/` holds the phone and laptop builds at
the same version, so both devices stay in sync.

## Troubleshooting

- **Hub asks to upgrade the editor version** — fine, click through; the code
  uses only long-stable APIs.
- **Scene looks empty in the editor** — it is, by design. Press Play.
- **`tools/` folder** — development-only (an offline C# syntax-check rig).
  Unity ignores it; you can delete it safely.
