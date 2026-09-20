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
    // Curve report: does difficulty actually ramp?
    static void CurveReport(LevelDefinition[] levels)
    {
        Console.WriteLine();
        Console.WriteLine("=== difficulty curve (route tightest margin per level) ===");
        for (int i = 0; i < levels.Length; i++)
        {
            LevelReachability.Report r = LevelReachability.Analyze(levels[i]);
            float worst = 1f;
            foreach (LevelReachability.Hop h in r.RouteHops)
                if (h.Margin < worst) worst = h.Margin;
            LevelDefinition l = levels[i];
            int hazards = l.Spinners.Count + l.Gusts.Count;
            Console.WriteLine("  L" + (i + 1).ToString().PadLeft(2) + " " +
                l.Name.PadRight(26) +
                "margin " + (r.RouteHops.Count == 0 ? " n/a"
                    : (worst * 100f).ToString("F0").PadLeft(3) + "%") +
                "  routejumps " + r.RouteHops.Count.ToString().PadLeft(2) +
                "  hazards " + hazards +
                "  spinners " + l.Spinners.Count +
                "  movers " + l.Movers.Count);
        }
    }

    static int Main(string[] args)
    {
        int detail = -1;
        bool margins = false;
        bool curve = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--detail" && i + 1 < args.Length)
                int.TryParse(args[i + 1], out detail);
            if (args[i] == "--margins") margins = true;
            if (args[i] == "--curve") curve = true;
        }

        if (curve)
        {
            CurveReport(LevelLibrary.Levels);
            return 0;
        }


            LevelDefinition[] levels = LevelLibrary.Levels;
            Console.WriteLine("library: " + levels.Length + " levels");
            Console.WriteLine();

            int fails = 0;
            float tightestOverall = 1f;
            string tightestWhere = "";
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
                    " hops=" + r.Hops.Count +
                    " tightest=" + (r.Hops.Count == 0
                        ? "n/a" : (r.TightestMargin * 100f).ToString("F0") + "%") +
                    (r.PortalReachable ? "" : "  PORTAL UNREACHABLE"));

                if (r.Hops.Count > 0 && r.TightestMargin < tightestOverall)
                {
                    tightestOverall = r.TightestMargin;
                    tightestWhere = "L" + (i + 1) + " " + r.LevelName;
                }

                foreach (string p in r.Problems)
                    Console.WriteLine("     PROBLEM: " + p);
                foreach (string w in r.Warnings)
                    Console.WriteLine("     warn: " + w);
                if (margins)
                {
                    float floor = i < LevelReachability.ChildRegionLevelCount
                        ? LevelReachability.ChildMargin
                        : LevelReachability.TightMargin;
                    foreach (LevelReachability.Hop h in r.RouteHops)
                    {
                        if (h.Margin >= floor) continue;
                        Console.WriteLine("     " +
                            (h.Optional ? "optional" : "FORCED  ") +
                            " " + (h.Margin * 100f).ToString("F0") + "%  gap " +
                            h.Gap.ToString("F1") + "/" +
                            h.Range.ToString("F1") + "  rise " +
                            h.Rise.ToString("F1") +
                            "  " + h.From + " -> " + h.To);
                    }
                }
                if (i == detail && r.Path != null && r.Path.Count > 0)
                {
                    Console.Write("     path: ");
                    for (int k = 0; k < r.Path.Count; k++)
                        Console.Write(r.Path[k] +
                            (k + 1 < r.Path.Count ? " -> " : "\n"));
                }
            }

            Console.WriteLine();
            if (tightestOverall < 1f)
                Console.WriteLine("tightest jump in the game: " +
                    (tightestOverall * 100f).ToString("F0") + "% margin at " +
                    tightestWhere);
            Console.WriteLine(fails == 0
                ? "ALL LEVELS REACHABLE — spawn to portal under real physics."
                : fails + " LEVEL(S) WITH BLOCKING ISSUES");
            return fails == 0 ? 0 : 1;
        }
    }
}
