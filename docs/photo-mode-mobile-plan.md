# Photo Mode Mobile Availability Plan

## Current state
Photo mode is gated to desktop only. The gate lives at `UIManager.cs:1627`:
```csharp
if (IsDesktopPlatform())
    pausePhotoButton = MakeButton(...);
```

The save path uses `Environment.SpecialFolder.MyPictures` (desktop concept),
and OPEN FOLDER calls `Application.OpenURL(photoFolder)` which is a desktop
behavior. Both work fine on desktop. On mobile, MyPictures may return an
empty string, and OpenURL on a folder path is unreliable.

## Changes (all in UIManager.cs)

### 1. Remove the desktop gate on the PHOTO button
Line 1627: delete the `if (IsDesktopPlatform())` wrapping. The button
exists on all platforms. The `rowY(2)` position works for both since the
derived pause-stack step pushes `stackTop` up when needed.

### 2. Save path — platform-aware
In `ShowPhotoMode()` (line 1768): use `persistentDataPath` on mobile,
`MyPictures` on desktop:
```csharp
photoFolder = IsDesktopPlatform()
    ? System.IO.Path.Combine(
          System.Environment.GetFolderPath(
              System.Environment.SpecialFolder.MyPictures),
          "GemRush3D")
    : System.IO.Path.Combine(
          Application.persistentDataPath, "GemRush3D");
```

### 3. OPEN FOLDER — hide on mobile
In `CapturePhotoRoutine` (line 1833):
```csharp
photoOpenFolder.gameObject.SetActive(
    TryWritePng(path, png) && IsDesktopPlatform());
```
On desktop: shows after success. On mobile: never shown (no reliable way
to open a file manager). The status text still shows the path, which is
useful for debugging and consistent across platforms.

### 4. Status text — keep as-is
`Strings.PhotoSavedTo(path)` shows "Saved to /path/...". On mobile the
path is less useful to the player but is honest about where the file is,
and is useful for debugging. No change needed.

### 5. Strings — no changes needed
The existing strings are platform-neutral: "PHOTO", "CAPTURE", "DONE",
"Saved to {path}", "OPEN FOLDER". Only OPEN FOLDER's visibility changes,
not its text.

## What NOT to change
- PhotoMode.cs (orbit camera): pure logic, no platform concerns.
- Scorecard: renders identically on both platforms.
- `TryWritePng`: platform-agnostic, already uses `File.WriteAllBytes`.
- Pause panel stack layout: derived step handles any row count; the
  existing `stackRows` calculation already accommodates PHOTO on desktop.
  Touch currently has `stackRows=3` (no PHOTO row) vs desktop `stackRows=4`.
  With PHOTO on all platforms, `stackRows` should always be 4 when PHOTO
  exists. But actually: the `stackRows` calculation is
  `IsDesktopPlatform() ? 4 : 3` — it currently excludes PHOTO on touch.
  Changing to always 4 when PHOTO button exists is the fix.

  Wait — the PHOTO button exists on touch now (gate removed). But
  `stackRows` still checks `IsDesktopPlatform()` to decide 3 vs 4. If
  PHOTO exists on touch but `stackRows=3`, the MENU/SETTINGS row overlaps
  PHOTO. Fix: `stackRows = 4` whenever `pausePhotoButton` is non-null.
  The derived step already handles the spacing.

## Files to edit
- `Assets/Scripts/UIManager.cs`: lines 1586-1654 (BuildPause), 1763-1780
  (ShowPhotoMode), 1833 (CapturePhotoRoutine success path)

## Verification
- In-editor: play level 0, pause, confirm PHOTO appears on touch-layout
  forced path.
- Save path: capture, check `persistentDataPath/GemRush3D/` has a PNG.
- OPEN FOLDER: should NOT appear after capture (mobile path).
- Full EditMode suite passes.
