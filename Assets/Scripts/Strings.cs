using UnityEngine;

namespace GemRush
{
    /// Every user-facing string the UI shows (directives D11). One file to
    /// hand translators when the day comes; zero English literals in the
    /// layout code. Story prose lives in Story.cs, level missions and win
    /// lines live with their levels, and the atlas region names are data
    /// in LevelLibrary.Regions — those are the other sanctioned homes.
    public static class Strings
    {
        // ---------- Menu ----------

        public const string MenuTitle = "GEM RUSH 3D";
        public const string MenuTagline =
            "Pip vs. Gloomfang — a very small hero, a very large storm";
        public const string Play = "PLAY";
        public const string Atlas = "ATLAS";
        public const string Settings = "SETTINGS";
        public const string Back = "BACK";
        public const string Menu = "MENU";
        public const string Jump = "JUMP";

        public const string GoalLine =
            "Collect gems for stars, dodge the red spinners, reach the portal!";

        public static string InstructionsGamepad()
        {
            return "Move: Left Stick / D-Pad    Jump: (A) / Cross    " +
                "Pause: Start\n" + GoalLine +
                "\nMenus: D-Pad choose · (A) confirm · (B) back · " +
                "shoulders flip pages.";
        }

        public static string InstructionsTouch()
        {
            return "On touch: drag the left side to move, tap " + Jump + ".\n" +
                GoalLine + "\nKeyboard: WASD / Arrows + Space.";
        }

        public static string InstructionsKeyboard()
        {
            return "Move: WASD / Arrows    Jump: Space    (ENTER works too)\n" +
                GoalLine + "\nGamepad: Left Stick + (A) — plug one in and " +
                "this line follows it.";
        }

        public static string LevelLabel(int number)
        {
            return "LEVEL " + number;
        }

        public static string LevelLockedRow(int number)
        {
            return LevelLabel(number) + "\n" + Locked;
        }

        public static string LevelUnlockedRow(int number, int stars, int max,
            bool daily)
        {
            return LevelLabel(number) + "\n" + Stars(stars, max) +
                (daily ? "  · DAILY" : "");
        }

        // ---------- Session recap ----------

        /// Shown once, on the menu of a brand-new save, where the recap
        /// normally sits. Its job is to say hello and hand over the
        /// errand - no instructions to memorise, no form to fill in, no
        /// tutorial to dismiss. The game teaches itself; this is the
        /// introduction, not the lesson. Never shown again after the
        /// player's first jewel.
        public const string FirstVisitWelcome =
            "Welcome, Pip. The sky realm has lost its light, and you are " +
            "just the right size to get it back.\nPress PLAY whenever " +
            "you're ready. There's no hurry up here.";

        public static string VisitRecapProgress(int stars, int medals)
        {
            return string.Format(
                "Since you arrived: +{0} stars, +{1} medal{2}.\nThe garden noticed.",
                stars, medals, medals == 1 ? "" : "s");
        }

        public static string VisitRecapGifts(int gifts, bool takenToday)
        {
            return "Gloomfang's Gifts: " + gifts +
                (takenToday ? "  ·  today's gift is yours"
                            : "  ·  today's gift is still out there");
        }

        // ---------- HUD ----------

        public static string HudLevel(int level)
        {
            return "LV " + level;
        }

        public static string HudGems(int gems, int total)
        {
            return string.Format("Gems  {0} / {1}", gems, total);
        }

        /// Gem counter with a streak suffix. Only visible at streak ≥ 2 —
        /// the first gem in a chain reads as a normal pickup; from the
        /// second, the multiplier tells the player their chain is alive.
        public static string HudGemsCombo(int gems, int total, int streak)
        {
            if (streak <= 1) return HudGems(gems, total);
            return string.Format("Gems  {0} / {1}  \u00D7{2}",
                gems, total, streak);
        }

        public static string HudLives(int lives)
        {
            return string.Format("Lives  {0}", lives);
        }

        // ---------- Win / game over / completion ----------

        public const string WinTitle = "LEVEL COMPLETE!";
        public const string FirstClear = "First clear!";
        public const string BestPrefix = "Best ";
        public const string NewRecordSuffix = "  (NEW!)";
        public const string NewBestFlash = "NEW BEST!";

