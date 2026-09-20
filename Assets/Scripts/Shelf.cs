using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Pip's shelf (DESIGN.md: "the start island slowly fills with
    /// trophies — the world remembers you"). Built on the home island's
    /// start platform; each trophy appears when its achievement is met,
    /// read live from save data — zero new save keys. A newly earned
    /// trophy twinkles once on arrival (a quiet reactive-world beat).
    public static class Shelf
    {
        // Trophy bit flags — the "seen" mask lives in PlayerPrefs so a
        // freshly earned trophy can twinkle exactly once.
        const int TrophyPlush = 1 << 0;
        const int TrophyStar = 1 << 1;
        const int TrophyBell = 1 << 2;
        const int TrophyLantern = 1 << 3;
        const int TrophyAurora = 1 << 4;
        const int TrophyGift = 1 << 5;
        const string SeenKey = "shelf_seen";

        /// True when every level of the named region has been cleared at
        /// least once. Shared with the atlas stamps.
        public static bool RegionCleared(string regionName)
        {
            foreach (var region in LevelLibrary.Regions)
            {
                if (region.Name != regionName) continue;
                for (int i = 0; i < region.Count; i++)
                    if (SaveSystem.BestTime(region.First + i) < 0f)
                        return false;
                return true;
            }
            return false;
        }

        /// True when every level of the named region holds 3 stars.
        public static bool RegionPerfect(string regionName)
        {
            foreach (var region in LevelLibrary.Regions)
            {
                if (region.Name != regionName) continue;
                for (int i = 0; i < region.Count; i++)
                    if (SaveSystem.Stars(region.First + i) < 3)
                        return false;
                return true;
            }
            return false;
        }

        /// Builds the shelf (with whatever trophies are earned) on the
        /// home island. Called from LevelBuilder for the level-0 world.
        public static void Create(Transform parent, Vector3 platformTop)
        {
            placedFlags.Clear();
            placedPositions.Clear();
            GameObject shelf = new GameObject("PipShelf");
            shelf.transform.SetParent(parent, false);
            shelf.transform.localPosition = platformTop;

            Material wood = ArtLib.Solid(new Color(0.62f, 0.45f, 0.28f), 0f);
            Material woodDark = ArtLib.Solid(new Color(0.48f, 0.34f, 0.20f), 0f);

            // The frame: two posts and two planks, against the island's
            // west edge, facing the spawn (east).
            Vector3 posts = new Vector3(0.12f, 0.9f, 0.12f);
            ArtLib.DecorCube(shelf.transform, new Vector3(-0.7f, 0.45f, 0f),
                posts, Quaternion.identity, woodDark);
            ArtLib.DecorCube(shelf.transform, new Vector3(0.7f, 0.45f, 0f),
                posts, Quaternion.identity, woodDark);
            ArtLib.DecorCube(shelf.transform, new Vector3(0f, 0.62f, 0f),
                new Vector3(1.7f, 0.09f, 0.5f), Quaternion.identity, wood);
            ArtLib.DecorCube(shelf.transform, new Vector3(0f, 1.18f, 0f),
                new Vector3(1.7f, 0.09f, 0.5f), Quaternion.identity, wood);
            // A little roof so it reads as furniture, not lumber.
            ArtLib.DecorCube(shelf.transform, new Vector3(0f, 1.42f, 0f),
                new Vector3(1.85f, 0.07f, 0.6f), Quaternion.identity, woodDark);

            // Earned trophies, in fixed slots (top plank, bottom plank).
            int mask = 0;
            if (SaveSystem.UnlockedLevel >= 9)
                mask |= Place(shelf.transform, TrophyPlush,
                    new Vector3(-0.45f, 1.32f, 0f), BuildPlush);
            int stars = SaveSystem.TotalStars(LevelLibrary.Levels.Length);
            if (stars >= 30)
                mask |= Place(shelf.transform, TrophyStar,
                    new Vector3(0.45f, 1.32f, 0f),
                    t => BuildStar(t, stars));
            if (RegionCleared("The Bell Towers"))
                mask |= Place(shelf.transform, TrophyBell,
                    new Vector3(-0.45f, 0.76f, 0f), BuildBell);
            if (RegionCleared("The Long Winter"))
                mask |= Place(shelf.transform, TrophyLantern,
                    new Vector3(0f, 0.76f, 0f), BuildLantern);
            if (SaveSystem.AuroraUnlocked)
                mask |= Place(shelf.transform, TrophyAurora,
                    new Vector3(0.45f, 0.76f, 0f), BuildCrystal);
            if (SaveSystem.Gifts > 0)
                mask |= Place(shelf.transform, TrophyGift,
                    new Vector3(0f, 1.32f, 0f), BuildGift);

            // New-trophy twinkle: each first arrival sparkles once.
            int seen = PlayerPrefs.GetInt(SeenKey, 0);
            int fresh = mask & ~seen;
            if (fresh != 0)
            {
                for (int i = 0; i < placedFlags.Count; i++)
                    if ((fresh & placedFlags[i]) != 0)
                        Fx.Burst(placedPositions[i], ArtLib.Gold * 1.4f, 14);
                PlayerPrefs.SetInt(SeenKey, mask);
                PlayerPrefs.Save();
                CloudSaveMirror.Snapshot();
            }
        }

        // ------------------------------------------------------------------
        // Trophy builders — each a small deterministic composition.
        // ------------------------------------------------------------------

        // Placement bookkeeping for the fresh-trophy twinkle: flags in the
        // order they were placed, with their world positions.
        static readonly List<int> placedFlags = new List<int>(6);
        static readonly List<Vector3> placedPositions = new List<Vector3>(6);

        static int Place(Transform shelf, int flag, Vector3 position,
            System.Action<Transform> builder)
        {
            GameObject trophy = new GameObject("Trophy" + flag);
            trophy.transform.SetParent(shelf, false);
            trophy.transform.localPosition = position;
            builder(trophy.transform);
            placedFlags.Add(flag);
            placedPositions.Add(shelf.TransformPoint(position));
            return flag;
        }

        static void BuildPlush(Transform t)
        {
            // Gloomfang, pocket-sized: a storm-gray cloud with eyes.
            Material body = ArtLib.Solid(new Color(0.45f, 0.47f, 0.55f), 0f);
            ArtLib.DecorSphere(t, new Vector3(0f, 0.16f, 0f),
                new Vector3(0.3f, 0.26f, 0.26f), body);
            ArtLib.DecorSphere(t, new Vector3(-0.14f, 0.24f, 0f),
                new Vector3(0.16f, 0.14f, 0.14f), body);
            ArtLib.DecorSphere(t, new Vector3(0.14f, 0.24f, 0f),
                new Vector3(0.16f, 0.14f, 0.14f), body);
            Material white = ArtLib.Solid(new Color(0.97f, 0.97f, 1f), 0f);
            Material dark = ArtLib.Solid(new Color(0.1f, 0.1f, 0.12f), 0f);
            foreach (float x in new[] { -0.07f, 0.07f })
            {
                ArtLib.DecorSphere(t, new Vector3(x, 0.18f, 0.11f),
                    new Vector3(0.05f, 0.06f, 0.03f), white);
                ArtLib.DecorSphere(t, new Vector3(x, 0.18f, 0.13f),
                    new Vector3(0.025f, 0.03f, 0.015f), dark);
            }
        }

        static void BuildStar(Transform t, int stars)
        {
            // A gold star on a plinth; grander as milestones climb.
            ArtLib.DecorCube(t, new Vector3(0f, 0.06f, 0f),
                new Vector3(0.22f, 0.12f, 0.22f),
                Quaternion.identity, ArtLib.Solid(ArtLib.Stone, 0f));
            GameObject starGo = new GameObject("StarSprite");
            starGo.transform.SetParent(t, false);
            starGo.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            starGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            SpriteRenderer renderer = starGo.AddComponent<SpriteRenderer>();
            renderer.sprite = Fx.StarSprite();
            float scale = stars >= 90 ? 0.9f : stars >= 60 ? 0.72f : 0.58f;
            renderer.transform.localScale = Vector3.one * scale;
            Material starMat = ArtLib.Solid(ArtLib.Gold, 0.6f);
            renderer.sharedMaterial = starMat;
        }

        static void BuildBell(Transform t)
        {
            // The Silent Spire, pocket edition.
            Material bell = ArtLib.Solid(ArtLib.Gold, 0.5f);
            ArtLib.DecorCube(t, new Vector3(0f, 0.03f, 0f),
                new Vector3(0.18f, 0.06f, 0.18f),
                Quaternion.identity, ArtLib.Solid(ArtLib.Stone, 0f));
            ArtLib.DecorSphere(t, new Vector3(0f, 0.2f, 0f),
                new Vector3(0.14f, 0.17f, 0.14f), bell);
            ArtLib.DecorSphere(t, new Vector3(0f, 0.09f, 0f),
                new Vector3(0.05f, 0.05f, 0.05f), bell);
        }

        static void BuildLantern(Transform t)
        {
            // The sunstone lantern, resting.
            Material frame = ArtLib.Solid(ArtLib.Stone, 0f);
            ArtLib.DecorCube(t, new Vector3(0f, 0.2f, 0f),
                new Vector3(0.16f, 0.24f, 0.16f), Quaternion.identity, frame);
            ArtLib.DecorSphere(t, new Vector3(0f, 0.2f, 0f),
                new Vector3(0.09f, 0.09f, 0.09f),
                ArtLib.Solid(ArtLib.Gold, 1.0f));
        }

        static void BuildCrystal(Transform t)
        {
            // A sliver of the festival aurora, kept.
            Material crystal = ArtLib.Solid(
                new Color(0.35f, 0.95f, 0.65f), 0.8f);
            ArtLib.SetFade(crystal, 0.85f);
            ArtLib.DecorCube(t, new Vector3(0f, 0.22f, 0f),
                new Vector3(0.09f, 0.34f, 0.09f),
                Quaternion.Euler(0f, 45f, 8f), crystal);
        }

        static void BuildGift(Transform t)
        {
            // Gloomfang's first gift box, never opened (it's a shelf, not
            // a museum of openings).
            ArtLib.DecorCube(t, new Vector3(0f, 0.11f, 0f),
                new Vector3(0.2f, 0.16f, 0.2f),
                Quaternion.identity, ArtLib.Solid(ArtLib.GemPink, 0f));
            ArtLib.DecorCube(t, new Vector3(0f, 0.11f, 0f),
                new Vector3(0.22f, 0.05f, 0.06f),
                Quaternion.identity, ArtLib.Solid(ArtLib.Gold, 0f));
            ArtLib.DecorCube(t, new Vector3(0f, 0.11f, 0f),
                new Vector3(0.06f, 0.05f, 0.22f),
                Quaternion.identity, ArtLib.Solid(ArtLib.Gold, 0f));
        }
    }
}
