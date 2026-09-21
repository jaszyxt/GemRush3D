using UnityEngine;

namespace GemRush
{
    /// All levels in the game, in play order. Adding a level = adding a
    /// method here (or to a level pack) and one line to the Levels builder.
    public static class LevelLibrary
    {
        public static readonly LevelDefinition[] Levels = BuildAllLevels();

        /// One charted region of the atlas: a contiguous run of levels in
        /// play order. The atlas screen groups levels by these; adding a
        /// pack = adding its levels AND its region line here.
        public struct Region
        {
            public string Roman;
            public string Name;
            public int First;
            public int Count;
        }

        /// The golden-gem gate map: which level's golden gem unlocks a
        /// given B-side. -1 for everything that is not a gated remix.
        /// B-side 28 "Gust Alley — Nightfall" <- 19 "Gust Alley";
        /// B-side 29 "The Garden That Dreams" <- 22 "The First Bell";
        /// B-side 30 "The Ascent — Nightfall" <- 2 "The Ascent".
        public static int BSideSourceIndex(int levelIndex)
        {
            if (levelIndex == 28) return 19;
            if (levelIndex == 29) return 22;
            if (levelIndex == 30) return 2;
            return -1;
        }

        /// True when a level is an OPTIONAL remix rather than a step of the
        /// main road. The B-Sides are a side branch: they are unlocked by
        /// finding a hidden golden gem, and they are never required to
        /// reach the rest of the game.
        public static bool IsOptionalRemix(int levelIndex)
        {
            return BSideSourceIndex(levelIndex) >= 0;
        }

        /// The level the ladder should advance to after clearing
        /// <paramref name="levelIndex"/>: the next MAIN-ROAD level,
        /// skipping any optional remix.
        ///
        /// This exists because the ladder used to be `index + 1`, which put
        /// the golden-gated B-Sides (28-30) directly in the critical path:
        /// unlocking is strictly sequential, so a player who never found
        /// the hidden golden on level 2, 19 or 22 could never open levels
        /// 31-39 — the entire ending. Missing a secret must cost a secret,
        /// never the game. Goldens still gate the remixes themselves; they
        /// no longer gate progress.
        public static int NextMainRoadLevel(int levelIndex)
        {
            int last = Levels.Length - 1;
            int next = levelIndex + 1;
            while (next < last && IsOptionalRemix(next)) next++;
            // Clearing the final level has nowhere to advance to; clamp so
            // the stored unlocked-count can never grow past the library.
            return next > last ? last : next;
        }

        public static readonly Region[] Regions =
        {
            new Region { Roman = "I",     Name = "The Storm",            First = 0,  Count = 3 },
            new Region { Roman = "II",    Name = "The Rematch",          First = 3,  Count = 3 },
            new Region { Roman = "III",   Name = "The Undercloud",       First = 6,  Count = 3 },
            new Region { Roman = "IV",    Name = "The Two Suns",         First = 9,  Count = 3 },
            new Region { Roman = "V",     Name = "The Far Isles",        First = 12, Count = 3 },
            new Region { Roman = "VI",    Name = "Gloomfang's Day Off",  First = 15, Count = 1 },
            new Region { Roman = "VII",   Name = "The Sky Garden",       First = 16, Count = 3 },
            new Region { Roman = "VIII",  Name = "Storm Chasers",        First = 19, Count = 3 },
            new Region { Roman = "IX",    Name = "The Bell Towers",      First = 22, Count = 3 },
            new Region { Roman = "X",     Name = "Mirror Skies",         First = 25, Count = 3 },
            new Region { Roman = "XI",    Name = "The B-Sides",          First = 28, Count = 3 },
            new Region { Roman = "XII",   Name = "The Long Winter",      First = 31, Count = 3 },
            new Region { Roman = "XIII",  Name = "The Aurora Festival",  First = 34, Count = 3 },
            new Region { Roman = "XIV",   Name = "The Homecoming",       First = 37, Count = 3 }
        };

        static LevelDefinition[] BuildAllLevels()
        {
            // Built by appending to a List rather than pre-sizing an array
            // from a hand-summed length expression. That old form silently
            // TRUNCATED the library if a pack was added without also
            // editing the sum — levels would vanish from the game with no
            // error anywhere. Appending cannot lose content; a missing
            // pack is now a missing line, which the audit tests catch.
            System.Collections.Generic.List<LevelDefinition> all =
                new System.Collections.Generic.List<LevelDefinition>();

            all.Add(FirstSteps());
            all.Add(SpinnerGauntlet());
            all.Add(TheAscent());

            AddPack(all, LevelPackTwo.Levels);
            AddPack(all, LevelPackThree.Levels);
            AddPack(all, LevelPackFour.Levels);
            AddPack(all, LevelPackFive.Levels);
            AddPack(all, LevelPackSix.Levels);
            AddPack(all, LevelPackSeven.Levels);
            AddPack(all, LevelPackEight.Levels);
            AddPack(all, LevelPackNine.Levels);
            AddPack(all, LevelPackTen.Levels);
            AddPack(all, LevelPackBSides.Levels);
            AddPack(all, LevelPackEleven.Levels);
            AddPack(all, LevelPackTwelve.Levels);
            AddPack(all, LevelPackThirteen.Levels);

            return all.ToArray();
        }

