using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Visual regression harness. Captures a fixed set of scenes — one per
    /// realm plus the menu and the win screen — into a baseline folder,
    /// and on later runs reports a per-scene DIFF against that baseline.
    ///
    /// Why this exists: the visual identity is code, so a change anywhere
    /// can silently alter a realm nobody looked at. Audits by eye caught
    /// things late (the mirrored level sign survived weeks of play because
    /// the sign is small and behind the camera). A baseline plus a numeric
    /// diff turns "does this still look right?" into a measurement.
    ///
    /// It reports, per scene: mean absolute channel delta, the fraction of
    /// pixels meaningfully changed, and whether that breaches this file's
    /// thresholds. It does NOT judge beauty — a large diff may be a
    /// deliberate art change. It answers "did this move?", and forces a
    /// human to say why.
    ///
    /// Menu: GemRush/Visual Baseline/Capture (writes the baseline) and
    /// GemRush/Visual Baseline/Compare (diffs against it). Play mode only.
    /// </summary>
    public static class VisualBaselineProbe
    {
        public static string Report { get; private set; } = "not run";

        // Where baselines live. Deliberately OUTSIDE Assets/: writing PNGs
        // into the project triggers an import + domain reload mid-run, which
        // used to kill the probe's own session (see PhotoProbe's history).
        static string Root
        {
            get
            {
                string project = Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(project, "tools", "visual-baseline");
            }
        }

        // A scene is one level index plus a label. Chosen to cover every
        // realm family and both UI extremes, not to be exhaustive: this
        // runs on every art change, so it must stay quick (~10 scenes).
        struct Scene
        {
            public string Name;
            public int Level;      // -1 = no level (menu)
            public bool WinScreen; // force the win panel instead of the world
            /// Which UI screen to raise over the world, if any. These are
            /// the UI agent's screens: the SAME pipeline that guards the art
            /// guards the layout, so a moved button or a broken grid shows
            /// up as a diff instead of waiting for someone to open the
            /// screen and notice.
            public UiScreen Ui;
        }

        enum UiScreen
        {
            None,
            Settings, Pause, Atlas, Briefing, GameOver, Complete
        }

        static readonly Scene[] Scenes =
        {
            // --- world/art scenes ---
            new Scene { Name = "menu", Level = -1 },
            new Scene { Name = "day-first-steps", Level = 0 },
            new Scene { Name = "undercloud", Level = 7 },
            new Scene { Name = "sunset-two-suns", Level = 9 },
            new Scene { Name = "wind-far-isles", Level = 12 },
            new Scene { Name = "winter", Level = 31 },
            new Scene { Name = "aurora-festival", Level = 33 },
            new Scene { Name = "bells", Level = 22 },
            new Scene { Name = "win-screen", Level = 0, WinScreen = true },
            // --- UI screens (the layout half: a moved button is a diff) ---
            new Scene { Name = "ui-settings", Level = 0, Ui = UiScreen.Settings },
            new Scene { Name = "ui-pause", Level = 0, Ui = UiScreen.Pause },
            new Scene { Name = "ui-atlas", Level = -1, Ui = UiScreen.Atlas },
            new Scene { Name = "ui-briefing", Level = 0, Ui = UiScreen.Briefing },
            new Scene { Name = "ui-gameover", Level = 0, Ui = UiScreen.GameOver },
            new Scene { Name = "ui-complete", Level = 0, Ui = UiScreen.Complete },
        };

        // Thresholds. Mean channel delta is on a 0..1 scale; the changed
        // fraction is a share of pixels. Tuned loose enough that a
        // harmless re-render (AA jitter, particle phase) passes, tight
        // enough that a palette change or a moved object fails.
        const float MeanDeltaLimit = 0.020f;   // ~5/255 average
        const float ChangedLimit = 0.080f;     // ~8% of pixels

        static Runner runner;
        static int phase;
        static int waitFrames;
        static bool captureMode;
        static readonly List<float> means = new List<float>();
        static readonly List<float> changed = new List<float>();

        [MenuItem("GemRush/Visual Baseline/Capture")]
        public static void Capture() { Begin(true); }

        [MenuItem("GemRush/Visual Baseline/Compare")]
        public static void Compare() { Begin(false); }

        static void Begin(bool capture)
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[VisualBaseline] play mode only.");
                return;
            }
            captureMode = capture;
            Report = "";
            phase = 0;
            waitFrames = 0;
            means.Clear();
            changed.Clear();
            Directory.CreateDirectory(Root);
            Application.runInBackground = true;
            var go = new GameObject("VisualBaselineRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[VisualBaseline] " + (capture ? "capture" : "compare") +
                " started; folder=" + Root);
        }

        class Runner : MonoBehaviour
        {
            float age;
            void Update()
            {
                // A survivor must never haunt a later session (the lesson
                // from PhotoProbe): retire if the run overruns or the world
                // is torn down under us.
                age += Time.unscaledDeltaTime;
                if (age > 180f || GemRush.GameManager.Instance == null)
                {
                    Debug.Log("[VisualBaseline] runner retired (age " +
                        (int)age + "s).");
                    Destroy(gameObject);
                }
                VisualBaselineProbe.Step();
            }
        }

        static void Finish()
        {
            if (runner != null)
            {
                runner.gameObject.SetActive(false);
                Object.Destroy(runner.gameObject);
                runner = null;
            }
            Debug.Log("[VisualBaseline] report:\n" + Report);
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            int sceneIndex = phase / 2;
            if (sceneIndex >= Scenes.Length)
            {
                Finish();
                return;
            }
            Scene scene = Scenes[sceneIndex];
            bool isSetup = (phase % 2) == 0;

            if (isSetup)
            {
                SetupScene(scene);
                // A settle window: level build, camera snap, particle ramp.
                // UI panels animate in over 0.2s, so they need a full second
                // to reach their rest pose or the diff would chase the fade.
                frames(scene.Ui != UiScreen.None ? 90
                    : scene.WinScreen ? 90 : 45);
                phase++;
                return;
            }

            // Capture the frame as the player would see it. (An earlier
            // comment here claimed the HUD was hidden for world scenes to
            // keep the ticking clock out of the diff — it never was, and the
            // HUD is part of what should be guarded anyway.)
            CaptureScene(scene);
            phase++;
        }

        static void SetupScene(Scene scene)
        {
            GameManager gm = GemRush.GameManager.Instance;
            UIManager ui = GemRush.UIManager.Instance;
            if (scene.Level < 0)
            {
                gm.GoToMenu();
            }
            else
            {
                gm.PlayLevel(scene.Level);
                if (scene.WinScreen)
                {
                    // Force the win panel without playing the level through.
                    ui.ShowWin(scene.Level, 2, 42.5f, 55f, false, 7,
                        LevelLibrary.Levels[scene.Level].GemCount);
                }
            }

            // Raise a UI screen over whatever world is up. These use the
            // manager's own entry points, so the capture is of the screen a
            // player actually sees, laid out by the real code path.
            switch (scene.Ui)
            {
                case UiScreen.Settings:
                    Invoke(ui, "ShowSettings");
                    break;
                case UiScreen.Pause:
                    gm.PauseGame();
                    break;
                case UiScreen.Atlas:
                    ui.ShowAtlas();
                    break;
                case UiScreen.Briefing:
                    // The briefing band is opt-in; force it on for the shot
                    // so the band itself is guarded, then restore.
                    bool wasOn = SaveSystem.MissionTextOn;
                    SaveSystem.MissionTextOn = true;
                    ui.ShowLevelIntro(scene.Level < 0 ? 0 : scene.Level);
                    SaveSystem.MissionTextOn = wasOn;
                    break;
                case UiScreen.GameOver:
                    ui.ShowGameOver();
                    break;
                case UiScreen.Complete:
                    // ShowComplete takes stars, max stars, medals AND
                    // goldens. This probe only captures the screen's layout,
                    // so zeros for the cumulative totals are correct — but
                    // all four arguments are required. A stale 3-argument
                    // call here broke the whole EDITOR compile, and because
                    // Editor scripts are outside the offline stub check,
                    // nothing caught it until a build reported the confusing
                    // "Error building Player because scripts are compiling".
                    ui.ShowComplete(0, LevelLibrary.Levels.Length * 3, 0, 0);
                    break;
            }
        }

        static void Invoke(object target, string method)
        {
            System.Reflection.MethodInfo info = target.GetType().GetMethod(method,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (info != null) info.Invoke(target, null);
        }

        static void CaptureScene(Scene scene)
        {
            string path = Path.Combine(Root, scene.Name + ".png");
            byte[] png = EncodeFrame();

            if (captureMode)
            {
                File.WriteAllBytes(path, png);
                Report += "WROTE " + scene.Name + "\n";
                return;
            }

            if (!File.Exists(path))
            {
                Report += "FAIL " + scene.Name + "  (no baseline at " + path + ")\n";
                return;
            }
            byte[] baseline = File.ReadAllBytes(path);
            float mean; float changedFrac;
            Compare(baseline, png, out mean, out changedFrac);
            means.Add(mean);
            changed.Add(changedFrac);

            bool ok = mean <= MeanDeltaLimit && changedFrac <= ChangedLimit;
            Report += (ok ? "PASS " : "FAIL ") + scene.Name +
                "  mean=" + mean.ToString("F4") +
                "  changed=" + (changedFrac * 100f).ToString("F1") + "%" + "\n";
        }

        /// Grabs the current frame including Screen Space Overlay UI.
        static byte[] EncodeFrame()
        {
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] png = shot.EncodeToPNG();
            Object.Destroy(shot);
            return png;
        }

        /// Decodes both PNGs and measures how far apart they are. Uses
        /// per-pixel channel distance so a moved object (large local
        /// change) scores high while a whole-image tint shift (small
        /// everywhere) still registers via the mean.
        static void Compare(byte[] a, byte[] b, out float mean, out float changedFrac)
        {
            Texture2D ta = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D tb = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ta.LoadImage(a);
            tb.LoadImage(b);

            // Resolution change (a different window size) makes a pixel
            // diff meaningless: report a hard failure rather than a
            // misleading number.
            if (ta.width != tb.width || ta.height != tb.height)
            {
                mean = 1f;
                changedFrac = 1f;
                Object.Destroy(ta);
                Object.Destroy(tb);
                return;
            }

            Color32[] pa = ta.GetPixels32();
            Color32[] pb = tb.GetPixels32();
            double total = 0;
            int changedCount = 0;
            const int PerPixelChangedThreshold = 24; // ~9% of a channel

            for (int i = 0; i < pa.Length; i++)
            {
                int dr = Mathf.Abs(pa[i].r - pb[i].r);
                int dg = Mathf.Abs(pa[i].g - pb[i].g);
                int db = Mathf.Abs(pa[i].b - pb[i].b);
                int d = dr + dg + db; // max 765
                total += d / 765.0;
                if (dr + dg + db > PerPixelChangedThreshold * 3) changedCount++;
            }

            mean = (float)(total / pa.Length);
            changedFrac = changedCount / (float)pa.Length;
            Object.Destroy(ta);
            Object.Destroy(tb);
        }

        static void frames(int n) { waitFrames = n; }

        /// Directory holding the baselines, for docs and tooling.
        public static string BaselineFolder { get { return Root; } }
    }
}
