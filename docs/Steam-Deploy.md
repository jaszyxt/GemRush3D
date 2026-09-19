# Steam Deployment — Study & Runbook (Gem Rush 3D)

Status: study complete 2026-09-20; nothing Steam-specific exists in the repo yet.
The game already builds and runs as a **Windows x64 standalone** (Unity
6000.6.0f1, `Builds/GemRush3D.exe`), which is the artifact Steam wants. What's
missing is entirely on the account/upload side.

## The pipeline at a glance

1. **Steamworks partner account** (one-time) → identity/bank/tax verification.
2. **$100 Steam Direct fee per app** (non-refundable, recouped once the app
   earns $1,000 Adjusted Gross Revenue). Paid when creating the app credit.
3. **Create the app** → you get an **AppID** immediately; builds can be
   uploaded before the store page exists.
4. **Build the Windows player** into a clean staging folder.
5. **Upload with SteamPipe** (`steamcmd` from the Steamworks SDK
   `tools/ContentBuilder/`, driven by two small VDF scripts).
6. **Store page + pricing** → submit for Valve review.
7. Valve reviews **both the build and the store page** (a few business days
   each). Push the default branch live from the web UI when approved.

## One-time setup (account, paperwork, cost)

- Sign up at partner.steamgames.com as a sole trader/company; complete
  identity, bank and tax interview; sign the Steam Distribution Agreement.
- Pay the **$100 app credit** when you create the app.
- Make a **dedicated build account** with "Edit App Metadata" + "Publish App
  Changes To Steam" permissions and a Steam Mobile / phone number attached.
  Security changes trigger a 3-day waiting period — set it up once and leave
  it alone. Pushing builds to a *released* app requires phone confirmation.
- Checks to do early: search Steam for the exact name "Gem Rush 3D" for
  collisions (the name is reserved when the store page is created, not at app
  creation).

## Project prep (GemRush3D-specific)

