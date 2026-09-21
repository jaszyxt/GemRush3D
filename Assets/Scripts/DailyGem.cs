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
            Material gold = ArtLib.Solid(ArtLib.Gold, 2.2f);
            ArtLib.DecorSphere(golden.transform, Vector3.zero,
                new Vector3(0.5f, 0.5f, 0.5f), gold);
            BoxCollider trigger = golden.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = Vector3.one * 1.3f;
            DailyStar star = golden.AddComponent<DailyStar>();
            star.today = Today();
            star.transform.localPosition = spot;
        }
    }

    /// The golden gem itself. Gentle spin, generous trigger, one gift.
    public class DailyStar : MonoBehaviour
    {
        public string today;
        bool taken;

        void Update()
        {
            transform.Rotate(0f, 120f * Time.deltaTime, 0f);
            transform.localPosition += Vector3.up *
                (Mathf.Sin(Time.time * ArtLib.HoverBobRate) * 0.003f);
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
