using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GemRush
{
    /// Steam Cloud save mirror (docs/Steam-Deploy.md phase 2): Steam syncs
    /// files, PlayerPrefs lives in the registry — so every save also writes
    /// a typed JSON snapshot to persistentDataPath, and boot restores it
    /// when the file is NEWER (monotonic sequence counter, never wall
    /// clocks). Inert until Steam Auto-Cloud is enabled on the path; a
    /// harmless local JSON until then. Editor builds are excluded so the
    /// probe rigs (which manage PlayerPrefs directly) never fight it.
    public static class CloudSaveMirror
    {
        const string FileName = "gemrush_cloud.json";
        const string SeqKey = "gemrush_v2_cloudseq";

        static string PathFor()
        {
            return System.IO.Path.Combine(
                Application.persistentDataPath, FileName);
        }

        /// Every key the game owns, as (key, type) pairs. PlayerPrefs has
        /// no enumeration API — this manifest IS the surface; a new key
        /// family must register here or it will not sync.
        static IEnumerable<KeyValuePair<string, char>> Manifest()
        {
            // Scalars.
            yield return Pair("gemrush_v2_unlockedCount", 'i');
            yield return Pair("gemrush_v2_gifts", 'i');
            yield return Pair("gemrush_v2_giftdate", 's');
            yield return Pair("gemrush_v2_sound", 'i');
            yield return Pair("gemrush_v2_voice", 'i');
            yield return Pair("gemrush_v2_haptics", 'i');
            yield return Pair("gemrush_v2_shadows", 'i');
            yield return Pair("gemrush_v2_shake", 'i');
            yield return Pair("gemrush_v2_lefty", 'i');
            yield return Pair("gemrush_v2_fullscreen", 'i');
            yield return Pair("gemrush_v2_textlarge", 'i');
            yield return Pair("gemrush_v2_aurora", 'i');
            yield return Pair("gemrush_v2_firstflight", 's');
            yield return Pair("gemrush_v2_cloudseq", 'i');
            yield return Pair("shelf_seen", 'i');
            // Per-level families (level name keys via SaveSystem's rules).
            for (int i = 0; i < LevelLibrary.Levels.Length; i++)
            {
                string key = SaveSystem.KeyForLevel(i);
                yield return Pair("gemrush_v2_" + key + "_best", 'f');
                yield return Pair("gemrush_v2_" + key + "_stars", 'i');
                yield return Pair("gemrush_v2_" + key + "_golden", 'i');
                yield return Pair("ghost_" + key, 's');
            }
        }

        static KeyValuePair<string, char> Pair(string key, char type)
        {
            return new KeyValuePair<string, char>(key, type);
        }

        // ------------------------------------------------------------------
        // Snapshot: registry -> JSON (called after every save)
        // ------------------------------------------------------------------

        public static void Snapshot()
        {
            #if UNITY_EDITOR
            return; // probes manage PlayerPrefs directly; never fight them
            #else
            WriteSnapshotFile();
            #endif
        }

        /// The snapshot core, unguarded so the MirrorProbe can verify the
        /// serialization in the editor (the public wrapper stays inert
        /// there so runtime probes never fight the file).
        internal static void WriteSnapshotFile()
        {
            try
            {
                long seq = PlayerPrefs.GetInt(SeqKey, 0) + 1;
                PlayerPrefs.SetInt(SeqKey, (int)seq);

                using (var writer = new StringWriter())
                {
                    writer.Write("{");
                    writer.Write("\"schema\":1,");
                    writer.Write("\"seq\":" + seq + ",");
                    writer.Write("\"device\":\"" +
                        SystemInfo.deviceName.Replace("\"", "") + "\",");
                    writer.Write("\"timeUtc\":\"" +
                        DateTime.UtcNow.ToString("o") + "\",");
                    writer.Write("\"entries\":{");
                    bool first = true;
                    foreach (var entry in Manifest())
                    {
                        if (!PlayerPrefs.HasKey(entry.Key)) continue;
                        if (!first) writer.Write(",");
                        first = false;
                        writer.Write("\"" + entry.Key + "\":{");
                        if (entry.Value == 'i')
                            writer.Write("\"t\":\"i\",\"v\":\"" +
                                PlayerPrefs.GetInt(entry.Key) + "\"");
                        else if (entry.Value == 'f')
                            writer.Write("\"t\":\"f\",\"v\":\"" +
                                PlayerPrefs.GetFloat(entry.Key)
                                    .ToString("R",
                                        System.Globalization.CultureInfo
                                            .InvariantCulture) + "\"");
                        else
                            writer.Write("\"t\":\"s\",\"v\":\"" +
                                PlayerPrefs.GetString(entry.Key)
                                    .Replace("\\", "\\\\")
                                    .Replace("\"", "\\\"") + "\"");
                        writer.Write("}");
                    }
                    writer.Write("}}");

                    WriteAtomic(PathFor(), writer.ToString());
                }
                PlayerPrefs.Save();
            }
            catch (Exception)
            {
                // The mirror must never break the game; a failed snapshot
                // just means this device uploads its previous file.
            }
        }

        static void WriteAtomic(string path, string contents)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, contents);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        // ------------------------------------------------------------------
        // Restore: JSON -> registry, only when the file is newer (boot,
        // before the first PlayerPrefs read)
        // ------------------------------------------------------------------

        public static void Restore()
        {
            #if UNITY_EDITOR
            return;
            #else
            try
            {
                string path = PathFor();
                if (!File.Exists(path)) return;
                string json = File.ReadAllText(path);
                long fileSeq = ReadSeq(json);
                long localSeq = PlayerPrefs.GetInt(SeqKey, 0);
                if (fileSeq <= localSeq)
                {
                    // Local is the same or newer: registry stays the source
                    // of truth; refresh the file so the cloud uploads it.
                    if (fileSeq < localSeq) Snapshot();
                    return;
                }

                // The file is newer (another machine's sync): apply every
                // typed entry — the type tag is load-bearing, a float
                // written as an int would silently wipe best times.
                ApplyEntries(json);
                PlayerPrefs.Save();
            }
            catch (Exception)
            {
                // A torn or foreign file is ignored; the registry rules.
            }
            #endif
        }

        static long ReadSeq(string json)
        {
            int i = json.IndexOf("\"seq\":");
            if (i < 0) return -1;
            int start = i + 6;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end])))
                end++;
            long value;
            long.TryParse(json.Substring(start, end - start), out value);
            return value;
        }

        static void ApplyEntries(string json)
        {
            // Minimal reader for the exact format Snapshot writes; avoids
            // a JSON dependency in the zero-asset project.
            int i = json.IndexOf("\"entries\":");
            if (i < 0) return;
            int cursor = i + 10;
            while (cursor < json.Length)
            {
                int keyStart = json.IndexOf("\"", cursor);
                if (keyStart < 0) return;
                int keyEnd = json.IndexOf("\"", keyStart + 1);
                if (keyEnd < 0) return;
                string key = json.Substring(keyStart + 1,
                    keyEnd - keyStart - 1);

                int typePos = json.IndexOf("\"t\":\"", keyEnd);
                int valuePos = json.IndexOf("\"v\":\"", typePos);
                if (typePos < 0 || valuePos < 0) return;
                char type = json[typePos + 5];
                int valueStart = valuePos + 5;
                // Values may contain ESCAPED quotes: skip backslash
                // pairs so an escaped quote does not end the value early.
                int valueEnd = valueStart;
                bool escaped = false;
                while (valueEnd < json.Length)
                {
                    char c = json[valueEnd];
                    if (escaped) { escaped = false; valueEnd++; continue; }
                    if (c == '\\') { escaped = true; valueEnd++; continue; }
                    if (c == '"') break;
                    valueEnd++;
                }
                if (valueEnd >= json.Length) return;
                string value = Unescape(json.Substring(valueStart,
                    valueEnd - valueStart));

                if (type == 'i')
                {
                    int parsed;
                    if (int.TryParse(value, out parsed))
                        PlayerPrefs.SetInt(key, parsed);
                }
                else if (type == 'f')
                {
                    float parsed;
                    if (float.TryParse(value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo
                                .InvariantCulture, out parsed))
                        PlayerPrefs.SetFloat(key, parsed);
                }
                else
                {
                    PlayerPrefs.SetString(key, value);
                }

                // Advance past this entry's closing brace.
                cursor = json.IndexOf("}", valueEnd);
                if (cursor < 0) return;
                cursor++;
            }
        }

        static string Unescape(string s)
        {
            // JSON unescape order matters: a literal backslash pair must
            // survive the escaped-quote pass, so park it on a placeholder
            // first, then restore it.
            const string pairPlaceholder = "\x0001";
            return s.Replace("\\\\", pairPlaceholder)
                .Replace("\\\"", "\"")
                .Replace(pairPlaceholder, "\\");
        }
    }
}
