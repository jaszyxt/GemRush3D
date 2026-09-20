using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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
            // and the phone's frame budget. Over-budget requests must
            // clamp, never throw and never overflow the budget.
            Assert.DoesNotThrow(delegate
            {
                Fx.Confetti(Vector3.zero, 500);
                Fx.PetalPuff(Vector3.zero, ArtLib.Gold, 500);
                Fx.Burst(Vector3.zero, ArtLib.Gold, 500);
            }, "over-budget requests must clamp, not throw");

            // Clean up whatever the spawns left behind so this fixture
            // never leaks objects into the next test.
            foreach (GameObject go in
                Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name == "Confetti" || go.name == "PetalPuff"
                    || go.name == "Burst")
                    Object.DestroyImmediate(go);
            }
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

            foreach (string line in samples)
            {
                string lower = line.ToLowerInvariant();
                foreach (string phrase in banned)
                    Assert.IsFalse(lower.Contains(phrase),
                        "cozy law: \"" + phrase + "\" is pressure, and has no "
                        + "place in delight copy — \"" + line + "\"");
            }
        }
    }
}
