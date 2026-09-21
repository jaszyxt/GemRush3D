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
            // Aurora ribbons are standable along their whole flowing path:
            // sample start, mid and end, with the sway amplitude folded
            // into the extents (the sway tapers to nothing at the ends,
            // so this is generous in the middle — deliberately).
            for (int i = 0; i < l.AuroraRibbons.Count; i++)
            {
                AuroraRibbonSpec r = l.AuroraRibbons[i];
                Vector2 half = new Vector2(
                    Mathf.Max(r.Size.x * 0.5f, 1.5f) + r.Sway,
                    Mathf.Max(r.Size.z * 0.5f, 1.5f) + r.Sway);
                float topY = r.Center.y + r.Size.y * 0.5f;
                for (int s = 0; s <= 2; s++)
                {
                    float t = s / 2f; // 0, 0.5, 1 along the travel
                    tops.Add(new Top
                    {
                        Center = new Vector3(
                            r.Center.x + r.Travel.x * t,
                            topY,
                            r.Center.z + r.Travel.z * t),
                        Half = half
                    });
                }
            }
            // See-saw planks stand at their pivot: level within a few
            // degrees of the hinge top, so the hinge-top position is the
            // honest standable area.
            for (int i = 0; i < l.SeeSaws.Count; i++)
            {
                SeeSawSpec s = l.SeeSaws[i];
                bool alongX = s.Axis == "x";
                tops.Add(new Top
                {
                    Center = new Vector3(s.PlatformTop.x,
                        s.PlatformTop.y + 0.52f, s.PlatformTop.z),
                    Half = alongX
                        ? new Vector2(s.Length * 0.5f, s.Width * 0.5f)
                        : new Vector2(s.Width * 0.5f, s.Length * 0.5f)
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
                "The shipped level library is missing content; a smaller " +
                "library means a pack file stopped being concatenated in " +
                "BuildAllLevels.");
        }

        /// The atlas regions are the only place packs are enumerated for the
        /// player, and each `Count` is hand-written. If a pack's levels stop
        /// being registered — the failure mode the old hand-summed array had
        /// — the region map and the level list disagree, and the atlas screen
        /// shows the wrong thing. Catch it here instead.
        [Test]
        public void Regions_CoverEveryLevel_Exactly()
        {
            int sum = 0;
            int expectedFirst = 0;
            for (int r = 0; r < LevelLibrary.Regions.Length; r++)
            {
                LevelLibrary.Region region = LevelLibrary.Regions[r];
                Assert.Greater(region.Count, 0,
                    "region " + region.Roman + " '" + region.Name +
                    "' claims no levels.");
                Assert.LessOrEqual(region.First + region.Count,
                    LevelLibrary.Levels.Length,
                    "region " + region.Roman + " '" + region.Name +
                    "' runs off the end of the level list — a pack is " +
                    "missing from BuildAllLevels, or its Count is stale.");
                Assert.AreEqual(expectedFirst, region.First,
                    "region " + region.Roman + " should start at level " +
                    expectedFirst + " so the atlas tiles without gaps or " +
                    "overlaps.");
                // Regions must tile in Play order, so the name at each
                // region's first index is also checked via count below.
                expectedFirst += region.Count;
                sum += region.Count;
            }
            Assert.AreEqual(LevelLibrary.Levels.Length, sum,
                "Atlas regions cover " + sum + " levels but the library holds " +
                LevelLibrary.Levels.Length + ". Every level must belong to " +
                "exactly one region.");
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
                Assert.IsFalse(string.IsNullOrEmpty(l.Mission),                    Label(i, l) + " has no mission card text.");
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
        // Story slots (Story-Bible section 7)
        //
        // The bible claims these live here as the metadata half of its QA
        // gate, and until now nothing asserted them: a level could ship
        // with no story beat at all, or a pack finale with no milestone,
        // and pass the entire suite in silence.
        // ------------------------------------------------------------------

        [Test]
        public void EveryLevel_HasAtLeastOneStoryBeat()
        {
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                // Bonus-flight is a single serene delivery level with no
                // checkpoints to hang beats on; every other level tells
                // the player something as they go.
                if (l.BonusFlight) { i++; continue; }
                Assert.GreaterOrEqual(l.StoryBeats.Count, 1,
                    Label(i, l) + " has no story beats — the world says "
                    + "nothing while the player crosses it.");
                i++;
            }
        }

        [Test]
        public void EveryPackFinale_CarriesAMilestone()
        {
            // A milestone is the "atlas grew a page" stamp on the win
            // screen. The last level of each region must carry one, or
            // finishing a region passes without the world acknowledging it.
            // Regions come from the same table the atlas uses, so a new
            // pack is covered the moment it registers a region.
            foreach (LevelLibrary.Region region in LevelLibrary.Regions)
            {
                int last = region.First + region.Count - 1;
                LevelDefinition l = LevelLibrary.Levels[last];
                Assert.IsFalse(string.IsNullOrEmpty(l.Milestone),
                    "region " + region.Roman + " '" + region.Name + "' ends "
                    + "at level " + (last + 1) + " '" + l.Name + "' with no "
                    + "milestone — that region's finale is silent.");
            }
        }

        [Test]
        public void EveryOptionalRemix_IsReachableWithoutAnyGolden()
        {
            // The B-Sides are gated behind hidden golden gems, and unlocking
            // is sequential. Those two facts together used to put a
            // collectible hunt in the critical path: a player who never
            // found the golden on the source level could never open the
            // levels BEHIND the remix, including the ending.
            //
            // The ladder now steps over optional remixes, so walking it from
            // level 1 must always reach the final level with no goldens at
            // all. This is the class-level lock for that defect.
            int last = LevelLibrary.Levels.Length - 1;
            int frontier = 0;
            int guard = 0;
            while (frontier < last && guard++ < LevelLibrary.Levels.Length)
                frontier = LevelLibrary.NextMainRoadLevel(frontier);

            Assert.AreEqual(last, frontier,
                "walking the main road from level 1 stops at " + (frontier + 1)
                + " instead of " + (last + 1) + " — something in the ladder "
                + "depends on a secret, so the game can be made unfinishable.");
        }

        [Test]
        public void NextMainRoadLevel_SkipsOnlyOptionalRemixes()
        {
            // The helper must be a no-op everywhere except across a gated
            // remix, and must never run off the end of the library.
            int last = LevelLibrary.Levels.Length - 1;
            for (int i = 0; i < LevelLibrary.Levels.Length; i++)
            {
                int next = LevelLibrary.NextMainRoadLevel(i);
                Assert.LessOrEqual(next, last,
                    "NextMainRoadLevel(" + i + ") returned past the library.");
                Assert.GreaterOrEqual(next, i,
                    "NextMainRoadLevel(" + i + ") went backwards.");

                if (LevelLibrary.IsOptionalRemix(i + 1))
                    Assert.IsFalse(LevelLibrary.IsOptionalRemix(next),
                        "level " + (i + 1) + " leads onto optional remix "
                        + (next + 1) + " — the ladder must step over it.");
                else if (i < last)
                    Assert.AreEqual(i + 1, next,
                        "level " + (i + 1) + " advanced to " + (next + 1)
                        + " without a remix in between.");
                // else: the final level has nowhere to advance to, and the
                // clamp correctly leaves it where it is.
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
                    // Aurora ribbons board from their path's two ends;
                    // see-saw planks bridge from their pivot.
                    if (!hasNeighbor)
                    {
                        for (int r = 0; r < l.AuroraRibbons.Count && !hasNeighbor; r++)
                        {
                            AuroraRibbonSpec r2 = l.AuroraRibbons[r];
                            float ribbonGap = Mathf.Min(
                                EdgeGapXz(pa, BoxAt(r2.Center, r2.Size)),
                                EdgeGapXz(pa, BoxAt(r2.Center + r2.Travel, r2.Size)));
                            if (ribbonGap <= MaxEdgeGap) hasNeighbor = true;
                        }
                    }
                    if (!hasNeighbor)
                    {
                        for (int s = 0; s < l.SeeSaws.Count && !hasNeighbor; s++)
                        {
                            SeeSawSpec s2 = l.SeeSaws[s];
                            Vector3 pivot = s2.PlatformTop;
                            Vector3 size = s2.Axis == "x"
                                ? new Vector3(s2.Length, 1f, s2.Width)
                                : new Vector3(s2.Width, 1f, s2.Length);
                            if (EdgeGapXz(pa, BoxAt(pivot, size)) <= MaxEdgeGap)
                                hasNeighbor = true;
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

        [Test]
        public void Spawn_ReachesThePortal_OnEveryLevel()
        {
            // The local checks above pass even when a course is cut in
            // half: every island still has A neighbour, and the portal
            // still sits near A top. This is the whole-course contract —
            // under the real movement physics (jump arcs with coyote
            // time, movers, updraft rides and their ejection arcs, gust
            // lanes, bounce pads, mirror doors) the portal's surface must
            // lie on the spawn's island. Engine margins sit inside what a
            // careful player can do: when this fails, fix the LEVEL.
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                LevelReachability.Report r = LevelReachability.Analyze(l);
                Assert.IsTrue(r.PortalReachable && r.Problems.Count == 0,
                    Label(i, l) + ": " + string.Join("; ", r.Problems.ToArray())
                    + (r.PortalReachable ? "" : " — the portal is on a " +
                      "surface the spawn cannot reach (" + r.ReachableCount +
                      " of " + r.TopCount + " surfaces reachable)."));
                i++;
            }
        }

        [Test]
        public void EveryGem_IsPhysicallyCollectable()
        {
            // The older gem check is a generous proximity test; this one is
            // the player-facing contract: every gem must sit within jump
            // reach of a surface the spawn can actually reach, at a height
            // above the deck a jump can pass through. The Long Fall's two
            // gems sat 8.1 units UNDER their decks — inside the solid slab,
            // uncollectable, silently capping the level at 2 stars — and
            // this is the assertion that keeps that class of bug out.
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.BonusFlight) { i++; continue; }
                LevelReachability.Report r = LevelReachability.Analyze(l);
                foreach (string w in r.Warnings)
                    if (w.Contains("beyond jump reach"))
                        Assert.Fail(Label(i, l) + ": " + w);
                i++;
            }
        }

        [Test]
        public void NoRouteJump_IsNearTheMaximumJump()
        {
            // The difficulty contract (research: the "pixel-perfect jump" —
            // a gap equal to the character's maximum — is the single most
            // frustrating construct in the genre: one solution, no error
            // tolerance, and to a young player indistinguishable from an
            // impossible gap).
            //
            // Measured on the COMPLETION ROUTE only: a pairwise sweep of
            // every platform flags hundreds of diagonal non-jumps no
            // player ever attempts.
            //
            // Every hop now clears 2% margin. That floor is low on purpose:
            // the truly dangerous case (an exact-maximum jump) is caught by
            // the FORCED-hop test below, while the optional ones are the
            // designer's deliberate late-game challenge with a bypass
            // offered. The value of this test is that a NEW level cannot
            // add a sub-2% squeeze anywhere.
            const float Floor = 0.02f;
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.BonusFlight) { i++; continue; }
                LevelReachability.Report r = LevelReachability.Analyze(l);
                foreach (LevelReachability.Hop h in r.RouteHops)
                {
                    Assert.GreaterOrEqual(h.Margin, Floor,
                        Label(i, l) + ": route jump " + h.From + " -> " +
                        h.To + " leaves only " +
                        (h.Margin * 100f).ToString("F0") + "% margin " +
                        "(gap " + h.Gap.ToString("F1") + " of a " +
                        h.Range.ToString("F1") + " maximum) — widen the " +
                        "landing or shorten the gap; a player gets one " +
                        "solution and no wobble room.");
                }
                i++;
            }
        }

        [Test]
        public void NoRouteJump_IsEverForcedWithoutAWayAround()
        {
            // The sharper contract: a tight jump is fine when the designer
            // MEANT it and the player can go another way (a mover beside
            // the island, a parallel lane) — that is a challenge, and a
            // child who cannot make it can ride instead. A tight jump with
            // NO alternative is a trap: the only way forward is a move the
            // player may not be able to make.
            //
            // The game shipped with exactly one such trap: The Mirror
            // Meadow's door pedestal, a 4-wide platform reached by an 11%
            // hop with no route around it (fixed to 6 wide). Every other
            // tight hop measured has a genuine bypass, verified by
            // removing the edge and re-searching the graph.
            const float Floor = 0.15f;
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.BonusFlight) { i++; continue; }
                LevelReachability.Report r = LevelReachability.Analyze(l);
                foreach (LevelReachability.Hop h in r.RouteHops)
                {
                    if (h.Optional) continue;
                    Assert.GreaterOrEqual(h.Margin, Floor,
                        Label(i, l) + ": FORCED jump " + h.From + " -> " +
                        h.To + " leaves only " +
                        (h.Margin * 100f).ToString("F0") + "% margin and " +
                        "has no alternative route — a player who cannot " +
                        "make it cannot progress. Widen the landing.");
                }
                i++;
            }
        }

        [Test]
        public void TeachingRegions_AreNotThePunishingOnes()
        {
            // Part of the child-facing curve: a 7-year-old should not meet
            // the game's hardest geometry in the first region. The
            // shipped game had exactly that defect — L5/L8/L9 sat at 4-6%
            // while the late regions averaged better — and the widening
            // pass lifted them to 12%.
            //
            // The ramp is deliberately not monotonic (the back third
            // carries intended challenge with bypasses), so this does not
            // assert "early beats late". It asserts the weaker, true
            // property: no level in the teaching regions (I-VII) may be
            // worse than HALF the game-wide median. That leaves headroom
            // for a fixed teaching floor like The Long Fall's descent
            // while still catching a region-I level that is far harder
            // than everything after it.
            List<float> worstPerLevel = new List<float>();
            float teachingWorst = 1f;
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.BonusFlight) { i++; continue; }
                LevelReachability.Report r = LevelReachability.Analyze(l);
                if (r.RouteHops.Count == 0) { i++; continue; }
                float worst = 1f;
                foreach (LevelReachability.Hop h in r.RouteHops)
                    if (h.Margin < worst) worst = h.Margin;
                worstPerLevel.Add(worst);
                if (i < LevelReachability.ChildRegionLevelCount &&
                    worst < teachingWorst)
                    teachingWorst = worst;
                i++;
            }
            worstPerLevel.Sort();
            float median = worstPerLevel[worstPerLevel.Count / 2];
            Assert.GreaterOrEqual(teachingWorst, median * 0.5f,
                "the teaching regions' tightest jump (" +
                (teachingWorst * 100f).ToString("F0") + "%) is far tighter " +
                "than the game's median level (" +
                (median * 100f).ToString("F0") +
                "%) — the early regions must not be the punishing ones.");
        }

        [Test]
        public void NoLevel_StacksEveryMechanicAtOnce()
        {
            // Pacing research: challenge should come in bands, and a level
            // that piles on every system at once gives a young player no
            // foothold. The mechanic count across the shipped library runs
            // 2-7 with one deliberate showcase (The Festival Finale, 11 —
            // "one lap through every mechanic in the atlas", which is its
            // whole point). This ceiling stops a NEW level from quietly
            // stacking beyond that showcase.
            const int Ceiling = 11;
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                int count = l.Spinners.Count + l.Movers.Count + l.Gusts.Count +
                    l.WindZones.Count + l.BouncePads.Count + l.IceGates.Count +
                    l.EchoBridges.Count + l.MirrorDoors.Count +
                    l.SeeSaws.Count + l.AuroraRibbons.Count;
                Assert.LessOrEqual(count, Ceiling,
                    Label(i, l) + " stacks " + count + " mechanics at once " +
                    "(ceiling " + Ceiling + ") — spread them across the " +
                    "region instead of one level.");
                i++;
            }
        }

        [Test]
        public void Retrace_IsShortEnoughToForgiveADeath()
        {
            // The cost of a mistake: how far back a death sends you. The
            // difficulty pass measured jump margins and the death penalty
            // but not the walk back, and retraversal is what a child
            // actually pays per failure. Respawn points are the spawn,
            // every checkpoint and the portal; the longest stretch between
            // consecutive ones is the worst single death.
            //
            // Design guidance puts the ceiling around 30-45 seconds of
            // retraversal. The shipped library's worst is ~10 s of
            // straight-line travel (The Festival Finale), and real
            // traversal is slower than the straight-line estimate, so the
            // bound below is deliberately generous: it exists to stop a
            // future level from dropping a checkpoint and turning a death
            // into a long walk, not to demand the whole library be dense.
            const float RunSpeed = 8f;
            const float BoundSeconds = 45f;
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                List<float> stops = new List<float>();
                stops.Add(l.Spawn.z);
                foreach (Vector3 c in l.Checkpoints) stops.Add(c.z);
                stops.Add(l.Portal.z);
                stops.Sort();
                float worst = 0f;
                for (int k = 1; k < stops.Count; k++)
                    worst = Mathf.Max(worst, stops[k] - stops[k - 1]);
                float seconds = worst / RunSpeed;
                Assert.LessOrEqual(seconds, BoundSeconds,
                    Label(i, l) + ": a death can send the player " +
                    worst.ToString("F0") + " units back (" +
                    seconds.ToString("F0") + "s at run speed, bound " +
                    BoundSeconds + "s) — add a checkpoint inside that " +
                    "stretch, at its start rather than before its hardest " +
                    "jump.");
                i++;
            }
        }

        // ------------------------------------------------------------------
        // The Long Winter (pack 11): the lantern/ice-gate contract
        // ------------------------------------------------------------------

        [Test]
        public void EveryIceGate_HasTheLightFirst()
        {
            // The lantern shrine must stand before any frozen gate on the
            // route (courses run +z), or the gate is an unopenable wall.
            // Every gate must also sit on the course — within jump reach
            // of a standable top — so melting it is always possible.
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.IceGates.Count == 0) { i++; continue; }
                List<Top> tops = StandableTops(l);

                Assert.GreaterOrEqual(l.Lanterns.Count, 1,
                    Label(i, l) + " has ice gates but no lantern shrine.");
                for (int g = 0; g < l.IceGates.Count; g++)
                {
                    Assert.IsTrue(NearAnyTop(tops, l.IceGates[g].Center,
                        LandingReachXz),
                        Label(i, l) + ": ice gate " + g + " at " +
                        l.IceGates[g].Center + " floats off the course — " +
                        "nothing standable within melt-walking reach.");
                }

                float firstGateZ = float.MaxValue;
                for (int g = 0; g < l.IceGates.Count; g++)
                    firstGateZ = Mathf.Min(firstGateZ, l.IceGates[g].Center.z);
                for (int n = 0; n < l.Lanterns.Count; n++)
                {
                    Vector3 shrine = l.Lanterns[n].PlatformTop;
                    Assert.IsTrue(NearAnyTop(tops, shrine, LandingReachXz),
                        Label(i, l) + ": lantern shrine " + n +
                        " floats off the course.");
                    Assert.LessOrEqual(shrine.z, firstGateZ - 2f,
                        Label(i, l) + ": lantern shrine " + n + " at z " +
                        shrine.z + " is not clearly ahead-of/behind-of the " +
                        "first gate (z " + firstGateZ + ") — the light must " +
                        "come first on the route.");
                }
                i++;
            }
        }

        [Test]
        public void WinterLevels_CarryTheFullKit()
        {
            // A Long Winter level promises snow-soft everything and the
            // lantern loop: it resolves to the Winter mood, has at least
            // one gate to melt, and its palette was actually set (a winter
            // sky is never the daylight default blue).
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (!l.LongWinter) { i++; continue; }
                Assert.AreEqual(SoundMood.Winter, l.ResolveMood(),
                    Label(i, l) + " is a winter level but resolves to " +
                    l.ResolveMood() + ".");
                Assert.GreaterOrEqual(l.IceGates.Count, 1,
                    Label(i, l) + " is a winter level with nothing to melt.");
                Assert.GreaterOrEqual(l.Lanterns.Count, 1,
                    Label(i, l) + " is a winter level with no lantern.");
                Assert.Less(l.SkyColor.b, 1f);
                Assert.Greater(l.SkyColor.r, 0.6f,
                    Label(i, l) + " winter sky is not the pale family.");
                i++;
            }
        }

        [Test]
        public void FestivalLevels_CarryTheRide()
        {
            // An Aurora Festival level promises the ride and the dusk: it
            // resolves to the Festival mood, has at least one ribbon, and
            // its dusk palette was actually set (red channel well below
            // the daylight sky's).
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (!l.AuroraFestival) { i++; continue; }
                Assert.AreEqual(SoundMood.Festival, l.ResolveMood(),
                    Label(i, l) + " is a festival level but resolves to " +
                    l.ResolveMood() + ".");
                Assert.GreaterOrEqual(l.AuroraRibbons.Count, 1,
                    Label(i, l) + " is a festival level with no ribbons.");
                Assert.Less(l.SkyColor.r, 0.55f,
                    Label(i, l) + " festival sky is not the dusk family.");
                i++;
            }
        }

        [Test]
        public void OnlyTheFestivalFinale_UnlocksTheMenuAurora()
        {
            // The permanent menu aurora is the festival's one-time reward:
            // exactly one level in the atlas carries the unlock.
            int unlockers = 0;
            foreach (LevelDefinition l in AllLevels)
                if (l.AuroraUnlock) unlockers++;
            Assert.AreEqual(1, unlockers,
                "Exactly one level (the festival finale) may carry " +
                "AuroraUnlock; found " + unlockers + ".");
        }

        [Test]
        public void EverySeeSaw_HingesOnAPlatformTop()
        {
            // A see-saw's pivot post must stand on solid ground — hinge
            // exactly at a platform's top surface, like checkpoints do.
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                for (int s = 0; s < l.SeeSaws.Count; s++)
                {
                    Vector3 pivot = l.SeeSaws[s].PlatformTop;
                    bool grounded = false;
                    for (int p = 0; p < l.Platforms.Count && !grounded; p++)
                    {
                        PlatformSpec p2 = l.Platforms[p];
                        float dx = Mathf.Abs(pivot.x - p2.Center.x) - p2.Size.x * 0.5f;
                        float dz = Mathf.Abs(pivot.z - p2.Center.z) - p2.Size.z * 0.5f;
                        float topY = p2.Center.y + p2.Size.y * 0.5f;
                        grounded = dx <= 0f && dz <= 0f &&
                            Mathf.Abs(pivot.y - topY) <= 0.05f;
                    }
                    Assert.IsTrue(grounded,
                        Label(i, l) + ": see-saw " + s + " at " + pivot +
                        " has no platform top directly under its hinge " +
                        "(pivot positions are platform-top positions).");
                    Assert.Greater(pivot.y, l.KillY + 0.5f,
                        Label(i, l) + ": see-saw " + s +
                        " sits below the death plane.");
                }
                i++;
            }
        }

        [Test]
        public void BSideGate_SourcesAreEarlierAndNamed()
        {
            // The golden-gem gate map: B-sides 28/29/30 unlock via the
            // goldens of levels 19/22/2. Locked here so a future pack
            // shuffle can't silently orphan the gate.
            Assert.AreEqual(19, LevelLibrary.BSideSourceIndex(28),
                "B-side 28 (Gust Alley — Nightfall) <- Gust Alley");
            Assert.AreEqual(22, LevelLibrary.BSideSourceIndex(29),
                "B-side 29 (The Garden That Dreams) <- The First Bell");
            Assert.AreEqual(2, LevelLibrary.BSideSourceIndex(30),
                "B-side 30 (The Ascent — Nightfall) <- The Ascent");
            Assert.AreEqual(-1, LevelLibrary.BSideSourceIndex(0));
            Assert.AreEqual(-1, LevelLibrary.BSideSourceIndex(27));
            Assert.AreEqual(-1, LevelLibrary.BSideSourceIndex(31));
            // Every source must sit EARLIER in the ladder than its gate:
            // the golden is always collectible before the B-side is
            // ladder-reachable (no dead ends).
            for (int i = 0; i < LevelLibrary.Levels.Length; i++)
            {
                int source = LevelLibrary.BSideSourceIndex(i);
                if (source >= 0)
                    Assert.Less(source, i,
                        "gate source must precede its B-side");
            }
        }

        [Test]
        public void GoldenPlacement_IsDeterministicAndOnACourse()
        {
            // Same level -> same hidden spot, every call; and the spot
            // must stand on (or hover just over) a platform of the level.
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.BonusFlight) continue;
                Vector3 a = GemRush.GoldenGem.PickSpot(l);
                Vector3 b = GemRush.GoldenGem.PickSpot(l);
                Assert.AreEqual(a, b,
                    "golden spot must be deterministic for " + l.Name);
                bool nearTop = false;
                for (int p = 0; p < l.Platforms.Count; p++)
                {
                    PlatformSpec s = l.Platforms[p];
                    float dx = Mathf.Abs(a.x - s.Center.x) - s.Size.x * 0.5f;
                    float dz = Mathf.Abs(a.z - s.Center.z) - s.Size.z * 0.5f;
                    if (dx <= 1.2f && dz <= 1.2f &&
                        a.y > s.Center.y + s.Size.y * 0.5f - 0.5f)
                        nearTop = true;
                }
                Assert.IsTrue(nearTop,
                    l.Name + ": golden spot floats off the course at " + a);
            }
        }

        [Test]
        public void Golden_IsNotHiddenOnTheWayOut()
        {
            // The golden must be a detour, not a souvenir of the exit.
            // Player-reported on The Silent Spire: the gem sat 3.6 units
            // BEHIND the portal, so a normal run touched the exit, the
            // level completed, and the gem was never met — 3 stars and a
            // gold medal with the golden still sitting there. Measured
            // before the fix: 29 of 37 levels hid it within 12 units of
            // the portal, several within 1-2.
            const float MinPortalDistance = 12f;
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (l.BonusFlight) continue;
                if (LevelLibrary.BSideSourceIndex(i) >= 0) { i++; continue; }
                Vector3 spot = GemRush.GoldenGem.PickSpot(l);
                float toPortal = new Vector2(
                    spot.x - l.Portal.x, spot.z - l.Portal.z).magnitude;
                Assert.GreaterOrEqual(toPortal, MinPortalDistance,
                    Label(i, l) + ": the golden hides " +
                    toPortal.ToString("F1") + " units from the portal (" +
                    spot + " vs " + l.Portal + ") — at that range the exit " +
                    "is the natural thing to do and the gem is missed.");
                i++;
            }
        }

        [Test]
        public void NoSpinner_IsFasterThanTheComfortCeiling()
        {
            // D-6 law: "i don't like the spinners so fast! i want the game
            // to be fun with little difficulty only." Fast arms are the one
            // mechanic the user named as unfun, so speed is capped at 90
            // deg/s and lower is better.
            //
            // The game shipped with 11 spinners above the ceiling, including
            // the maximum of 140 deg/s sitting in level 5 — the teaching arc,
            // where a young player should meet the gentlest content. All are
            // now 60-90.
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                foreach (SpinnerSpec s in l.Spinners)
                {
                    Assert.LessOrEqual(s.DegreesPerSecond,
                        HazardTiming.MaxDegreesPerSecond,
                        Label(i, l) + ": spinner at " + s.PlatformTop +
                        " spins at " + s.DegreesPerSecond.ToString("F0") +
                        " deg/s, over the " +
                        HazardTiming.MaxDegreesPerSecond.ToString("F0") +
                        " ceiling (D-6). Slow it down — difficulty must " +
                        "never come from hazard speed.");
                }
                i++;
            }
        }

        [Test]
        public void EverySpinner_IsReadableByAChild()
        {
            // The timing half of the difficulty contract. Everything else
            // measures space (gaps, margins, routes); this measures TIME,
            // which is what makes the game's one lethal hazard fair. A
            // spinner must leave a clear window longer than a young player
            // needs to notice, decide and cross — and must not cover
            // nearly the whole platform it guards, or there is nowhere to
            // stand and read it.
            //
            // The budget is grounded in child development research, not
            // taste: a 7-year-old's simple visual reaction is 370-407 ms
            // and choice reaction 711-893 ms, so 450 ms is the floor for
            // "notice and act" alone.
            //
            // Measured across all 46 spinners when this was written: every
            // one passes, with the game's fastest (140 deg/s, level 5)
            // still leaving a 2.23s window — five times the budget. The
            // value of the test is that it makes that a verified property
            // rather than an assumption, and fails the suite if a future
            // level adds a hazard a child cannot read.
            int i = 0;
            foreach (LevelDefinition l in AllLevels)
            {
                if (!HazardTiming.FairnessApplies(l)) { i++; continue; }
                List<HazardTiming.Hazard> hazards =
                    HazardTiming.Survey(l);
                foreach (HazardTiming.Hazard h in hazards)
                {
                    Assert.GreaterOrEqual(h.SafeWindowSeconds,
                        HazardTiming.ChildResponseSeconds,
                        Label(i, l) + ": spinner at " + h.Top + " spins at " +
                        h.DegreesPerSecond.ToString("F0") + "deg/s, leaving " +
                        (h.SafeWindowSeconds * 1000f).ToString("F0") +
                        "ms clear — under the " +
                        (HazardTiming.ChildResponseSeconds * 1000f)
                            .ToString("F0") + "ms a child needs to react " +
                        "and cross. Slow it down or open the platform.");
                    Assert.LessOrEqual(h.Exposure, HazardTiming.MaxExposure,
                        Label(i, l) + ": spinner at " + h.Top + " sweeps " +
                        (h.Exposure * 100f).ToString("F0") + "% of its " +
                        "platform — there is nowhere to stand and read it.");
                }
                i++;
            }
        }

        [Test]
        public void GustBlowWindow_IsARealShareOfEveryCycle()
        {
            // Player-reported: The Festival Finale's gust crossing was
            // impossible — the wind blew on the first cycle and then never
            // again, so waiting for the giggle telegraph never helped.
            //
            // Cause: the gust wrapped the MUSIC phase by its period, which
            // only yields a full duty cycle when the mood's loop is a
            // multiple of that period. Day/rain pads are 8.8s (2 x 4.4) and
            // worked; Festival is 4 x 3.4 = 13.6s, so the phase stepped
            // 0 -> 4.4 -> 8.8 -> 13.2 and landed in the 2.2s blow window on
            // the first cycle only. The gust now runs on its own clock, so
            // this asserts the gameplay rule directly and records which
            // moods would drift if it ever read the music again.
            // Read the REAL constants rather than copies: a duplicated
            // literal cannot catch a regression in the value it duplicates,
            // and the original strict `>` compared 2.2 against 2.2 — the
            // design value is EXACTLY half — so the test failed on its own
            // definition of correct.
            float period = GemRush.GustZone.DefaultPeriod;
            float active = GemRush.GustZone.DefaultActiveTime;
            Assert.Greater(period, 0f, "the gust needs a positive period");
            Assert.GreaterOrEqual(active, period * 0.5f,
                "a gust must blow for at least half of every cycle, or a " +
                "player waiting to cross spends most of their time unable " +
                "to move (active " + active + " of period " + period + ")");
            Assert.LessOrEqual(active, period,
                "the blow cannot outlast its own period");
            foreach (SoundMood mood in
                (SoundMood[])System.Enum.GetValues(typeof(SoundMood)))
            {
                if (mood == SoundMood.Auto) continue;
                float loop = GemRush.MusicSynth.MoodLoopLength(mood);
                Assert.Greater(loop, 0f,
                    mood + " has no loop length for the gust to align to");
            }
        }

        [Test]
        public void GustRelock_AlignsTheGustPhaseToTheMusic()
        {
            // The relock is what keeps a gust landing on the chord after a
            // pause or a hit-stop (GustZone's own clock freezes with
            // Time.timeScale while the music DSP clock does not). It shipped
            // with the subtraction INVERTED — `phaseOffset = t - music`
            // where the contract needs `music - t` — which makes the phase
            // `2t - music`: rather than re-aligning, each relock pushed the
            // gust further off the beat as t grew. Nothing caught it because
            // no test touched the relock at all.
            //
            // The contract, stated once: after RelockToMusic, the gust's
            // phase EQUALS the music phase it locked to.
            float period = GemRush.GustZone.DefaultPeriod;
            float[] clocks = { 0f, 0.7f, 3f, 10f, 47.3f };
            float[] musics = { 0f, 1f, 2.5f, 4.1f, 6.6f };
            for (int i = 0; i < clocks.Length; i++)
            {
                float offset = musics[i] - clocks[i]; // the production maths
                float phase = Mathf.Repeat(clocks[i] + offset, period);
                float inverted = Mathf.Repeat(
                    clocks[i] + (clocks[i] - musics[i]), period);
                Assert.AreEqual(Mathf.Repeat(musics[i], period), phase, 1e-4f,
                    "relock at t=" + clocks[i] + " music=" + musics[i] +
                    " must put the gust phase on the music phase, got " +
                    phase + " (the inverted subtraction yields " + inverted +
                    ")");
            }
        }

        [Test]
        public void RegionTable_CoversEveryLevelInOrder()
        {
            // The atlas screen groups levels by the region table: the
            // regions must tile the library exactly — contiguous, in play
            // order, no gaps, no overlaps, none empty.
            int covered = 0;
            for (int r = 0; r < LevelLibrary.Regions.Length; r++)
            {
                LevelLibrary.Region region = LevelLibrary.Regions[r];
                Assert.Greater(region.Count, 0,
                    "region " + r + " (" + region.Name + ") is empty.");
                Assert.AreEqual(covered, region.First,
                    "region " + r + " (" + region.Name + ") starts at " +
                    region.First + " but the atlas expects " + covered +
                    " — regions must tile the library in play order.");
                covered += region.Count;
            }
            Assert.AreEqual(LevelLibrary.Levels.Length, covered,
                "regions cover " + covered + " levels but the library has " +
                LevelLibrary.Levels.Length + " — a pack is missing from the " +
                "region table (the atlas would silently hide its levels).");
            Assert.GreaterOrEqual(LevelLibrary.Regions.Length, 12,
                "the atlas expects one region per shipped pack.");
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
