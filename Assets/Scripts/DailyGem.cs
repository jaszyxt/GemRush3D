using UnityEngine;

namespace GemRush
{
    /// The Daily Gem: one level per day hides a single golden gem. Find it
    /// and Gloomfang's Gift counter ticks up. Miss a day and nothing bad
    /// happens — ever. The target level is date-seeded among *unlocked*
    /// levels only, so the gift is always reachable.
    public static class DailyGem
    {
        /// Set by GameBootstrap before a world is built, so the builder
        /// knows whether THIS level is today's host.
        public static int ActiveLevelIndex = -1;

        static string Today()
        {
            return System.DateTime.Today.ToString("yyyy-MM-dd");
        }

        static int DateSeed()
        {
            System.DateTime d = System.DateTime.Today;
            return d.Year * 372 + d.Month * 31 + d.Day;
        }

        /// Today's host level: a stable pick among the unlocked ones.
        public static int TodayIndex()
        {
            int unlocked = SaveSystem.UnlockedLevel + 1;
            int count = LevelLibrary.Levels.Length;
            unlocked = Mathf.Clamp(unlocked, 1, count);
            return DateSeed() % unlocked;
        }

        public static bool GiftAlreadyCollectedToday()
        {
            return SaveSystem.LastGiftDate == Today();
        }

        /// Called by LevelBuilder: spawns the golden gem if this build is
        /// today's host level and the gift hasn't been taken yet.
        public static void PlaceIfActive(LevelDefinition level, Transform parent)
        {
            if (ActiveLevelIndex != TodayIndex()) return;
            if (GiftAlreadyCollectedToday()) return;

            // Deterministic spot: an arena-sized platform away from any
            // spinner AND away from the portal, picked by the date seed.
            // The portal rule is the same class of bug the golden gem had
            // (player-reported): a gift sitting at the exit is missed by
            // anyone running for the finish, so a daily reward would
            // silently go unclaimed on the days it landed there.
            System.Random rng = new System.Random(DateSeed());
            GameObject golden = new GameObject("DailyStar");
            golden.transform.SetParent(parent, false);
            int tries = 0;
            Vector3 fallback = Vector3.zero;
            bool haveFallback = false;
            while (tries++ < 20)
            {
                PlatformSpec p = level.Platforms[rng.Next(level.Platforms.Count)];
                if (Mathf.Min(p.Size.x, p.Size.z) < 5f) continue;
                float px = ((float)rng.NextDouble() * 2f - 1f) * (p.Size.x * 0.5f - 1f);
                float pz = ((float)rng.NextDouble() * 2f - 1f) * (p.Size.z * 0.5f - 1f);
                Vector3 spot = p.Center + new Vector3(px,
                    p.Size.y * 0.5f + 1.2f, pz);

                bool nearSpinner = false;
                foreach (SpinnerSpec s in level.Spinners)
                {
                    if (Vector2.Distance(
                        new Vector2(spot.x, spot.z),
                        new Vector2(s.PlatformTop.x, s.PlatformTop.z)) < 6f)
                    {
                        nearSpinner = true;
                        break;
                    }
                }
                if (nearSpinner) continue;

                // Keep the gift off the exit pad.
                float toPortal = Vector2.Distance(
                    new Vector2(spot.x, spot.z),
                    new Vector2(level.Portal.x, level.Portal.z));
                if (toPortal < MinPortalDistance)
                {
                    // Usable if nothing better turns up — a gift near the
                    // exit still beats no gift at all.
                    if (!haveFallback) { fallback = spot; haveFallback = true; }
                    continue;
                }

                Spawn(golden, spot);
                return;
            }

            // Every try was rejected. This used to destroy the object and
            // place nothing, so on a spinner-dense level the day's gift
            // could silently not exist while the UI still counted it. Fall
            // back to a spinner-free spot, then to anything standable.
            if (haveFallback) { Spawn(golden, fallback); return; }
            Vector3 relaxed = RelaxedSpot(level);
            if (relaxed != Vector3.zero) { Spawn(golden, relaxed); return; }
            Object.Destroy(golden);
        }

