using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace GemRush.Tests
{
    /// <summary>
    /// Headless level audit (engineering queue #3 in DESIGN.md): validates
    /// every level's geometry and metadata so content can grow fast without
    /// playtesting everything. Pure data checks — no scene objects needed.
    ///
    /// The reach heuristics are deliberately generous: they must stay green
    /// on all shipped levels while still catching genuinely broken content
    /// (an island nobody can jump to, a gem under the death plane, a
    /// spinner floating in air). When a check fails, tune the LEVEL, not
    /// the threshold.
    /// </summary>
    [TestFixture]
    public class LevelAuditTests
    {
        // A hop reaches ~3-5 units of gap and ~1 unit of rise comfortably;
        // audit margins sit well above that so intended-but-quirky layouts
        // don't trip it.
        const float LandingReachXz = 4.2f;   // XZ distance from a top edge to a "gettable" target
        const float MaxOrphanDistance = 13f; // tops farther than this from ANY other top are suspect
        const float SpinnerTopTolerance = 0.35f;

        static IEnumerable<LevelDefinition> AllLevels
        {
            get { return LevelLibrary.Levels; }
        }

        static string Label(int index, LevelDefinition l)
        {
            return string.Format("level {0} '{1}'", index, l.Name);
        }

        // A standable surface: center of the top face + its XZ half extents.
        struct Top
        {
            public Vector3 Center;      // top-face center, world
            public Vector2 Half;       // XZ half extents
            public override string ToString()
            {
                return string.Format("top@{0}", Center);
            }
        }

        static List<Top> StandableTops(LevelDefinition l)
        {
            List<Top> tops = new List<Top>();
            for (int i = 0; i < l.Platforms.Count; i++)
            {
                PlatformSpec p = l.Platforms[i];
                tops.Add(new Top
                {
                    Center = new Vector3(p.Center.x, p.Center.y + p.Size.y * 0.5f, p.Center.z),
                    Half = new Vector2(p.Size.x * 0.5f, p.Size.z * 0.5f)
                });
            }
            for (int i = 0; i < l.Movers.Count; i++)
            {
                MoverSpec m = l.Movers[i];
                // A mover sweeps between Center and Center+Offset; treat both
                // extremes as standable (plus its half extents).
                Vector2 half = new Vector2(Mathf.Max(m.Size.x * 0.5f, 2f),
                                           Mathf.Max(m.Size.z * 0.5f, 2f));
                float topY = m.Center.y + m.Size.y * 0.5f;
                tops.Add(new Top { Center = m.Center, Half = half });
                tops.Add(new Top
                {
                    Center = m.Center + m.Offset,
                    Half = half
                });
                // Keep the top height honest for the offset end too.
                Top fix = tops[tops.Count - 1];
                fix.Center = new Vector3(fix.Center.x, topY + m.Offset.y, fix.Center.z);
                tops[tops.Count - 1] = fix;
                tops[tops.Count - 2] = new Top
                {
                    Center = new Vector3(m.Center.x, topY, m.Center.z),
                    Half = half
                };
            }
            // Echo bridges are standable while their bell's tone rings them
            // solid — the intended way to cross (and to trail gems) in the
            // Bell Towers packs.
            for (int i = 0; i < l.EchoBridges.Count; i++)
            {
                EchoBridgeSpec b = l.EchoBridges[i];
                tops.Add(new Top
                {
                    Center = new Vector3(b.Center.x,
                        b.Center.y + b.Size.y * 0.5f, b.Center.z),
                    Half = new Vector2(Mathf.Max(b.Size.x * 0.5f, 1.5f),
                                       Mathf.Max(b.Size.z * 0.5f, 1.5f))
                });
            }
            return tops;
        }

        static bool NearAnyTop(List<Top> tops, Vector3 point, float extraReach)
        {
            for (int i = 0; i < tops.Count; i++)
            {
                float dx = Mathf.Abs(point.x - tops[i].Center.x) - tops[i].Half.x;
                float dz = Mathf.Abs(point.z - tops[i].Center.z) - tops[i].Half.y;
                float distXz = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f)
                                        + Mathf.Max(dz, 0f) * Mathf.Max(dz, 0f));
                if (distXz <= extraReach) return true;
            }
            return false;
        }

        static bool InAnyWind(LevelDefinition l, Vector3 point)
        {
            for (int i = 0; i < l.WindZones.Count; i++)
            {
                WindSpec w = l.WindZones[i];
                if (Mathf.Abs(point.x - w.Center.x) <= w.Size.x * 0.5f + 2f &&
                    Mathf.Abs(point.z - w.Center.z) <= w.Size.z * 0.5f + 2f &&
                    point.y >= w.Center.y - w.Size.y * 0.5f - 1f &&
                    point.y <= w.Center.y + w.Size.y * 0.5f + 14f)
                    return true;
            }
            return false;
        }

        // Tailwind gusts carry Pip through their box and onward along the
        // blow direction, so the gettable space is the box swept along
        // Direction (plus lift headroom above it).
        static bool InAnyGust(LevelDefinition l, Vector3 point)
        {
            for (int i = 0; i < l.Gusts.Count; i++)
            {
                GustSpec g = l.Gusts[i];
                // The swept box: from the gust center to the far end of the
                // carry (box depth + 10 units of glide beyond it).
                float carry = g.Size.z + 10f;
                Vector3 far = g.Center + Vector3.Scale(
                    g.Direction, new Vector3(carry, 0f, carry));
                Vector3 lo = Vector3.Min(g.Center, far);
                Vector3 hi = Vector3.Max(g.Center, far);
                float margin = 2.5f;
                if (point.x >= lo.x - g.Size.x * 0.5f - margin &&
                    point.x <= hi.x + g.Size.x * 0.5f + margin &&
                    point.z >= lo.z - g.Size.z * 0.5f - margin &&
                    point.z <= hi.z + g.Size.z * 0.5f + margin &&
                    point.y >= g.Center.y - g.Size.y * 0.5f - margin &&
                    point.y <= g.Center.y + g.Size.y * 0.5f + 8f)
                    return true;
            }
            return false;
        }

        static bool Reachable(LevelDefinition l, List<Top> tops, Vector3 point)
        {
            return l.BonusFlight || NearAnyTop(tops, point, LandingReachXz)
                || InAnyWind(l, point) || InAnyGust(l, point);
        }

        // ------------------------------------------------------------------
        // Metadata
        // ------------------------------------------------------------------

        [Test]
        public void LevelCount_IsSubstantial()
        {
            Assert.GreaterOrEqual(LevelLibrary.Levels.Length, 24,
                "The shipped game has 30 levels; a smaller library means a " +
                "pack file stopped being concatenated in BuildAllLevels.");
        }

        [Test]
        public void EveryLevel_HasUniqueNotEmptyName()
        {
            HashSet<string> seen = new HashSet<string>();
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                Assert.IsFalse(string.IsNullOrEmpty(l.Name),
                    Label(i, l) + " has no name.");
                Assert.IsTrue(seen.Add(l.Name),
                    Label(i, l) + " duplicates an earlier level name (save " +
                    "keys and the menu use the name).");
                i++;
            }
        }

        [Test]
        public void EveryLevel_HasMissionAndWinLine()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                Assert.IsFalse(string.IsNullOrEmpty(l.Mission),
                    Label(i, l) + " has no mission card text.");
                Assert.IsFalse(string.IsNullOrEmpty(l.WinLine),
                    Label(i, l) + " has no win line.");
                i++;
            }
        }

        [Test]
        public void EveryLevel_HasGemsToChase()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                Assert.GreaterOrEqual(l.GemCount, 10,
                    Label(i, l) + " has fewer than 10 gems; star math " +
                    "(3 = all, 2 = half) expects a full trail.");
                i++;
            }
        }

        // ------------------------------------------------------------------
        // Geometry
        // ------------------------------------------------------------------

        [Test]
        public void KillY_IsBelowEveryPlatform()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                for (int p = 0; p < l.Platforms.Count; p++)
                {
                    float bottom = l.Platforms[p].Center.y - l.Platforms[p].Size.y * 0.5f;
                    Assert.IsTrue(l.KillY < bottom - 0.5f,
                        Label(i, l) + ": platform " + p +
                        " bottom (" + bottom + ") is at/below KillY (" +
                        l.KillY + ") — it would kill on landing.");
                }
                i++;
            }
        }

        [Test]
        public void Spawn_LandsOnOrNearATop_AndAboveKillY()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.BonusFlight) { i++; continue; }
                List<Top> tops = StandableTops(l);
                Assert.IsTrue(NearAnyTop(tops, l.Spawn, LandingReachXz),
                    Label(i, l) + ": spawn " + l.Spawn +
                    " is not above/near any platform top.");
                Assert.Greater(l.Spawn.y, l.KillY + 0.5f,
                    Label(i, l) + ": spawn is below the death plane.");
                i++;
            }
        }

        [Test]
        public void Portal_IsReachable()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.BonusFlight) { i++; continue; }
                List<Top> tops = StandableTops(l);
                Assert.IsTrue(Reachable(l, tops, l.Portal),
                    Label(i, l) + ": portal " + l.Portal +
                    " floats away from every platform top, wind or mover.");
                Assert.Greater(l.Portal.y, l.KillY + 0.5f,
                    Label(i, l) + ": portal sits below the death plane.");
                i++;
            }
        }

        [Test]
        public void EveryGem_IsGettable()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                List<Top> tops = StandableTops(l);
                for (int g = 0; g < l.Gems.Count; g++)
                {
                    Assert.IsTrue(Reachable(l, tops, l.Gems[g]),
                        Label(i, l) + ": gem " + g + " at " + l.Gems[g] +
                        " is not within jump reach of any top or wind column.");
                    Assert.Greater(l.Gems[g].y, l.KillY + 0.5f,
                        Label(i, l) + ": gem " + g +
                        " floats below the death plane.");
                }
                i++;
            }
        }

        // XZ edge gap between two platform AABBs (negative = overlapping).
        static float EdgeGapXz(PlatformSpec a, PlatformSpec b)
        {
            float dx = Mathf.Abs(a.Center.x - b.Center.x)
                     - (a.Size.x + b.Size.x) * 0.5f;
            float dz = Mathf.Abs(a.Center.z - b.Center.z)
                     - (a.Size.z + b.Size.z) * 0.5f;
            return Mathf.Max(Mathf.Max(dx, dz), 0f);
        }

        [Test]
        public void NoOrphanIslands()
        {
            // Measured edge-to-edge: design says 3-5 unit gaps are a hop;
            // anything whose nearest neighbour edge is further than a
            // mover/pad/wind/gust could plausibly bridge is suspect.
            const float MaxEdgeGap = 10f;
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                for (int a = 0; a < l.Platforms.Count; a++)
                {
                    PlatformSpec pa = l.Platforms[a];
                    bool hasNeighbor = false;
                    for (int b = 0; b < l.Platforms.Count && !hasNeighbor; b++)
                    {
                        if (b == a) continue;
                        PlatformSpec pb = l.Platforms[b];
                        float gap = EdgeGapXz(pa, pb);
                        float rise = Mathf.Abs(pa.Center.y - pb.Center.y);
                        if (gap <= MaxEdgeGap && rise <= 4f) hasNeighbor = true;
                    }
                    // Movers (and their sweep ends), wind columns, gust lanes
                    // and bounce pads also justify a lone island.
                    if (!hasNeighbor)
                    {
                        for (int m = 0; m < l.Movers.Count && !hasNeighbor; m++)
                        {
                            MoverSpec m2 = l.Movers[m];
                            float moverGap = Mathf.Min(
                                EdgeGapXz(pa, BoxAt(m2.Center, m2.Size)),
                                EdgeGapXz(pa, BoxAt(m2.Center + m2.Offset, m2.Size)));
                            if (moverGap <= MaxEdgeGap) hasNeighbor = true;
                        }
                    }
                    if (!hasNeighbor)
                    {
                        for (int w = 0; w < l.WindZones.Count && !hasNeighbor; w++)
                            if (EdgeGapXz(pa, BoxAt(l.WindZones[w].Center,
                                    l.WindZones[w].Size)) <= MaxEdgeGap + 4f)
                                hasNeighbor = true;
                    }
                    if (!hasNeighbor)
                    {
                        for (int g = 0; g < l.Gusts.Count && !hasNeighbor; g++)
                            if (EdgeGapXz(pa, BoxAt(l.Gusts[g].Center,
                                    l.Gusts[g].Size)) <= MaxEdgeGap + 6f)
                                hasNeighbor = true;
                    }
                    if (!hasNeighbor && l.BouncePads.Count > 0) hasNeighbor = true;
                    Assert.IsTrue(hasNeighbor,
                        Label(i, l) + ": platform " + a + " at " + pa.Center +
                        " is beyond every bridge (platform edges, movers, wind, " +
                        "gusts, pads) — nobody can reach it.");
                }
                i++;
            }
        }

        static PlatformSpec BoxAt(Vector3 c, Vector3 s)
        {
            return new PlatformSpec(c.x, c.y, c.z, s.x, s.y, s.z);
        }

        [Test]
        public void EverySpinnerStands_OnAPlatformTop()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                List<Top> tops = StandableTops(l);
                for (int s = 0; s < l.Spinners.Count; s++)
                {
                    Vector3 st = l.Spinners[s].PlatformTop;
                    bool grounded = false;
                    for (int t = 0; t < tops.Count && !grounded; t++)
                    {
                        float dx = Mathf.Abs(st.x - tops[t].Center.x) - tops[t].Half.x;
                        float dz = Mathf.Abs(st.z - tops[t].Center.z) - tops[t].Half.y;
                        grounded = dx <= 1f && dz <= 1f &&
                            Mathf.Abs(st.y - tops[t].Center.y) <= SpinnerTopTolerance;
                    }
                    Assert.IsTrue(grounded,
                        Label(i, l) + ": spinner " + s + " at " + st +
                        " does not sit on any platform or mover top " +
                        "(within " + SpinnerTopTolerance + " of the surface).");
                }
                i++;
            }
        }

        [Test]
        public void EveryCheckpoint_StandsOnAPlatformTop()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                List<Top> tops = StandableTops(l);
                for (int c = 0; c < l.Checkpoints.Count; c++)
                {
                    Vector3 cp = l.Checkpoints[c];
                    bool grounded = false;
                    for (int t = 0; t < tops.Count && !grounded; t++)
                    {
                        float dx = Mathf.Abs(cp.x - tops[t].Center.x) - tops[t].Half.x;
                        float dz = Mathf.Abs(cp.z - tops[t].Center.z) - tops[t].Half.y;
                        grounded = dx <= 1f && dz <= 1f &&
                            Mathf.Abs(cp.y - tops[t].Center.y) <= SpinnerTopTolerance;
                    }
                    Assert.IsTrue(grounded,
                        Label(i, l) + ": checkpoint " + c + " at " + cp +
                        " floats off its platform top (checkpoint positions " +
                        "are top-surface positions, like spinners).");
                    Assert.Greater(cp.y, l.KillY + 0.5f,
                        Label(i, l) + ": checkpoint " + c +
                        " sits below the death plane.");
                }
                i++;
            }
        }

        [Test]
        public void EveryGustExit_HasALanding()
        {
            // A gust that dumps Pip past the last platform is a death trap
            // disguised as a ride: the swept far end of every gust lane must
            // end within jump reach of a standable top.
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.BonusFlight) { i++; continue; }
                List<Top> tops = StandableTops(l);
                for (int g = 0; g < l.Gusts.Count; g++)
                {
                    GustSpec gust = l.Gusts[g];
                    Vector3 exit = gust.Center + Vector3.Scale(
                        gust.Direction, new Vector3(
                            gust.Size.z * 0.5f + 10f, 0f, gust.Size.z * 0.5f + 10f));
                    Assert.IsTrue(Reachable(l, tops, exit),
                        Label(i, l) + ": gust " + g + " blows out to " + exit +
                        " with no platform, wind or gust reach there — the " +
                        "ride ends in the void.");
                }
                i++;
            }
        }

        // ------------------------------------------------------------------
        // Derived data sanity
        // ------------------------------------------------------------------

        [Test]
        public void MedalTimes_AreOrdered_AndPositive()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                Assert.Greater(l.GoldTime, 3f,
                    Label(i, l) + ": gold medal time is suspiciously low.");
                Assert.Less(l.GoldTime, l.SilverTime, Label(i, l));
                Assert.Less(l.SilverTime, l.BronzeTime, Label(i, l));
                Assert.IsNotEmpty(l.MedalFor(l.GoldTime), Label(i, l));
                i++;
            }
        }

        [Test]
        public void HeartsAndPads_AreAboveKillY()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                for (int h = 0; h < l.Hearts.Count; h++)
                    Assert.Greater(l.Hearts[h].y, l.KillY + 0.5f,
                        Label(i, l) + ": heart " + h + " is below the death plane.");
                for (int b = 0; b < l.BouncePads.Count; b++)
                    Assert.Greater(l.BouncePads[b].y, l.KillY + 0.5f,
                        Label(i, l) + ": bounce pad " + b + " is below the death plane.");
                i++;
            }
        }
    }
}
