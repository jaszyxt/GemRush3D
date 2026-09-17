# CI setup — one-time steps

Two workflows live in `.github/workflows/`:

- **`syntax-check.yml`** — compiles every game script against the Unity
  stubs (`tools/Check.csproj`) on every push. Free, no Unity license, no
  setup needed. Already active.
- **`android-build.yml`** — GameCI: runs the EditMode level-audit tests,
  builds the signed APK (`GemRush.EditorTools.BuildAndroid.Build`) and
  attaches it to a GitHub Release on `v*` tags. Needs the one-time setup
  below; until then it simply never triggers (tags/manual dispatch only).

## One-time: Unity license secrets (≈5 minutes)

1. Open **Unity Hub → Preferences → Licenses → Add → Get a free personal
   license** (click Add even if a license already appears listed).
2. Copy `C:\ProgramData\Unity\Unity_lic.ulf`.
3. From this folder (gh CLI is installed and authenticated):

   ```
   gh secret set UNITY_LICENSE < "C:\ProgramData\Unity\Unity_lic.ulf"
   gh secret set UNITY_EMAIL    <your-unity-account-email>
   gh secret set UNITY_PASSWORD <your-unity-account-password>
   ```

Signing secrets (`GEMRUSH_KEYSTORE_BASE64`, `GEMRUSH_SIGNING`) are
already set — CI reproduces the local `tools/gemrush.keystore` +
`tools/signing.txt` flow from them.

Personal licenses expire every few weeks; when builds start failing on
license activation, repeat steps 1–3.

## Releasing

```
git tag v1.9.1 && git push --tags
```

The workflow runs the audit tests, builds the signed APK, uploads it as
an artifact and creates a GitHub Release with the APK attached.

## Cost

The repo is private: 2,000 free Actions minutes/month. syntax-check uses
~15s per push (Linux 1x). A cold android build is ~30–60 min, warm
(with the Library cache) much less — the tag-only trigger keeps usage
far under budget. Making the repo public makes all of it free.
