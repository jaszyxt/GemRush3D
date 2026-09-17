using UnityEngine;

namespace GemRush
{
    /// Persistent progress and settings, stored in PlayerPrefs with
    /// versioned keys. Progress is keyed by level NAME (not index), so
    /// inserting new packs can never shift a player's stars onto the wrong
    /// levels. Legacy index-keyed saves are migrated once, automatically.
    public static class SaveSystem
    {
        const string Prefix = "gemrush_v2_";
        const string LegacyPrefix = "gemrush_v1_";
        static bool migrated;

        static SaveSystem()
        {
            MigrateIfNeeded();
        }

        /// One-time upgrade from the v1 index-keyed save: carries unlocked
        /// progress, best times and stars over to name-keyed entries, then
        /// removes the old keys so the migration can never double-run.
        static void MigrateIfNeeded()
        {
            if (migrated) return;
            migrated = true;
            if (!PlayerPrefs.HasKey(LegacyPrefix + "unlocked")) return;

            PlayerPrefs.SetInt(Prefix + "unlockedCount",
                PlayerPrefs.GetInt(LegacyPrefix + "unlocked", 0) + 1);

            LevelDefinition[] levels = LevelLibrary.Levels;
            for (int i = 0; i < levels.Length; i++)
            {
                float best = PlayerPrefs.GetFloat(LegacyPrefix + "best_" + i, -1f);
                int stars = PlayerPrefs.GetInt(LegacyPrefix + "stars_" + i, 0);
                if (best >= 0f)
                    PlayerPrefs.SetFloat(Prefix + Key(levels[i].Name) + "_best", best);
                if (stars > 0)
                    PlayerPrefs.SetInt(Prefix + Key(levels[i].Name) + "_stars", stars);
                PlayerPrefs.DeleteKey(LegacyPrefix + "best_" + i);
                PlayerPrefs.DeleteKey(LegacyPrefix + "stars_" + i);
            }
            Move(LegacyPrefix + "gifts", Prefix + "gifts");
            Move(LegacyPrefix + "giftdate", Prefix + "giftdate");
            Move(LegacyPrefix + "sound", Prefix + "sound");
            Move(LegacyPrefix + "haptics", Prefix + "haptics");
            Move(LegacyPrefix + "shadows", Prefix + "shadows");
            PlayerPrefs.DeleteKey(LegacyPrefix + "unlocked");
            PlayerPrefs.Save();
        }

        static void Move(string from, string to)
        {
            if (!PlayerPrefs.HasKey(from)) return;
            PlayerPrefs.SetInt(to, PlayerPrefs.GetInt(from, 0));
            PlayerPrefs.DeleteKey(from);
        }

        /// Stable storage key for a level: lowercase letters and digits only.
        static string Key(string levelName)
        {
            char[] chars = levelName.ToLower().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
            return new string(chars);
        }

        static string LevelKey(int level, string field)
        {
            MigrateIfNeeded();
            int index = Mathf.Clamp(level, 0, LevelLibrary.Levels.Length - 1);
            return Prefix + Key(LevelLibrary.Levels[index].Name) + "_" + field;
        }

        // ---------- Progression ----------

        /// Highest unlocked level index, derived from an unlocked COUNT so
        /// it stays meaningful as levels are added.
        public static int UnlockedLevel
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "unlockedCount", 1) - 1; }
        }

        public static void UnlockLevel(int index)
        {
            MigrateIfNeeded();
            if (index > UnlockedLevel)
                PlayerPrefs.SetInt(Prefix + "unlockedCount", index + 1);
        }

        public static float BestTime(int level)
        {
            return PlayerPrefs.GetFloat(LevelKey(level, "best"), -1f);
        }

        public static int Stars(int level)
        {
            return PlayerPrefs.GetInt(LevelKey(level, "stars"), 0);
        }

        /// Keeps the best result per level. Returns true if a record improved.
        public static bool RecordResult(int level, float time, int stars)
        {
            MigrateIfNeeded();
            bool improved = false;
            float best = BestTime(level);
            if (best < 0f || time < best)
            {
                PlayerPrefs.SetFloat(LevelKey(level, "best"), time);
                improved = true;
            }
            if (stars > Stars(level))
            {
                PlayerPrefs.SetInt(LevelKey(level, "stars"), stars);
                improved = true;
            }
            if (improved) Save();
            return improved;
        }

        public static int TotalStars(int levelCount)
        {
            int total = 0;
            for (int i = 0; i < levelCount; i++) total += Stars(i);
            return total;
        }

        /// Gold/silver/bronze medals are derived from best times, so they
        /// need no storage of their own.
        public static int TotalMedals(int levelCount)
        {
            int total = 0;
            for (int i = 0; i < levelCount; i++)
            {
                float best = BestTime(i);
                if (best < 0f) continue;
                string medal = LevelLibrary.Levels[i].MedalFor(best);
                if (medal != "") total++;
            }
            return total;
        }

        // ---------- The Daily Gem ----------

        public static int Gifts
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "gifts", 0); }
        }

        public static string LastGiftDate
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetString(Prefix + "giftdate", ""); }
        }

        public static void AddGift(string today)
        {
            MigrateIfNeeded();
            PlayerPrefs.SetInt(Prefix + "gifts", Gifts + 1);
            PlayerPrefs.SetString(Prefix + "giftdate", today);
            Save();
        }

        // ---------- Session recap ----------

        /// Snapshot taken at boot, so the menu can show what changed since.
        public static int VisitStartStars;
        public static int VisitStartMedals;

        public static void SnapshotVisit()
        {
            int count = LevelLibrary.Levels.Length;
            VisitStartStars = TotalStars(count);
            VisitStartMedals = TotalMedals(count);
        }

        // ---------- Settings ----------

        public static bool SoundOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "sound", 1) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "sound", value ? 1 : 0); Save(); }
        }

        public static bool HapticsOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "haptics", 1) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "haptics", value ? 1 : 0); Save(); }
        }

        public static bool ShadowsOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "shadows", 1) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "shadows", value ? 1 : 0); Save(); }
        }

        static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
