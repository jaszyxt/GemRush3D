# HANDOFF — tooling session 2026-09-17 (~10:00–10:30)

Changes made in the tooling session ("research free tools, install them,
use my github account, consider graphics, flag paid gold standards").

## Shipped & installed on the phone
- **v1.9.1 (versionCode 14)** — installed on RFCW40396ZN, launch smoke-tested
  (process alive after 8s).
  - **Haptics actually work now.** Every previous APK lacked
    `android.permission.VIBRATE` (JNI vibrate threw SecurityException, swallowed
    by the catch — silent no-op on device). Haptics.cs rewritten:
    - API 29+ predefined effects (click/tick/heavy click) — device-tuned.
    - API 26+ amplitude control (Light 72 / Medium 140 / Heavy 230).
    - `Handheld.Vibrate` fallback both covers old devices AND makes Unity
      stamp VIBRATE into the manifest. Verified via aapt on the built APK.
  - Intensity remap: death + win = Heavy, bounce-pad launch = Medium.

## Tooling installed in the project
- **Repo**: git initialized, pushed to **private**
  https://github.com/jaszyxt/GemRush3D (LFS stripped — was accidentally
  tracking the embedded PPv3 doc images). gh CLI installed + authenticated
  as jaszyxt. Signing secrets for CI (`GEMRUSH_KEYSTORE_BASE64`,
  `GEMRUSH_SIGNING`) already set.
- **Tests**: `Assets/Scripts/GemRush.asmdef` added (game code now compiles
  as GemRush.dll — behavior identical). 12 EditMode level-audit tests in
  `Assets/Tests/EditMode/LevelAuditTests.cs` (DESIGN.md eng queue #3):
  metadata/uniqueness, gem reachability incl. gust lanes + wind columns,
  portal/spawn reachability, KillY sanity, orphan islands (edge-gap based),
  spinner grounding, medal-time ordering. All 24 levels pass in 0.6s.
- **CI**: `.github/workflows/syntax-check.yml` (free stub-compile on every
  push — **active and green**; the stubs were extended from the old 24-script
  subset to the full 82-script API surface, plus `!tools/Check.csproj`
  gitignore negation) and `android-build.yml` (GameCI v4: audit tests +
  signed APK + GitHub Release on `v*` tags — **waiting on the one-time Unity
  license secrets**, steps in README-CI.md ≈5 min).
- **Packages**: com.unity.memoryprofiler 1.1.12, com.unity.performance.
  profile-analyzer 1.4.0 (editor-only dev tools).
- `tools/Check.csproj` replaces the stale csc-response.txt list (glob-based).

## Research findings (full report in the session transcript)
- Skipped as no-value: UniTask (zero async/coroutines in codebase; Unity 6
  Awaitable covers future needs), DOTween (hand-rolled tweens work; no UPM
  install exists), NaughtyAttributes (no inspector workflow), analytics SDKs
  (N=1 sideloaded; Unity built-in Diagnostics toggle = the only worthwhile
  one, needs Unity Dashboard link), Firebase (defer to Play Store launch).
- **Graphics (the strategic item): Unity announced BiRP is deprecated from
  6.5, supported only through 6.7 LTS — URP is the go-forward pipeline.**
  This codebase is a near-best-case migration (3 shader-name strings +
  material property remap in ArtLib/Fx/Backdrop, ~1–2 days) and unlocks
  code-only VolumeProfile (bloom for gems/portal, per-level color grading —
  no asset files, satisfies the zero-asset rule). Recommended next milestone.
  Quick wins that work TODAY on BiRP: runtime panoramic skybox texture from
  the palette data; blob/contact shadows under characters and gems.
- Paid gold standards (all flagged OVERKILL except): DOTween Pro $15,
  Stylized Clouds Generator URP $20, Stylized Water 3 $25–50 sale — total
  < $100 for everything actually relevant.

## Notes for the next session
- A `[GustDebug]` Debug.Log appeared in GustZone.cs during this session —
  not mine (concurrent session's debugging?). Compiles fine; consider
  stripping before the next release.
- Version bump note: the editor must REFRESH after editing
  EnsureShaders.cs before invoking Build() via execute_code, or the stale
  compiled method stamps the old version (bit me once; 10:14 build said
  1.9.0 until refreshed).
- Do not push a `v*` tag until the UNITY_LICENSE/EMAIL/PASSWORD secrets
  exist (README-CI.md), or the release build will fail.
- Unity 6.5+ BiRP deprecation → plan URP migration before 6.7 LTS EOL;
  drop the embedded com.unity.postprocessing when migrating (URP volumes
  replace it cleanly).