        public static string WinStats(int level, string time, string medal,
            int gems, int total, string bestLine)
        {
            return string.Format(
                "Level {0}      Time  {1}{2}      Gems  {3}/{4}\n{5}",
                level, time, medal, gems, total, bestLine);
        }

        public static string MedalBurst(string medal)
        {
            return "   " + medal + "!";
        }

        public const string OverTitle = "GAME OVER";
        public const string OverSub =
            "Every legend takes a nap sometimes. One more try, Pip —\n" +
            "Gloomfang isn't getting less gloomy on his own.";
        public const string TryAgain = "TRY  AGAIN";
        public const string NextLevel = "NEXT  LEVEL";
        public const string Replay = "REPLAY";
        public const string Next = "NEXT";

        public const string CompleteTitle = "EVERY PORTAL LIT!";
        public const string CompleteSub =
            "The storm has a job now. The map has room left.\n" +
            "Pip's shelf keeps one spot open — for whatever comes next.";

        /// Shown on the win screen when a ghost run exists. Without this,
        /// a translucent duplicate of Pip appears on replay and the player
        /// has no idea what it is — reads as a rendering bug.
        public const string GhostHint =
            "Your best run waits — race it back.";

        /// The standing line on the menu when nothing has changed this
        /// session. Replaces the blank that the most common player state
        /// used to show — the corner reads as dead for a returning player.
        public static string VisitRecapStanding(int totalStars, int totalMedals)
        {
            if (totalMedals > 0)
                return "Total stars  " + totalStars +
                    "  ·  Medals  " + totalMedals;
            if (totalStars > 0)
                return "Total stars  " + totalStars;
            return "";
        }
        /// First time the player takes a photo. The mode is always in the
        /// pause menu but nobody ever says so — this puts one word there.
        public const string PhotoFirstHint =
            "Pause the game to take a photo of this view.";

        public static string CompleteStats(int totalStars, int maxStars,
            int totalMedals, int totalGoldens)
        {
            // The old version showed only "Total stars N / M" and nothing
            // else — so the game's biggest moment was also its most
            // unrecognising. Medals, goldens and the warmest framing of
            // partial completion were all computed and unused.
            string golds = totalGoldens >= 0
                ? "\nGoldens found  " + totalGoldens : "";
            string medals = totalMedals > 0
                ? "\nMedals earned  " + totalMedals : "";
            if (totalStars == maxStars)
                return "All levels cleared — " + maxStars +
                    " of " + maxStars + " stars, every one." +
                    medals + golds;
            if (totalStars >= maxStars * 0.8f)
                return "Almost every star in the sky.\nTotal stars  " +
                    totalStars + " / " + maxStars + medals + golds;
            return "Total stars  " + totalStars + " / " + maxStars +
                medals + golds;
        }

        // ---------- Pause / quit ----------

        public const string Paused = "PAUSED";
        public const string Resume = "RESUME";
        public const string RestartLevel = "RESTART LEVEL";
        /// Restart wipes every gem and the whole run timer, so it asks
        /// first — the same guard the QUIT path already had. Wording is
        /// plain about the cost without being stern about it.
        public const string RestartConfirmTitle = "START THIS LEVEL AGAIN?";
        public const string RestartConfirmYes = "RESTART";
        public const string QuitTitle = "QUIT THE GAME?";
        public const string Quit = "QUIT";
        public const string Cancel = "CANCEL";

        // ---------- Settings ----------

        public const string SettingsTitle = "SETTINGS";
        public const string SettingSound = "Sound";
        public const string SettingShake = "Screen Shake";
        public const string SettingHaptics = "Haptics";
        public const string SettingShadows = "Shadows";
        public const string SettingLefty = "Left-handed Controls";
        public const string SettingVoice = "Voice";
        public const string SettingMusic = "Music";
        public const string SettingAmbience = "Ambience";
        public const string SettingMissionText = "Mission Text";
        public const string SettingTextSize = "Text Size";
        public const string SettingFullscreen = "Fullscreen";
        public const string SettingResetProgress = "Reset Progress";
        public const string ResetConfirmTitle = "START OVER?";
        public const string ResetConfirmYes = "START OVER";
        public const string On = "ON";
        public const string Off = "OFF";
        public const string TextSizeNormal = "NORMAL";
        public const string TextSizeLarge = "LARGE";