- `ProjectSettings`: `companyName` is still `DefaultCompany` — set a real
  studio name (also feeds the exe's copyright metadata) before the ship build.
- Produce the ship build into a **clean staging folder** (e.g.
  `build/steam/win/`), not `Builds/` — `Builds/` contains
  `GemRush3D_BackUpThisFolder_ButDontShipItWithYourGame`, which must never
  reach the depot (the depot scripts below exclude it anyway).
- Ship list for Unity 6 Windows x64 (all of these are required at the root):
  `GemRush3D.exe`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`,
  `GemRush3D_Data/`, `MonoBleedingEdge/`, `D3D12/`, `DirectML.dll`,
  `dstorage.dll`, `dstoragecore.dll`.
- Exclude from depot: the BackUp folder, `*.pdb`, any `.apk`.
- Versioning: keep `bundleVersion` (currently 1.22.1) in sync with the commit
  tag convention (`v1.22.1 (code 32)`); put the same string in the SteamPipe
  build `Desc` so builds are traceable.
- Scripting backend is Mono (default) — fine for Steam. IL2CPP is optional
  hardening; don't switch for v1.
- Min spec to state on the page: Windows 10 64-bit. The build is x64-only.

## SteamPipe runbook

Get the SDK (partner site → "Download Steamworks SDK"), then use
`tools/ContentBuilder/`. Bootstrap by running `builder\steamcmd.exe` once.

`scripts/app_build_<AppID>.vdf`:

```
"AppBuild"
{
  "AppID"       "<AppID>"
  "Desc"        "v1.22.1 (code 32)"
  "ContentRoot" "..\staging\win"       // folder holding GemRush3D.exe etc.
  "BuildOutput" "..\output"
  "Depots"
  {
    "<DepotID>"  "depot_build_<DepotID>.vdf"   // 'Windows Content' depot
  }
}
```

`scripts/depot_build_<DepotID>.vdf`:

```
"DepotBuild"
{
  "DepotID"     "<DepotID>"
  "ContentRoot" "..\staging\win"
  "FileMapping"
  {
    "LocalPath" "*"
    "DepotPath" "."
    "Recursive" "1"
  }
  "FileExclusion" "GemRush3D_BackUpThisFolder_ButDontShipItWithYourGame\*"
  "FileExclusion" "*.pdb"
  "FileExclusion" "*.apk"
}
```

Upload (from `ContentBuilder/`):

```
builder\steamcmd.exe +login <build-account> +run_app_build ..\scripts\app_build_<AppID>.vdf +quit
```

First login needs password + SteamGuard once; afterwards the saved
`config\config.vdf` allows passwordless runs — that's the hook for CI
(README-CI pipeline can call the same command after a successful build).
`"Preview" "1"` dry-runs (manifest only, no upload); failures leave `*.log`
in the output folder. Upload to a **private beta branch** first, install it
via Steam from your own library, and only move it to the default branch after
the build test passes. The default branch on a released app is always set
live manually in the web UI (`SetLive` in the script works for beta branches
only).

## Store page checklist

- Graphical assets: header capsule 1232×706, small capsule 462×174, library
  capsules 600×900 and 300×450, hero/banner, community icon 184×184.
- ≥5 screenshots (1920×1080) — we already produce screenshots for the repo;
  Steam needs clean UI shots without debug overlays.
- Trailer (strongly recommended; required for a good Coming Soon page).
- Description, features list, system requirements.
- Pricing: paid needs ≥ $0.99 USD; free is allowed. Decide before review.
- **Put the page live as "Coming Soon" early** — wishlisting during the
  months before launch is the main free traffic mechanism on Steam.

## Steam Cloud caveat

Progress is stored in **PlayerPrefs** (`SaveSystem.cs`, `gemrush_v2_*` keys),
which on Windows lives in the registry. Steam Cloud syncs *files only*, so
out of the box there is nothing to sync. Options:
1. Skip Steam Cloud at launch (fine for a level-puzzle game).
2. Later: mirror the PlayerPrefs blob to a small JSON under
   `persistentDataPath` on save, then enable Steam Auto-Cloud on that path.

## Post-launch (phase 2, optional)

- **Steamworks SDK integration** (Steamworks.NET or Facepunch.Steamworks
  package) for achievements, Rich Presence, and playtime — requires the
  AppID compiled in. For local testing before the app is approved, a
  `steam_appid.txt` with the AppID next to the exe opts into a sandbox.
- **Steam Deck**: Windows builds run through Proton; the game already
  supports gamepad, so a "Playable"/"Verified" review is plausible after a
  compatibility pass.
- Trading cards, achievements icons, seasonal sales — later.

## Realistic timeline

| Step | Wall time |
|---|---|
| Paperwork + verification + $100 | a few business days |
| App creation → AppID | same day |
| Staging build + first SteamPipe upload | 1 day of work |
| Store page assets + copy | 1–2 days of work |
| Valve store + build review | a few business days |
| Release | immediate after approval |

Total: roughly **1–2 weeks calendar**, most of it waiting on verification and
review. Work that can start today without the account: name check, staging
> **Status 2026-09-20 (v1.25.0): DONE — companyName = PipStudio (stamped by the build script), staging via `GemRush/Stage Steam Build (win)` into `build/steam/win/` (pdb/apk/backup excluded), VDF templates in `tools/ContentBuilder/scripts/`. Remaining user-side: account, $100 fee, store assets, trailer.**

build script, store assets from existing screenshots, the two VDF scripts
(AppID placeholder).

## Sources

- [Uploading to Steam (Steamworks docs)](https://partner.steamgames.com/doc/sdk/uploading)
- [Getting started as a Steamworks partner](https://partner.steamgames.com/doc/gettingstarted)
- [Steam Direct Fee](https://partner.steamgames.com/doc/gettingstarted/appfee)
