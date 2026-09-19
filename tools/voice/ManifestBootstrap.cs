// Bootstrap manifest writer — runs the same collection logic as
// GemRush.EditorTools.VoiceLinesExporter without needing the Unity
// editor, against Unity API stubs (tools/stubs). Text data in
// LevelLibrary/packs/Story is pure C#, so this produces a manifest
// byte-compatible with the in-editor exporter (which remains the
// canonical path once Unity is available).
//
// Build & run (from the repo root):
//   mono <unity>/Roslyn/csc.exe -nologo -langversion:9.0 -out:bootstrap.exe \
//        -recurse:tools/stubs/*.cs Assets/Scripts/Level*.cs Assets/Scripts/Story.cs \
//        Assets/Scripts/VoiceOver.cs tools/voice/ManifestBootstrap.cs
//   mono bootstrap.exe
//
// The runtime never sees this file; Assets/Resources/Voice/manifest.json
// stays the single contract between code and generated audio.
using System.Collections.Generic;
using System.IO;
using System.Text;
using GemRush;

public static class ManifestBootstrap
{
    const string ManifestPath = "Assets/Resources/Voice/manifest.json";

    public static int Main()
    {
        System.Console.WriteLine("[boot] collecting lines...");
        List<VoiceOver.VoiceEntry> fresh = CollectLines();
        System.Console.WriteLine("[boot] collected " + fresh.Count);

        VoiceOver.VoiceManifest manifest = new VoiceOver.VoiceManifest();
        System.Console.WriteLine("[boot] manifest entries null? " +
            (manifest.entries == null));
        Dictionary<string, VoiceOver.VoiceEntry> byKey =
            new Dictionary<string, VoiceOver.VoiceEntry>();
        foreach (VoiceOver.VoiceEntry line in fresh)
        {
            string key = line.cast + "/" + line.hash;
            VoiceOver.VoiceEntry entry;
            if (!byKey.TryGetValue(key, out entry))
            {
                manifest.entries.Add(line);
                byKey[key] = line;
            }
            else
            {
                System.Console.WriteLine("[boot] aliasing " + line.id + " -> " + entry.id);
                List<string> all = new List<string>(entry.aliases ?? new string[0]);
                if (!all.Contains(line.id)) all.Add(line.id);
                entry.aliases = all.ToArray();
            }
        }
        System.Console.WriteLine("[boot] deduped to " + manifest.entries.Count);

        Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath));
        System.Console.WriteLine("[boot] dir ready, writing...");
        WriteManifest(ManifestPath, manifest);
        System.Console.WriteLine("[boot] written.");

        int words = 0;
        foreach (VoiceOver.VoiceEntry e in manifest.entries)
            if (!string.IsNullOrEmpty(e.text))
                words += e.text.Split(' ').Length;
        System.Console.WriteLine("[Voice] " + fresh.Count + " lines as " +
            manifest.entries.Count + " clips, ~" + words + " words -> " +
            ManifestPath);
        return 0;
    }

    static List<VoiceOver.VoiceEntry> CollectLines()
    {
        List<VoiceOver.VoiceEntry> lines = new List<VoiceOver.VoiceEntry>();

        foreach (LevelDefinition level in LevelLibrary.Levels)
        {
            try
            {
                Add(lines, VoiceIds.Narrator, VoiceIds.Mission(level.Name), level.Mission);
                Add(lines, VoiceIds.Narrator, VoiceIds.Win(level.Name), level.WinLine);
                Add(lines, VoiceIds.Narrator, VoiceIds.Milestone(level.Name), level.Milestone);
                for (int i = 0; i < level.StoryBeats.Count; i++)
                    Add(lines, VoiceIds.Narrator,
                        VoiceIds.Beat(level.Name, i), level.StoryBeats[i]);
            }
            catch (System.Exception ex)
            {
                System.Console.Error.WriteLine("FAILED at level '" +
                    (level != null ? level.Name : "<null level>") + "': " + ex.Message);
                throw;
            }
        }

        for (int i = 0; i < Story.Epilogue.Length; i++)
            Add(lines, VoiceIds.Narrator, VoiceIds.Epilogue(i), Story.Epilogue[i]);
        for (int i = 0; i < Story.MenuQuotes.Length; i++)
            Add(lines, VoiceIds.Narrator, VoiceIds.Quote(i), Story.MenuQuotes[i]);

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

    // Minimal JsonUtility-shaped writer: same field names and pretty-ish
    // layout; the runtime reads it back through JsonUtility.FromJson.
    static void WriteManifest(string path, VoiceOver.VoiceManifest manifest)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{\n    \"version\": ").Append(manifest.version)
          .Append(",\n    \"entries\": [\n");
        for (int i = 0; i < manifest.entries.Count; i++)
        {
            VoiceOver.VoiceEntry e = manifest.entries[i];
            sb.Append("        {\n")
              .Append("            \"id\": ").Append(Json(e.id)).Append(",\n")
              .Append("            \"cast\": ").Append(Json(e.cast)).Append(",\n")
              .Append("            \"hash\": ").Append(Json(e.hash)).Append(",\n")
              .Append("            \"file\": ").Append(Json(e.file)).Append(",\n")
              .Append("            \"duration\": ").Append(
                  e.duration.ToString(System.Globalization.CultureInfo.InvariantCulture))
              .Append(",\n")
              .Append("            \"aliases\": [").Append(JsonArray(e.aliases))
              .Append("],\n")
              .Append("            \"text\": ").Append(Json(e.text)).Append("\n")
              .Append("        }");
            sb.Append(i < manifest.entries.Count - 1 ? ",\n" : "\n");
        }
        sb.Append("    ]\n}\n");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    static string JsonArray(string[] items)
    {
        if (items == null || items.Length == 0) return "";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < items.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Json(items[i]));
        }
        return sb.ToString();
    }

    static string Json(string s)
    {
        if (s == null) return "null";
        StringBuilder sb = new StringBuilder("\"");
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append("\"").ToString();
    }
}
