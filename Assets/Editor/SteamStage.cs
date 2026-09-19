using System.IO;
using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// Steam staging (docs/Steam-Deploy.md "start today" item): copies the
    /// ship list from Builds/ into a clean build/steam/win/ folder — never
    /// shipping the Unity backup folder, PDBs or the APK to the depot.
    /// SteamPipe's ContentRoot points at this folder; the VDF templates
    /// live in tools/ContentBuilder/scripts/.
    public static class SteamStage
    {
        const string Source = "Builds";
        const string StageRoot = "build/steam/win";

        // The Unity 6 x64 ship list (docs/Steam-Deploy.md).
        static readonly string[] ShipFiles =
        {
            "GemRush3D.exe",
            "UnityPlayer.dll",
            "UnityCrashHandler64.exe",
            "DirectML.dll",
            "dstorage.dll",
            "dstoragecore.dll"
        };
        static readonly string[] ShipFolders =
        {
            "GemRush3D_Data",
            "MonoBleedingEdge",
            "D3D12"
        };

        [MenuItem("GemRush/Stage Steam Build (win)")]
        public static void Stage()
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            string src = Path.Combine(projectRoot, Source);
            string dst = Path.Combine(projectRoot, StageRoot);

            if (!File.Exists(Path.Combine(src, "GemRush3D.exe")))
            {
                Debug.LogWarning("[SteamStage] No Windows build in Builds/ " +
                    "— run the release build first.");
                return;
            }
            if (Directory.Exists(dst))
                Directory.Delete(dst, true);
            Directory.CreateDirectory(dst);

            int files = 0;
            foreach (string file in ShipFiles)
            {
                string from = Path.Combine(src, file);
                if (!File.Exists(from))
                {
                    Debug.LogWarning("[SteamStage] Missing ship file: " +
                        file);
                    continue;
                }
                File.Copy(from, Path.Combine(dst, file), true);
                files++;
            }
            foreach (string folder in ShipFolders)
            {
                string from = Path.Combine(src, folder);
                if (!Directory.Exists(from))
                {
                    Debug.LogWarning("[SteamStage] Missing ship folder: " +
                        folder);
                    continue;
                }
                files += CopyClean(from, Path.Combine(dst, folder));
            }

            Debug.Log("[SteamStage] Staged " + files + " files into " +
                StageRoot + " (pdb/apk/backup excluded). Next: steamcmd " +
                "runfile with the VDF templates in " +
                "tools/ContentBuilder/scripts/.");
        }

        /// Recursive copy that refuses the depot's three banned guests:
        /// PDB symbols, the APK, and Unity's backup folder.
        static int CopyClean(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            int files = 0;
            foreach (string file in Directory.GetFiles(src))
            {
                string name = Path.GetFileName(file);
                if (name.EndsWith(".pdb") || name.EndsWith(".apk"))
                    continue;
                File.Copy(file, Path.Combine(dst, name), true);
                files++;
            }
            foreach (string dir in Directory.GetDirectories(src))
            {
                string name = Path.GetFileName(dir);
                if (name.StartsWith("GemRush3D_BackUpThisFolder"))
                    continue;
                files += CopyClean(dir, Path.Combine(dst, name));
            }
            return files;
        }
    }
}
