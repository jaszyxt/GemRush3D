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
            // spinner, picked by the date seed.
            System.Random rng = new System.Random(DateSeed());
            GameObject golden = new GameObject("DailyStar");
            golden.transform.SetParent(parent, false);
            int tries = 0;
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

                Material gold = ArtLib.Solid(ArtLib.Gold, 2.2f);
                ArtLib.DecorSphere(golden.transform, Vector3.zero,
                    new Vector3(0.5f, 0.5f, 0.5f), gold);
                BoxCollider trigger = golden.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = Vector3.one * 1.3f;
                DailyStar star = golden.AddComponent<DailyStar>();
                star.today = Today();
                star.transform.localPosition = spot;
                return;
            }
            Object.Destroy(golden);
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
