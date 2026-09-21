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

        /// The highest StarTrail tier the player has already been told
        /// about, so each new trail colour announces itself exactly once
        /// per device. 0 = none announced yet.
        public static int TrailTierAnnounced
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "trailtier", 0); }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "trailtier", value); Save(); }
        }

        // ---------- Settings ----------

        public static bool SoundOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "sound", 1) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "sound", value ? 1 : 0); Save(); }
        }

        /// The score. Under the master Sound gate; lets a player keep the
        /// music while muting the wind and rumble beds (or the reverse).
        public static bool MusicOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "music", 1) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "music", value ? 1 : 0); Save(); }
        }

        /// Ambient beds (wind, rumble, rain, snow) and the portal hum —
        /// the atmospheric layer under the score.
        public static bool AmbienceOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "ambience", 1) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "ambience", value ? 1 : 0); Save(); }
        }

        /// Voice narration. Defaults on and rides under the master Sound
        /// gate (no sound, no voice); the VoiceOver channel reads this.
        public static bool VoiceOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "voice", 1) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "voice", value ? 1 : 0); Save(); }
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

        /// Vestibular comfort: the death shake is the only camera motion
        /// effect in the game, so one toggle covers motion sensitivity.
        public static bool ShakeOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "shake", 1) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "shake", value ? 1 : 0); Save(); }
        }

        /// Mirrors the touch layout: joystick on the dominant side, jump in
        /// the other thumb's arc.
        public static bool LeftyOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "lefty", 0) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "lefty", value ? 1 : 0); Save(); }
        }

        /// Desktop-only display mode: borderless fullscreen when true,
        /// windowed when false. Mobile builds never read it — they are
        /// always fullscreen.
        public static bool FullscreenOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "fullscreen", 1) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "fullscreen", value ? 1 : 0); Save(); }
        }

        /// Show the level's mission briefing as on-screen text at level
        /// start. Off by default: the briefing is narrated (VoiceOver is
        /// the intended channel), so the band is opt-in rather than
        /// something every player has to dismiss.
        public static bool MissionTextOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "missiontext", 0) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "missiontext", value ? 1 : 0); Save(); }
        }

        /// Larger UI text for low-vision players: every label re-derives
        /// from its designed size (UIManager.ApplyTextSize), so Large mode
        /// never overflows a layout fitted for the default.
        public static bool TextLargeOn
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "textlarge", 0) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "textlarge", value ? 1 : 0); Save(); }
        }

        /// Cleared the Aurora Festival finale: a permanent aurora hangs
        /// over the menu screen forever. Pure celebration — nothing is
        /// gated behind it.
        public static bool AuroraUnlocked
        {
            get { MigrateIfNeeded(); return PlayerPrefs.GetInt(Prefix + "aurora", 0) == 1; }
            set { MigrateIfNeeded(); PlayerPrefs.SetInt(Prefix + "aurora", value ? 1 : 0); Save(); }
        }

        /// The golden-gem remix gate: one hidden golden gem per level;
        /// finding a source level's golden unlocks its night remix
        /// (B-side). Every golden is also plain collection.
        public static bool GoldenFound(int level)
        {
            MigrateIfNeeded();
            return PlayerPrefs.GetInt(LevelKey(level, "golden"), 0) == 1;
        }

        public static void SetGoldenFound(int level)
        {
            MigrateIfNeeded();
            PlayerPrefs.SetInt(LevelKey(level, "golden"), 1);
            Save();
        }

        /// How many goldens have been found across the atlas.
        public static int TotalGoldens(int levelCount)
        {
            MigrateIfNeeded();
            int n = 0;
            for (int i = 0; i < levelCount; i++)
                if (GoldenFound(i)) n++;
            return n;
        }

        /// The date of Pip's very first flight on this device, recorded
        /// once on first boot. The living calendar celebrates its
        /// anniversary with a festival week of confetti skies — gently,
        /// with no FOMO ever.
        public static string FirstFlightDate
        {
            get
            {
                MigrateIfNeeded();
                string date = PlayerPrefs.GetString(Prefix + "firstflight", "");
                if (string.IsNullOrEmpty(date))
                {
                    date = System.DateTime.Today.ToString("yyyy-MM-dd");
                    PlayerPrefs.SetString(Prefix + "firstflight", date);
                    Save();
                }
                return date;
            }
        }

        /// The normalized key for a level's name (the cloud mirror and
        /// ghost store share these rules).
        public static string KeyForLevel(int level)
        {
            MigrateIfNeeded();
            int index = Mathf.Clamp(level, 0, LevelLibrary.Levels.Length - 1);
            return Key(LevelLibrary.Levels[index].Name);
        }

        static void Save()
        {
            PlayerPrefs.Save();
            CloudSaveMirror.Snapshot();
        }

        // ---------- Windows hive migration (one-time) ----------

        // v1.25.0 changed companyName DefaultCompany -> PipStudio for exe
        // metadata, which silently MOVED PlayerPrefs' registry hive on
        // Windows (HKCU/Software/<company>/<product>) — every pre-1.25
        // install looked wiped. This one-time migration copies the old
        // hive's values into the new one (old wins, the real progress)
        // and flags it done. Player builds only; harmless elsewhere.
        public static void MigrateCompanyHive()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (PlayerPrefs.GetInt(Prefix + "hivemigrated", 0) == 1) return;
            try
            {
                // Microsoft.Win32.Registry is NOT in the netstandard 2.1
                // player profile — the typed call broke every player build
                // (CS1069). Mono's runtime keeps the API inside mscorlib on
                // Windows, so resolve it by reflection; where stripping has
                // removed it, the migration silently skips. Best-effort by
                // design: never blocks boot.
                System.Reflection.Assembly corlib = typeof(int).Assembly;
                System.Type regT = corlib.GetType("Microsoft.Win32.Registry");
                System.Type keyT =
                    corlib.GetType("Microsoft.Win32.RegistryKey");
                if (regT == null || keyT == null) return;
                object currentUser =
                    regT.GetField("CurrentUser").GetValue(null);
                if (currentUser == null) return;
                System.Reflection.MethodInfo open = keyT.GetMethod(
                    "OpenSubKey", new[] { typeof(string) });
                System.Reflection.MethodInfo create = keyT.GetMethod(
                    "CreateSubKey", new[] { typeof(string) });
                System.Reflection.MethodInfo names = keyT.GetMethod(
                    "GetValueNames");
                System.Reflection.MethodInfo kindOf = keyT.GetMethod(
                    "GetValueKind", new[] { typeof(string) });
                System.Reflection.MethodInfo read = keyT.GetMethod(
                    "GetValue", new[] { typeof(string) });
                System.Reflection.MethodInfo write = keyT.GetMethod(
                    "SetValue", new[] { typeof(string), typeof(object),
                        kindOf.ReturnType });
                object old = open.Invoke(currentUser, new object[] {
                    @"Software\DefaultCompany\Gem Rush 3D" });
                object current = create.Invoke(currentUser, new object[] {
                    @"Software\PipStudio\Gem Rush 3D" });
                if (old != null && current != null)
                {
                    string[] valueNames = (string[])names.Invoke(old, null);
                    foreach (string name in valueNames)
                    {
                        if (string.IsNullOrEmpty(name)) continue;
                        object kind = kindOf.Invoke(old,
                            new object[] { name });
                        object value = read.Invoke(old,
                            new object[] { name });
                        write.Invoke(current,
                            new[] { name, value, kind });
                    }
                }
                if (old != null)
                    keyT.GetMethod("Close").Invoke(old, null);
                if (current != null)
                    keyT.GetMethod("Close").Invoke(current, null);
                PlayerPrefs.SetInt(Prefix + "hivemigrated", 1);
                Save();
            }
            catch (System.Exception)
            {
                // Migration is best-effort; never block boot.
            }
#endif
        }
    }
}
