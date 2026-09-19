using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// Exports every voice-over line in the game to
    /// Assets/Resources/Voice/manifest.json — the contract between the
    /// in-code narrative text and the offline generator (tools/voice).
    ///
    /// One entry per distinct (cast, text) pair: levels that share text
    /// (the B-Sides remix their originals' prose) share one clip through
    /// aliases. Durations and file paths survive re-export, so running
    /// this never invalidates already-generated audio.
    ///
    /// The manifest's text hash is the heart of the pipeline: the runtime
    /// refuses to voice a line whose on-screen text no longer matches the
    /// hash the clip was generated from.
    public static class VoiceLinesExporter
    {
        public const string ManifestPath = "Assets/Resources/Voice/manifest.json";

        [MenuItem("GemRush/Voice/Export Voice Lines")]
        public static void ExportMenu()
        {
            ExportAll();
        }

        /// Headless-friendly entry: -executeMethod GemRush.EditorTools.VoiceLinesExporter.ExportAll
        public static void ExportAll()
        {
            List<VoiceOver.VoiceEntry> fresh = CollectLines();

            // Preserve file/duration from the previous manifest: text the
            // generator has already recorded keeps its clip mapping.
            VoiceOver.VoiceManifest existing = ReadManifest();
            Dictionary<string, VoiceOver.VoiceEntry> prior =
                new Dictionary<string, VoiceOver.VoiceEntry>();
            if (existing != null && existing.entries != null)
                foreach (VoiceOver.VoiceEntry e in existing.entries)
                    if (!string.IsNullOrEmpty(e.hash))
                        prior[e.cast + "/" + e.hash] = e;

            VoiceOver.VoiceManifest manifest = new VoiceOver.VoiceManifest();
            Dictionary<string, VoiceOver.VoiceEntry> byKey =
                new Dictionary<string, VoiceOver.VoiceEntry>();
            int aliases = 0;
            foreach (VoiceOver.VoiceEntry line in fresh)
            {
                string key = line.cast + "/" + line.hash;
                VoiceOver.VoiceEntry entry;
                if (!byKey.TryGetValue(key, out entry))
                {
                    entry = line;
                    VoiceOver.VoiceEntry old;
                    if (prior.TryGetValue(key, out old))
                    {
                        entry.file = old.file;
                        entry.duration = old.duration;
                        entry.text = old.text; // survive re-export
                    }
                    manifest.entries.Add(entry);
                    byKey[key] = entry;
                }
                if (byKey[key].id != line.id)
                {
                    // Same words, another ID (remix, duplicate beat): alias.
                    List<string> all = new List<string>(
                        byKey[key].aliases ?? new string[0]);
                    if (!all.Contains(line.id))
                    {
                        all.Add(line.id);
                        aliases++;
                    }
                    byKey[key].aliases = all.ToArray();
                }
            }

            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Voice");
            File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, true));
            AssetDatabase.Refresh();

            int clips = manifest.entries.Count;
            int lines = fresh.Count;
            float words = 0;
            foreach (VoiceOver.VoiceEntry e in fresh)
                words += e.text.Split(' ').Length;
            Debug.Log("[Voice] Exported " + lines + " lines as " + clips +
                " clips (" + aliases + " alias IDs, ~" + (int)words +
                " words). Run tools/voice/generate.py to produce audio for " +
                MissingCount(manifest) + " missing clip(s).");
        }

        static int MissingCount(VoiceOver.VoiceManifest manifest)
        {
            int n = 0;
            foreach (VoiceOver.VoiceEntry e in manifest.entries)
                if (e.duration <= 0f) n++;
            return n;
        }

        static VoiceOver.VoiceManifest ReadManifest()
        {
            if (!File.Exists(ManifestPath)) return null;
            return JsonUtility.FromJson<VoiceOver.VoiceManifest>(
                File.ReadAllText(ManifestPath));
        }

        /// Every voicable line in the game, in stable play order.
        static List<VoiceOver.VoiceEntry> CollectLines()
        {
            List<VoiceOver.VoiceEntry> lines = new List<VoiceOver.VoiceEntry>();

            foreach (LevelDefinition level in LevelLibrary.Levels)
            {
                Add(lines, VoiceIds.Narrator, VoiceIds.Mission(level.Name),
                    level.Mission);
                Add(lines, VoiceIds.Narrator, VoiceIds.Win(level.Name),
                    level.WinLine);
                Add(lines, VoiceIds.Narrator, VoiceIds.Milestone(level.Name),
                    level.Milestone);
                for (int i = 0; i < level.StoryBeats.Count; i++)
                    Add(lines, VoiceIds.Narrator,
                        VoiceIds.Beat(level.Name, i), level.StoryBeats[i]);
            }

            for (int i = 0; i < Story.Epilogue.Length; i++)
                Add(lines, VoiceIds.Narrator, VoiceIds.Epilogue(i),
                    Story.Epilogue[i]);
            for (int i = 0; i < Story.MenuQuotes.Length; i++)
                Add(lines, VoiceIds.Gloomfang, VoiceIds.Quote(i),
                    Story.MenuQuotes[i]);

            return lines;
        }

        static void Add(List<VoiceOver.VoiceEntry> lines, string cast,
            string id, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            lines.Add(new VoiceOver.VoiceEntry
            {
                id = id,
                cast = cast,
                hash = VoiceOver.Hash(text),
                file = cast + "/" + id,
                duration = 0f,
                aliases = new string[0],
                text = text
            });
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string leaf = Path.GetFileName(path);
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }

    /// Import settings for generated voice clips: mono, compressed in
    /// memory, modest quality — narration is 24 kHz mono speech, and the
    /// default import settings would waste APK bytes on it.
    public class VoiceClipImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Voice/")) return;
            AudioImporter importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.4f;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            importer.defaultSampleSettings = settings;
        }
    }
}
