using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// The game builds every material at runtime from shaders looked up by
    /// name (Shader.Find). Shaders that nothing in a build references are
    /// stripped by Unity, which works fine in the editor but crashes on
    /// device. This pins the required shaders onto the project's
    /// "Always Included Shaders" list as soon as the project is opened.
    [InitializeOnLoad]
    public static class EnsureShaders
    {
        const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";

        static readonly string[] RequiredShaders =
        {
            "Universal Render Pipeline/Lit",    // all world materials (URP)
            "Skybox/Procedural",           // runtime skybox
            "Sprites/Default",             // portal fill
            "Universal Render Pipeline/Particles/Unlit", // particle bursts
            "UI/Default",                  // uGUI
            "Unlit/Texture",               // Backdrop sky gradient
            "Unlit/Color",                 // Backdrop island silhouettes
            "Universal Render Pipeline/Unlit" // Backdrop fallback
        };

        static EnsureShaders()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[GemRush] Could not load " + GraphicsSettingsPath);
                return;
            }

            SerializedObject graphicsSettings = new SerializedObject(assets[0]);
            SerializedProperty included =
                graphicsSettings.FindProperty("m_AlwaysIncludedShaders");
            if (included == null || !included.isArray)
            {
                Debug.LogWarning("[GemRush] m_AlwaysIncludedShaders not found.");
                return;
            }

            HashSet<string> existing = new HashSet<string>();
            for (int i = 0; i < included.arraySize; i++)
            {
                Shader shader = included.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (shader != null) existing.Add(shader.name);
            }

            bool changed = false;
            for (int i = 0; i < RequiredShaders.Length; i++)
            {
                if (existing.Contains(RequiredShaders[i])) continue;
                Shader shader = Shader.Find(RequiredShaders[i]);
                if (shader == null)
                {
                    Debug.LogWarning("[GemRush] Shader not found: " + RequiredShaders[i]);
                    continue;
                }
                included.InsertArrayElementAtIndex(included.arraySize);
                included.GetArrayElementAtIndex(included.arraySize - 1)
                    .objectReferenceValue = shader;
                changed = true;
                Debug.Log("[GemRush] Pinned shader into build: " + RequiredShaders[i]);
            }

            if (changed) graphicsSettings.ApplyModifiedProperties();
        }
    }

    /// Headless release builder: sets identity, icon and signing, then builds
    /// the Android APK. Run with
    /// Unity.exe -batchmode -projectPath <proj> -executeMethod GemRush.EditorTools.BuildAndroid.Build -quit
    public static class BuildAndroid
    {
        const string BuildMenuPath = "GemRush/Build Android APK (Release)";

        /// The one place a release version is written by hand:
        /// GemRush.version at the repo root. Everything else
        /// (bundleVersion, the Android versionCode, git tags, docs)
        /// derives from it — the old hardcoded pair here drifted from
        /// HANDOFF and Steam-Deploy without anyone noticing.
        ///
        /// Deliberately NOT named "VERSION": IL2CPP compiles the C++
        /// libc++ headers, and <variant> does `#include <version>`.
        /// Windows filesystems are case-insensitive, so a root file named
        /// VERSION resolves for that include and clang tries to compile
        /// its contents as C++ — "version(1,1): error: expected
        /// unqualified-id", 20 errors, Android build dead. Renaming is
        /// the fix; the name must never collide with a C++ std header.
        const string VersionFileName = "GemRush.version";

        /// Absolute path to the version file, resolved against the
        /// PROJECT ROOT rather than the process working directory: a
        /// relative "VERSION" silently yielded the 0.0.1 fallback (and a
        /// versionCode of 1) whenever the editor's cwd was not the
        /// project, which reads as a valid build while regressing Play's
        /// monotonic code.
        static string VersionFilePath
        {
            get
            {
                return System.IO.Path.Combine(
                    System.IO.Directory.GetParent(Application.dataPath).FullName,
                    VersionFileName);
            }
        }

        /// Minor/patch ceiling for the derived Android versionCode. Play
        /// requires it to increase monotonically; encoding major*10000 +
        /// minor*100 + patch keeps that true for any sane version, and
        /// 43 (the old hand-held value) maps to 0.0.43 — so a bump to
        /// 1.27.0 or beyond is always greater.
        public static int DeriveVersionCode(string version)
        {
            string[] parts = version.Split('.');
            int major = parts.Length > 0 ? ParsePart(parts[0]) : 0;
            int minor = parts.Length > 1 ? ParsePart(parts[1]) : 0;
            int patch = parts.Length > 2 ? ParsePart(parts[2]) : 0;
            return major * 10000 + minor * 100 + patch;
        }

        static int ParsePart(string text)
        {
            int value = 0;
            for (int i = 0; i < text.Length; i++)
                if (text[i] >= '0' && text[i] <= '9')
                    value = value * 10 + (text[i] - '0');
            return value;
        }

        /// Reads GemRush.version, falling back to a loud default rather
        /// than silently shipping a stale number if the file goes missing.
        public static string ReadVersion()
        {
            try
            {
                if (System.IO.File.Exists(VersionFilePath))
                    return System.IO.File.ReadAllText(VersionFilePath).Trim();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GemRush] Could not read " + VersionFileName +
                    ": " + e.Message);
            }
            Debug.LogError("[GemRush] " + VersionFileName + " missing — shipping " +
                "fallback version 0.0.1. Create " + VersionFileName +
                " at the repo root.");
            return "0.0.1";
        }

        /// Triggerable from the editor menu or via Unity MCP's
        /// menu-item execution tool.
        [MenuItem(BuildMenuPath)]
        public static void Build()
        {
            PlayerSettings.productName = "Gem Rush 3D";
            // Steam-Deploy doc: DefaultCompany must never ship (it feeds
            // the exe's copyright metadata); match the signing cert's
            // organization.
            PlayerSettings.companyName = "PipStudio";
            // But the ANDROID package identity must stay the legacy one:
            // Unity derives it from companyName, and every installed
            // device knows com.DefaultCompany.GemRush3D — a new identity
            // would orphan their saves. Pin it explicitly.
            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.Android, "com.DefaultCompany.GemRush3D");
            PlayerSettings.bundleVersion = ReadVersion();
            PlayerSettings.Android.bundleVersionCode = DeriveVersionCode(
                PlayerSettings.bundleVersion);

            // Desktop window UX (D8): a resizable borderless-fullscreen
            // window at the UI's native reference size that keeps running
            // when it loses focus. The in-game Settings Fullscreen row
            // overrides the mode per machine at runtime.
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = true;

            ApplyIcon();
            ApplySigning();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android, BuildTarget.Android);
            }

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = new string[] { "Assets/Scenes/Game.unity" };
            options.locationPathName = "Builds/GemRush3D.apk";
            options.target = BuildTarget.Android;

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[GemRush] Build failed: " + report.summary.result);
                EditorApplication.Exit(1);
            }
            Debug.Log("[GemRush] APK built: " +
                System.IO.Path.GetFullPath(options.locationPathName));

            // Windows standalone from the same session — same version and
            // content, so laptop players and phone players stay in sync.
            BuildPlayerOptions winOptions = new BuildPlayerOptions();
            winOptions.scenes = new string[] { "Assets/Scenes/Game.unity" };
            winOptions.locationPathName = "Builds/GemRush3D.exe";
            winOptions.target = BuildTarget.StandaloneWindows64;

            BuildReport winReport = BuildPipeline.BuildPlayer(winOptions);
            if (winReport.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[GemRush] Windows build failed: " +
                    winReport.summary.result);
                EditorApplication.Exit(1);
            }
            Debug.Log("[GemRush] Windows build: " +
                System.IO.Path.GetFullPath(winOptions.locationPathName));

            // The point of the Windows pass: the laptop copy. Install the
            // fresh build to the local per-user folder and point the
            // Desktop shortcut at it, so every update is playable the
            // moment the build finishes.
            DeployWindowsInstall();
        }

        // ------------------------------------------------------------------
        // Local Windows install (the "laptop copy")
        // ------------------------------------------------------------------

        const string InstallDirName = "GemRush3D";
        const string ShortcutName = "Gem Rush 3D";

        static string InstallRoot()
        {
            return System.IO.Path.Combine(
                System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.LocalApplicationData),
                "Programs", InstallDirName);
        }

        /// Copies the freshly built Windows player from Builds/ into the
        /// per-user install folder and (re)writes the Desktop shortcut.
        /// Runs automatically after every build; also available standalone
        /// via the menu. Tolerates the game running: locked files skip
        /// with a loud warning instead of failing the build.
        [MenuItem("GemRush/Install Windows Build (Local)")]
        public static void DeployWindowsInstall()
        {
            string projectRoot =
                System.IO.Directory.GetParent(Application.dataPath).FullName;
            string src = System.IO.Path.Combine(projectRoot, "Builds");
            string dst = InstallRoot();

            if (!System.IO.File.Exists(
                    System.IO.Path.Combine(src, "GemRush3D.exe")))
            {
                Debug.LogWarning("[GemRush] No Windows build in Builds/ yet " +
                    "— nothing to install. Build first.");
                return;
            }
            System.IO.Directory.CreateDirectory(dst);

            // The game data first (it IS the update), then the scripting
            // runtime and render plugins, then the loose files — so a
            // locked loose DLL (the game still running) can't hold back
            // the parts that matter. Every file copies with overwrite;
            // locked files skip individually. Nothing is ever DELETED:
            // a half-updated install would be worse than a stale one,
            // and the next deploy (game closed) overwrites the rest.
            int copied = 0;
            int skipped = 0;
            foreach (string dirName in new string[]
                     {
                         "GemRush3D_Data", "MonoBleedingEdge", "D3D12"
                     })
            {
                string dirSrc = System.IO.Path.Combine(src, dirName);
                if (!System.IO.Directory.Exists(dirSrc)) continue;
                string dirDst = System.IO.Path.Combine(dst, dirName);
                CopyDirectory(dirSrc, dirDst, ref copied, ref skipped);
            }

            // Top-level files: the player launcher plus every loose dll.
            foreach (string file in System.IO.Directory.GetFiles(src))
            {
                if (System.IO.Path.GetFileName(file) ==
                    "GemRush3D_BackUpThisFolder_ButDontShipItWithYourGame")
                    continue;
                CopyFileResilient(file, System.IO.Path.Combine(dst,
                    System.IO.Path.GetFileName(file)), ref copied, ref skipped);
            }

            WriteDesktopShortcut(dst);
            if (skipped == 0)
                Debug.Log("[GemRush] Windows install updated at " + dst +
                    " (" + copied + " files). Desktop shortcut: " +
                    ShortcutName + ".");
            else
                Debug.LogWarning("[GemRush] Windows install PARTIALLY " +
                    "updated at " + dst + " (" + copied + " copied, " +
                    skipped + " locked — the game is still running). Close " +
                    "the game and re-run GemRush/Install Windows Build " +
                    "(Local) — or rebuild; the next deploy self-heals.");
        }

        /// (Re)writes the Desktop shortcut so it always launches the
        /// installed copy. Idempotent: same name, same target every time.
        ///
        /// Written through the Windows Script Host COM object directly,
        /// in-process. An earlier version shelled out to powershell.exe
        /// with the paths pasted into a command string — that meant a path
        /// containing a quote (a profile like C:\Users\O'Brien\) could both
        /// break the command and land in executable position. Calling COM
        /// needs no shell, no command text and no Process.Start at all, so
        /// there is nothing to escape and nothing to inject into.
        ///
        /// Any failure is non-fatal: the install itself has already
        /// succeeded by the time we get here, and the log says so.
        static void WriteDesktopShortcut(string installRoot)
        {
            string desktop = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.DesktopDirectory);
            string lnk = System.IO.Path.Combine(desktop,
                ShortcutName + ".lnk");
            string exe = System.IO.Path.Combine(installRoot, "GemRush3D.exe");
            try
            {
                WriteShortcutCom(lnk, exe, installRoot);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GemRush] Desktop shortcut update failed (" +
                    e.Message + ") — the game itself installed fine. Launch " +
                    exe + " directly.");
            }
        }

        /// Writes a .lnk through the Windows Script Host COM object.
        ///
        /// If COM is unavailable, the existing shortcut is LEFT ALONE and a
        /// warning is logged. It is never overwritten with anything this
        /// method cannot verify.
        ///
        /// That rule is the scar from two failed attempts:
        ///
        /// 1. A powershell.exe shell-out with the paths pasted into a command
        ///    string. Correct escaping (verified against injection payloads),
        ///    but a reviewer cannot confirm that from the call site, and the
        ///    security gate stopped it — rightly, since a safer form exists.
        ///
        /// 2. A hand-rolled .lnk, byte for byte, to remove the shell entirely.
        ///    It wrote a syntactically valid Shell Link whose LinkFlags said
        ///    HasArguments where the code meant HasName, so Windows read the
        ///    name string as an argument list: TargetPath came back EMPTY.
        ///    Worse, it OVERWROTE a working shortcut with that broken file —
        ///    replacing a stale-but-usable icon with a dead one.
        ///
        /// The lesson: writing a binary format you cannot read back is not a
        /// safe substitute for a mechanism you cannot call. COM is the only
        /// path here that produces a shortcut this code can verify, so COM is
        /// the only path that writes.
        static void WriteShortcutCom(string lnk, string exe, string workDir)
        {
            try
            {
                WriteShortcutViaWsh(lnk, exe, workDir);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GemRush] Desktop shortcut not refreshed (" +
                    e.Message + "). The EXISTING shortcut is untouched. If " +
                    "the install path changes, recreate it by hand pointing " +
                    "at " + exe + ".");
            }
        }

        /// The one route that writes: late-bound WScript.Shell via COM.
        ///
        /// It may throw "Unmanaged activation is not supported" under Unity's
        /// Mono. That is expected and handled above — the shortcut is then
        /// left as it is, which is strictly better than replacing a working
        /// icon with a file we cannot verify.
        static void WriteShortcutViaWsh(string lnk, string exe, string workDir)
        {
            System.Type shellType = System.Type.GetTypeFromProgID(
                "WScript.Shell", true);
            object shell = System.Activator.CreateInstance(shellType);
            try
            {
                object shortcut = shellType.InvokeMember(
                    "CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod, null,
                    shell, new object[] { lnk });
                System.Type scType = shortcut.GetType();
                scType.InvokeMember("TargetPath",
                    System.Reflection.BindingFlags.SetProperty, null,
                    shortcut, new object[] { exe });
                scType.InvokeMember("WorkingDirectory",
                    System.Reflection.BindingFlags.SetProperty, null,
                    shortcut, new object[] { workDir });
                scType.InvokeMember("Save",
                    System.Reflection.BindingFlags.InvokeMethod, null,
                    shortcut, null);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
            }
        }

        /// Clear the read-only attribute on a file (a copied-in player can
        /// carry it, and File.Delete refuses to remove read-only files).
        static void UnlockFile(string path)
        {
            if (!System.IO.File.Exists(path)) return;
            System.IO.FileAttributes attrs = System.IO.File.GetAttributes(path);
            if ((attrs & System.IO.FileAttributes.ReadOnly) != 0)
                System.IO.File.SetAttributes(path,
                    attrs & ~System.IO.FileAttributes.ReadOnly);
        }

        /// Copies every file under src into dst (structure preserved),
        /// overwriting. Locked or unreadable files are skipped and
        /// counted — never fatal, never partial-deleting.
        static void CopyDirectory(string src, string dst,
            ref int copied, ref int skipped)
        {
            foreach (string file in System.IO.Directory.GetFiles(src))
                CopyFileResilient(file,
                    System.IO.Path.Combine(dst,
                        System.IO.Path.GetFileName(file)),
                    ref copied, ref skipped);
            foreach (string dir in System.IO.Directory.GetDirectories(src))
                CopyDirectory(dir,
                    System.IO.Path.Combine(dst,
                        System.IO.Path.GetFileName(dir)),
                    ref copied, ref skipped);
        }

        static void CopyFileResilient(string srcFile, string dstFile,
            ref int copied, ref int skipped)
        {
            try
            {
                UnlockFile(dstFile);
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(dstFile));
                System.IO.File.Copy(srcFile, dstFile, true);
                copied++;
            }
            catch (System.Exception e)
            {
                skipped++;
                if (skipped <= 3) // don't spam the console; the count tells all
                    Debug.LogWarning("[GemRush] Skipped locked file " +
                        System.IO.Path.GetFileName(dstFile) +
                        " (the game is still running). " + e.Message);
            }
        }

        static void ApplyIcon()
        {
            const string iconPath = "Assets/Textures/AppIcon.png";
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (icon == null)
            {
                Debug.Log("[GemRush] Painting app icon.");
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(iconPath));
                Texture2D painted = PaintIcon();
                System.IO.File.WriteAllBytes(iconPath, painted.EncodeToPNG());
                AssetDatabase.ImportAsset(iconPath);
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            }
            if (icon != null)
            {
                // Unity 6: Android icons are adaptive-only; Legacy/Round are
                // deprecated as errors.
                NamedBuildTarget android = NamedBuildTarget.Android;
                PlatformIconKind kind =
                    UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
                PlatformIcon[] slots = PlayerSettings.GetPlatformIcons(android, kind);
                for (int i = 0; i < slots.Length; i++) slots[i].SetTexture(icon);
                PlayerSettings.SetPlatformIcons(android, kind, slots);
                Debug.Log("[GemRush] App icon applied (adaptive).");
            }
        }

        static void ApplySigning()
        {
            string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            string keystore = System.IO.Path.Combine(projectRoot, "tools", "gemrush.keystore");
            string passFile = System.IO.Path.Combine(projectRoot, "tools", "signing.txt");
            if (!System.IO.File.Exists(keystore) || !System.IO.File.Exists(passFile))
            {
                Debug.LogWarning("[GemRush] No keystore/signing.txt; debug-signed build.");
                return;
            }
            string password = "";
            string aliasName = "gemrush";
            foreach (string line in System.IO.File.ReadAllLines(passFile))
            {
                if (line.StartsWith("storepass=")) password = line.Substring(10);
                else if (line.StartsWith("alias=")) aliasName = line.Substring(6);
            }
            // Without this flag Unity silently ignores the keystore below and
            // ships debug-signed — the exact bug v1.0.0's APK had.
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = password;
            PlayerSettings.Android.keyaliasName = aliasName;
            PlayerSettings.Android.keyaliasPass = password;
            Debug.Log("[GemRush] Signing with release keystore 'gemrush'.");
        }


        // ---------- Icon painter ----------

        static Texture2D PaintIcon()
        {
            int s = 512;
            Color[] px = new Color[s * s];

            for (int y = 0; y < s; y++)
            {
                float t = y / (float)(s - 1);
                Color sky = Color.Lerp(new Color(0.25f, 0.5f, 0.93f),
                    new Color(0.62f, 0.83f, 1f), t);
                for (int x = 0; x < s; x++) px[y * s + x] = sky;
            }

            FillEllipse(px, s, 415, 405, 55, 55, new Color(1f, 0.95f, 0.72f));
            FillEllipse(px, s, 105, 415, 75, 26, new Color(1f, 1f, 1f));
            FillEllipse(px, s, 145, 438, 55, 24, new Color(1f, 1f, 1f));
            FillEllipse(px, s, 430, 320, 60, 20, new Color(1f, 1f, 1f));

            FillEllipse(px, s, 256, 150, 215, 48, new Color(0.36f, 0.72f, 0.34f));
            FillEllipse(px, s, 256, 116, 190, 42, new Color(0.5f, 0.37f, 0.25f));

            FillRhombus(px, s, 132, 300, 72, 92, new Color(0.98f, 0.3f, 0.75f));
            FillRhombus(px, s, 132, 300, 28, 38, new Color(1f, 0.65f, 0.9f));
            FillRhombus(px, s, 395, 250, 45, 58, new Color(0.98f, 0.3f, 0.75f));
            FillRhombus(px, s, 395, 250, 18, 24, new Color(1f, 0.65f, 0.9f));

            FillEllipse(px, s, 256, 300, 86, 106, new Color(1f, 0.55f, 0.15f));
            FillEllipse(px, s, 232, 272, 44, 60, new Color(1f, 0.68f, 0.35f));
            FillEllipse(px, s, 338, 288, 30, 24, new Color(0.85f, 0.3f, 0.1f));
            FillEllipse(px, s, 226, 328, 17, 19, Color.white);
            FillEllipse(px, s, 286, 328, 17, 19, Color.white);
            FillEllipse(px, s, 230, 328, 8, 10, new Color(0.1f, 0.1f, 0.12f));
            FillEllipse(px, s, 282, 328, 8, 10, new Color(0.1f, 0.1f, 0.12f));

            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static void FillEllipse(Color[] px, int s, int cx, int cy, int rx, int ry, Color c)
        {
            int x0 = Mathf.Clamp(cx - rx, 0, s - 1);
            int x1 = Mathf.Clamp(cx + rx, 0, s - 1);
            int y0 = Mathf.Clamp(cy - ry, 0, s - 1);
            int y1 = Mathf.Clamp(cy + ry, 0, s - 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) / (float)rx;
                    float dy = (y - cy) / (float)ry;
                    if (dx * dx + dy * dy <= 1f) px[y * s + x] = c;
                }
            }
        }

        static void FillRhombus(Color[] px, int s, int cx, int cy, int rx, int ry, Color c)
        {
            int x0 = Mathf.Clamp(cx - rx, 0, s - 1);
            int x1 = Mathf.Clamp(cx + rx, 0, s - 1);
            int y0 = Mathf.Clamp(cy - ry, 0, s - 1);
            int y1 = Mathf.Clamp(cy + ry, 0, s - 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = Mathf.Abs(x - cx) / (float)rx;
                    float dy = Mathf.Abs(y - cy) / (float)ry;
                    if (dx + dy <= 1f) px[y * s + x] = c;
                }
            }
        }
    }
}
