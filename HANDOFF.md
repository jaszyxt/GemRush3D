# HANDOFF — side-chat session 2026-09-17 (~08:00–09:00)

Changes made in the side chat, for the main thread to pick up.

## Shipped & installed
- **v1.8.1 (versionCode 11)** — installed on the phone (RFCW40396ZN), verified clean.
  - Course-side clouds exiled: spawn band moved from |x| 12–26 to **|x| 20–32**, count 12→10.
  - Gloomfang follower station moved off the camera axis: **(1.8, 2.2, −2.4)** relative to Pip.

## Built & verified, NOT yet installed
- **v1.8.2 (versionCode 12)** — APK in `Builds/GemRush3D.apk`, aapt-verified. The install
  command was cancelled before it ran; **the phone is still on v1.8.1**. Install on next
  opportunity (straight update, save preserved).
  - Gloomfang v2 (player-reported view-blocking fix, player approved "you decide"):
    - **Camera-anchored station**: hovers at the camera's right edge (right × 3.2, up × ~1.5,
      back × 2.2) — measured on-screen at viewport x≈0.73 while Pip stays at x≈0.50.
      He cannot end up between camera and Pip by construction.
    - **Real-cloud sphere body at 78% vapor opacity** (ArtLib.SetFade), warmer
      silver-lavender palette; eyes and gold badge remain fully opaque.
    - **Shared builder** `Gloomfang.BuildBody(...)` now also drives the playable
      Gloomfang in Level 16 (`PlayerController.BuildGloomfangVisuals` delegates to it).

## Files touched this session
- `Assets/Scripts/Gloomfang.cs` — full rewrite (see above)
- `Assets/Scripts/PlayerController.cs` — BuildGloomfangVisuals delegates to shared builder
- `Assets/Scripts/LevelBuilder.cs` — cloud exclusion band widened (low layer)
- `Assets/Editor/EnsureShaders.cs` — version 1.8.2 / versionCode 12

## Standing policies adopted (user-issued, keep honoring)
1. **Player point-of-view reports = confirmed defects, top priority** — above roadmap and
   new content. The user comments from direct play on the phone (RFCW40396ZN).
2. **Continuous release loop**: after each verified build → auto-install → immediately
   continue development on the next increment → build → install → repeat. The cycle
   never waits for a prompt.
3. Decoration clouds (distant/high layers) are approved as-is — do not re-tune them.

## Open items
- Install v1.8.2 (see above) — it supersedes the installed v1.8.1.
- Expansion queue continues per `DESIGN.md` (Pack 7 Sky Garden shipped in v1.8.0;
  Storm Chasers / tailwind gusts next).
