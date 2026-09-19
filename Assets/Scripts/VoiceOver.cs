using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Stable, shared voice-line IDs. The editor exporter and the runtime
    /// both build IDs through this class, so a clip can never drift away
    /// from the text it was generated for. The sanitizer matches
    /// SaveSystem.Key() so level names map to the same tokens everywhere.
    public static class VoiceIds
    {
        public const string Narrator = "narrator";

        /// Lowercase; every non-alphanumeric becomes '_' — same rule as
        /// SaveSystem.Key(), applied to level names.
        public static string Key(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            char[] chars = name.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
            return new string(chars);
        }

        public static string Mission(string levelName)
        {
            return "mission_" + Key(levelName);
        }

        public static string Win(string levelName)
        {
            return "win_" + Key(levelName);
        }

        public static string Milestone(string levelName)
        {
            return "milestone_" + Key(levelName);
        }

        public static string Beat(string levelName, int checkpointIndex)
        {
            return "beat_" + Key(levelName) + "_" + checkpointIndex;
        }

        public static string Epilogue(int page)
        {
            return "epilogue_" + page;
        }

        public static string Quote(int index)
        {
            return "quote_" + index;
        }
    }

    /// The voice-over channel. Reads Assets/Resources/Voice/manifest.json
    /// (written by GemRush > Voice > Export Voice Lines; clips generated
    /// offline by tools/voice) and plays one line at a time, ducking the
    /// score underneath.
    ///
    /// The never-wrong-text guarantee: callers pass the exact on-screen
    /// text along with the ID. If the manifest's hash of the line does not
    /// match, the clip is stale (text edited since generation) and stays
    /// silent — voice can never contradict what the player is reading.
    public class VoiceOver : MonoBehaviour
    {
        public static VoiceOver Instance { get; private set; }

        AudioSource source;

        /// Play level of the narration channel; calibrated in the mix pass
        /// (docs/Audio-Design.md #9) and locked by AudioAuditTests.
        public const float VoiceVolume = 0.62f;

        [System.Serializable]
        public class VoiceEntry
        {
            public string id;
            public string cast;
            public string hash;
            public string file;      // Resources path without extension
            public float duration;   // seconds; 0 = not generated yet
            public string[] aliases; // other IDs sharing this clip
            public string text;      // the spoken line (consumed by generate.py)
        }

        [System.Serializable]
        public class VoiceManifest
        {
            public int version = 1;
            public List<VoiceEntry> entries = new List<VoiceEntry>();
        }

        static VoiceManifest manifest;
        static Dictionary<string, VoiceEntry> lookup;

        void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.priority = 0; // voice outranks everything
            // Calibrated to ~-20 LUFS at play level: present in the world,
            // not shouting over it (mobile norm ~-18; narration sits lower).
            source.volume = VoiceVolume;
        }

        static void EnsureManifest()
        {
            if (manifest != null) return;
            TextAsset asset = Resources.Load<TextAsset>("Voice/manifest");
            if (asset == null) return;
            manifest = JsonUtility.FromJson<VoiceManifest>(asset.text);
            lookup = new Dictionary<string, VoiceEntry>();
            if (manifest == null || manifest.entries == null) return;
            for (int i = 0; i < manifest.entries.Count; i++)
            {
                VoiceEntry e = manifest.entries[i];
                if (string.IsNullOrEmpty(e.id)) continue;
                lookup[e.id] = e;
                if (e.aliases != null)
                    for (int a = 0; a < e.aliases.Length; a++)
                        lookup[e.aliases[a]] = e;
            }
        }

        /// FNV-1a, 32-bit, over the UTF-8 bytes — identical to the hash
        /// the exporter and the Python generator compute.
        public static string Hash(string text)
        {
            const uint prime = 16777619u;
            const uint offset = 2166136261u;
            uint h = offset;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text ?? "");
            for (int i = 0; i < bytes.Length; i++)
            {
                h ^= bytes[i];
                h *= prime;
            }
            return h.ToString("x8");
        }

        public bool IsPlaying
        {
            get { return source != null && source.isPlaying; }
        }

        /// Play the line `id`, but only if `text` still matches what the
        /// clip was generated from. Interrupts whatever is playing.
        public void Play(string id, string text)
        {
            if (!SaveSystem.VoiceOn || !SaveSystem.SoundOn) return;
            if (GameManager.Instance != null &&
                GameManager.Instance.State == GameState.Paused) return;
            EnsureManifest();
            if (lookup == null || string.IsNullOrEmpty(id)) return;
            VoiceEntry entry;
            if (!lookup.TryGetValue(id, out entry)) return;
            if (entry.duration <= 0f) return; // not generated yet
            if (entry.hash != Hash(text ?? "")) return; // stale — stay silent

            AudioClip clip = Resources.Load<AudioClip>("Voice/" + entry.file);
            if (clip == null) return;

            // One voice at a time: the newest line wins. The score ducks
            // for the line plus a breath on either side.
            source.Stop();
            source.clip = clip;
            source.Play();
            if (AudioManager.Instance != null)
                AudioManager.Instance.DuckFor(entry.duration + 1.2f, 0.3f);
        }

        public void Stop()
        {
            if (source != null && source.isPlaying) source.Stop();
        }

        void Update()
        {
            // The sound setting is polled a few times a second elsewhere;
            // here an instant mute is kinder — a flipped toggle should
            // silence the current line immediately.
            if (source != null && source.isPlaying &&
                (!SaveSystem.SoundOn || !SaveSystem.VoiceOn))
                source.Stop();
        }
    }
}
