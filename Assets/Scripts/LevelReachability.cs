using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Deterministic spawn-to-portal reachability analysis for a level.
    ///
    /// The older audit heuristics in LevelAuditTests check that every
    /// platform has SOME neighbour; this engine answers the sharper
    /// question the game actually asks: starting from the spawn, under the
    /// real movement physics, can Pip reach the portal? It builds a graph
    /// of standable surfaces (platforms, mover sweeps, echo bridges,
    /// aurora ribbons, see-saw planks), wires jump-arc edges between them
    /// with the exact constants from PlayerController and the project's
    /// gravity, adds the special movement edges (updraft columns, gust
    /// lanes, bounce pads, mirror doors), then breadth-first searches from
    /// the spawn's surface to the portal's surface.
    ///
    /// A failure here means a level (or an island in it) is genuinely
    /// impossible — fix the LEVEL, never the model.
    public static class LevelReachability
    {
        // ---- Movement constants, mirrored from the live components ----
        public const float Gravity = 9.81f;         // DynamicsManager
        public const float JumpVelocity = 9.5f;     // PlayerController.jumpVelocity
        public const float RunSpeed = 8f;           // PlayerController.moveSpeed
        public const float BounceVelocity = 13f;    // BouncePad.LaunchVelocity

        // Safety margins so a jump the model calls "possible" is possible
        // in hand-too: not every takeoff has a full runway, air control
        // blends in over a few frames, and real players mis-time hops.
        const float SpeedSafety = 0.95f;   // horizontal speed at takeoff
        const float MaxRiseMargin = 0.15f; // rise headroom below the apex
        const float AirtimeCap = 3.2f;     // long-drop generosity clamp
        const float LandExpand = 0.3f;     // feet OverlapSphere radius
        const float TakeoffExpand = 0.8f;  // coyote-time walk-off grace
        const float ApexForRise = 0.5f * JumpVelocity * JumpVelocity / Gravity;

        /// One standable surface: the top face's world center + XZ half
        /// extents. Group: surfaces of one moving body (a mover's sweep
        /// samples, a ribbon's path samples) share a group and are always
        /// mutually reachable — you ride the body between its samples.
        public struct Top
        {
            public Vector3 Center;
            public Vector2 Half;
            public int Group;
            public string Kind;
            /// Echo bridges: which bell solidifies them (only meaningful
            /// when Kind is "echo bridge"; struct default 0 would alias
            /// bell 0).
            public int BellIndex;

            public float TopY { get { return Center.y; } }
        }

        /// One platform-to-platform hop and how much room it leaves the
        /// player. Margin = 1 − gap/range: 0.0 means the gap is exactly
        /// Pip's maximum jump ("pixel-perfect" — the single most
        /// frustrating construct in the platformer taxonomy), 0.5 means
        /// the gap is half of what he can clear.
        public struct Hop
        {
            public string From;
            public string To;
            public float Gap;
            public float Range;
            public float Rise;
            public float Margin;
            /// True when the player is not forced through this jump: either
            /// endpoint has another reachable neighbour (a mover beside the
            /// island, a parallel route), so a tight margin here is the
            /// designer's intended challenge and a child can ride instead.
            /// False means this hop is the only way through — a tight
            /// margin there is a trap, not a test.
            public bool Optional;
        }

        /// Outcome of one level's analysis. Problems block finishing
        /// (or float content nobody can ever touch); warnings are softer.
        public class Report
        {
            public string LevelName;
            public bool PortalReachable;
            public int TopCount;
            public int ReachableCount;
            public List<int> Path = new List<int>();
            public List<string> Problems = new List<string>();
            public List<string> Warnings = new List<string>();
            /// Every jump edge between platform-like surfaces, with the
            /// tightest first — the difficulty instrument.
            public List<Hop> Hops = new List<Hop>();
            /// Just the hops the completion route actually takes, in
            /// order: the jumps a player is genuinely asked to make.
            public List<Hop> RouteHops = new List<Hop>();
            /// The tightest hop on the level (Margin nearest zero).
            public float TightestMargin = 1f;
            public bool Bad
            {
                get { return !PortalReachable || Problems.Count > 0; }
            }
        }

        // ------------------------------------------------------------------
        // Entry point
        // ------------------------------------------------------------------

        public static Report Analyze(LevelDefinition level)
        {
            Report r = new Report();
            r.LevelName = level.Name;

            if (level.BonusFlight)
            {
                // Gloomfang flights: no gravity, no fall deaths, free 3D
                // drift. Nothing to be unreachable.
                r.PortalReachable = true;
                r.ReachableCount = 1;
                r.TopCount = 1;
                return r;
            }

            if (level.EchoBridges.Count > 0 && level.Bells.Count == 0)
                r.Problems.Add("level has echo bridges but no bell to " +
                    "solidify them — the bridge can never exist.");

            List<Top> tops = BuildTops(level);
            r.TopCount = tops.Count;

            // Kill-plane discipline: nothing standable may sit at or below
            // the death plane.
            for (int i = 0; i < tops.Count; i++)
            {
                if (tops[i].TopY <= level.KillY + 0.5f)
                    r.Problems.Add("top " + Describe(tops[i]) +
                        " sits at/below KillY " + level.KillY +
                        " — landing there dies.");
            }

            // ---- Graph edges ----
            List<List<int>> adj = new List<List<int>>();
            for (int i = 0; i < tops.Count; i++) adj.Add(new List<int>());
            for (int i = 0; i < tops.Count; i++)
            {
                for (int j = i + 1; j < tops.Count; j++)
                {
                    bool linked;
                    if (tops[i].Group == tops[j].Group && tops[i].Group >= 0)
                        linked = true; // one moving body: ride between samples
                    else
                        linked = JumpLinks(level, tops[i], tops[j]);
                    if (linked) { adj[i].Add(j); adj[j].Add(i); }
                    // Measure the hop whether or not it is linked: the
                    // margin is a difficulty fact, not a connectivity one.
                    RecordHop(r, tops[i], tops[j]);
                }
            }
            AddWindEdges(level, tops, adj);
            AddGustEdges(level, tops, adj);
            AddBouncePadEdges(level, tops, adj);
            AddMirrorDoorEdges(level, tops, adj, r);

            // ---- Start and goal ----
            List<int> starts = SnapTops(tops, level.Spawn, 1.2f, -1f, 3.5f);
            if (starts.Count == 0)
            {
                r.Problems.Add("spawn " + level.Spawn +
                    " is not on/near any standable surface.");
                return r;
            }

            // The portal trigger is 2.4 x 3.4 x 1.6 with its base on a
            // platform top: walking in needs the capsule within ~1.7 xz.
            List<int> goals = SnapTops(tops, level.Portal, 1.7f, -1.5f, 2f);
            if (goals.Count == 0)
            {
                // Generous fallback: a jump can carry Pip through the
                // trigger mid-arc. The older audit allows 4.2 here.
                goals = SnapTops(tops, level.Portal, 4.2f, -1.5f, 4.5f);
                if (goals.Count > 0)
                    r.Warnings.Add("portal is off every platform; only " +
                        "jump-reach touches it (is the arch meant to float?)");
            }
            if (goals.Count == 0)
            {
                r.Problems.Add("portal " + level.Portal +
                    " floats beyond all jump reach — nothing can touch it.");
                return r;
            }

            // ---- Echo lock ----
            // A bridge exists only while its bell's tone rings, and a bell
            // can only be rung by standing at it. Solve the fixed point:
            // begin with every bridge intangible, ring whatever bells are
            // reachable, allow those bridges, re-run, until nothing new
            // lights up. A bell still outside the set at the fixed point is
            // one nobody can ever reach — the route through it needs itself.
            bool[] echoAllowed = new bool[tops.Count];
            bool[] seen = Bfs(tops.Count, adj, starts, echoAllowed, tops);
            while (true)
            {
                bool changed = false;
                for (int b = 0; b < level.Bells.Count; b++)
                {
                    int bt = SnapTop(tops, level.Bells[b].PlatformTop,
                        1f, -0.5f, 1.5f);
                    if (bt < 0 || !seen[bt]) continue;
                    for (int i = 0; i < tops.Count; i++)
                        if (tops[i].Kind == "echo bridge" &&
                            tops[i].BellIndex == level.Bells[b].Index &&
                            !echoAllowed[i])
                        {
                            echoAllowed[i] = true;
                            changed = true;
                        }
                }
                if (!changed) break;
                seen = Bfs(tops.Count, adj, starts, echoAllowed, tops);
            }
            for (int b = 0; b < level.Bells.Count; b++)
            {
                int bt = SnapTop(tops, level.Bells[b].PlatformTop,
                    1f, -0.5f, 1.5f);
                if (bt < 0 || !seen[bt])
                    r.Problems.Add("bell " + level.Bells[b].Index +
                        " can never be rung — it is not reachable even with " +
                        "every echo bridge whose bell is reachable made " +
                        "solid (circular echo lock, or the bell floats).");
            }

            // The tone must outlast the crossing: ring to the far edge of
            // each of its bridges at run speed, plus reaction time.
            for (int b = 0; b < level.Bells.Count; b++)
            {
                BellSpec bell = level.Bells[b];
                for (int e = 0; e < level.EchoBridges.Count; e++)
                {
                    EchoBridgeSpec br = level.EchoBridges[e];
                    if (br.BellIndex != bell.Index) continue;
                    float far = FarthestCornerXz(bell.PlatformTop, br.Center,
                        new Vector2(br.Size.x * 0.5f, br.Size.z * 0.5f));
                    float need = far / RunSpeed + 1.5f;
                    if (bell.ToneSeconds < need)
                        r.Problems.Add("bell " + bell.Index + "'s tone (" +
                            bell.ToneSeconds + "s) expires before its echo " +
                            "bridge at " + br.Center + " can be crossed " +
                            "(~" + need.ToString("F1") + "s at run speed) — " +
                            "the echo drops mid-crossing.");
                }
            }

            // ---- Portal ----
            bool won = false;
            int reachedGoal = -1;
            for (int g = 0; g < goals.Count; g++)
                if (seen[goals[g]]) { won = true; reachedGoal = goals[g]; break; }

            r.ReachableCount = CountTrue(seen);
            r.PortalReachable = won;
            if (!won)
            {
                r.Problems.Add("portal is on a surface the spawn cannot " +
                    "reach: " + r.ReachableCount + " of " + tops.Count +
                    " surfaces are on the spawn's island.");
                List<int> stranded = new List<int>();
                for (int i = 0; i < tops.Count; i++)
                    if (!seen[i]) stranded.Add(i);
                r.Warnings.Add("unreachable surfaces: " +
                    DescribeSome(tops, stranded));
            }
            else
            {
                // Reconstruct one path for debugging/reporting.
                int[] prev = new int[tops.Count];
                Bfs(tops.Count, adj, starts, echoAllowed, tops, prev);
                int goal = reachedGoal;
                for (int at = goal; at >= 0; at = prev[at]) r.Path.Add(at);
                r.Path.Reverse();
                // The jumps this route actually asks for — the only hops
                // a difficulty band should be judged on.
                for (int k = 1; k < r.Path.Count; k++)
                {
                    int ai = r.Path[k - 1];
                    int bi = r.Path[k];
                    Top from = tops[ai];
                    Top to = tops[bi];
                    if (!IsJumpSurface(from) || !IsJumpSurface(to)) continue;
                    if (from.Group == to.Group && from.Group >= 0) continue;
                    float rise = to.TopY - from.TopY;
                    float range = JumpRange(JumpVelocity, rise);
                    if (range <= 0f) continue;
                    float gap = RectDistXz(from, to, TakeoffExpand, LandExpand);
                    if (gap > range) continue; // crossed by a ride
                    // Forced or optional: a hop is OPTIONAL only if the
                    // player can actually skip it — there is a route from
                    // this hop's start to its landing that does not use
                    // this hop, and is not meaningfully longer. Merely
                    // having a second neighbour is not a bypass (that is
                    // just the next link in the same chain), which is why
                    // a plain neighbour count marks every chain hop as
                    // optional and hides the traps.
                    bool optional = HasBypass(adj, tops, ai, bi);
                    r.RouteHops.Add(new Hop
                    {
                        From = Describe(from),
                        To = Describe(to),
                        Gap = gap,
                        Range = range,
                        Rise = rise,
                        Margin = 1f - gap / range,
                        Optional = optional
                    });
                }
            }

            CheckCheckpoints(level, tops, seen, r);
            CheckGems(level, tops, seen, r);
            return r;
        }

        // ------------------------------------------------------------------
        // Surfaces
        // ------------------------------------------------------------------

        static List<Top> BuildTops(LevelDefinition l)
        {
            List<Top> tops = new List<Top>();
            int group = 0;

            for (int i = 0; i < l.Platforms.Count; i++)
            {
                PlatformSpec p = l.Platforms[i];
                tops.Add(new Top
                {
                    Center = new Vector3(p.Center.x,
                        p.Center.y + p.Size.y * 0.5f, p.Center.z),
                    Half = new Vector2(p.Size.x * 0.5f, p.Size.z * 0.5f),
                    Group = -1,
                    Kind = "platform"
                });
            }

            for (int i = 0; i < l.Movers.Count; i++)
            {
                MoverSpec m = l.Movers[i];
                // Sample the sweep; samples share a group (you ride it).
                for (int s = 0; s <= 4; s++)
                {
                    float t = s / 4f;
                    tops.Add(new Top
                    {
                        Center = new Vector3(
                            m.Center.x + m.Offset.x * t,
                            m.Center.y + m.Size.y * 0.5f + m.Offset.y * t,
                            m.Center.z + m.Offset.z * t),
                        Half = new Vector2(m.Size.x * 0.5f, m.Size.z * 0.5f),
                        Group = group,
                        Kind = "mover"
                    });
                }
                group++;
            }

            for (int i = 0; i < l.EchoBridges.Count; i++)
            {
                EchoBridgeSpec b = l.EchoBridges[i];
                tops.Add(new Top
                {
                    Center = new Vector3(b.Center.x,
                        b.Center.y + b.Size.y * 0.5f, b.Center.z),
                    Half = new Vector2(b.Size.x * 0.5f, b.Size.z * 0.5f),
                    Group = -1,
                    Kind = "echo bridge",
                    BellIndex = b.BellIndex
                });
            }

            for (int i = 0; i < l.AuroraRibbons.Count; i++)
            {
                AuroraRibbonSpec rb = l.AuroraRibbons[i];
                // Samples along the travel; sway folded into the extents.
                Vector2 half = new Vector2(
                    rb.Size.x * 0.5f + rb.Sway, rb.Size.z * 0.5f + rb.Sway);
                for (int s = 0; s <= 4; s++)
                {
                    float t = s / 4f;
                    tops.Add(new Top
                    {
                        Center = new Vector3(
                            rb.Center.x + rb.Travel.x * t,
                            rb.Center.y + rb.Size.y * 0.5f + rb.Travel.y * t,
                            rb.Center.z + rb.Travel.z * t),
                        Half = half,
                        Group = group,
                        Kind = "aurora ribbon"
                    });
                }
                group++;
            }

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
                        : new Vector2(s.Width * 0.5f, s.Length * 0.5f),
                    Group = -1,
                    Kind = "see-saw"
                });
            }

            return tops;
        }

        static string Describe(Top t)
        {
            return t.Kind + "@" + t.Center;
        }

        static string DescribeSome(List<Top> tops, List<int> idx)
        {
            int show = Mathf.Min(idx.Count, 6);
            List<string> parts = new List<string>();
            for (int i = 0; i < show; i++)
                parts.Add(Describe(tops[idx[i]]));
            if (idx.Count > show) parts.Add("…" + (idx.Count - show) + " more");
            return string.Join("; ", parts.ToArray());
        }

        // ------------------------------------------------------------------
        // Geometry + physics
        // ------------------------------------------------------------------

        /// Breadth-first search over the surfaces. Echo bridges count only
        /// while `echoAllowed` says their bell has rung; fills `prev` with
        /// the BFS tree when non-null (−1 at the roots).
        static bool[] Bfs(int n, List<List<int>> adj, List<int> starts,
            bool[] echoAllowed, List<Top> tops, int[] prev = null)
        {
            bool[] seen = new bool[n];
            Queue<int> frontier = new Queue<int>();
            foreach (int s in starts)
            {
                if (seen[s]) continue;
                seen[s] = true;
                if (prev != null) prev[s] = -1;
                frontier.Enqueue(s);
            }
            while (frontier.Count > 0)
            {
                int cur = frontier.Dequeue();
                for (int k = 0; k < adj[cur].Count; k++)
                {
                    int next = adj[cur][k];
                    if (seen[next]) continue;
                    if (tops[next].Kind == "echo bridge" && !echoAllowed[next])
                        continue;
                    seen[next] = true;
                    if (prev != null) prev[next] = cur;
                    frontier.Enqueue(next);
                }
            }
            return seen;
        }

        /// XZ distance from a point to the farthest corner of a rect — the
        /// walk a bell tone must outlast.
        static float FarthestCornerXz(Vector3 p, Vector3 c, Vector2 half)
        {
            float best = 0f;
            for (int cx = -1; cx <= 1; cx += 2)
                for (int cz = -1; cz <= 1; cz += 2)
                {
                    float dx = c.x + half.x * cx - p.x;
                    float dz = c.z + half.y * cz - p.z;
                    best = Mathf.Max(best,
                        Mathf.Sqrt(dx * dx + dz * dz));
                }
            return best;
        }

        // ---- Difficulty bands (research-derived) ----
        // A hop whose gap is near Pip's maximum is the "pixel-perfect
        // jump" the platformer-design literature names as the single most
        // frustrating construct: one solution, no error tolerance, and for
        // a young player indistinguishable from an impossible gap. Two
        // bands, measured as margin = 1 − gap/range:
        //   < TightMargin       — frustrating at any age
        //   < ChildMargin       — too tight in the teaching regions (I–VII)
        // Regions VIII+ are allowed the tighter band so the last third of
        // the game can still bite.
        public const float TightMargin = 0.15f;
        public const float ChildMargin = 0.25f;
        public const int ChildRegionLevelCount = 19; // regions I-VII

        /// Hops that fail the band for their level's region. Measured on
        /// the ROUTE, not on every pair: with 40-100 surfaces per level a
        /// pairwise sweep flags hundreds of diagonal non-jumps that no
        /// player ever attempts, which drowns the real signal. The route
        /// is the sequence of hops the shortest completion actually takes,
        /// so that is what gets measured.
        public static List<Hop> OffendingHops(LevelDefinition level, int index)
        {
            Report r = Analyze(level);
            float floor = index < ChildRegionLevelCount
                ? ChildMargin : TightMargin;
            List<Hop> bad = new List<Hop>();
            for (int i = 0; i < r.RouteHops.Count; i++)
                if (r.RouteHops[i].Margin < floor) bad.Add(r.RouteHops[i]);
            bad.Sort(delegate (Hop x, Hop y)
            {
                return x.Margin.CompareTo(y.Margin);
            });
            return bad;
        }

        /// XZ distance between two axis-aligned rects (0 when overlapping).
        static float RectDistXz(Vector3 cA, Vector2 hA, Vector3 cB, Vector2 hB)
        {
            float dx = Mathf.Abs(cA.x - cB.x) - (hA.x + hB.x);
            float dz = Mathf.Abs(cA.z - cB.z) - (hA.y + hB.y);
            if (dx <= 0f && dz <= 0f) return 0f;
            return Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f)
                            + Mathf.Max(dz, 0f) * Mathf.Max(dz, 0f));
        }

        /// Measure one hop for the difficulty report. Only platform-like
        /// surfaces (the green decks and the movers that ferry between
        /// them) are measured: bridges, ribbons and see-saws are rides,
        /// not jumps, and including them would flood the report with
        /// near-zero gaps that no one has to jump.
        static void RecordHop(Report r, Top a, Top b)
        {
            bool aJump = IsJumpSurface(a);
            bool bJump = IsJumpSurface(b);
            if (!aJump || !bJump) return;
            if (a.Group == b.Group && a.Group >= 0) return; // one body

            float rise = b.TopY - a.TopY;
            float range = JumpRange(JumpVelocity, rise);
            if (range <= 0f) return; // not a jump candidate at all
            float gap = RectDistXz(a, b, TakeoffExpand, LandExpand);
            if (gap > range) return; // not a crossing anyone would attempt
            float margin = 1f - gap / range;
            r.Hops.Add(new Hop
            {
                From = Describe(a),
                To = Describe(b),
                Gap = gap,
                Range = range,
                Rise = rise,
                Margin = margin
            });
            if (margin < r.TightestMargin) r.TightestMargin = margin;
        }

        static bool IsJumpSurface(Top t)
        {
            return t.Kind == "platform" || t.Kind == "mover";
        }

        /// True when the player can get from A to B WITHOUT this hop —
        /// a genuine bypass (a mover beside the island, a parallel lane).
        /// Tested by removing the edge and re-searching: that is the only
        /// definition that distinguishes "there is another way" from
        /// "there is another platform along the same chain".
        static bool HasBypass(List<List<int>> adj, List<Top> tops,
            int a, int b)
        {
            Queue<int> frontier = new Queue<int>();
            bool[] seen = new bool[tops.Count];
            seen[a] = true;
            frontier.Enqueue(a);
            while (frontier.Count > 0)
            {
                int cur = frontier.Dequeue();
                for (int k = 0; k < adj[cur].Count; k++)
                {
                    int next = adj[cur][k];
                    // Skip the hop under test, both directions (the graph
                    // is undirected; crossing it either way is the hop).
                    if ((cur == a && next == b) || (cur == b && next == a))
                        continue;
                    if (seen[next]) continue;
                    if (next == b) return true;
                    seen[next] = true;
                    frontier.Enqueue(next);
                }
            }
            return false;
        }

        /// Tightest-first, for reporting.
        public static List<Hop> TightestHops(Report r, int count)
        {
            List<Hop> sorted = new List<Hop>(r.Hops);
            sorted.Sort(delegate (Hop x, Hop y)
            {
                return x.Margin.CompareTo(y.Margin);
            });
            if (sorted.Count > count) sorted.RemoveRange(count,
                sorted.Count - count);
            return sorted;
        }

        static float RectDistXz(Top a, Top b, float expandA, float expandB)
        {
            return RectDistXz(a.Center,
                new Vector2(a.Half.x + expandA, a.Half.y + expandA),
                b.Center,
                new Vector2(b.Half.x + expandB, b.Half.y + expandB));
        }

        static bool RectContains(Top t, Vector3 p, float expand,
            float yMinAboveTop, float yMaxAboveTop)
        {
            float dy = p.y - t.TopY;
            if (dy < yMinAboveTop || dy > yMaxAboveTop) return false;
            return Mathf.Abs(p.x - t.Center.x) <= t.Half.x + expand &&
                   Mathf.Abs(p.z - t.Center.z) <= t.Half.y + expand;
        }

        /// Horizontal range of a jump landing at rise `rise` (negative =
        /// drop). Return launch speed times the descending-branch time.
        static float JumpRange(float launchVelocity, float rise)
        {
            float apex = 0.5f * launchVelocity * launchVelocity / Gravity;
            if (rise > apex - MaxRiseMargin) return -1f;
            float disc = launchVelocity * launchVelocity
                - 2f * Gravity * rise;
            if (disc < 0f) return -1f;
            float t = (launchVelocity + Mathf.Sqrt(disc)) / Gravity;
            return SpeedSafety * RunSpeed * Mathf.Min(t, AirtimeCap);
        }

        /// True when a running jump from surface a can land on surface b.
        static bool JumpLinks(LevelDefinition l, Top a, Top b)
        {
            float rise = b.TopY - a.TopY;
            float range = JumpRange(JumpVelocity, rise);
            if (range < 0f) return false;
            return RectDistXz(a, b, TakeoffExpand, LandExpand) <= range;
        }

        // ------------------------------------------------------------------
        // Special edges: wind, gusts, pads, doors
        // ------------------------------------------------------------------

        static void ConnectClique(List<int> members, List<List<int>> adj)
        {
            for (int i = 0; i < members.Count; i++)
                for (int j = i + 1; j < members.Count; j++)
                {
                    adj[members[i]].Add(members[j]);
                    adj[members[j]].Add(members[i]);
                }
        }

        /// An updraft is a shared elevator AND a launcher. Standing in the
        /// column blends Pip's rise toward the lift strength, so he pops
        /// out of the top still climbing (~0.9x lift on a short column;
        /// 0.8 here, deliberately shy) and then arcs ballistically onto
        /// the ledge the wind points at. Surfaces in/beside the column
        /// form a clique (enter, rise, hop out); the column's top
        /// rectangle additionally launches to every top within pad-like
        /// range of that ejection arc.
        static void AddWindEdges(LevelDefinition l, List<Top> tops,
            List<List<int>> adj)
        {
            for (int w = 0; w < l.WindZones.Count; w++)
            {
                WindSpec wind = l.WindZones[w];
                Vector3 wc = wind.Center;
                Vector2 whalf = new Vector2(wind.Size.x * 0.5f,
                    wind.Size.z * 0.5f);
                float bottom = wind.Center.y - wind.Size.y * 0.5f;
                float top = wind.Center.y + wind.Size.y * 0.5f;
                List<int> members = new List<int>();
                for (int i = 0; i < tops.Count; i++)
                {
                    float y = tops[i].TopY;
                    if (y < bottom - 1f || y > top + 14f) continue;
                    if (RectDistXz(tops[i].Center, tops[i].Half,
                            wc, whalf + new Vector2(2f, 2f)) <= 0.01f)
                        members.Add(i);
                }
                ConnectClique(members, adj);

                // The ejection arc off the column's top.
                float launch = 0.8f * wind.Lift;
                Top mouth = new Top
                {
                    Center = new Vector3(wc.x, top, wc.z),
                    Half = whalf + new Vector2(1.5f, 1.5f), // drift while rising
                    Group = -1,
                    Kind = "updraft mouth"
                };
                for (int j = 0; j < tops.Count; j++)
                {
                    if (members.Contains(j)) continue;
                    float rise = tops[j].TopY - top;
                    float range = JumpRange(launch, rise);
                    if (range < 0f) continue;
                    if (RectDistXz(mouth, tops[j], 0f, LandExpand) > range)
                        continue;
                    // Ride + eject links every member to the landing,
                    // and the landing can hop back into the wind.
                    for (int m = 0; m < members.Count; m++)
                    {
                        adj[members[m]].Add(j);
                        adj[j].Add(members[m]);
                    }
                }
            }
        }

        /// A gust delivers one surface to another only if the ride
        /// actually lands: jump in from the takeoff (rise ~2.2 units
        /// before the wind damps the jump), get carried at gust strength
        /// while the box holds you near its lift, then fall ballistically
        /// once the box ends. The delivery is real only if the arc is
        /// still above the target's top when it reaches the target's near
        /// edge — a lane that lets go short dumps Pip into the void (the
        /// Silent Spire gap; this model is what keeps that class of bug
        /// out of the library).
        static void AddGustEdges(LevelDefinition l, List<Top> tops,
            List<List<int>> adj)
        {
            const float entryRise = 2.2f; // jump into the lane: rise before damping
            const float carrySpeed = 8f;  // gust strength IS the ride speed

            for (int g = 0; g < l.Gusts.Count; g++)
            {
                GustSpec gust = l.Gusts[g];
                Vector3 dir = gust.Direction.normalized;
                bool alongX = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);
                float sign = alongX ? Mathf.Sign(dir.x) : Mathf.Sign(dir.z);
                // The wind's REAL vertical hold-up: the entry blend fights
                // gravity continuously, so the ride settles at lift − g/4
                // (PlayerController lerps vel.y toward gustLift at 4/s
                // while useGravity pulls at 9.81). A lift under ~2.5 still
                // sinks the rider — lift 1.2 was how the Silent Spire lane
                // kept killing people on a "helpful" wind.
                float exitVy = gust.Lift - Gravity / 4f;
                // The exit plane is the box's far face across the blow.
                float exitPlane = alongX
                    ? gust.Center.x + sign * gust.Size.x * 0.5f
                    : gust.Center.z + sign * gust.Size.z * 0.5f;

                List<int> members = new List<int>();
                for (int i = 0; i < tops.Count; i++)
                {
                    Top t = tops[i];
                    if (t.TopY < gust.Center.y - gust.Size.y * 0.5f - 4f ||
                        t.TopY > gust.Center.y + gust.Size.y * 0.5f + 8f)
                        continue;
                    if (RectDistXz(t.Center, t.Half, gust.Center,
                            new Vector2(gust.Size.x * 0.5f + 2.5f,
                                gust.Size.z * 0.5f + 2.5f)) > 0.01f)
                        continue;
                    members.Add(i);
                }

                Vector2 boxHalf = new Vector2(gust.Size.x * 0.5f,
                    gust.Size.z * 0.5f);

                // Directed: the wind only blows one way. Any surface can be
                // the target — the delivery arc itself is the gate, so a
                // lane that ends short of the next platform is a FAIL, and
                // a long lift-boosted glide that genuinely reaches is not.
                for (int a = 0; a < members.Count; a++)
                {
                    for (int b = 0; b < tops.Count; b++)
                    {
                        if (members[a] == b) continue;
                        Top from = tops[members[a]];
                        Top to = tops[b];
                        bool insideBox = RectDistXz(to.Center, to.Half,
                            gust.Center, boxHalf) <= 0.01f;
                        // Distance from the exit plane to the target's
                        // near edge along the blow (0 = carried over it).
                        float near, raw;
                        if (alongX)
                        {
                            near = to.Center.x - sign * to.Half.x;
                            raw = (near - exitPlane) * sign;
                        }
                        else
                        {
                            near = to.Center.z - sign * to.Half.y;
                            raw = (near - exitPlane) * sign;
                        }
                        if (raw < 0f && !insideBox) continue; // upwind
                        float d = Mathf.Max(0f, raw);
                        float t = d / carrySpeed;
                        float y = from.TopY + entryRise + exitVy * t
                            - 0.5f * Gravity * t * t;
                        if (y >= to.TopY - 0.5f)
                            adj[members[a]].Add(b);
                    }
                }
            }
        }

        /// A bounce pad supercharges its host surface: launch velocity 13
        /// reaches ~8.6 units of rise and ~20 units of gap.
        static void AddBouncePadEdges(LevelDefinition l, List<Top> tops,
            List<List<int>> adj)
        {
            for (int p = 0; p < l.BouncePads.Count; p++)
            {
                Vector3 pad = l.BouncePads[p];
                int host = -1;
                for (int i = 0; i < tops.Count && host < 0; i++)
                    if (RectContains(tops[i], pad, 0.6f, -0.4f, 0.4f))
                        host = i;
                if (host < 0)
                {
                    // Not registered as a problem here; the pad's platform
                    // may itself be an echo bridge the pad list predates.
                    continue;
                }
                for (int j = 0; j < tops.Count; j++)
                {
                    if (j == host) continue;
                    float rise = tops[j].TopY - tops[host].TopY;
                    float range = JumpRange(BounceVelocity, rise);
                    if (range < 0f) continue;
                    if (RectDistXz(tops[host], tops[j],
                            TakeoffExpand, LandExpand) <= range)
                    {
                        adj[host].Add(j);
                        adj[j].Add(host);
                    }
                }
            }
        }

        /// Mirror doors teleport: the surface under door A and the surface
        /// under door B are one hop apart (exit lands just in front of the
        /// twin).
        static void AddMirrorDoorEdges(LevelDefinition l, List<Top> tops,
            List<List<int>> adj, Report r)
        {
            for (int d = 0; d < l.MirrorDoors.Count; d++)
            {
                int a = SnapTop(tops, l.MirrorDoors[d].DoorA, 2f, -1f, 1f);
                int b = SnapTop(tops, l.MirrorDoors[d].DoorB, 2f, -1f, 1f);
                if (a >= 0 && b >= 0)
                {
                    if (a != b) { adj[a].Add(b); adj[b].Add(a); }
                }
                else
                    r.Warnings.Add("mirror door " + d + " (A " +
                        l.MirrorDoors[d].DoorA + ", B " +
                        l.MirrorDoors[d].DoorB + ") has a side with no " +
                        "surface under it — the exit may drop into void.");
            }
        }

        static int SnapTop(List<Top> tops, Vector3 p, float expand,
            float yMinAboveTop, float yMaxAboveTop)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < tops.Count; i++)
            {
                if (!RectContains(tops[i], p, expand,
                        yMinAboveTop, yMaxAboveTop)) continue;
                float d = RectDistXz(tops[i].Center, tops[i].Half,
                    p, Vector2.zero);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        static List<int> SnapTops(List<Top> tops, Vector3 p, float expand,
            float yMinAboveTop, float yMaxAboveTop)
        {
            List<int> found = new List<int>();
            for (int i = 0; i < tops.Count; i++)
                if (RectContains(tops[i], p, expand,
                        yMinAboveTop, yMaxAboveTop))
                    found.Add(i);
            return found;
        }

        // ------------------------------------------------------------------
        // Soft checks: checkpoints and gems
        // ------------------------------------------------------------------

        static void CheckCheckpoints(LevelDefinition l, List<Top> tops,
            bool[] seen, Report r)
        {
            for (int c = 0; c < l.Checkpoints.Count; c++)
            {
                Vector3 cp = l.Checkpoints[c];
                int at = SnapTop(tops, cp, 1f, -0.5f, 1.5f);
                if (at < 0)
                    r.Warnings.Add("checkpoint " + c + " at " + cp +
                        " is not on any standable surface.");
                else if (!seen[at])
                    r.Warnings.Add("checkpoint " + c + " at " + cp +
                        " sits on a surface the spawn cannot reach.");
            }
        }

        static void CheckGems(LevelDefinition l, List<Top> tops,
            bool[] seen, Report r)
        {
            for (int g = 0; g < l.Gems.Count; g++)
            {
                Vector3 gem = l.Gems[g];
                if (gem.y <= l.KillY + 0.5f)
                {
                    r.Warnings.Add("gem " + g + " at " + gem +
                        " is below the death plane.");
                    continue;
                }
                if (!GemGettable(l, tops, seen, gem))
                    r.Warnings.Add("gem " + g + " at " + gem +
                        " is beyond jump reach of every reachable surface" +
                        " (blocks 3-star, not the finish).");
            }
        }

        static bool GemGettable(LevelDefinition l, List<Top> tops,
            bool[] seen, Vector3 gem)
        {
            // Jump reach, audit parity: within landing-reach in XZ of a
            // reachable top and inside the vertical window a jump (or a
            // short drop) can sweep.
            for (int i = 0; i < tops.Count; i++)
            {
                if (!seen[i]) continue;
                float dy = gem.y - tops[i].TopY;
                if (dy < -8f || dy > ApexForRise + 1f) continue;
                if (RectDistXz(tops[i].Center, tops[i].Half,
                        gem, Vector2.zero) <= 4.2f)
                    return true;
            }
            // Bounce-pad apex gems: the launch reaches ~8.6 up, and the
            // trigger fires with the capsule already a step onto the pad.
            for (int p = 0; p < l.BouncePads.Count; p++)
            {
                Vector3 pad = l.BouncePads[p];
                if (Mathf.Abs(pad.x - gem.x) <= 1.7f &&
                    Mathf.Abs(pad.z - gem.z) <= 1.7f &&
                    gem.y - pad.y <= 8.2f && gem.y >= pad.y)
                    return true;
            }
            return InAnyWind(l, gem) || InAnyGust(l, gem);
        }

        static bool InAnyWind(LevelDefinition l, Vector3 p)
        {
            for (int i = 0; i < l.WindZones.Count; i++)
            {
                WindSpec w = l.WindZones[i];
                if (Mathf.Abs(p.x - w.Center.x) <= w.Size.x * 0.5f + 2f &&
                    Mathf.Abs(p.z - w.Center.z) <= w.Size.z * 0.5f + 2f &&
                    p.y >= w.Center.y - w.Size.y * 0.5f - 1f &&
                    p.y <= w.Center.y + w.Size.y * 0.5f + 14f)
                    return true;
            }
            return false;
        }

        static bool InAnyGust(LevelDefinition l, Vector3 p)
        {
            for (int i = 0; i < l.Gusts.Count; i++)
            {
                GustSpec g = l.Gusts[i];
                float carry = g.Size.z + 10f;
                Vector3 far = g.Center + Vector3.Scale(g.Direction,
                    new Vector3(carry, 0f, carry));
                Vector3 lo = Vector3.Min(g.Center, far);
                Vector3 hi = Vector3.Max(g.Center, far);
                float margin = 2.5f;
                if (p.x >= lo.x - g.Size.x * 0.5f - margin &&
                    p.x <= hi.x + g.Size.x * 0.5f + margin &&
                    p.z >= lo.z - g.Size.z * 0.5f - margin &&
                    p.z <= hi.z + g.Size.z * 0.5f + margin &&
                    p.y >= g.Center.y - g.Size.y * 0.5f - margin &&
                    p.y <= g.Center.y + g.Size.y * 0.5f + 8f)
                    return true;
            }
            return false;
        }

        static int CountTrue(bool[] flags)
        {
            int n = 0;
            for (int i = 0; i < flags.Length; i++) if (flags[i]) n++;
            return n;
        }
    }
}
