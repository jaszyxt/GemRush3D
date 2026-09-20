# Multiplatform UI/UX — Research Findings & Team Directives

**Owner:** UI/UX agent · **v1.0** · 2026-09-18
**Platforms in scope today:** Android (touch) · Windows (keyboard + mouse).
**Roadmap this document prepares for:** Steam / Steam Deck (KBM + gamepad), iOS.

**How to use this doc (every agent):** Section 3 is the gap list; Section 5 is
the numbered work queue with acceptance criteria. If you touch
`UIManager.cs`, `TouchControls.cs`, `GameManager.cs`, `GameBootstrap.cs`, or
`PlayerSettings`, read the directive that covers your change first. Any new UI
must follow the shared vocabulary below or it will be sent back.

**Shared vocabulary**

- **ref unit** — one unit of the uGUI reference resolution (1600×900,
  match-height). At 1080p on a modern phone held in landscape, 1 ref unit ≈
  **0.45 physical dp**. So 48 dp ≈ **105 ref units** — this ratio drives most
  of Section 4.3.
- **Safe area** — the part of the screen not under a notch, punch-hole,
  rounded corner, or system gesture bar.
- **Reading text** — anything the player must actually read (missions, story,
  stats), as opposed to glanced HUD numbers.

---

## 1. Executive summary

The UI layer is in good architectural shape for a two-platform game, and
several things are done better than most mobile ports (press-to-jump touch,
input-reset on interruption, aspect-aware camera). But the game is **not yet
honest multiplatform UI**: it has no safe-area handling (HUD text is clipped
by notches on modern phones), three of its five accessibility settings exist
in `SaveSystem` but two are unreachable from the UI, touch hit targets on
small buttons are ~23–29 dp (below the 44–48 dp industry floor), a connected
gamepad half-works by accident (stick moves Pip, no jump, no menu focus), the
desktop window is non-resizable, and the whole UI lives on one Canvas that the
per-frame timer rebuilds every frame.

Priorities: **P0** = correctness on hardware we already ship to; **P1** =
gamepad + performance + accessibility exposure (unblocks Steam/Deck);
**P2** = polish, localization-readiness, iOS.

## 2. What's already right — do not regress

These were audited in code and match published best practice. They are
constraints, not suggestions:

1. **CanvasScaler: ScaleWithScreenSize, 1600×900, `matchWidthOrHeight = 1`**
   (`UIManager.cs:64-70`). Match-height is the community/Unity consensus for
   landscape games, and the 900-height reference conveniently approximates
   dp-space. Keep it. Extra width is absorbed by anchored stretch panels.
2. **Floating virtual joystick** that spawns under the thumb (`TouchControls`)
   — the exact pattern UX literature recommends over fixed sticks. Analog
   throttle respected in `PlayerController.FixedUpdate` (keyboard full-throttle
   vs. stick magnitude).
3. **Jump fires on press, not release**, with `JumpHeld` for variable height /
   fly mode (`TouchControls.cs:99-113`) — removing the ~1-frame-plus
   Button.onClick latency from the most-pressed input.
4. **`TouchControls.ResetInput()` on `OnApplicationPause` / `OnApplicationFocus`**
   (`GameManager.cs:53-69`) — the finger-id leak on OS interruption is
   handled, and play always resumes into the pause menu, never mid-air.
5. **Aspect-aware camera** (`CameraFollow.RecalculateFraming`): dollies back
   up to 1.5× on narrow (4:3) screens so course visibility is preserved. This
   is why we can ship tablets and 16:10 (Steam Deck) without re-tuning levels.
6. **Motion-comfort setting respected in code** (`CameraFollow.Shake` checks
   `SaveSystem.ShakeOn`), haptics grammar follows AOSP guidance with a
   retrigger-fuse (`Haptics.cs`), and vibration is fully settings-aware.
7. **Non-blocking story/intro overlays** (`raycastTarget = false`) so touch
   play continues underneath.
8. **Landscape lock + never-sleep + 60 FPS target** (`GameBootstrap.cs:38-40`)
   and `Time.maximumDeltaTime` clamp for resume-after-hitch.

## 3. Gap analysis

| ID | Gap | Severity | Where |
|----|-----|----------|-------|
| G1 | No safe-area handling — HUD left/right texts, pause button, jump button can sit under notch/cutout/gesture bar in landscape | **P0 blocker** | `UIManager.cs:236-264`, `TouchControls.cs:65-71` |
| G2 | Touch hit targets far below floor on small buttons: pause 58 u ≈ 27 dp, SETTINGS 54 u ≈ 25 dp, REPLAY/MENU 62 u ≈ 28 dp (48 dp needs ≈ 105 u) | **P0** | `UIManager.cs` (all `MakeButton` sizes) |
| G3 | Orphan settings: `ShakeOn` and `LeftyOn` exist, work, and are stored — but no UI toggle exposes them; Settings shows only Sound/Haptics/Shadows | **P0** | `SaveSystem.cs:199-211`, `UIManager.cs:389-450` |
| G4 | Android back / Esc doesn't follow navigation hierarchy: Settings panel ignores back; menu back does nothing silently; README claims "Esc quits" (it doesn't) | **P0** | `GameManager.cs:79-83`, README Controls table |
| G5 | HUD updated every frame via 4× `string.Format` allocations; whole UI on one Canvas so the timer dirties every screen's meshes | **P1** | `GameManager.cs:73-77`, `UIManager.cs:60-71` |
| G6 | No gamepad support. Legacy Input Manager accident: a plugged-in gamepad's left stick *already moves Pip* through the default `Horizontal/Vertical` axes, but there is no jump button, no menu focus, no first-selected object, no visible focus highlight | **P1 (blocks Steam/Deck)** | `PlayerController.cs:271-279`, `UIManager.cs:73-76` |
| G7 | Text-size floors below accessibility minimums: instructions 18 u ≈ 16 px @1080p-equivalent; XAG 101 requires ≥ 26 px @1080p console / ≥ 18 px PC for reading text | **P1** | `UIManager.cs:186-191` and small labels |
| G8 | Desktop window not resizable (`resizableWindow: 0`), no window-size persistence, no fullscreen toggle in Settings | **P1** | `ProjectSettings/ProjectSettings.asset:104` |
| G9 | Settings unreachable from Pause (only from menu) — a run must be abandoned to change volume/haptics | **P1** | `UIManager.cs:452-475` |
| G10 | Hardcoded English strings throughout `UIManager`/`Story`; legacy `Text` + single font, no fallback chain for future locales | **P2** | repo-wide |
| G11 | Photosensitivity audit never done for bursts/bloom sweeps (rule: no more than 3 flashes/sec, avoid full-screen flashes) | **P2** | `Fx.cs`, `BloomSweep.cs`, `PostFx.cs` |
| G12 | Haptics are Android-only; iOS path missing (Handheld.Vibrate works on iOS and is already our fallback path) | **P2** | `Haptics.cs:69` |
| G13 | Orientation hard-locks `LandscapeLeft`; users with a left-side notch (or a stand) can't choose the other landscape | **P2** | `GameBootstrap.cs:38` |