        static void AddPack(System.Collections.Generic.List<LevelDefinition> all,
            LevelDefinition[] pack)
        {
            for (int i = 0; i < pack.Length; i++) all.Add(pack[i]);
        }

        // ------------------------------------------------------------------
        // Level 1 — "First Steps": wide platforms, one spinner, one mover.
        // Teaches moving, jumping, gems, the checkpoint and the portal.
        // ------------------------------------------------------------------
        static LevelDefinition FirstSteps()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "First Steps";
            l.Mission = "GLOOMFANG — a storm with abandonment issues — swallowed the Sunstones. The first ones are just ahead. Light the portal. Try not to look down.";
            l.WinLine = "One portal lit. Gloomfang pretends not to care. He cares.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 18f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-2f, 2f, 26f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 3f, 44f, 9f, 1f, 9f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(0f, 4f, 56f, 12f, 1f, 12f));   // spinner arena
            l.Platforms.Add(new PlatformSpec(5f, 5f, 68f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(-1f, 6f, 76f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 10.5f, 94f, 11f, 1f, 11f));// summit

            l.Movers.Add(new MoverSpec(0f, 2f, 34f, new Vector3(5f, 0f, 0f), 4f));
            l.Movers.Add(new MoverSpec(-5f, 7f, 84f, new Vector3(0f, 4f, 0f), 5f));

            l.Spinners.Add(new SpinnerSpec(0f, 4.5f, 56f, 60f));

            l.Checkpoints.Add(new Vector3(0f, 3.5f, 44f));
            l.StoryBeats.Add("The Sunstones hum when Pip gets close. Somewhere overhead, a very large storm flinches.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 18f));
            l.Gems.Add(new Vector3(-2f, 3.6f, 26f));
            l.Gems.Add(new Vector3(0f, 3.7f, 34f));     // over the mover's path
            l.Gems.Add(new Vector3(0f, 4.7f, 42f));
            l.Gems.Add(new Vector3(-3f, 4.7f, 46f));
            l.Gems.Add(new Vector3(4.5f, 5.3f, 60.5f));
            l.Gems.Add(new Vector3(-4.5f, 5.3f, 51.5f));
            l.Gems.Add(new Vector3(5f, 6.7f, 68f));
            l.Gems.Add(new Vector3(4.5f, 12.3f, 90f));

            l.Portal = new Vector3(0f, 11f, 96f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 2 — "Spinner Gauntlet": two spinners, a fast mover, narrow
        // bridge islands and tight gaps.
        // ------------------------------------------------------------------
        static LevelDefinition SpinnerGauntlet()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "Spinner Gauntlet";
            l.Mission = "Gloomfang left guardians to spin you into next week. They're big, they're red, they're very committed. Be smarter than they are round.";
            l.WinLine = "Two wheels outspun. The guardians have requested a rematch. Request denied.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 8f, 1f, 8f));      // start
            l.Platforms.Add(new PlatformSpec(0f, 0f, 10f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(3f, 1f, 18f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 2f, 29f, 10f, 1f, 10f));   // spinner arena A
            l.Platforms.Add(new PlatformSpec(5f, 3f, 41f, 5f, 1f, 5f));
            l.Platforms.Add(new PlatformSpec(0f, 4f, 59f, 9f, 1f, 9f));     // checkpoint isle
            l.Platforms.Add(new PlatformSpec(-2f, 5f, 69f, 4f, 1f, 4f));    // bridge isles
            l.Platforms.Add(new PlatformSpec(2f, 6f, 77f, 4f, 1f, 4f));
            l.Platforms.Add(new PlatformSpec(0f, 7f, 89f, 12f, 1f, 12f));   // spinner arena B
            l.Platforms.Add(new PlatformSpec(0f, 11.5f, 111f, 11f, 1f, 11f));// summit

            l.Movers.Add(new MoverSpec(0f, 3f, 49f, new Vector3(4f, 0f, 0f), 3.5f));
            l.Movers.Add(new MoverSpec(-5f, 8f, 101f, new Vector3(0f, 4f, 0f), 4.5f));

            l.Spinners.Add(new SpinnerSpec(0f, 2.5f, 29f, 75f));
            l.Spinners.Add(new SpinnerSpec(0f, 7.5f, 89f, 75f));

            l.Checkpoints.Add(new Vector3(0f, 4.5f, 59f));
            l.StoryBeats.Add("Guardian rule one: spin. Rule two: see rule one. Pip's rule one: be somewhere else.");

            l.Gems.Add(new Vector3(0f, 1.6f, 10f));
            l.Gems.Add(new Vector3(3f, 2.6f, 18f));
            l.Gems.Add(new Vector3(4.5f, 3.3f, 33f));
            l.Gems.Add(new Vector3(-4.5f, 3.3f, 25f));
            l.Gems.Add(new Vector3(5f, 4.6f, 41f));
            l.Gems.Add(new Vector3(0f, 5.7f, 57f));
            l.Gems.Add(new Vector3(-3f, 5.7f, 61f));
            l.Gems.Add(new Vector3(-2f, 6.6f, 69f));
            l.Gems.Add(new Vector3(2f, 7.6f, 77f));
            l.Gems.Add(new Vector3(5f, 8.3f, 93f));
            l.Gems.Add(new Vector3(-5f, 8.3f, 85f));
            l.Gems.Add(new Vector3(4.5f, 13.3f, 107f));

            l.Portal = new Vector3(0f, 12f, 113f);
            return l;
        }

        // ------------------------------------------------------------------
        // Level 3 — "The Ascent": a vertical climb chained together with
        // elevators, rest islands and a high spinner arena.
        // ------------------------------------------------------------------
        internal static LevelDefinition TheAscent()
        {
            LevelDefinition l = new LevelDefinition();
            l.Name = "The Ascent";
            l.Mission = "Gloomfang waits at the summit, holding the last light and a grudge the size of the weather. Every legend ends at the top. Climb, Pip. Take the sky back.";
            l.WinLine = "The summit is yours. Somewhere above, a very large storm quietly apologizes.";
            l.Milestone = "REGION CHARTED: THE STORM. The atlas has its first ink.";

            l.Platforms.Add(new PlatformSpec(0f, 0f, 0f, 10f, 1f, 10f));     // start
            // Rest 1 is 8 wide, not 6: the jump from the start deck is
            // 10.9 against an 11.0 maximum (a 1% margin — no wobble room),
            // and a child who tries to skip the elevator deserves a landing
            // rather than a fall. The mover still ferries the climb.
            l.Platforms.Add(new PlatformSpec(0f, 3.5f, 19f, 8f, 1f, 8f));    // rest 1
            l.Platforms.Add(new PlatformSpec(4f, 7.5f, 35f, 6f, 1f, 6f));    // rest 2
            l.Platforms.Add(new PlatformSpec(-2f, 12.5f, 52f, 6f, 1f, 6f));  // rest 3
            // The two mid-climb islands were 4 wide with a 10.9 gap against
            // an 11.0 maximum jump — a 1% margin, i.e. an exact-max jump
            // with no room for a wobble (the difficulty pass measures route
            // hops and these were the tightest in the game). Four more
            // units of deck give a child a real landing without changing
            // the climb, the movers or the pacing.
            l.Platforms.Add(new PlatformSpec(3f, 13.5f, 60f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(-3f, 14.5f, 68f, 6f, 1f, 6f));
            l.Platforms.Add(new PlatformSpec(0f, 15.5f, 80f, 10f, 1f, 10f)); // high arena
            l.Platforms.Add(new PlatformSpec(0f, 21.5f, 103f, 12f, 1f, 12f));// summit

            l.Movers.Add(new MoverSpec(0f, 0f, 10f, new Vector3(0f, 3.5f, 0f), 5f));
            l.Movers.Add(new MoverSpec(4f, 3.5f, 27f, new Vector3(0f, 4f, 0f), 5.5f));
            l.Movers.Add(new MoverSpec(-2f, 7.5f, 43f, new Vector3(0f, 5f, 0f), 6f));
            l.Movers.Add(new MoverSpec(0f, 15.5f, 91f, new Vector3(0f, 6f, 0f), 7f));

            l.Spinners.Add(new SpinnerSpec(0f, 16f, 80f, 90f));

            l.Checkpoints.Add(new Vector3(0f, 4f, 19f));
            l.Checkpoints.Add(new Vector3(-2f, 13f, 52f));
            l.Checkpoints.Add(new Vector3(0f, 22f, 100f));
            l.StoryBeats.Add("The higher Pip climbs, the quieter the wind gets. Gloomfang is watching. Gloomfang is nervous.");
            l.StoryBeats.Add("Halfway up the old beacon route. The stones here remember other climbers. None of them were this small.");
            l.StoryBeats.Add("One ledge from the top. Pip can see the whole sky realm from here — and every portal still waiting.");

            l.Gems.Add(new Vector3(0f, 5f, 10f));      // grab at the first lift's top
            l.Gems.Add(new Vector3(0f, 5.1f, 19f));
            l.Gems.Add(new Vector3(4f, 9.5f, 27f));    // second lift
            l.Gems.Add(new Vector3(4f, 9.1f, 35f));
            l.Gems.Add(new Vector3(-2f, 14.1f, 50f));
            l.Gems.Add(new Vector3(0.5f, 14.1f, 54f));
            l.Gems.Add(new Vector3(3f, 15.1f, 60f));
            l.Gems.Add(new Vector3(-3f, 16.1f, 68f));
            l.Gems.Add(new Vector3(4.5f, 17.1f, 84f));
            l.Gems.Add(new Vector3(-4.5f, 17.1f, 76f));
            l.Gems.Add(new Vector3(0f, 20f, 91f));     // final long lift
            l.Gems.Add(new Vector3(-4f, 23.1f, 100f));
            l.Gems.Add(new Vector3(4f, 23.1f, 100f));
            l.Gems.Add(new Vector3(0f, 23.1f, 106f));

            l.Portal = new Vector3(0f, 22f, 109f);
            return l;
        }
    }
}
