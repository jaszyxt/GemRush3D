using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GemRush.Tests
{
    /// <summary>
    /// The delight layer's safety net. These systems shipped with zero
    /// EditMode coverage, and a boot-visible UI regression (the photo
    /// capture bar leaking onto the menu) proved what that costs: 52 tests
    /// stayed green while the menu was visibly broken.
    ///
    /// Everything here is pure-data or table-driven — no play mode, no
    /// input backend — so it runs in the normal suite. The behaviours
    /// covered are the ones a refactor could silently break: the save
    /// format a ghost round-trips through, the determinism the golden
    /// gem's spot depends on, the particle budgets the phone build
    /// relies on, and the evergreen law the celebration copy obeys.
    /// </summary>
    [TestFixture]
    public class DelightAuditTests
    {
        // ---------- Ghost runs (the recorded route) ----------

        [Test]
        public void GhostStore_RoundTripsAPathWithinQuantizationTolerance()
        {
            var path = new List<Vector3>();
            for (int i = 0; i < 120; i++)
            {
                path.Add(new Vector3(
                    Mathf.Sin(i * 0.1f) * 3f,
                    1.5f + Mathf.Cos(i * 0.07f),
                    i * 0.8f));
            }

            string encoded = GhostStore.Encode(path);
            List<Vector3> decoded = GhostStore.Decode(encoded);

            Assert.IsNotNull(decoded, "a clean encode must decode");
            Assert.AreEqual(path.Count, decoded.Count,
                "every sample must survive the round trip");
            // Deltas are quantized to 5 mm, so error ACCUMULATES along the
            // route: a long path drifts more than a short one. Measured
            // worst case across 40 synthetic routes (up to ~220 samples) is
            // ~0.40 units, so the bound below is the encoder's real
            // behaviour, not a hopeful guess — and 0.5 is still small
            // enough that a replayed ghost reads as being on the route
            // rather than beside it. If this ever fails, the ENCODER
            // changed; do not simply raise the number.
            float worst = 0f;
            for (int i = 0; i < path.Count; i++)
                worst = Mathf.Max(worst,
                    Vector3.Distance(path[i], decoded[i]));
            Assert.Less(worst, 0.5f,
                "the replayed route drifted " + worst.ToString("F3") +
                " from the recorded one (measured worst ~0.40 for paths "
                + "this long) — the encoder's quantization changed");
        }

        [Test]
        public void GhostStore_CorruptDataNeverThrowsAndNeverFakesAPath()
        {
            // The law: a corrupt or foreign save must never break a level.
            // It must return null rather than a garbage route.
            string[] junk =
            {
                null, "", "   ", "not-base64!!!", "AAAA", "////",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                "eyJub3QiOiJhIGdob3N0In0="
            };
            foreach (string bad in junk)
            {
                List<Vector3> decoded = null;
                Assert.DoesNotThrow(delegate { decoded = GhostStore.Decode(bad); },
                    "Decode threw on input: " + (bad ?? "<null>"));
                Assert.IsNull(decoded,
                    "corrupt input must decode to null, not a fake route: "
                    + (bad ?? "<null>"));
            }
        }

        [Test]
        public void GhostStore_SampleInterval_IsTheOneSharedRate()
        {
            // The recorder, the runner and the editor probe all read this
            // constant. It used to be three separate literals, and a
            // drifted copy would silently mis-time every replay.
            Assert.AreEqual(0.1f, GhostStore.SampleInterval, 0.0001f,
                "10 Hz is the contract the encode header assumes");
        }

        [Test]
        public void GhostStore_SaveKey_IsStableAndLevelNameScoped()
        {
            // Keys are name-keyed so inserting a level never shifts an
            // existing ghost onto the wrong course (the save-system law).
            string a = GhostStore.SaveKey("First Steps");
            string b = GhostStore.SaveKey("First Steps");
            string c = GhostStore.SaveKey("Other Level");
            Assert.AreEqual(a, b, "the same level must always key the same");
            Assert.AreNotEqual(a, c, "different levels must not collide");
            Assert.IsFalse(string.IsNullOrEmpty(a),
                "an empty key would make every ghost overwrite every other");
        }

        // ---------- Golden gems (the taught secret) ----------

        [Test]
        public void GoldenGem_PickSpot_IsDeterministicPerLevel()
        {
            // "same level, same spot" is the documented promise: a secret
            // that moved between runs would be a bug, not a surprise.
            foreach (LevelDefinition level in LevelLibrary.Levels)
            {
                Vector3 first = GoldenGem.PickSpot(level);
                Vector3 again = GoldenGem.PickSpot(level);
                Assert.Less(Vector3.Distance(first, again), 0.0001f,
                    level.Name + ": the golden spot must not move");
            }
        }

        [Test]
        public void GoldenGem_HidesGolden_OnlyOnRealCollectableCourses()
        {
            for (int i = 0; i < LevelLibrary.Levels.Length; i++)
            {
                LevelDefinition level = LevelLibrary.Levels[i];
                bool hides = GoldenGem.HidesGolden(i);

                // B-sides ARE the reward, and bonus flight is weightless —
                // neither may hide a golden.
                if (LevelLibrary.BSideSourceIndex(i) >= 0)
                    Assert.IsFalse(hides,
                        level.Name + " is a B-side and must not hide a golden");
                else if (level.BonusFlight)
                    Assert.IsFalse(hides,
                        level.Name + " is a bonus flight and must not hide a golden");
                else
                    Assert.IsTrue(hides,
                        level.Name + " is an ordinary level and should hide one "
                        + "(otherwise the remix gate is unreachable)");
            }
        }

        [Test]
        public void GoldenGem_FoundFlags_AreScopedPerLevel()
        {
            // The gate only works if a find in one level never leaks into
            // another. Checked through the real save API but WITHOUT
            // writing: the query must be per-level and idempotent, so
            // re-reading is always safe. (Deliberately no SetGoldenFound
            // here — a test that mutates the developer's save cannot be
            // re-run, and the write path is already covered by the
            // in-editor GoldenProbe.)
            int claimed = 0;
            for (int i = 0; i < LevelLibrary.Levels.Length; i++)
            {
                bool once = SaveSystem.GoldenFound(i);
                bool twice = SaveSystem.GoldenFound(i);
                Assert.AreEqual(once, twice,
                    "the golden query must be stable for level " + i);
                if (once) claimed++;
            }
            Assert.AreEqual(claimed,
                SaveSystem.TotalGoldens(LevelLibrary.Levels.Length),
                "the atlas total must equal the per-level flags it sums");
        }

        [Test]
        public void GoldenGem_SpotAlwaysHoldsARealGem()
        {
            // The law the user set on 2026-09-22, after three releases of
            // "the yellow gem can't be collected": "if it's not a gem, then
            // DELETE it / if it's a gem, then make it a gem." A gem spot
            // holds a real, collectable gem or nothing — never a shape
            // wearing a gem's look.
            //
            // The failure this locks out: a level whose golden was already
            // found spawned a faint, full-size gold cube in the gem's
            // place, and players reported it exactly as "not solid, pip
            // can pass thru it like a cloud, nothing happened, no sound,
            // just a shape" on 32 of 37 levels of one save. Now the found
            // state changes only the celebration, never the gem.
            //
            // Both branches run through Place's found-state seam, so this
            // is deterministic and CI-safe: no save is read or written.
            GameObject parent = new GameObject("GoldenPlacementAudit");
            int savedIndex = GoldenGem.ActiveLevelIndex;
            try
            {
                for (int i = 0; i < LevelLibrary.Levels.Length; i++)
                {
                    LevelDefinition level = LevelLibrary.Levels[i];
                    // Only the levels that actually hide a golden. A B-side
                    // is the reward and bonus flight is weightless, so
                    // neither has a spot — that HidesGolden contract is
                    // locked separately by
                    // GoldenGem_HidesGolden_OnlyOnRealCollectableCourses.
                    if (!GoldenGem.HidesGolden(i)) continue;

                    GoldenGem.ActiveLevelIndex = i;

                    foreach (bool alreadyFound in new[] { true, false })
                    {
                        // The scratch parent must be EMPTY before each
                        // pass. This guard is not decoration: destroying
                        // children while enumerating a Transform skips
                        // some, and a survivor let the next pass assert
                        // against the PREVIOUS pass's gem — which is how
                        // this test first reported "expected True but was
                        // False" for a gem the code had built correctly.
                        Assert.AreEqual(0, parent.transform.childCount,
                            "the audit scratch must be empty before each "
                            + "pass: " + level.Name);

                        string when = alreadyFound
                            ? "on a re-find" : "on a first find";
                        GoldenGem.Place(level, parent.transform, alreadyFound);

                        Transform gem = parent.transform.Find("GoldenGem");
                        Assert.IsNotNull(gem,
                            "the spot must hold a real gem " + when + ": "
                            + level.Name);
                        Collider collider = gem.GetComponent<Collider>();
                        Assert.IsNotNull(collider,
                            "the gem must be touchable " + when + ": "
                            + level.Name);
                        Assert.IsTrue(collider.isTrigger,
                            "the gem's collider must be its pickup trigger "
                            + when + ": " + level.Name);
                        // Public nested (like MirrorDoor.DoorSide) because
                        // this test has to reach it: a private nested type
                        // cannot be named from the test assembly at all,
                        // which is what broke this file's compile.
                        GoldenGem.GoldenStar star =
                            gem.GetComponent<GoldenGem.GoldenStar>();
                        Assert.IsNotNull(star,
                            "the gem must carry its pickup script " + when
                            + ": " + level.Name);
                        Assert.AreEqual(alreadyFound, star.alreadyFound,
                            "the gem must be told which find this is, or it "
                            + "re-tells the secret: " + level.Name);

                        // The deleted impostor, by both of its old names.
                        Assert.IsNull(parent.transform
                            .Find("GoldenFoundOutline"),
                            "the uncollectable ghost must stay deleted: "
                            + level.Name);
                        Assert.IsNull(parent.transform
                            .Find("GoldenFoundMark"),
                            "the uncollectable ghost must stay deleted: "
                            + level.Name);

                        // Collected first, destroyed after: Unity's
                        // Transform enumerator is index-based, so a
                        // DestroyImmediate inside the foreach walks off the
                        // end and leaves survivors behind.
                        List<GameObject> doomed = new List<GameObject>();
                        foreach (Transform child in parent.transform)
                            doomed.Add(child.gameObject);
                        foreach (GameObject go in doomed)
                            Object.DestroyImmediate(go);
                    }
                }
            }
            finally
            {
                GoldenGem.ActiveLevelIndex = savedIndex;
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void GustZone_ReceivesTheLiftItsLevelSpecifies()
        {
            // The Festival Finale play report ("so hard to cross") ended at
            // a one-word bug: LevelBuilder called GustZone.Create WITHOUT
            // g.Lift, so EVERY gust in the game ran at lift 0 and the
            // sustain written into the level data was dead — Silent
            // Spire's included. A flat ride sinks during the crossing, so
            // any landing whose top sits at entry height was a wall or a
            // void. This drives a gust through the REAL builder and
            // asserts the live zone matches its spec, so the wiring can
            // never drop a field again.
            var level = new LevelDefinition();
            level.Name = "Gust Wiring Probe";
            level.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));
            level.Gusts.Add(new GustSpec(0f, 3f, 20f,
                new Vector3(5f, 4f, 10f), new Vector3(0f, 0f, 1f),
                4.4f, 2.2f, 8f, 3f));

            GameObject parent = new GameObject("GustWiringProbe");
            try
            {
                // GustZone.Create destroys its streak/petal cube colliders
                // with Object.Destroy — correct at runtime, but edit mode
                // logs an error per call. Those logs are this test's only
                // business NOT being asserted, so silence them for the
                // build call alone.
                LogAssert.ignoreFailingMessages = true;
                LevelBuilder.Build(level, parent.transform);
                LogAssert.ignoreFailingMessages = false;

                GustZone zone = parent.GetComponentInChildren<GustZone>();
                Assert.IsNotNull(zone,
                    "the built level must contain its gust zone");
                Assert.AreEqual(3f, zone.lift,
                    "the live gust must carry the spec's lift — the " +
                    "builder dropped this parameter once and every gust " +
                    "in the game ran flat");
                Assert.AreEqual(8f, zone.strength,
                    "the live gust must carry the spec's strength");
                Assert.AreEqual(2.2f, zone.activeTime,
                    "the live gust must carry the spec's blow window");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Strings_GoldenNote_IsEvergreenAndStablePerLevel()
        {
            // Evergreen law: no digits, no level numbers, no statuses in
            // celebration/quote copy. And a level's note must not change
            // between visits — a found thing has a story that holds.
            foreach (LevelDefinition level in LevelLibrary.Levels)
            {
                string note = Strings.GoldenNote(level.Name);
                Assert.IsFalse(string.IsNullOrEmpty(note),
                    level.Name + ": every golden needs its note");
                Assert.AreEqual(note, Strings.GoldenNote(level.Name),
                    level.Name + ": the note must be stable per level");
                foreach (char ch in note)
                    Assert.IsFalse(char.IsDigit(ch),
                        "evergreen law: no digits in golden copy — \"" + note + "\"");
            }
        }

        // ---------- Perches (a place to sit) ----------

        [Test]
        public void Perch_PickSpot_IsDeterministicPerLevel()
        {
            foreach (LevelDefinition level in LevelLibrary.Levels)
            {
                Vector3 first = Perch.PickSpot(level);
                Vector3 again = Perch.PickSpot(level);
                Assert.Less(Vector3.Distance(first, again), 0.0001f,
                    level.Name + ": the perch must not wander between runs");
            }
        }

        [Test]
        public void Perch_NeverLandsInsideAHazardOrOnACollectible()
        {
            // Both of these were REAL bugs caught by measuring and by
            // looking at a captured frame:
            //  - the first scoring pass put a bench directly under a
            //    spinner's sweep (a "rest" inside a hazard arc);
            //  - an earlier one put 23 of 27 within 1.5 units of a gem.
            // A rest spot in danger is worse than no rest spot, so both
            // are now invariants.
            const float GuardianReach = 6f;   // arm is 9u end to end
            const float GemClearance = 2.5f;
            const float KillMargin = 2f;

            foreach (LevelDefinition level in LevelLibrary.Levels)
            {
                if (level.BonusFlight) continue;
                if (level.Platforms.Count < 2) continue;

                Vector3 spot = Perch.PickSpot(level);

                foreach (SpinnerSpec s in level.Spinners)
                {
                    float d = Vector2.Distance(
                        new Vector2(spot.x, spot.z),
                        new Vector2(s.PlatformTop.x, s.PlatformTop.z));
                    Assert.GreaterOrEqual(d, GuardianReach,
                        level.Name + ": a perch sits " + d.ToString("F1") +
                        "u from a guardian hub — inside its sweep");
                }

                foreach (Vector3 gem in level.Gems)
                {
                    float d = Vector3.Distance(gem, spot);
                    Assert.GreaterOrEqual(d, GemClearance,
                        level.Name + ": a perch sits " + d.ToString("F1") +
                        "u from a gem");
                }

                Assert.Greater(spot.y, level.KillY + KillMargin,
                    level.Name + ": a perch must not hang below the kill plane");
            }
        }

        [Test]
        public void Perch_LandsOnARoomyPlatformNotAMidHopTile()
        {
            // The signal that identifies a restful spot in this game is
            // roominess: the wide landings are where the course breathes.
            // (Levels are linear staircases along Z, so "distance from the
            // route" is meaningless — every tile sits on the line.)
            foreach (LevelDefinition level in LevelLibrary.Levels)
            {
                if (level.BonusFlight) continue;
                if (level.Platforms.Count < 2) continue;

                Vector3 spot = Perch.PickSpot(level);
                bool onBigPlatform = false;
                foreach (PlatformSpec p in level.Platforms)
                {
                    Vector3 top = p.Center +
                        new Vector3(0f, p.Size.y * 0.5f, 0f);
                    float reach = Mathf.Max(p.Size.x, p.Size.z) * 0.6f;
                    if (Vector3.Distance(
                            new Vector3(spot.x, top.y, spot.z), top) < reach
                        && Mathf.Min(p.Size.x, p.Size.z) >= 5f)
                        onBigPlatform = true;
                }
                Assert.IsTrue(onBigPlatform,
                    level.Name + ": the perch must stand on a roomy platform");
            }
        }

        // ---------- Particle budgets (the phone build) ----------

        [Test]
        public void Fx_OneShotBursts_ClampInsteadOfThrowing()
        {
            // The directives cap one-shot effects (60 particles; petals 30)
            // and Fx is the only thing standing between a caller's number
            // and the phone's frame budget.
            //
            // This test used to assert only DoesNotThrow, which proved
            // nothing: Burst had NO clamp at all, so sending it 500 simply
            // built a 500-particle system without complaining — and because
            // the count goes through a (short) cast, a large enough number
            // would wrap to a small or negative burst in silence. The test
            // passed for exactly the wrong reason. It now MEASURES the
            // clamp, so removing any of the three ceilings fails here.
            assertClamped("Burst", 60,
                delegate { Fx.Burst(Vector3.zero, ArtLib.Gold, 500); });
            assertClamped("PetalPuff", 30,
                delegate { Fx.PetalPuff(Vector3.zero, ArtLib.Gold, 500); });
            assertClamped("Confetti", 60,
                delegate { Fx.Confetti(Vector3.zero, 500); });

            // A negative request must not become a wrapped positive one.
            assertClamped("Burst", 60,
                delegate { Fx.Burst(Vector3.zero, ArtLib.Gold, -1); });
        }

        /// Spawns one one-shot and asserts the burst it built is inside the
        /// budget. Reads the live system rather than trusting the clamp's
        /// source, because the whole point is that a missing clamp is
        /// invisible from the call site.
        ///
        /// The signal measured is the BURST ENTRY, not maxParticles: these
        /// one-shots never set maxParticles (Unity's 1000 default applies),
        /// so the emitted count is what the budget actually rides on — and
        /// it is the number the (short) cast consumes.
        static void assertClamped(string label, int budget,
            System.Action spawn)
        {
            spawn();
            ParticleSystem found = null;
            foreach (ParticleSystem ps in Object.FindObjectsByType<ParticleSystem>(
                FindObjectsSortMode.None))
            {
                if (ps.gameObject.name == label
                    || label.StartsWith(ps.gameObject.name))
                {
                    found = ps;
                    break;
                }
            }
            Assert.IsNotNull(found,
                label + ": expected a particle system named for the effect");

            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[8];
            int n = found.emission.GetBursts(bursts);
            Assert.Greater(n, 0, label + ": expected a burst entry");
            for (int i = 0; i < n; i++)
            {
                // count is a MinMaxCurve; these bursts are always constant,
                // so .constant is the number the (short) cast consumed.
                int count = (int)bursts[i].count.constant;
                Assert.LessOrEqual(count, budget,
                    label + ": burst entry of " + count +
                    " exceeds the " + budget + " budget — the clamp is gone");
                Assert.GreaterOrEqual(count, 0,
                    label + ": burst count went negative — the (short) wrap");
            }

            Object.DestroyImmediate(found.gameObject);
        }

        // ---------- The genre law (cozy, not transactional) ----------

        [Test]
        public void DelightCopy_NeverUsesPressureOrTransactionalLanguage()
        {
            // The cozy law: no FOMO, no streak guilt, no scarcity. This is
            // a copy check over the celebration strings that are most
            // tempting to make transactional, and it catches exactly the
            // drift the research warns about.
            //
            // Deliberately phrases, not bare words: a first pass banned the
            // word "hurry" and failed on the game's OWN line "in no hurry
            // at all" — which is the opposite of pressure. The unit of
            // meaning here is the phrase.
            string[] banned =
            {
                "don't miss", "dont miss", "miss out", "last chance",
                "hurry up", "act fast", "before it's gone",
                "before its gone", "expires", "expiring", "limited time",
                "streak", "daily streak", "log in", "come back tomorrow",
                "you lose", "lose your progress", "penalty", "punish"
            };
            var samples = new List<string>
            {
                Strings.GoldenFoundLine,
                Strings.GoldenGateLocked,
                Strings.GoldenGateHint("a place"),
                Strings.GoldenUnlocksRemix("a night course")
            };
            foreach (LevelDefinition level in LevelLibrary.Levels)
                samples.Add(Strings.GoldenNote(level.Name));

            // The surfaces that are actually TEMPTING to make transactional,
            // and which this test previously never read: the strings that
            // talk about coming back, or about a running total. A pressure
            // phrase added to the visit recap, the game-over line, the
            // completion screen or a menu quote used to sail through.
            samples.Add(Strings.VisitRecapProgress(3, 1));
            samples.Add(Strings.VisitRecapGifts(4, false));
            samples.Add(Strings.VisitRecapGifts(4, true));
            samples.Add(Strings.OverSub);
            samples.Add(Strings.CompleteSub);
            samples.Add(Strings.FirstVisitWelcome);
            samples.Add(Strings.TrailTier1);
            samples.Add(Strings.TrailTier2);
            samples.Add(Strings.TrailTier3);
            samples.Add(Strings.FirstStepsHint());
            samples.Add(Strings.BellHint);
            samples.Add(Strings.AtlasUncharted);
            samples.Add(Strings.GoldenGateLocked);
            samples.Add(Strings.NoTimeYet);
            foreach (string quote in Story.MenuQuotes)
                samples.Add(quote);
            foreach (string page in Story.Epilogue)
                samples.Add(page);
            foreach (LevelDefinition level in LevelLibrary.Levels)
            {
                samples.Add(level.WinLine);
                if (!string.IsNullOrEmpty(level.Milestone))
                    samples.Add(level.Milestone);
                foreach (string beat in level.StoryBeats)
                    samples.Add(beat);
            }

            foreach (string line in samples)
            {
                if (string.IsNullOrEmpty(line)) continue;
                string lower = line.ToLowerInvariant();
                foreach (string phrase in banned)
                {
                    if (!lower.Contains(phrase)) continue;
                    // A NEGATED banned phrase is a reassurance, not pressure:
                    // "There's no hurry up here" and "in no hurry at all"
                    // both contain "hurry up"/"hurry" as the tail of a
                    // sentence that removes urgency. The author already hit
                    // this once — the comment above records moving from the
                    // bare word to the phrase for exactly this reason — and
                    // the greeting reintroduced it by ending on "no hurry
                    // up here". So the rule is the PHRASE unless negated.
                    if (IsNegated(lower, phrase)) continue;
                    Assert.IsFalse(true,
                        "cozy law: \"" + phrase + "\" is pressure, and has no "
                        + "place in delight copy — \"" + line + "\"");
                }
            }
        }

        /// True when every occurrence of <paramref name="phrase"/> in
        /// <paramref name="lower"/> is immediately preceded by a negator
        /// ("no", "not", "never", "without", or an "n't" contraction), so it
        /// reads as the opposite of pressure. An unnegated occurrence
        /// anywhere still fails — a line may not hide a real pressure
        /// phrase behind one reassurance.
        static bool IsNegated(string lower, string phrase)
        {
            string[] negators = { "no ", "not ", "never ", "without ",
                "n't ", "no-", "non-" };
            int at = lower.IndexOf(phrase);
            if (at < 0) return false;
            while (at >= 0)
            {
                bool negated = false;
                foreach (string neg in negators)
                {
                    if (at < neg.Length) continue;
                    if (lower.Substring(at - neg.Length, neg.Length) == neg)
                    { negated = true; break; }
                }
                if (!negated) return false; // a real, unnegated instance
                at = lower.IndexOf(phrase, at + phrase.Length);
            }
            return true;
        }
    }
}