        public static string ToggleLabel(string name, bool on)
        {
            return name + ":  " + (on ? On : Off);
        }

        public static string TextSizeLabel(bool large)
        {
            return SettingTextSize + ":  " +
                (large ? TextSizeLarge : TextSizeNormal);
        }

        // ---------- The Atlas ----------

        public const string AtlasTitle = "THE ATLAS";
        public const string Locked = "LOCKED";
        public const string NoTimeYet = "Cleared awaits — no time yet";
        public const string Dot = "  ·  ";

        public static string AtlasHeader(string roman, string name)
        {
            return "REGION " + roman + " — " + name.ToUpper();
        }

        public static string AtlasStars(int total, int totalMax,
            int region, int regionMax)
        {
            return "STARS " + total + " / " + totalMax +
                "        REGION " + region + " / " + regionMax;
        }

        public static string AtlasPage(int page, int pages)
        {
            return page + " / " + pages;
        }

        public static string AtlasRowTitle(int number, string name)
        {
            return LevelLabel(number) + "  ·  " + name;
        }

        public static string AtlasRowDetail(int stars, string medal,
            string best)
        {
            return Stars(stars, 3) +
                (medal != "" ? Dot + medal : "") +
                Dot + BestPrefix + best;
        }

        public const string AtlasUncharted =
            "Charted skies, drawn in Pip's small, determined handwriting.";

        /// A short identity line per atlas region, shown under the region
        /// header. Every region looked identical before these — the story
        /// says the map is hand-drawn, but nothing was drawn.
        public static string AtlasRegionFlavour(string regionName)
        {
            switch (regionName)
            {
                case "The Storm":
                    return "Where it all started. The first spark of a very small hero.";
                case "The Rematch":
                    return "Gloomfang brought his best, and Pip brought snacks.";
                case "The Undercloud":
                    return "Down is just a direction. Here, it was a hiding place.";
                case "The Two Suns":
                    return "Two suns, one Tuesday, and the sky remembering what it was for.";
                case "The Far Isles":
                    return "Where the wind holds you up and nobody hurries.";
                case "Gloomfang's Day Off":
                    return "One level, one storm, and a sunset that asked for nothing.";
                case "The Sky Garden":
                    return "The garden that grew because someone finally sang.";
                case "Storm Chasers":
                    return "Where the smallest weather does the most running.";
                case "The Bell Towers":
                    return "Towers that remember every storm they ever sang.";
                case "Mirror Skies":
                    return "A sky that copies you, then gets there first.";
                case "The B-Sides":
                    return "After dark, the same places look different — and so does the work.";
                case "The Long Winter":
                    return "Snow holds still. Pip holds the lantern. Both are patient.";
                case "The Aurora Festival":
                    return "The sky said thank you, and it meant every colour.";
                case "The Homecoming":
                    return "The road home was worth the atlas it took to find.";
                default:
                    return "";
            }
        }
        public const string StampCharted = "REGION CHARTED";
        public const string StampPerfect = "PERFECT CHART";

        // ---------- Golden gems (remix gate) ----------

        public const string DailySuffix = "  · DAILY";
        public const string GoldenSuffix = "  · GOLD";
        public const string GoldenGateLocked = "FIND THE GOLDEN GEM";
        public static string GoldenGateHint(string sourceName)
        {
            return "Find the golden gem in " + sourceName;
        }
        public const string GoldenFoundLine =
            "A golden gem! The atlas glints.";
        public static string GoldenUnlocksRemix(string remixName)
        {
            return "The golden gem! " + remixName + " awaits at night.";
        }

        // A one-line, canon-voice note on why this golden was hiding
        // where it was: narrative weight instead of filler (the secret-
        // craft law — a found thing should have a story, not just a
        // pickup). Evergreen law applies: no counts, no level numbers,
        // no statuses; the narrator is fond of everyone, the gem included.
        static readonly string[] GoldenNotes =
        {
            "Someone set this one down off the path and forgot to worry about it.",
            "It waited here the whole time, perfectly patient, in no hurry at all.",
            "Not lost. Just kept somewhere the trail doesn't bother to go.",
            "A small gold thing that liked the quiet corner best.",
            "Left here for whoever wandered. That turned out to be you.",
            "It hid because hiding is a game — not because it didn't want finding."
        };

