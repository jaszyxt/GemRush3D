using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Visual-budget recorder. Walks each realm and reports the numbers
    /// that decide whether a phone can hold the frame rate: particle
    /// counts actually live, renderers, draw-call-relevant object counts,
    /// and FrameTimingManager's CPU/GPU frame times.
    ///
    /// Why: my judgement about "is this too heavy for a mid-range Android"
    /// is inference, not measurement. Recording the numbers per realm
    /// replaces the guess with a trend — the value is watching a number
    /// move between releases, not any single reading.
    ///
    /// It deliberately does NOT assert a budget. A wrong budget is worse
    /// than no budget (it either blocks legitimate work or lulls you), and
    /// frame time on a desktop editor says little about a phone. The
    /// report is for a human comparing runs.
    ///
    /// Menu: GemRush/Visual Budget/Record — play mode only.
    /// </summary>
    public static class BudgetProbe
    {
        public static string Report { get; private set; } = "not run";

        static Runner runner;
        static int phase;
        static int waitFrames;
        static readonly List<string> rows = new List<string>();

        // Same realm set as the visual baseline, so the two reports line up.
        static readonly int[] Levels = { 0, 7, 9, 12, 22, 31, 33 };

        [MenuItem("GemRush/Visual Budget/Record")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[BudgetProbe] play mode only.");
                return;
            }
            Report = "";
            rows.Clear();
            // Phase -1: force a menu reset first, so the probe always
            // starts from a known state regardless of what the editor
            // was doing when the menu item fired (a mid-level session
            // used to leave the state machine stuck and the runner
            // timed out silently with zero diagnostics).
            phase = -1;
            waitFrames = 0;
            Application.runInBackground = true;
            var go = new GameObject("BudgetProbeRunner");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            runner = go.AddComponent<Runner>();
            Debug.Log("[BudgetProbe] started (resetting to menu first)");
        }

        class Runner : MonoBehaviour
        {
            float age;
            void Update()
            {
                age += Time.unscaledDeltaTime;
                if (age > 240f || GemRush.GameManager.Instance == null)
                {
                    Debug.Log("[BudgetProbe] runner retired.");
                    Destroy(gameObject);
                }
                BudgetProbe.Step();
            }
        }

        static void Step()
        {
            if (!Application.isPlaying || runner == null) return;
            if (waitFrames > 0) { waitFrames--; return; }

            // Phase -1: force a clean menu state before the scene sequence.
            if (phase == -1)
            {
                GemRush.GameManager.Instance.GoToMenu();
                Debug.Log("[BudgetProbe] phase -1: menu reset done");
                waitFrames = 30;
                phase = 0;
                return;
            }

            int sceneIndex = phase / 2;
            if (sceneIndex >= Levels.Length)
            {
                Finish();
                return;
            }

            if ((phase % 2) == 0)
            {
                Debug.Log("[BudgetProbe] phase " + phase + ": loading level " +
                    Levels[sceneIndex] + " (" +
                    GemRush.LevelLibrary.Levels[Levels[sceneIndex]].Name + ")");
                GemRush.GameManager.Instance.PlayLevel(Levels[sceneIndex]);
                // Settle: level build, audio clip synthesis, particle ramp.
                // An unsettled read measures the loading hitch, not the
                // steady state.
                waitFrames = 90;
                phase++;
                return;
            }

            Measure(Levels[sceneIndex]);
            phase++;
        }

        static void Measure(int level)
        {
            string name = GemRush.LevelLibrary.Levels[level].Name;

            // Live particles across the whole scene: the number that
            // actually costs fill rate on a phone.
            ParticleSystem[] systems =
                Object.FindObjectsOfType<ParticleSystem>();
            int liveParticles = 0;
            int maxParticles = 0;
            foreach (ParticleSystem ps in systems)
            {
                liveParticles += ps.particleCount;
                maxParticles += ps.main.maxParticles;
            }

            int renderers = Object.FindObjectsOfType<MeshRenderer>().Length;

            // FrameTimingManager: CPU/GPU ms for the last frame, in the
            // editor this reflects the editor's own overhead, so treat it
            // as a relative signal between runs rather than a device truth.
            FrameTiming timing = new FrameTiming();
            uint count = FrameTimingManager.GetLatestTimings(1, new[] { timing });
            float cpuMs = count > 0 ? (float)timing.cpuFrameTime : -1f;
            float gpuMs = count > 0 ? (float)timing.gpuFrameTime : -1f;

            rows.Add(name.PadRight(24) +
                " particles=" + liveParticles.ToString().PadLeft(4) +
                "/" + maxParticles.ToString().PadLeft(4) +
                "  emitters=" + systems.Length.ToString().PadLeft(3) +
                "  renderers=" + renderers.ToString().PadLeft(4) +
                "  cpu=" + cpuMs.ToString("F1") + "ms" +
                "  gpu=" + gpuMs.ToString("F1") + "ms");
        }

        static void Finish()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Visual budget per realm ===");
            foreach (string r in rows) sb.AppendLine(r);
            sb.AppendLine("(editor numbers: compare runs, do not read as device truth)");
            Report = sb.ToString();
            if (runner != null)
            {
                runner.gameObject.SetActive(false);
                Object.Destroy(runner.gameObject);
                runner = null;
            }
            Debug.Log("[BudgetProbe] report:\n" + Report);
        }

        static void frames(int n) { waitFrames = n; }
    }
}