        /// The golden gem's rule, shared: a reward is never parked on the
        /// exit pad, where finishing the level is the natural thing to do.
        public const float MinPortalDistance = 15f;

        /// Last resort: any platform big enough to stand on, ignoring the
        /// spinner and portal preferences, so a gift always exists.
        static Vector3 RelaxedSpot(LevelDefinition level)
        {
            for (int i = 0; i < level.Platforms.Count; i++)
            {
                PlatformSpec p = level.Platforms[i];
                if (Mathf.Min(p.Size.x, p.Size.z) < 4f) continue;
                return p.Center + new Vector3(0f, p.Size.y * 0.5f + 1.2f, 0f);
            }
            return Vector3.zero;
        }

        static void Spawn(GameObject golden, Vector3 spot)
        {
            // The body lives on a CHILD and is the only thing that animates.
            // The root keeps the trigger and stays still — see DailyStar.
            Material gold = ArtLib.Solid(ArtLib.Gold, 2.2f);
            GameObject body = ArtLib.DecorSphere(golden.transform, Vector3.zero,
                new Vector3(0.5f, 0.5f, 0.5f), gold);

            SphereCollider trigger = golden.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.5f;

            DailyStar star = golden.AddComponent<DailyStar>();
            star.today = Today();
            star.body = body != null ? body.transform : null;
            golden.transform.localPosition = spot;
        }
    }

    /// The golden gem itself. Gentle spin, generous trigger, one gift.
    ///
    /// [Preserve] IS LOAD-BEARING — do not remove it. The daily gift is
    /// created ONLY through AddComponent<DailyStar>() and the type is
    /// referenced nowhere else in managed code, so IL2CPP's linker is free
    /// to strip it from the Android player. When that happens the component
    /// is gone from the build and the gift simply never collects — exactly
    /// the failure that made every golden uncollectable on the phone while
    /// working on the laptop, since Windows player builds run Mono, which
    /// strips nothing. See GoldenStar for the full account.
    [UnityEngine.Scripting.Preserve]
    public class DailyStar : MonoBehaviour
    {
        public string today;
        /// The visual child — the only thing that spins and bobs.
        public Transform body;
        bool taken;
        bool magnetized;
        Vector3 bodyBase;

        /// Same courtesy the ordinary gems extend (see GoldenGem): slide to
        /// Pip once he is close, which is why a normal gem is never missed.
        const float MagnetRadius = 2.2f;
        const float MagnetSpeed = 9f;

        void Start()
        {
            bodyBase = body != null ? body.localPosition : Vector3.zero;
        }

        /// The ROOT carries the trigger and must never move: this project
        /// runs with Physics.autoSyncTransforms OFF, so a transform moved
        /// in Update leaves its collider's physics position behind and the
        /// pickup stops firing. The gift used to bob its root here, the
        /// same defect that made every golden uncollectable. Only the
        /// visual child animates.
        void Update()
        {
            if (taken) return;

            if (body != null)
            {
                body.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
                body.localPosition = bodyBase + Vector3.up *
                    (Mathf.Sin(Time.time * ArtLib.HoverBobRate) * 0.14f);
            }

            if (!magnetized)
            {
                Transform pip = GameBootstrap.Player != null
                    ? GameBootstrap.Player.transform : null;
                if (pip != null && (pip.position - transform.position)
                        .sqrMagnitude < MagnetRadius * MagnetRadius)
                    magnetized = true;
            }
            if (magnetized)
            {
                Transform pip = GameBootstrap.Player != null
                    ? GameBootstrap.Player.transform : null;
                if (pip == null) return;
                transform.position = Vector3.MoveTowards(
                    transform.position, pip.position + Vector3.up * 0.6f,
                    MagnetSpeed * Time.deltaTime);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (taken) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            taken = true;

            SaveSystem.AddGift(today);
            Fx.Burst(transform.position, ArtLib.Gold * 1.8f, 30);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowStoryToast(
                    "Gloomfang's Gift found! (" + SaveSystem.Gifts + " total)");
            AudioManager.Instance.PlayGift();
            Destroy(gameObject);
        }
    }
}