        /// Deterministic per level (name-seeded, the save-key lesson), so
        /// a level's golden always carries the same note: a found thing
        /// has a story, and the story doesn't change between visits.
        public static string GoldenNote(string levelName)
        {
            if (string.IsNullOrEmpty(levelName)) return GoldenNotes[0];
            int seed = 0;
            for (int i = 0; i < levelName.Length; i++) seed += levelName[i];
            return GoldenNotes[seed % GoldenNotes.Length];
        }

        // ---------- Photo mode ----------

        public const string Photo = "PHOTO";
        public const string PhotoCapture = "CAPTURE";
        public const string PhotoDone = "DONE";
        public const string PhotoOpenFolder = "OPEN FOLDER";
        public const string PhotoHint = "A gentle orbit — CAPTURE takes the shot.";
        public static string PhotoSavedTo(string folder)
        {
            return "Saved to " + folder;
        }
        public static string PhotoSaveFailed(string folder)
        {
            return "Could not save to " + folder;
        }

        // ---------- Level intro ----------

        public static string IntroTitle(int number, string name)
        {
            return LevelLabel(number) + "  —  " + name.ToUpper();
        }

        // ---------- Shared bits ----------

        public static string Stars(int stars, int max)
        {
            return "Stars " + stars + "/" + max;
        }

        // ---------- World flavor (non-level story toasts) ----------

        /// Shown briefly when Pip is knocked back to the checkpoint. Death
        /// used to be completely silent — a burst, then a teleport with no
        /// word — which left the most frequent event in the game
        /// unacknowledged, especially for a child.
        ///
        /// These never scold and never grade: failure here is a nap, not a
        /// verdict (the design law). Several lines so the fourth death of a
        /// level is not the same sentence as the first. Picked by a stable
        /// per-life counter rather than at random, so a retry does not
        /// shuffle the words mid-run.
        static readonly string[] ComebackLines =
        {
            "Pip is fine. Pip is always fine. The guardian is still spinning, though.",
            "A short nap, a deep breath, and back up the hill.",
            "Gloomfang pretends not to have seen that. He saw it. He's rooting for you.",
            "The sky realm is patient. It has been waiting a thousand years; it can wait for a retry.",
            "Down here, the only thing that breaks is the fall. Pip doesn't.",
            "Take the run again. The gems will wait exactly where they were."
        };

        /// A comeback line for the given death count (0-based), stable for
        /// a given life so the words do not change under the player.
        public static string ComebackLine(int deathIndex)
        {
            if (deathIndex < 0) deathIndex = 0;
            return ComebackLines[deathIndex % ComebackLines.Length];
        }

        public const string LanternWake =
            "The sunstone wakes. Its warm little light hops up to travel with Pip.";

        public const string BellHint =
            "Ring the bell — its song builds the bridge.";

        /// Shown once, at the start of the very first level, in the same
        /// band the checkpoint beats use. The menu's instruction block is
        /// easy to miss and useless mid-jump; this says the two things a
        /// player actually needs in the moment they need them, then gets
        /// out of the way for good.
        public static string FirstStepsHint()
        {
            if (Input.touchSupported)
                return "Drag the left side to move. Tap JUMP to jump.";
            return "Move with WASD or the arrow keys. Jump with Space.";
        }

        /// StarTrail milestones: the sparkle wake behind Pip changes colour
        /// as stars are earned. Each tier announces itself once per device
        /// (the line explains why the colour changed), and the world keeps
        /// a permanent record of what the trail means.
        public const string TrailTier1 =
            "Fifteen stars — a golden sparkle wake now drifts behind Pip.";

        public const string TrailTier2 =
            "Thirty stars — the wake turns festival pink.";

        public const string TrailTier3 =
            "Forty-five stars — the wake glows aurora green, the sky's own colour.";

        // (A "TrailLegend" line used to sit here, described as a menu
        // legend shown once unlocked. Nothing ever displayed it — the
        // comment promised behaviour that did not exist, and the string
        // was unreachable. Removed rather than wired up: the three tier
        // lines above already announce everything the legend restated.)

        public static string TrailTierLine(int tier)
        {
            if (tier >= 3) return TrailTier3;
            if (tier == 2) return TrailTier2;
            return TrailTier1;
        }
    }
}
