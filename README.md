# Gem Rush 3D

A tiny 3D platformer built with Unity — and with **zero manual editor setup**:
the whole game (levels, character, UI, audio, icon, effects) is constructed
from code. Even the app icon is painted procedurally at build time.

![Genre](https://img.shields.io/badge/genre-3D%20platformer-green)

## Release build (already configured)

`Builds/GemRush3D.apk` is a **release-signed** build (v1.8.1, IL2CPP,
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

| Action | Keys |
|---|---|
| Move | `W A S D` or Arrow Keys |
| Jump | `Space` |
| Start / Play again | `Enter` (or click the buttons) |
| Quit | `Esc` |

## The game

The storm scattered the sky realm's **Sunstones**. You are **Pip**, the
Sky-Keeper's little helper: hop the islands, reclaim the gems, light each
portal — and chase the storm. Fifteen levels in five packs, rated 1–3 stars
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

Stars: **3** = all gems, **2** = half, **1** = finished. Best times and stars
are saved on the device; clearing a level unlocks the next. Touching a red
spinner arm (or falling) costs a life; you carry 3 per attempt — **heart
pickups** in the tougher levels grant one back (up to 5). **Bounce pads**
launch Pip sky-high; gems at their apex are yours if you dare. **Updraft
columns** in the Far Isles let Pip float to places jumps can't reach. The
menu has
level select (locked levels unlock as you clear), settings for
sound/haptics/shadows, and every level starts with a short mission card.
Checkpoints flash one-line **story beats** as you pass them — all fifteen
levels have them — finishing level 9 plays a four-page **epilogue**, and
the menu rotates Gloomfang flavor quotes. Each level plays a synthesized
ambient chord loop — warm in daylight, low and dark in the Undercloud.
Pause anytime with the HUD button or Esc.

## How it's built (code-first Unity)

| Script | Role |
|---|---|
| `GameBootstrap` | Entry point (`[RuntimeInitializeOnLoadMethod]`): lighting, managers, 60 FPS target, world for the current level |
| `GameManager` | State machine (menu/playing/paused/won/game over/complete), level progression, score, timer, lives |
| `LevelDefinition` | Pure data for a level: platforms, movers, spinners, gems, checkpoints, bounce pads, hearts, portal, mission text, story beats, atmosphere (sky/fog/sun) |
| `LevelLibrary` | Pack One levels in play order — add a level by adding a method + one line |
| `LevelPackTwo` / `LevelPackThree` / `LevelPackFour` | The Rematch, Undercloud and Two Suns packs, same pattern |
| `Story` | The completion epilogue and the menu's rotating flavor quotes |
| `BouncePad` / `HeartPickup` | Launch pad and extra-life pickup, both data-driven |
| `Updraft` | Standing-wind columns that carry Pip upward |
| `Gloomfang` | The storm himself, tagging along as weather support on post-story levels |
| `LevelBuilder` | Generic builder: turns any `LevelDefinition` into GameObjects |
| `SaveSystem` | Versioned `PlayerPrefs` wrapper: best times, stars, unlocked level, settings |
| `PlayerController` | Rigidbody movement, camera-relative input, coyote time + jump buffering, moving-platform carrying, hazard/fall death |
| `CameraFollow` | Smooth third-person follow camera |
| `MovingPlatform` / `Spinner` / `Gem` / `Checkpoint` / `GoalPortal` / `HazardMarker` | The interactive pieces |
| `TouchControls` | Floating virtual joystick + JUMP button, auto-created on touch devices |
| `UIManager` | Menu with level select, mission intro cards, HUD, pause, settings, win/game-over/completion screens — all built in code |
| `SfxSynth` / `AudioManager` | Sound effects and ambient chord-pad music synthesized at runtime; respects the sound setting |
| `Haptics` | Short Android vibration pulses (jump/gem/checkpoint/death/win), settings-aware |
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

`File → Build Profiles (or Build Settings) → Windows → Build`. Because
everything is code-driven, the built exe works out of the box.

## Troubleshooting

- **Hub asks to upgrade the editor version** — fine, click through; the code
  uses only long-stable APIs.
- **Scene looks empty in the editor** — it is, by design. Press Play.
- **`tools/` folder** — development-only (an offline C# syntax-check rig).
  Unity ignores it; you can delete it safely.