## 4. Research findings by domain

### 4.1 Screens, safe area, aspect ratios

- `Screen.safeArea` (px) converted to anchor deltas on a single root
  RectTransform is the standard uGUI pattern ("SafeAreaFitter"): recompute on
  enable and on any resolution/orientation change. Unity's uGUI 2.6 (Unity 6)
  ships a built-in **Safe Area** component for exactly this — but this project
  pins `com.unity.ugui 1.0.0`, so we need the ~20-line fitter in code (D1).
- Known caveat: on some Android punch-hole/rounded-corner devices
  `Screen.safeArea` reports incorrectly — the Device Simulator plus one real
  punch-hole phone is the acceptance bar, don't chase per-vendor hacks.
- **Backgrounds bleed, content doesn't**: full-screen panel images should keep
  reaching the physical edges (edge-to-edge looks intentional); only
  interactive elements and text anchor inside the safe root.
- Landscape phones put notches/cutouts on the **left/right edges** — exactly
  where `hudLevel` (x 0–0.22) and `hudLives` (x 0.68–1.0) sit today. The
  bottom 5.5 % (story toast band) collides with the Android gesture bar.

### 4.2 Scaling strategy (validated)

Match-height with a 900-unit reference is the right call for a
landscape-locked game and behaves like dp-space. Consequences to internalize:

- Ref-unit sizes are *not* dp. On a 6.1″ phone in landscape, height ≈ 400 dp,
  so 1 unit ≈ 0.45 dp. Anything tappable under ~100 units is a sub-48 dp
  target. This is the root cause of G2.
- Community consensus matches our choice (match height for landscape, match
  width for portrait; 0.5 blend only for orientation-agnostic UI). No change
  needed — but new UI must anchor to edges/corners (not center-anchors with
  magic offsets) so ultra-wide (21:9/32:9) stretch stays sane. The menu's
  fractional-anchor grid already complies.

### 4.3 Touch ergonomics

- Minimums: Google 48×48 dp, Apple 44×44 pt, WCAG 2.5.5 AAA 44 px — and all
  sources stress these are *floors*, not game targets; action buttons in
  shipped games run 60–96 dp. Our JUMP button (180 u ≈ 81 dp) is correct;
  small menu buttons are not.
- In landscape, both thumbs live in the bottom corners; frequent actions go
  in the natural thumb arc, system-edge gestures argue for generous margins
  from the physical edges — which is the same requirement as the safe area.
- Floating joystick with visual base + knob (we have it) beats fixed sticks;
  keep the joystick region thresholds in raw screen px (they are; don't move
  them under the safe root) but parent the JUMP button inside the safe root.
- The pause button in the top band is thumb-hostile; acceptable for a
  secondary control *if* it's big enough and Esc/Start equivalents exist
  (gamepad Start arrives in D5; Android back in D4).

### 4.4 Gamepad & input architecture ("secretly console first")

- The multiplatform-industry pattern is controller-first menus that PC
  enriches with mouse — not the reverse. Design every new screen so it is
  fully operable with arrows + confirm + cancel, then let the mouse click on
  top.
- Steam's Deck "Verified" bar checks: native controller support, UI
  legibility at **1280×800 (16:10)**, and readable text. Our CanvasScaler and
  camera-aspect clamp already satisfy the geometry; input is the missing half.
- Recommended path for this codebase: adopt `com.unity.inputsystem` with
  **Both** active input handling (legacy `Input.*` keeps working during
  migration), use `InputSystemUIInputModule` for menus, and poll devices
  directly (`Gamepad.current`, `Keyboard.current`) for gameplay — the Input
  System is fully usable from code with **zero `.inputactions` assets**,
  preserving the no-asset architecture. It buys unified device switching,
  rumble for gamepads, and rebinding later. Legacy Input Manager is frozen;
  extending it (custom InputManager.asset bindings, `Input.GetJoystickNames`
  sniffing) is the non-recommended fallback.
- Watch the legacy accident in G6 while gamepad work lands: today a stick
  moves Pip with no jump. Ship gamepad move + jump together in one release.

### 4.5 Menu focus & navigation

- Explicit focus model: every panel needs a **first-selected object** when it
  opens (`EventSystem.SetSelectedGameObject`), visible selection highlight
  (color/scale swap via `ISelectHandler`), arrow/stick navigation with sane
  wrap in the level grid, and B/Cancel = back/close. A menu that lights no
  focus is unreadable on a couch, regardless of platform.
- Default `StandaloneInputModule` Submit is bound to Space + Enter; Space is
  also Jump, so a focused button will double-fire — another reason the Input
  System module is the cleaner route.

### 4.6 UI performance

- uGUI rebuild granularity is the whole Canvas: any moved/resized/text-changed
  graphic dirties every mesh in that Canvas. Unity's official guidance is
  **split Canvases by change-rate** (static menu vs. dynamic HUD) and put
  every-frame text on its own subtree.
