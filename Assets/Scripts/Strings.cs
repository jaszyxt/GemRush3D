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

        public static string HudLives(int lives)
        {
            return string.Format("Lives  {0}", lives);
        }

        // ---------- Win / game over / completion ----------

        public const string WinTitle = "LEVEL COMPLETE!";
        public const string FirstClear = "First clear!";
        public const string BestPrefix = "Best ";
        public const string NewRecordSuffix = "  (NEW!)";

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

        public static string CompleteStats(int totalStars, int maxStars)
        {
            return string.Format(
                "All levels cleared!\nTotal stars  {0} / {1}",
                totalStars, maxStars);
        }

        // ---------- Pause / quit ----------

        public const string Paused = "PAUSED";
        public const string Resume = "RESUME";
        public const string RestartLevel = "RESTART LEVEL";
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
        public const string SettingTextSize = "Text Size";
        public const string SettingFullscreen = "Fullscreen";
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
        public const string StampCharted = "REGION CHARTED";
        public const string StampPerfect = "PERFECT CHART";

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

        public const string LanternWake =
            "The sunstone wakes. Its warm little light hops up to travel with Pip.";
    }
}
