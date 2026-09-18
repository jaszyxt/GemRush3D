using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// The musical identity of a level. Auto derives one from the level's
    /// realm flags; anything distinctive (golden hour, wind realms, bell
    /// towers) sets an explicit mood in its pack file.
    public enum SoundMood
    {
        Auto,
        Menu,
        Day,
        Dark,
        Sunset,
        Garden,
        Wind,
        Bells,
        Flight,
        Mirror
    }

    /// Pure data describing one level. No behaviour lives here — LevelBuilder
    /// turns a definition into GameObjects.
    public class LevelDefinition
    {
        public string Name = "Untitled";
        public string Mission = "";
        public string WinLine = "";

        /// Atlas stamp shown on the win screen. Set on the last level of
        /// each pack, so finishing a region feels like the world growing
        /// one page.
        public string Milestone = "";

        /// Which score the AudioManager plays here. Auto resolves from the
        /// realm flags below (flight/dark/garden/mirror); daylight levels
        /// land on Day unless the pack asks for something else.
        public SoundMood Mood = SoundMood.Auto;

        /// The resolved mood: explicit assignment first, then the realm
        /// that most defines how this level feels.
        public SoundMood ResolveMood()
        {
            if (Mood != SoundMood.Auto) return Mood;
            if (BonusFlight) return SoundMood.Flight;
            if (DarkRealm) return SoundMood.Dark;
            if (SkyGarden) return SoundMood.Garden;
            if (MirrorSkies) return SoundMood.Mirror;
            return SoundMood.Day;
        }

        /// One line of story shown when each checkpoint is touched, in order.
        /// Fewer lines than checkpoints is fine; extra lines are ignored.
        public List<string> StoryBeats = new List<string>();

        public Vector3 Spawn = new Vector3(0f, 1.5f, 0f);
        public float KillY = -12f;
        public Vector3 Portal = new Vector3(0f, 0.5f, 10f);

        /// Undercloud levels: dark fog, dimmer sun, moody sky.
        public bool DarkRealm = false;
        public Color SkyColor = new Color(0.47f, 0.72f, 0.98f);
        public Color FogColor = new Color(0.47f, 0.72f, 0.98f);

        public List<PlatformSpec> Platforms = new List<PlatformSpec>();
        public List<MoverSpec> Movers = new List<MoverSpec>();
        public List<SpinnerSpec> Spinners = new List<SpinnerSpec>();
        public List<Vector3> Gems = new List<Vector3>();
        public List<Vector3> Checkpoints = new List<Vector3>();

        /// Platform-top positions for springy launch pads.
        public List<Vector3> BouncePads = new List<Vector3>();

        /// World positions for one-extra-life pickups.
        public List<Vector3> Hearts = new List<Vector3>();

        /// Rising-wind columns: base center, box size, lift strength.
        public List<WindSpec> WindZones = new List<WindSpec>();

        /// Tailwind gusts: periodic wind walls that carry Pip across gaps.
        public List<GustSpec> Gusts = new List<GustSpec>();

        /// Echo bells: ring to solidify linked echo bridges for a while.
        public List<BellSpec> Bells = new List<BellSpec>();

        /// Hidden bridges that exist only while their bell's echo rings.
        public List<EchoBridgeSpec> EchoBridges = new List<EchoBridgeSpec>();

        /// Paired mirror doors: walking into A exits at B, and back.
        public List<MirrorDoorSpec> MirrorDoors = new List<MirrorDoorSpec>();

        /// Mirror Skies levels: a translucent Gloomfang drifts on the
        /// mirrored side of the sky, copying your every move.
        public bool MirrorSkies = false;

        /// Bonus flight level: Pip stays home and Gloomfang is playable —
        /// no gravity, gentle drift, no fall deaths.
        public bool BonusFlight = false;

        /// Sky Garden levels: gems carry melody notes, flower buds decorate
        /// the platforms, and touching the portal blooms the whole garden.
        public bool SkyGarden = false;

        public int GemCount { get { return Gems.Count; } }

        // ---------- Medal times ----------
        // Derived from course geometry so new levels get targets for free:
        // roughly a brisk, deathless run. Best times are already saved, so
        // medals need no extra storage.

        public float ParTime
        {
            get
            {
                float len = 0f;
                for (int i = 0; i < Platforms.Count; i++)
                    len = Mathf.Max(len, Platforms[i].Center.z + Platforms[i].Size.z * 0.5f);
                len = Mathf.Max(len, Portal.z);
                return len * 0.42f + Movers.Count * 5f + Spinners.Count * 4f
                    + BouncePads.Count * 2f + 10f;
            }
        }

        public float GoldTime { get { return ParTime; } }
        public float SilverTime { get { return ParTime * 1.35f; } }
        public float BronzeTime { get { return ParTime * 1.9f; } }

        public string MedalFor(float time)
        {
            if (time <= GoldTime) return "GOLD";
            if (time <= SilverTime) return "SILVER";
            if (time <= BronzeTime) return "BRONZE";
            return "";
        }
    }

    public class PlatformSpec
    {
        public Vector3 Center;
        public Vector3 Size = new Vector3(5f, 1f, 5f);

        public PlatformSpec(float x, float y, float z, float w, float h, float d)
        {
            Center = new Vector3(x, y, z);
            Size = new Vector3(w, h, d);
        }
    }

    public class MoverSpec
    {
        public Vector3 Center;
        public Vector3 Size = new Vector3(4f, 1f, 4f);
        public Vector3 Offset = new Vector3(4f, 0f, 0f);
        public float Period = 4f;

        public MoverSpec(float x, float y, float z, Vector3 offset, float period)
        {
            Center = new Vector3(x, y, z);
            Offset = offset;
            Period = period;
        }
    }

    public class SpinnerSpec
    {
        /// Position of the platform's TOP surface the spinner stands on.
        public Vector3 PlatformTop;
        public float DegreesPerSecond = 60f;
        /// Sleeping Guardian: wake radius (0 = always awake) and dozing pace.
        public float WakeRadius = 0f;
        public float SleepSpeed = 12f;

        public SpinnerSpec(float x, float y, float z, float speed)
        {
            PlatformTop = new Vector3(x, y, z);
            DegreesPerSecond = speed;
        }

        public SpinnerSpec(float x, float y, float z, float speed,
            float wakeRadius, float sleepSpeed)
        {
            PlatformTop = new Vector3(x, y, z);
            DegreesPerSecond = speed;
            WakeRadius = wakeRadius;
            SleepSpeed = sleepSpeed;
        }
    }

    public class WindSpec
    {
        public Vector3 Center;
        public Vector3 Size = new Vector3(3f, 6f, 3f);
        public float Lift = 11f;

        public WindSpec(float x, float y, float z, Vector3 size, float lift)
        {
            Center = new Vector3(x, y, z);
            Size = size;
            Lift = lift;
        }
    }

    public class GustSpec
    {
        public Vector3 Center;
        public Vector3 Size = new Vector3(5f, 4f, 12f);
        /// Unit axis the gust blows toward (e.g. +z down the course).
        public Vector3 Direction = new Vector3(0f, 0f, 1f);
        public float Period = 4.4f;     // matches two music chords
        public float ActiveTime = 2.2f; // how long each blow lasts
        public float Strength = 8f;     // horizontal carry speed while active
        /// Vertical sustain: without it gravity arcs Pip into the void
        /// mid-crossing. A real tailwind holds you up.
        public float Lift = 0f;

        public GustSpec(float x, float y, float z, Vector3 size,
            Vector3 direction, float period, float activeTime, float strength,
            float lift = 0f)
        {
            Center = new Vector3(x, y, z);
            Size = size;
            Direction = direction;
            Period = period;
            ActiveTime = activeTime;
            Strength = strength;
            Lift = lift;
        }
    }

    public class BellSpec
    {
        public Vector3 PlatformTop;   // where the bell stands
        public float ToneSeconds;     // how long its echo keeps bridges solid
        public int Index;             // which echo bridges it reveals

        public BellSpec(float x, float y, float z, float toneSeconds, int index)
        {
            PlatformTop = new Vector3(x, y, z);
            ToneSeconds = toneSeconds;
            Index = index;
        }
    }

    public class EchoBridgeSpec
    {
        public Vector3 Center;
        public Vector3 Size = new Vector3(3f, 0.5f, 10f);
        public int BellIndex;

        public EchoBridgeSpec(float x, float y, float z, Vector3 size, int bellIndex)
        {
            Center = new Vector3(x, y, z);
            Size = size;
            BellIndex = bellIndex;
        }
    }
    public class MirrorDoorSpec
    {
        public Vector3 DoorA;
        public Vector3 DoorB;

        public MirrorDoorSpec(float ax, float ay, float az,
            float bx, float by, float bz)
        {
            DoorA = new Vector3(ax, ay, az);
            DoorB = new Vector3(bx, by, bz);
        }
    }
}
