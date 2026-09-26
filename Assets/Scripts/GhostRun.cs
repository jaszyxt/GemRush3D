using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Ghost runs (D30 mastery, retention plan): your best-time run,
    /// replayed as a translucent Pip you race. The recorder samples the
    /// player's position ~10x a second while a level is being played;
    /// the best recording is stored per level (base64 in PlayerPrefs) and
    /// replayed as a glowing shell on later runs. Recording only happens
    /// on a run that BEATS the old best, so the ghost is always the run to
    /// beat. Pure kinematics: the ghost never touches physics, so it can
    /// never affect the race.
    public static class GhostStore
    {
        /// 10 Hz, shared with GhostRecorder and GhostRunner so the sample
        /// rate lives in exactly one place — it used to be a second
        /// hardcoded 0.1f in each, and a drifted copy would silently
        /// mis-time every replay.
        public const float SampleInterval = 0.1f;
        const int MaxSamples = 9000;         // 15 minutes of level

        /// Encode: seconds-since-start -> position, packed compactly.
        public static string Encode(List<Vector3> samples)
        {
            if (samples == null || samples.Count == 0) return "";
            // Header: sample count. Body: (dx, dy, dz) per sample at 10 Hz.
            // Deltas quantized to 5 mm; typical levels fit in a few KB.
            var bytes = new List<byte>(samples.Count * 6 + 4);
            int n = Math.Min(samples.Count, MaxSamples);
            bytes.AddRange(BitConverter.GetBytes(n));
            Vector3 previous = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 delta = samples[i] - previous;
                short dx = (short)Mathf.Clamp(delta.x * 200f, short.MinValue, short.MaxValue);
                short dy = (short)Mathf.Clamp(delta.y * 200f, short.MinValue, short.MaxValue);
                short dz = (short)Mathf.Clamp(delta.z * 200f, short.MinValue, short.MaxValue);
                bytes.AddRange(BitConverter.GetBytes(dx));
                bytes.AddRange(BitConverter.GetBytes(dy));
                bytes.AddRange(BitConverter.GetBytes(dz));
                previous = samples[i];
            }
            return Convert.ToBase64String(bytes.ToArray());
        }

        /// Decode: returns the sample list, or null on any problem (a
        /// corrupt or foreign save must never break a level).
        public static List<Vector3> Decode(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;
            try
            {
                byte[] bytes = Convert.FromBase64String(data);
                if (bytes.Length < 4) return null;
                int n = BitConverter.ToInt32(bytes, 0);
                if (n <= 0 || n > MaxSamples || bytes.Length < 4 + n * 6)
                    return null;
                var samples = new List<Vector3>(n);
                Vector3 position = Vector3.zero;
                int offset = 4;
                for (int i = 0; i < n; i++)
                {
                    float dx = BitConverter.ToInt16(bytes, offset) / 200f;
                    float dy = BitConverter.ToInt16(bytes, offset + 2) / 200f;
                    float dz = BitConverter.ToInt16(bytes, offset + 4) / 200f;
                    position += new Vector3(dx, dy, dz);
                    samples.Add(position);
                    offset += 6;
                }
                return samples;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// The player-prefs key for a level's best recording.
        public static string SaveKey(string levelName)
        {
            return "ghost_" + SaveKeySafe(levelName);
        }

        static string SaveKeySafe(string levelName)
        {
            char[] chars = levelName.ToLower().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
            return new string(chars);
        }

        public static string Load(string levelName)
        {
            return PlayerPrefs.GetString(SaveKey(levelName), "");
        }

        /// Stores a recording (replacing any previous best).
        public static void Save(string levelName, string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            PlayerPrefs.SetString(SaveKey(levelName), data);
            PlayerPrefs.Save();
            CloudSaveMirror.Snapshot();
        }
    }

    /// Records the player's position while a level is live. Created by
    /// GameManager when a level starts, kept if the run sets a new best.
    public class GhostRecorder : MonoBehaviour
    {
        public bool Recording { get; private set; }
        readonly List<Vector3> samples = new List<Vector3>(512);
        float nextSample;

        public static GhostRecorder Create(Transform parent)
        {
            var go = new GameObject("GhostRecorder");
            go.transform.SetParent(parent, false);
            return go.AddComponent<GhostRecorder>();
        }

        public void Begin()
        {
            samples.Clear();
            nextSample = 0f;
            Recording = true;
        }

        public void Stop()
        {
            Recording = false;
        }

        /// The captured path (or null if nothing meaningful was recorded).
        public List<Vector3> Finish()
        {
            Recording = false;
            return samples.Count > 4 ? new List<Vector3>(samples) : null;
        }

        void Update()
        {
            if (!Recording) return;
            var player = GameBootstrap.Player;
            if (player == null) return;
            var state = GameManager.Instance != null
                ? GameManager.Instance.State : GameState.Menu;
            if (state != GameState.Playing)
            {
                // Death/respawn or pause interrupts the run, but it must
                // not END it: a run with a death still has a route worth
                // showing. Skip sampling while the level is not live and
                // pick straight back up when play resumes — the ghost then
                // shows the real route, teleports and all, instead of
                // stopping dead at the first death.
                nextSample = 0f;
                return;
            }
            nextSample -= Time.deltaTime;
            if (nextSample <= 0f)
            {
                nextSample = GhostStore.SampleInterval;
                samples.Add(player.transform.position);
            }
        }
    }

    /// The translucent Pip shell replaying a best run. Pure kinematics:
    /// samples are interpolated on the unscaled clock so the ghost keeps
    /// its line even through a pause, and it never touches physics.
    public class GhostRunner : MonoBehaviour
    {
        List<Vector3> samples;
        float elapsed;

        public static GhostRunner Create(Transform parent, List<Vector3> samples)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.name = "Ghost";
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<MeshRenderer>();
            var mat = ArtLib.Solid(ArtLib.Air, 0f);
            ArtLib.SetFade(mat, 0f); // start invisible, fade in below
            renderer.sharedMaterial = mat;

            // Gentle materialization: the ghost fades in over 0.8s
            // rather than popping into existence at full opacity.
            Tweener.Value(0f, 0.30f, 0.8f, delegate (float k)
            {
                mat.color = new Color(mat.color.r, mat.color.g,
                    mat.color.b, k);
            });

            // Eyes so it reads as Pip, not a capsule.
            var eyeWhite = ArtLib.Solid(new Color(0.97f, 0.97f, 1f), 0f);
            var pupil = ArtLib.Solid(new Color(0.1f, 0.1f, 0.12f), 0f);
            foreach (float x in new[] { -0.16f, 0.16f })
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                UnityEngine.Object.Destroy(eye.GetComponent<Collider>());
                eye.transform.SetParent(go.transform, false);
                eye.transform.localPosition = new Vector3(x, 0.3f, 0.4f);
                eye.transform.localScale = new Vector3(0.15f, 0.17f, 0.1f);
                eye.GetComponent<MeshRenderer>().sharedMaterial = eyeWhite;
                GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                UnityEngine.Object.Destroy(dot.GetComponent<Collider>());
                dot.transform.SetParent(eye.transform, false);
                dot.transform.localPosition = new Vector3(0f, 0f, 0.45f);
                dot.transform.localScale = new Vector3(0.55f, 0.6f, 0.5f);
                dot.GetComponent<MeshRenderer>().sharedMaterial = pupil;
            }

            var ghost = go.AddComponent<GhostRunner>();
            ghost.samples = samples;
            go.transform.position = samples[0];
            return ghost;
        }

        void Update()
        {
            if (samples == null) return;
            var state = GameManager.Instance != null
                ? GameManager.Instance.State : GameState.Menu;
            if (state == GameState.Playing)
                elapsed += Time.deltaTime;

            // Interpolate along the samples: sample i happens at
            // i * SampleInterval. Past the end, the ghost waits at the
            // goal. The recorder and this clock both advance only while
            // the level is live, so a death's downtime is excluded from
            // the route on both sides and the line stays continuous.
            float t = elapsed / GhostStore.SampleInterval;
            int i = Mathf.Min((int)t, samples.Count - 1);
            int j = Mathf.Min(i + 1, samples.Count - 1);
            float f = Mathf.Clamp01(t - i);
            transform.position = Vector3.Lerp(samples[i], samples[j], f);
        }
    }
}