- `GameManager.Update` → `ui.UpdateHUD` every frame allocates four strings
  60×/s and (via the timer's 0.1 s format) dirties the canvas ~10×/s even
  when values repeat. Cache last-displayed values; only write `.text` on
  change (uGUI skips rebuild when the string is identical, but the
  allocations and comparisons still run).
- Raycast discipline is already good (`MakeText` disables raycast targets);
  keep it — every needless `raycastTarget` costs in the EventSystem raycast.
- `Outline` on every text doubles text geometry; acceptable at current
  element counts, but do not stack further effects (Shadow, multiple
  Outlines) on UI text.

### 4.7 Accessibility

- **Text size (XAG 101):** reading text ≥ 26 px @1080p on console-class
  targets, ≥ 18 px on PC. In our units (≈1.2 px per unit at 1080p) that is
  ≥ 22 units for reading text, ≥ 15 units for the smallest glanceable HUD
  text. Current violations: instructions block (18), level-button sublabels
  (19), menu quote (20), visit recap (19).
- **Contrast (XAG 102):** white on `onColor` green measures ≈ 4:1 — passes
  AA for large/bold text (3:1) but is the limit. Rule for all agents: **do
  not darken button fills**; if contrast must improve, thicken/brighten text.
- **Color independence:** hazards (red spinners) already carry motion +
  shape redundancy; the daily-gem highlight carries the literal text "DAILY".
  Standard for new features: never signal state by hue alone.
- **Motion:** `ShakeOn` exists and is honored — it just needs exposure (G3),
  which turns an invisible courtesy into a discoverable accommodation.
- **Layout:** haptic toggle already exists; a text-size multiplier is the
  next-cheapest accessibility win (P2, D10).

### 4.8 Platform conventions

- **Android back** = `KeyCode.Escape` in Unity; Google's pattern is a stack:
  close popup → back one screen → pause → (root) confirm-quit or nothing.
  Abrupt quit from anywhere is the anti-pattern. Our Settings panel currently
  swallows back with no effect.
- **Windows:** a desktop game that can't be resized or windowed reads as a
  port. `resizableWindow: 1` + remembered window rect + a fullscreen toggle
  in Settings is the baseline; borderless `FullScreenWindow` default is fine.
- **Steam Deck:** legibility target 1280×800; controller glyphs in prompts
  (text placeholders acceptable at first, e.g. "(A) Jump"); contextual
  hints beat static control charts — the menu instructions line should
  become input-aware (show the block for the last-used device).
- **iOS (prep):** `Handheld.Vibrate` works on iOS (single heavy pattern) —
  an `#if UNITY_IOS` branch using it keeps Haptics honest cross-platform;
  safe-area code is identical; nothing else blocks an iOS target.

## 5. Directives (work queue)

Every directive has an **owner**, **DoD** (definition of done), and touches
only files listed. Agents: claim by ID, keep IDs stable in PRs.

---

### D1 — Safe-area root (owner: UI/general agent) · **P0**

Create `Assets/Scripts/SafeArea.cs`:

```csharp
using UnityEngine;

/// Constrains a RectTransform to Screen.safeArea. All screens sit under this
/// node so notches, punch-holes, rounded corners and gesture bars never clip
/// interactive UI. Backgrounds that must bleed keep their own node outside.
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    Rect applied;

    void OnEnable() { Apply(); }

    void Update()   // cheap property read; catches rotation & resolution
    {
        if (Screen.safeArea != applied) Apply();
    }

    void Apply()
    {
        applied = Screen.safeArea;
        RectTransform rt = (RectTransform)transform;
        Vector2 min = applied.position;
        Vector2 max = applied.position + applied.size;
        rt.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height);
        rt.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
```

In `UIManager.Awake` (after the Canvas, before `BuildMenu`): create a full-
stretch child `"SafeRoot"` with `SafeArea`, and parent **all** `Build*` panels
to it. TouchControls' JUMP button inherits it; joystick *region logic* stays
in raw screen px (no change).

**DoD:** Device Simulator on a notched 20:9 and a punch-hole 19.5:9 phone:
`LV` text, `Lives` text, pause button, JUMP button and story toast all fully
visible; level-select grid intact at 16:9, 16:10, 4:3, 21:9.

### D2 — Touch hit-target floors (owner: UI/general agent) · **P0**

On **touch devices only** (`Input.touchSupported`), enforce: every tappable
target ≥ **100 × 100 ref units** (≈ 45–48 dp). Concretely: pause button
58 → 100; SETTINGS 190×54 → 220×100; level buttons 200×54 → 240×100 (reflow
grid step: 5 columns × 0.185 spacing no longer fits — derive spacing from
button size as the grid code already does); win/pause/game-over buttons
84 tall → 100. Keyboard/mouse sizes stay as-is — this is a per-mode layout,
which is exactly why the builders are code.

**DoD:** every button's measured rect ≥ 100 units in any touch build; menu
still fits 5×5 grid + PLAY + instructions between bands; no overlap at 4:3.

### D3 — Expose the orphan settings (owner: UI/general agent) · **P0**

Settings rows become: Sound, **Screen Shake**, Haptics, Shadows,
**Left-handed Controls**. Five rows at height 60–68, spacing ≈ 0.075 from
y 0.60 downward (fits above BACK at 0.15); extend `settingsLabels` to 5 and
`ToggleSetting`'s switch. `LeftyOn` must also live-mirror the touch layout —
currently it's read once at `TouchControls.Create` (`TouchControls.cs:37`);
on toggle, re-anchor `jumpRect` (or rebuild TouchControls) rather than
requiring a level restart.

**DoD:** toggles persist across app restarts; flipping Lefty mid-HUD mirrors
JUMP side immediately; Screen Shake off silences death shake instantly.

### D4 — Back/navigation stack (owner: gameplay/input agent) · **P0**

`GameManager.Update` Escape handling becomes a stack: if Settings open →
close Settings; else Playing → pause; Paused → resume; Menu → (desktop only)
quit-confirmation dialog; **never** quit from Android menu-back. Add a tiny
quit-confirm panel in `UIManager` (QUIT / CANCEL). Update the README controls
table to match reality (Esc pauses; Esc on menu asks to quit on desktop).

**DoD:** from Settings: back closes it; from pause: back resumes; from menu
on Android: nothing harmful; on Windows: confirm dialog. No state where back
is a no-op surprise.

### D5 — Gamepad, menus (owner: gameplay/input agent) · **P1**

Add `com.unity.inputsystem` to `Packages/manifest.json`; set active input
handling to **Both** (ProjectSettings via the `Ensure*` editor-script
pattern — add it to the build setup). Swap `StandaloneInputModule` →
`InputSystemUIInputModule`. For every panel: set first-selected on open,
explicit/clean `Navigation` (grid wraps per row), visible focus style
(brighten + slight scale on select; art agent owns the style), B/Cancel =
back (uses D4's stack). Keep mouse working unchanged.

**DoD:** with only a gamepad attached: launch → PLAY → play a level — zero
mouse/keyboard. Focus highlight always visible exactly once per panel.

### D6 — Gamepad, gameplay + rumble (owner: gameplay/input agent) · **P1**

Same release as D5 (kills G6's half-support): in `PlayerController`, merge
sources by max magnitude — keyboard axes, touch `MoveVector`,
`Gamepad.current.leftStick` (apply an 0.15 deadzone); jump mirrors the
TouchControls contract (`buttonSouth` press edge → buffered queue, hold →
`JumpHeld` semantics for variable height/fly); Start = `PauseGame()`.
Rumble: `SetMotorSpeeds` burst on death/win gated by `SaveSystem.HapticsOn`,
zeroed on pause/menu (respect D4's auto-pause). Menu instructions line
becomes input-aware: show the keyboard or gamepad block for the last-used
device (text placeholders "(A) Jump" are fine for now).

**DoD:** Xbox + PlayStation pads (via Windows) complete level 6 with stick +
A/Cross; unplugging mid-run falls back to keyboard without stuck inputs;
rumble never fires when Haptics is off.

### D7 — Canvas split + HUD update hygiene (owner: UI/general agent) · **P1**

One Canvas per screen (menu / HUD / win / over / complete / settings / pause
/ intro / toast), each a child of SafeRoot; nested dynamic Canvas for the
four HUD texts. Change `UpdateHUD` to cache the last-rendered level/gems/
lives/timer-string and skip `.text` writes when unchanged (timer string only
changes at 0.1 s resolution — ≤ 10 writes/s).

**DoD:** Profiler: timer ticking causes no mesh rebuilds outside the HUD
dynamic canvas; `string.Format` allocations in steady-state play ≤ 10/s
(verify with Memory Profiler or GC alloc column in Profiler).

### D8 — Desktop window UX (owner: platform/build agent) · **P1**

`resizableWindow: 1` (edit `ProjectSettings.asset` or set it in the editor
build script, matching the project's code-first tooling); remember window
position/size via PlayerPrefs on desktop; add Fullscreen/Windowed toggle to
Settings (visible only on desktop builds); default stays borderless
fullscreen.

**DoD:** Windows build resizes freely; UI relayouts correctly at 1280×720,
1920×1080, 2560×1440, 3440×1440; toggle persists; alt-enter behaves.

### D9 — Settings reachable from Pause (owner: UI/general agent) · **P1**

Settings panel opens as a layer above Pause (BACK returns to Pause, and D4's
back stack handles Esc identically). Time is already scaled 0 — no gameplay
risk.

**DoD:** from pause: SETTINGS opens, BACK returns to pause, no state leaks
(sound/haptics changes apply immediately mid-pause).

### D10 — Text floors + size setting (owner: UI/general agent, art support) · **P2**

Raise reading-text floors: instructions 18 → 22, level-button sublabels
19 → 20 (check two lines fit the D2-sized buttons), menu quote 20 → 22,
visit recap 19 → 22; keep glanceable HUD numerals ≥ 24. Add Settings row
"Text Size: Normal / Large" (0.9 / 1.1 multiplier applied in `MakeText`)
persisted via a new `SaveSystem` key.

**DoD:** no reading text below 22 units at default; Large mode never causes
overflow/clipping on any audited screen/aspect.

### D11 — Localization readiness (owner: UI/general agent) · **P2**

Extract all user-facing strings from `UIManager` into a static
`Strings` table (Story-style), including composed ones via format patterns.
No translation yet — the deliverable is: zero literals in layout code, one
file to hand translators, and a noted font-fallback caveat (LegacyRuntime/
Arial lack CJK; flag as an iOS/Steam-localization blocker later, not now).

**DoD:** `grep` finds no English UI literals outside `Strings`/`Story`/
`LevelDefinition` mission text.

### D12 — Photosensitivity + platform polish audit (owner: art/VFX agent) · **P2**

Audit `Fx.Burst` intensities, `BloomSweep`/win-star pops and `PostFx` ramps
against the ≤ 3 flashes/sec, no-full-screen-flash rule; document results in
this file's appendix. Add `#if UNITY_IOS` branch in `Haptics` (Handheld.
Vibrate) so an iOS target needs no haptics work. Evaluate (implement only if
trivially safe with D1 in place) allowing both landscape orientations.

**DoD:** audit notes appended here with pass/fail per effect; iOS branch
compiles and respects `HapticsOn`.

## 6. Testing matrix (every UI PR)

| Dimension | Coverage required |
|---|---|
| Aspect | 16:9, 16:10 (Deck), 4:3 (tablet), 19.5:9 & 20:9 (notched phones), 21:9 spot-check |
| Safe area | Device Simulator: notch + punch-hole + rounded-corner presets; one real Android phone before release |
| Input | KBM (Windows), touch (Device Simulator + real phone), gamepad (Xbox + DualSense via D5/D6) |
| Interruption | Phone call / home mid-air → resume into pause; unplugged gamepad mid-run |
| Text | Default + Large (D10); every screen, no clipping/overflow |

## 7. Sources

- Unity uGUI Safe Area component (2.6) — docs.unity3d.com; SafeAreaFitter pattern — bugnet.io; punch-hole caveat — discussions.unity.com; "Notches Hell" — hutonggames.com
- Unity official "UI performance optimization tips" (split canvases, raycast targets) — unity.com; canvas-rebuild granularity — thegamedev.guru; community UGUI findings — discussions.unity.com
- Xbox Accessibility Guidelines: XAG 101 Text display, XAG 102 Contrast — learn.microsoft.com/en-us/xbox/accessibility
- Touch targets: Material 48 dp (via LogRocket roundup), Apple HIG 44 pt, WCAG 2.5.5; thumb zones — Smashing Magazine, MobileFreeToPlay
- CanvasScaler consensus (match-height landscape) — docs.unity3d.com Canvas Scaler + discussions.unity.com
- Input System: "UI support / InputSystemUIInputModule" + "Migrate from old Input System" — docs.unity3d.com; pros/cons — r/unity 2025 thread
- Steam Deck program requirements (controller support, 1280×800 legibility) — partner.steamgames.com; Deck UX practices — 80.lv; controller-first menus — gamedeveloper.com ("Secretly console first")
- Android back button = Escape + navigation-hierarchy pattern — discussions.unity.com, Google UX guidance

## Appendix A — Photosensitivity + platform polish audit (D12) · art agent · 2026-09-19

Hard rule: no more than 3 flashes per second, no full-screen flashes.
Result: **PASS on every effect, with margin.** No full-screen flash
exists anywhere in the game; the largest luminance event is a level's
one-time grading change on load.

| Effect | Modulation | Verdict |
|---|---|---|
| `Fx.Burst` (all pickups/deaths/landings) | one-shot, 0.35–0.7 s lifetime, falls out of frame; gem-trail repetition is player-paced | PASS |
| Gem emission pulse | sine, ≈0.48 Hz, amplitude ×1.3–1.8 on small objects | PASS |
| Portal fill pulse | ≈0.48 Hz alpha sine, small screen area | PASS |
| Bounce-pad idle pulse | ≈0.48 Hz scale sine, ±0.025 | PASS |
| Guardian wake glow | eased over ~0.5 s, one-shot | PASS |
| Bell clapper + burst | 1.4 s damped swing; burst one-shot | PASS |
| Win stars (post-2026-09-19) | up to 3 small-area color/scale events at 0.34 s spacing (≈2.9/s), each ≈84 px | PASS — keep spacing ≥ 0.3 s if the cadence is ever retuned |
| `BloomSweep` garden wave | 0.7 s one-shot per bud, distance-staggered (never synchronized) | PASS |
| `PostFx` grading | static per realm; no runtime ramps | PASS |
| Camera shake on death | single 0.25 s jitter, disabled by the Screen Shake setting | PASS (motion-comfort honored) |

**iOS haptics (D12 item 2): done.** `Haptics.RawPulse` now has a
`UNITY_IOS` branch using `Handheld.Vibrate` (single system-strength
buzz), gated by `SaveSystem.HapticsOn`. An iOS target needs no further
haptics work; amplitude gradations remain an Android feature.

**Both landscape orientations (D12 item 3): evaluated, deferred.**
Keep the `LandscapeLeft` lock for now: composition and UI bands are
landscape-tuned, and supporting both orientations doubles the
safe-area/aspect verification matrix for an unmeasured player benefit.
Revisit alongside the iOS target. (Art rationale in
`docs/Art-Direction.md` §7.)

**Found during audit (fixed same day):** `SafeArea` applied oversized
`Screen.safeArea` reports verbatim — the editor game view reports the
whole desktop resolution, and some Android punch-hole devices misreport
too — stretching every screen off-screen. Anchors are now clamped to
0..1 so a bogus report degrades to "no inset", never a broken UI.

## Appendix B — Implementation status (UI/general agent · 2026-09-19)

| ID | Status | Notes |
|----|--------|-------|
| D1 | **Done** | `SafeArea.cs` (with the over-report clamp from Appendix A) + `SafeRoot` in `UIManager.Awake`; every screen parents under it |
| D2 | **Done** | `TouchTarget()` floors every button at 100×100 on touch; small buttons re-anchored. **Correction to D2's original DoD:** a 5×5 grid of 100-unit buttons cannot fit the menu band — the rows overlapped and buried each other's tap area. Touch level select is therefore **paged (5×2, ‹ › arrows, "n / N" readout)** and opens on the page holding the newest unlocked level; desktop keeps the dense grid |
| D3 | **Done** | Settings expose Screen Shake and Left-handed Controls; lefty live-mirrors the jump button via `TouchControls.ApplySide()` (no restart); Fullscreen row on desktop (bonus) |
| D4 | **Done** | `GameManager.HandleBackNavigation()` walks the stack (dialog → settings → pause/resume → desktop menu quit-confirm); Android never quits |
| D5 | **Done** | `com.unity.inputsystem` 1.11.2 + `EnsureInput.cs` pins Active Input Handling to **Both** (same write-ProjectSettings pattern as EnsureShaders; editor restart needed once for the native backend). `InputSystemUIInputModule` replaces `StandaloneInputModule`. Every panel gets a first-selected object on open (`UIManager.Focus`), explicit `Navigation` via `MenuNav.cs` (grids wrap per row; the paged touch grid re-wires on flip — gamepad shoulders also flip pages), and a visible focus highlight (`FocusFX.cs`: brighten + 1.08× scale, suppressed for locked buttons). Mouse/touch unchanged |
| D6 | **Done** | `GamepadInput.cs` is the single guarded door to the Input System (1 Hz probe, silent no-ops if the backend is not live). Gameplay merges keyboard axes, `GamepadInput.LeftStick` (0.15 radial deadzone, rescaled) and the touch joystick **by max magnitude**; jump = buttonSouth press edge into the same buffer + hold for variable height/fly; Start toggles pause; B walks D4's stack; rumble = `Haptics.GamepadBurst` on death (heavy) and win (light), gated by `HapticsOn`, zeroed on pause/menu/focus-loss (`TickRumble` is realtime-based). Menu instructions line is input-aware (gamepad/touch/keyboard blocks, rewrites live). **Verified via virtual gamepad injection in-editor; Xbox/DualSense hardware pass still owed before Steam** |
| D7 | **Done** | HUD readouts on a nested `HudDynamic` canvas; `UpdateHUD` change-cached (clock ≤ 10 string builds/s, steady frames allocate nothing); `ShowHUD` resets the cache |
| D8 | **Done** | `resizableWindow: 1` set in the build script (runs for every build); `DesktopWindow.cs` remembers the windowed rect via PlayerPrefs (size via `Screen.SetResolution`, position via user32 on Windows only; skipped in fullscreen/editor/mobile) |
| D9 | **Done** | SETTINGS button on the pause menu; Settings hides pause while open and restores it on close (pause draws above Settings in sibling order, so the hide is required) |
| D10 | **Done** | Text floors raised (instructions/quote/recap → 22, desktop level sublabels ≥ 20); "Text Size: NORMAL/LARGE" row re-derives every registered label from its base at 1.15× (never compounding); Enter is guarded while overlays hold the screen |
| D11 | **Done** | `Assets/Scripts/Strings.cs` holds every user-facing UI string (constants + composed methods); `UIManager`, `TouchControls` (JUMP) and the lantern toast reference it. DoD grep is clean: no English UI literals outside `Strings`/`Story`/`LevelDefinition`. Region names are data in `LevelLibrary.Regions` (sanctioned); medal names stay in `LevelDefinition.MedalFor` (exempt). Suite 27/27 after the sweep |
| D12 | **Done** (by art agent) | See Appendix A |

**Verification state:** in-editor compile clean (Unity 6000.6, live
refresh). EditMode suite (now 18 tests: the 14 above + 4 `MenuNavTests`)
run green after the D5/D6 batch. Gamepad paths verified by injecting a
virtual gamepad in-editor (menu focus walk, A-confirm, Start pause/resume,
B-back, stick movement, unplug fallback); a real Xbox + DualSense hardware
pass is still owed before any Steam submission. Touch paging and the
pause-settings layout had one Device-Simulator pass on a notched 20:9
preset.

**Resolved:** the `GoalPortal.Update()` NRE at line 75 noted on 2026-09-18
is fixed — `AudioManager.Instance` was the unguarded dereference during
teardown; the proximity-hum report now no-ops without an audio manager.

## Appendix C — Quality-audit fix run (UI/general agent · 2026-09-19 evening)

Second quality audit over the grown UI layer (gamepad, atlas, photo mode,
share card): **all 3 P1 findings and the P2 list fixed same day.**

| Finding | Fix |
|---|---|
| **P1** Gamepad Start during photo mode resumed gameplay under the orbit camera | `GameManager.Update`: Start while `PhotoModeOpen` calls `ClosePhotoMode()` — hands the pause menu back, symmetric with "Start toggles pause" |
| **P1** ATLAS button unreachable by gamepad/keyboard (missed the D5 nav sweep) | `atlasMenuButton` captured; `WireMenuNav` (both branches) wires SETTINGS↔ATLAS horizontally and drops both into the grid's top-right cell; covered by `UIAuditTests.UIManager_AtlasButton_IsReachableInMenuNav` |
| **P1** PHOTO offered on mobile but desktop-only under the hood (MyPictures path) | PHOTO pause button built only when `IsDesktopPlatform()`; mobile path (`persistentDataPath` + share) is the documented future item |
| **P2** `HideAll` leaked the photo bar/scorecard | Both retired in `HideAll`; covered by `UIAuditTests.UIManager_HideAll_RetiresThePhotoBar` |
| **P2** `RefreshSettings` hardcoded English row names (latent D11 divergence) | Uses the `Strings.Setting*` constants, same as build time |
| **P2** Menu hint followed pad *presence*, not last use | `CurrentInstructionMode` reads `GamepadInput.LastDeviceWasGamepad`; accepted residual: mouse/touch activity does not flip the flag back |
| **P3** `WireAtlasNav` would index `atlasRows[-1]` on a zero-level region | Zero-count guard |
| **P3** Capture comment claimed "at 2x"; capture is native res | Comment corrected |

**Verification:** in-editor compile clean; EditMode suite **29/29** (27 prior
+ 2 new). Deferred: FocusFX focused-scale vs press-tween conflict (cosmetic);
scorecard star/name-band spacing (one render check, in-flight share-card
owner); MarkOther on mouse/touch; mobile photo path.

**Note:** the boot-visible photo bar itself was independently fixed in
`00d4f5c` (art agent) — `HideAll` coverage remains as belt-and-braces.

## Appendix D — 2026 research pass (UI/UX agent · 2026-09-21)

A fresh literature pass over the UI/UX surface, checked line-by-line against
the shipped code (audit quoted `UIManager.cs`, `TouchControls.cs`, `Fx.cs`
and `SaveSystem.cs`). Findings are stated as **research says / we do /
verdict**, so the gap is visible rather than implied.

### D.1 Findings

| Area | Research says | We do | Verdict |
|---|---|---|---|
| Touch targets | 44 pt / 48 dp floor; **48–60 pt for ages 6–8**; ≥ 8 dp spacing | `TouchTarget()` floors at 100 ref units ≈ **45 dp** | **Under the kids' bar** → D13 |
| Thumb ergonomics | Primary actions bottom corners; top ~40 % is a dead zone on > 6" phones | Pause button is **top-centre** of the HUD | Tunable → D14 |
| Text rendering | Legacy `UI.Text` deprecated (no bug fixes); uGUI is **not** migrated to ATG until ~6.8 | All legacy `Text`; 0 TextMeshPro | **Deferred by decision** → D17 |
| UI batching | Split canvases by change rate; group siblings by material | Root canvas + one `HudDynamic` split; every label carries an `Outline` | One cheap gain left → D15 |
| Kid cognitive load | Working memory 3–5 items; **≤ 2 screens to content**; audio over text | Menu → Play is one step; **Settings is a single 9-row list** | Rows exceed the scanning budget → D16 |
| HUD via eye-tracking | Minimise fixation load; keep status consolidated | 4 status readouts, one band, change-cached | **No change needed** |
| Wayfinding | Even minimal landmarks stabilise orientation in screen-based 3D | **Zero** minimap / compass / marker; only a near-range portal hum | Real gap → D18 |

### D.2 The queue (D13–D18)

**D13 · Kid-scale touch targets (P1).** Raise the touch floor to **120 ref
units (≈ 54 dp)** — inside the 48–60 pt band the research recommends for
ages 6–8, and still within the paged 5×2 grid budget. Re-verify the grid and
the settings 2-column layout against it; assert inter-target spacing
(≥ 8 dp) in a test rather than assuming it.
**DoD:** every tappable target ≥ 120×120 touch units; spacing assertion
green; no screen overflows at 4:3 / 16:10 / 20:9.

**D14 · Pause button out of the dead zone (P2).** Move the HUD pause control
from top-centre to a thumb-reachable edge. Requires a real layout pass: it
shares the band with `hudLives` (x 0.68–1.0) and the touch floor already
pushed it to y 0.93, so collision with the lives readout and the notch must
be checked, not assumed.
**DoD:** in-editor proof of no overlap at 4:3 and 20:9; anchor test pinned.

**D15 · One batching break to remove (P2).** Buttons already share
`Fx.CircleSprite()`, but each label's `Outline` is a second material.
**Measure first** with the Frame Debugger at our scale (5 visible level
buttons + labels); if it breaks batches, share one outline material rather
than instancing per label.
**DoD:** Frame Debugger before/after numbers recorded here; no visual change.

**D16 · Settings grouping (P2).** Nine boolean rows is a long scan for a
6–8 audience (≤ 2 screens to content, 3–5 items in working memory). Group
the same rows under **Audio / Display / Controls** so the panel reads as
three short lists.
**DoD:** every row still present and reachable; touch floor respected;
layout test pinned per group.

**D17 · TMP/ATG readiness (P3 · deferred, not built).** Legacy `Text` is
deprecated and gets no fixes, but **Unity forces no migration before ~6.8**,
and ATG reaches uGUI only around then. Our zero-imported-assets rule makes
the SDF font asset the real obstacle. **Trigger: pick this up when
localization or a Steam build is scheduled** — not before.

**D18 · Diegetic wayfinding (P2).** Research flags orientation as a genuine
gap in screen-based 3D; we have no navigational aid beyond a near-range
portal hum. No new HUD element (that would contradict "UI never speaks
louder than gameplay"). Instead widen the portal's **audio and emissive cue
range** so the goal reads from further away, and verify the near-range cue
is doing its job.
**DoD:** portal audible/visible from the level's far end on the longest
courses; no gameplay change.

### D.3 Method note

D13 rests on the best-evidenced research in this pass (touch-target sizing
for young children is well studied). **D14 and D15 are unmeasured layout and
performance questions** — both get reproduced in-editor before any change is
claimed, the same way the pause-overlap bug was reproduced before it was
fixed. Every layout change ships with an anchor-math test pin in the
existing `UIAuditTests` pattern; the suite must stay green.

### D.4 Sources

- [HUD eye-movement systematic review (2025)](https://doaj.org/article/702d307bbd31415ba6c2c3fd10c4440a)
- [UX guidelines for HUD design, indie devs — HCI in Games 2025](https://dl.acm.org/doi/10.1007/978-3-031-92578-8_6)
- [Minimal landmarks stabilise orientation in screen-based 3D (ACM 2025)](https://dlnext.acm.org/doi/pdf/10.1145/3772318.3791522)
- [Readability thresholds and attributable failure (2025)](https://www.theseus.fi/handle/10024/900621)
- [Processing subtitled instructions — eye-tracking (JEMR 2025)](https://mdpi-res.com/d_attachment/jemr/jemr-18-00044/article_deploy/jemr-18-00044-v2.pdf)
- [Unity — performance considerations for runtime UI](https://docs.unity3d.com/Manual/UIE-performance-consideration-runtime.html)
- [Unity — controlling the dynamic atlas (2026)](https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-control-textures-of-the-dynamic-atlas.html)
- [UGUI draw-call reduction and batching failure analysis](https://dev.to/gameoptim/ugui-drawcall-reduction-atlas-packing-and-batching-failure-analysis-1gaa)
- [TextMeshPro breaking canvas batching](https://bugnet.io/blog/fix-unity-textmeshpro-canvas-batching-broken)
- [Adapting Causa for mobile (Unity)](https://unity.com/cn/blog/adapting-causa-into-the-dusk-for-mobile)

### D.5 D13 implementation notes (2026-09-21)

**Shipped in two commits** (`0929354`, `e5e50f4`), in the order planned:
fix the latent collisions, then raise the floor.

**The floor is now `UIManager.TouchFloorUnits = 120` ref units ≈ 54 dp**
(mid-band for the 48–60 pt the research recommends for ages 6–8). It is a
single constant — every layout that must clear a floored button reads it,
so the number can move again without silently breaking a stack.

**What the work actually turned up.** Raising the floor exposed that the
layouts were already broken *at the old 100-unit floor*:

| Defect | Size | How it surfaced |
|---|---|---|
| Settings grid ↔ BACK | **59.5 units of overlap** | Live, shipped, player-visible |
| Pause PHOTO ↔ MENU | 1.0 unit | Already short of the targets |
| Desktop settings column | **ran off-screen entirely** (rows to y=−444, BACK at −524) | Found only after the containment assertion was added |

**Root cause of the blindness — the real deliverable.** The edit-mode
suite runs in the editor, where `Input.touchSupported` is **always false**,
so every touch layout in the game was unverifiable. The one settings test
that existed compared *anchor* values (`0.215 > 0.17` passes) rather than
rendered rects. Both are fixed:

- `UIManager.ForceTouchLayoutForTests` + `IsTouchLayout()` — one seam the
  layouts and `TouchTarget` share, so tests build either layout.
- Rect-extent probes (`YBand`/`XBand`/`Band`) and `AssertNoOverlap`, which
  fail with both spans quoted.
- `Settings_TouchLayout_NoOverlap`, `Settings_DesktopLayout_NoOverlap`,
  `Pause_TouchLayout_NoOverlap`, `ResultScreens_TouchLayout_NoOverlap`.
- Containment assertions (on-screen, both ends) — their absence is what let
  the off-screen desktop column through.

Each test was **confirmed failing against the old code before the fix**, so
they are proven to catch the real defect rather than passing vacuously.

**Two design consequences worth recording:**
1. **Settings is a 2-column grid on every platform now.** This was forced,
   not cosmetic: nine rows of 120-unit targets plus BACK need ~1100 units
   of a 900-unit screen, so a single column can only fit by shrinking rows
   *below* the target size — which is precisely how the desktop column came
   to run off the bottom edge.
2. **BACK floors like any other button.** Its half-height is derived from
   the same constant, and it is positioned from the *actual* last row, so
   it cannot be pushed off-screen.

**Still open from D13:** inter-target *spacing* is not yet asserted against
the ≥8 dp guidance (the grid gaps exceed it comfortably in practice, but
it is unmeasured); and the whole pass needs one real-device look, since a
Device Simulator and the editor Game view do not reproduce a phone's
aspect or safe area.

### D.6 D15 — measured, and closed as unnecessary (2026-09-21)

**The queue item's premise was wrong, and measuring first is what caught
it.** D15 assumed each label's `Outline` was "a second material and a
batching break", based on the general guidance to group siblings by
material. It is not.

`UnityEngine.UI.Outline` derives from `Shadow`, which implements
`IMeshModifier`. Measured in-editor:

- an outlined graphic has **exactly one `IMeshModifier`** and **no extra
  material** — the effect is baked into the same vertex buffer, same font
  atlas, same material as the text itself;
- it costs **4 extra copies of each glyph's geometry** (the outline is four
  offset duplicates), i.e. vertex/overdraw cost, not draw-call cost.

So there is no batching break to fix. What the guidance about "grouping by
material" targets — per-label material instances, multiple font assets,
sprite-atlas breaks — does not apply here: the UI uses one procedural
sprite material and one built-in font throughout.

**Verdict: no change.** Recorded rather than actioned, because the value of
the measurement is the negative result: it stops this item being re-raised.
The real (small) cost of `Outline` is per-glyph vertices, which is a
concern only if UI vertex counts ever show up in a profile — nothing
currently suggests they do (97 graphics on the busiest panel).

**Method note for future D15-style items:** the Frame Debugger is the right
instrument for draw-call questions, but it needs the game paused and the
editor focused, and the MCP path for it proved unreliable here. For
*material/call-count* questions specifically, inspecting `IMeshModifier`
membership and material identity is decisive and cheap; reserve the Frame
Debugger for counting real submitted batches, and treat "this component
probably breaks batching" as a hypothesis to verify, never a finding.

### D.7 D14 + D18 implementation (2026-09-21)

**D14 — pause button reserved slot** (`2402e5d`). Measured before touching
it, and the audit's estimate was close but under: at the raised touch floor
the button overlapped the **clock by 20 units and the lives counter by 4**
(it had been estimated at ~10). Root cause is that it was placed *by eye*
into a gap that closed when targets grew to 120.

Fixed structurally rather than by nudging: the top band is now **split** —
the clock ends at 0.60, the lives counter starts at 0.78, and the button
owns the segment between them, sized from `TouchFloorUnits` and clamped to
its segment. Raising the floor again cannot push it into a neighbour.

Also routed `TouchControls.Create` through the shared layout seam. It gated
on raw `Input.touchSupported`, so the joystick and jump button were as
untestable as everything else was before D13 — a HUD-layout test literally
could not see them.

*Test:* `Hud_PauseButton_FitsTheTopBand` (both layouts) — no overlap with
any of the four readouts, exact target size, on-screen containment. It
caught the button poking 10.5 units above the top edge on its first run.

**D18 — portal cue reach** (`12889c6`). The audit quantified the gap: `1 −
d/28` meant the hum read **exactly zero for most of every level** (courses
are 76–171 units spawn-to-portal), and the audio manager's 0.005 play
threshold pushed the real onset closer still. As the only long-range
wayfinding cue in the game, it was not doing that job.

Cue range is now **120 units with an ease-in (t²)**, so the hum stays a
whisper early and rises over the last third — "getting close" still reads
as an event rather than a drone. The **peak volume is deliberately
unchanged**: proximity maps linearly to volume, so a longer ramp is
automatically quieter at distance while the near-field mixing (hum sits
under the music bed at `HumMaxVolume = 0.08`) stays exactly as calibrated.

*Test:* `PortalCue_ReachesAcrossTheCourse` — asserts the range and that the
cue is audible at the midpoint of the shortest course. It pins the
*property* that regressed, not the literal constant, so a future retune is
free as long as the cue still reaches.

**Queue status after this pass:** D13, D14, D15 (measured, not needed),
D17 (deferred by decision), D18 all closed. **D16 (settings grouping) is
the only open item**, and it is P2 cosmetic — the settings panel is
functional, tested, and fits; grouping it into Audio/Display/Controls is a
readability improvement, not a defect fix.

### D.8 D16 — settings grouped, and a latent bug found doing it (2026-09-21)

**Shipped** (`db77729`). The settings grid reads in column order, so the old
flat list produced these adjacencies:

| Before | After |
|---|---|
| Sound · Voice | Sound · Voice |
| **Mission Text · Screen Shake** | Mission Text · Haptics |
| **Haptics · Shadows** | Text Size · Fullscreen |
| **Left-handed Controls · Text Size** | Screen Shake · Shadows |
| | Left-handed Controls |

Unrelated rows sat next to each other (Mission Text beside Screen Shake,
Haptics beside Shadows), which forced a player hunting for one option to
read all eight — the cognitive-load cost D16 identified. Rows are now
grouped: **audio/briefing → display → play**.

**The reorder exposed a bug worth more than the reorder.** Rows were
toggled by *positional index*, and the touch list (8 rows) and desktop list
(9 rows) are different lengths sharing one index space. Reordering the
rows — or adding one — would have silently toggled **the wrong setting**.
Settings are now keyed by a `SettingId` enum, and one table drives build,
refresh, toggle and the toggle-blip, so display order is free to change and
nothing can point at the wrong row.

*Test:* `Settings_RelatedRows_AreAdjacent` asserts the clusters stay
contiguous **in display order** — which is what the player actually sees,
not the source order.

---

## Appendix D — queue closed

Every item in the D13–D18 queue is now resolved:

| ID | Outcome |
|----|---------|
| **D13** | **Done** — touch floor 100 → 120 units (~54 dp); all layouts re-solved; test harness built that makes touch layouts verifiable at all |
| **D14** | **Done** — pause button given a reserved segment of the top band; clock overlap measured at 20 units, worse than the 10 estimated |
| **D15** | **Closed, not needed** — measured: `Outline` is an `IMeshModifier`, not a material break; there was nothing to fix |
| **D16** | **Done** — rows grouped; positional-toggle bug found and removed |
| **D17** | **Deferred by decision** — TMP/ATG at localization or Steam, not before |
| **D18** | **Done** — portal cue 28 → 120 units with an ease-in; peak volume deliberately unchanged |

**Still owed, and it is not code:** a **real-device pass**. Every layout in
this appendix is asserted numerically (70+ tests) but none of it has been
seen on a phone. The editor Game view does not reproduce a device's aspect
ratio or safe area, so simulator/editor checks cannot close this. That,
plus the gamepad hardware pass and installing a current build, is the
remaining verification work.
