// Standalone reachability sweep over the whole level library.
// Compiled outside Unity with Mono's csc against UnityEngine.CoreModule
// (the level data is pure C#, so the analysis needs no editor).
// Usage: audit.exe [--detail N]  (N = zero-based level index)
using System;
using GemRush;

namespace GemRush.Tools
{
    static class Program
    {
        static int Main(string[] args)
        {
            int detail = -1;
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "--detail" && i + 1 < args.Length)
                    int.TryParse(args[i + 1], out detail);

            LevelDefinition[] levels = LevelLibrary.Levels;
            Console.WriteLine("library: " + levels.Length + " levels");
            Console.WriteLine();

            int fails = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                LevelReachability.Report r;
                try
                {
                    r = LevelReachability.Analyze(levels[i]);
                }
                catch (Exception e)
                {
                    Console.WriteLine("FAIL #" + i + " (L" + (i + 1) + ") " +
                        levels[i].Name + " — analyzer exception: " + e.Message);
                    fails++;
                    continue;
                }

                if (r.Bad) fails++;
                string mark = r.Bad ? "FAIL" : "ok  ";
                Console.WriteLine(mark + " #" + i + " (L" + (i + 1) + ") " +
                    r.LevelName + "  surfaces=" + r.TopCount +
                    " reached=" + r.ReachableCount +
                    (r.PortalReachable ? "" : "  PORTAL UNREACHABLE"));
                foreach (string p in r.Problems)
                    Console.WriteLine("     PROBLEM: " + p);
                foreach (string w in r.Warnings)
                    Console.WriteLine("     warn: " + w);
                if (i == detail && r.Path != null && r.Path.Count > 0)
                {
                    Console.Write("     path: ");
                    for (int k = 0; k < r.Path.Count; k++)
                        Console.Write(r.Path[k] +
                            (k + 1 < r.Path.Count ? " -> " : "\n"));
                }
            }

            Console.WriteLine();
            Console.WriteLine(fails == 0
                ? "ALL LEVELS REACHABLE — spawn to portal under real physics."
                : fails + " LEVEL(S) WITH BLOCKING ISSUES");
            return fails == 0 ? 0 : 1;
        }
    }
}
