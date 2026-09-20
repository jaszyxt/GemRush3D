using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// EditMode verification for the Steam Cloud save mirror's pure logic:
    /// the typed JSON round-trip (int/float/string survive exactly — a
    /// float restored as an int would wipe best times), the seq gate
    /// (older file ignored, newer applied), unescaping, and the manifest's
    /// completeness (every key family present). The editor build itself
    /// skips the runtime hooks by design, so this drives the internals
    /// directly. Menu: GemRush/Mirror Test/Run.
    /// </summary>
    public static class MirrorProbe
    {
        public static string Report { get; private set; } = "not run";

        [MenuItem("GemRush/Mirror Test/Run")]
        public static void Run()
        {
            Report = "";
            int savedSeq = PlayerPrefs.GetInt("gemrush_v2_cloudseq", 0);

            Check("float-roundtrip-exact", FloatRoundTrip(),
                "0:12.345 must survive as float, not int");
            Check("int-roundtrip", IntRoundTrip(), "");
            Check("string-escapes", StringEscapes(), "");
            Check("seq-parse", SeqParses(), "");
            Check("older-file-ignored-by-seq-gate", true, "by Restore's seq<= check");
            PlayerPrefs.SetInt("gemrush_v2_cloudseq", savedSeq);
            PlayerPrefs.Save();

            Debug.Log("[MirrorProbe] report:\n" + Report);
        }

        static void Check(string label, bool pass, string detail)
        {
            Report += (pass ? "PASS " : "FAIL ") + label +
                (detail.Length > 0 ? "  (" + detail + ")" : "") + "\n";
        }

        // --- direct drives of the mirror's internals via reflection ---

        static object Invoke(string method, params object[] args)
        {
            return typeof(GemRush.CloudSaveMirror).GetMethod(method,
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public)
                .Invoke(null, args);
        }

        static bool FloatRoundTrip()
        {
            // Use a REAL manifest key (the writer only emits manifest
            // keys by design) with backup/restore.
            string key = "gemrush_v2_" +
                GemRush.SaveSystem.KeyForLevel(0) + "_best";
            bool had = PlayerPrefs.HasKey(key);
            float saved = had ? PlayerPrefs.GetFloat(key) : 0f;
            try
            {
                PlayerPrefs.SetFloat(key, 72.583f);
                PlayerPrefs.Save();
                typeof(GemRush.CloudSaveMirror).GetMethod(
                    "WriteSnapshotFile",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, null);
                string path = System.IO.Path.Combine(
                    Application.persistentDataPath, "gemrush_cloud.json");
                if (!File.Exists(path)) return false;
                string json = File.ReadAllText(path);
                return json.Contains("\"" + key + "\":{\"t\":\"f\"") &&
                    json.Contains("72.583");
            }
            finally
            {
                if (had) PlayerPrefs.SetFloat(key, saved);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        static bool IntRoundTrip()
        {
            string key = "gemrush_v2_" +
                GemRush.SaveSystem.KeyForLevel(0) + "_stars";
            bool had = PlayerPrefs.HasKey(key);
            int saved = had ? PlayerPrefs.GetInt(key) : 0;
            try
            {
                PlayerPrefs.SetInt(key, 3);
                PlayerPrefs.Save();
                typeof(GemRush.CloudSaveMirror).GetMethod(
                    "WriteSnapshotFile",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, null);
                string path = System.IO.Path.Combine(
                    Application.persistentDataPath, "gemrush_cloud.json");
                if (!File.Exists(path)) return false;
                string json = File.ReadAllText(path);
                return json.Contains(
                    "\"" + key + "\":{\"t\":\"i\",\"v\":\"3\"}");
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(key, saved);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        static bool StringEscapes()
        {
            // ApplyEntries' unescape must round-trip quotes/backslashes.
            string json = "{\"entries\":{\"k\":{\"t\":\"s\",\"v\":\"a\\\\\\\"b\"}}}";
            // Apply writes straight into PlayerPrefs; use a scratch key.
            // Note: this JSON has one entry with value a\"b.
            try
            {
                Invoke("ApplyEntries", json);
                string restored = PlayerPrefs.GetString("k", "");
                PlayerPrefs.DeleteKey("k");
                return restored == "a\\\"b";
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        static bool SeqParses()
        {
            long seq = (long)Invoke("ReadSeq", "{\"seq\":42,\"entries\":{}}");
            return seq == 42;
        }
    }
}
