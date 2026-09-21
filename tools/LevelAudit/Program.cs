// Standalone reachability sweep over the whole level library.
// Compiled outside Unity with Mono's csc against UnityEngine.CoreModule
// (the level data is pure C#, so the analysis needs no editor).
// Usage: audit.exe [--detail N]  (N = zero-based level index)
using System;
using System.Collections.Generic;
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

    // Retrace distance: how far back a death can send you. The difficulty
    // pass measured jump margins and the death penalty but never the WALK
    // BACK, which is where the worst numbers in the game actually are —
    // retraversal is the cost a child pays per mistake. Respawn points are
    // the spawn, each checkpoint and the portal; the figure reported is the
    // longest stretch between consecutive ones, in course units and in
    // seconds at run speed (research cites 30-45 s as the upper bound).
    const float RunSpeed = 8f;
    const float RetraceSecondsBound = 45f;

    static void RetraceReport(LevelDefinition[] levels)
    {
        Console.WriteLine("=== retrace (longest walk back after a death) ===");
        Console.WriteLine("  bound: " + RetraceSecondsBound.ToString("F0") +
            "s (" + (RetraceSecondsBound * RunSpeed).ToString("F0") +
            " units at run speed)");
        int over = 0;
        float worstAll = 0f;
        string worstName = "";
        for (int i = 0; i < levels.Length; i++)
        {
            LevelDefinition l = levels[i];
            List<float> stops = new List<float>();
            stops.Add(l.Spawn.z);
            foreach (UnityEngine.Vector3 c in l.Checkpoints) stops.Add(c.z);
            stops.Add(l.Portal.z);
            stops.Sort();
            float worst = 0f;
            for (int k = 1; k < stops.Count; k++)
                worst = Math.Max(worst, stops[k] - stops[k - 1]);
            float seconds = worst / RunSpeed;
            bool bad = seconds > RetraceSecondsBound;
            if (bad) over++;
            if (worst > worstAll) { worstAll = worst; worstName = l.Name; }
            Console.WriteLine((bad ? "  OVER " : "  ok   ") +
                " L" + (i + 1).ToString().PadLeft(2) + " " +
                l.Name.PadRight(26) +
                " stops " + stops.Count +
                "  worst " + worst.ToString("F0").PadLeft(3) + "u" +
                "  " + seconds.ToString("F0").PadLeft(3) + "s");
        }
        Console.WriteLine();
        Console.WriteLine("worst in game: " + worstAll.ToString("F0") +
            "u at " + worstName + "; " + over + " level(s) over the bound");
    }

    // Hazard timing: the spinner is the game's only lethal object, and its
    // fairness is a WINDOW, not a distance — how long the arm is clear at
    // the point the player must cross, against how long the crossing
    // takes. Report in play order so the ramp is visible as numbers.
    static void HazardReport(LevelDefinition[] levels)
    {
        Console.WriteLine("=== hazard timing (spinners; child response budget " +
            (HazardTiming.ChildResponseSeconds * 1000f).ToString("F0") +
            "ms, max exposure " + (HazardTiming.MaxExposure * 100f).ToString("F0") +
            "%) ===");
        int unfair = 0;
        for (int i = 0; i < levels.Length; i++)
        {
            List<HazardTiming.Hazard> hs = HazardTiming.Survey(levels[i]);
            if (hs.Count == 0) continue;
            bool applies = HazardTiming.FairnessApplies(levels[i]);
            Console.WriteLine("  L" + (i + 1).ToString().PadLeft(2) + " " +
                levels[i].Name + (applies ? "" : "  [flight: contact is harmless]"));
            foreach (HazardTiming.Hazard h in hs)
            {
                bool bad = applies && HazardTiming.IsUnfair(h);
                if (bad) unfair++;
                Console.WriteLine((bad ? "    BAD " : "    ok  ") +
                    h.DegreesPerSecond.ToString("F0").PadLeft(3) + "deg/s" +
                    "  rev " + h.RevolutionSeconds.ToString("F2") + "s" +
                    "  pass " + (h.ArmPassSeconds * 1000f).ToString("F0").PadLeft(4) + "ms" +
                    "  cross " + h.CrossSeconds.ToString("F2") + "s" +
                    "  passes/cross " + h.PassesPerCross.ToString("F2") +
                    "  window " + h.SafeWindowSeconds.ToString("F2") + "s" +
                    "  exposure " + (h.Exposure * 100f).ToString("F0") + "%" +
                    "  " + (h.WakeRadius > 0f ? "sleepy" : "awake"));
            }
        }
        Console.WriteLine();
        Console.WriteLine(unfair + " hazard(s) outside the child budget");
    }

    static int Main(string[] args)
    {
        int detail = -1;
        bool margins = false;
        bool curve = false;
        bool retrace = false;
        bool hazards = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--detail" && i + 1 < args.Length)
                int.TryParse(args[i + 1], out detail);
            if (args[i] == "--margins") margins = true;
            if (args[i] == "--curve") curve = true;
            if (args[i] == "--retrace") retrace = true;
            if (args[i] == "--hazards") hazards = true;
        }

        if (curve)
        {
            CurveReport(LevelLibrary.Levels);
            return 0;
        }
        if (retrace)
        {
            RetraceReport(LevelLibrary.Levels);
            return 0;
        }
        if (hazards)
        {
            HazardReport(LevelLibrary.Levels);
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
