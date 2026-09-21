using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// The golden-gem remix gate (Celeste cassette model, per the
    /// Fun-Creativity queue): every level hides one golden gem, off the
    /// beaten path. Finding the golden gem in a level that has a night
    /// remix UNLOCKS that remix (the B-sides at indices 28-30); every
    /// other golden is pure collection — the atlas glints. Like the
    /// DailyGem's gift, collecting stays OUT of gem/star math: a golden
    /// find writes its own save key and nothing else.
    public static class GoldenGem
    {
        /// The library index currently being built — set by
        /// GameBootstrap.BuildWorld BEFORE the rain remix can swap the
        /// level definition object (the shelf lesson: never key off the
        /// definition's identity).
        public static int ActiveLevelIndex = -1;

        /// B-side levels never hide a golden (they are the reward), and
        /// neither does bonus flight (weatherless, weightless).
        public static bool HidesGolden(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= LevelLibrary.Levels.Length)
                return false;
            int source = LevelLibrary.BSideSourceIndex(levelIndex);
            if (source >= 0) return false; // this IS a B-side
            return !LevelLibrary.Levels[levelIndex].BonusFlight;
        }

        /// Deterministic "hidden" spot: seeded by the level's name, the
        /// candidate platforms score by distance to the nearest gem plus
        /// a slice of distance from spawn — the golden lands where the
        /// trail thins, same place every run. Pure: same level, same spot.
        ///
        /// A candidate too close to the portal is REJECTED outright. Both
        /// scoring terms reward being far from the trail and far from
        /// spawn, and on a linear course that maximises at the finish, so
        /// the gem reliably landed on the exit pad: measured, 29 of 37
        /// levels hid it within 12 units of the portal and several within
        /// 1-2. A player running to the exit touches the portal, the level
        /// completes, and the gem they walked past is never collected —
        /// reported from play on The Silent Spire, where it sat 3.6 units
        /// behind the exit. "Hidden off the beaten path" cannot mean "on
        /// the way out".
        public const float MinPortalDistance = 15f;

        public static Vector3 PickSpot(LevelDefinition level)
        {
            int seed = 0;
            for (int i = 0; i < level.Name.Length; i++) seed += level.Name[i];
            System.Random rng = new System.Random(seed);

            float bestScore = -1f;
            Vector3 best = level.Spawn + Vector3.up;
            int found = 0;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                PlatformSpec p = level.Platforms[
                    rng.Next(level.Platforms.Count)];
                if (Mathf.Min(p.Size.x, p.Size.z) < 4f) continue;

                float px = ((float)rng.NextDouble() * 2f - 1f) *
                    (p.Size.x * 0.5f - 1f);
                float pz = ((float)rng.NextDouble() * 2f - 1f) *
                    (p.Size.z * 0.5f - 1f);
                Vector3 spot = p.Center +
                    new Vector3(px, p.Size.y * 0.5f + 1.2f, pz);

                // Never hide the prize on the way out.
                float toPortal = Vector3.Distance(
                    new Vector3(spot.x, 0f, spot.z),
                    new Vector3(level.Portal.x, 0f, level.Portal.z));
                if (toPortal < MinPortalDistance) continue;

                // Off the trail: far from every gem, and a nod toward the
                // far end of the course.
                float nearestGem = 999f;
                for (int g = 0; g < level.Gems.Count; g++)
                    nearestGem = Mathf.Min(nearestGem,
                        Vector3.Distance(level.Gems[g], spot));
                float fromSpawn = Vector3.Distance(level.Spawn, spot);
                float score = nearestGem + fromSpawn * 0.3f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = spot;
                    found++;
                }
            }
            // Every candidate rejected (a very short level, or one whose
            // whole course sits near its portal): fall back to the plain
            // scoring so a golden still exists rather than silently
            // vanishing, and let the test surface the crowding.
            if (found == 0) return PickSpotIgnoringPortal(level);
            return best;
        }

        /// The pre-fix scoring, kept as the fallback so the gate above can
        /// never leave a level without its golden.
        static Vector3 PickSpotIgnoringPortal(LevelDefinition level)
        {
            int seed = 0;
            for (int i = 0; i < level.Name.Length; i++) seed += level.Name[i];
            System.Random rng = new System.Random(seed);

            float bestScore = -1f;
            Vector3 best = level.Spawn + Vector3.up;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                PlatformSpec p = level.Platforms[
                    rng.Next(level.Platforms.Count)];
                if (Mathf.Min(p.Size.x, p.Size.z) < 4f) continue;

                float px = ((float)rng.NextDouble() * 2f - 1f) *
                    (p.Size.x * 0.5f - 1f);
                float pz = ((float)rng.NextDouble() * 2f - 1f) *
                    (p.Size.z * 0.5f - 1f);
                Vector3 spot = p.Center +
                    new Vector3(px, p.Size.y * 0.5f + 1.2f, pz);

                float nearestGem = 999f;
                for (int g = 0; g < level.Gems.Count; g++)
                    nearestGem = Mathf.Min(nearestGem,
                        Vector3.Distance(level.Gems[g], spot));
                float fromSpawn = Vector3.Distance(level.Spawn, spot);
                float score = nearestGem + fromSpawn * 0.3f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = spot;
                }
            }
            return best;
        }

        /// Spawns the level's golden gem if it hides one and has not been
        /// found yet. Called from LevelBuilder.Build, next to the
        /// DailyGem hook.
        public static void PlaceIfHidden(LevelDefinition level,
            Transform parent)
        {
            int index = ActiveLevelIndex;
            if (!HidesGolden(index)) return;

            // Found already: leave the gentle trace instead of a void.
            // Odyssey's softness — a found thing keeps a faint outline so
            // returning to the spot reads as "yours", never as "gone".
            if (SaveSystem.GoldenFound(index))
            {
                GoldenSignal.FoundOutline(level, parent);
                return;
            }

            Vector3 spot = PickSpot(level);

            // Built EXACTLY like the working pink gem (see Gem.Create): one
            // primitive object that owns its renderer, its collider and the
            // pickup script, all together. The previous version used a bare
            // GameObject with a hand-added SphereCollider and a separate
            // visual child, and it RENDERED but never registered a hit —
            // the player passed straight through it. Rather than keep
            // theorising about why, this mirrors the pattern the game has
            // already proven works on every one of its gems.
            GameObject golden = GameObject.CreatePrimitive(PrimitiveType.Cube);
            golden.name = "GoldenGem";
            golden.transform.SetParent(parent, false);
            golden.transform.localPosition = spot;
            golden.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            golden.transform.localScale = Vector3.one * 1.15f;

            Material bodyMat = ArtLib.Solid(ArtLib.Gold, 2.2f);
            golden.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

            // The primitive's own collider becomes the pickup trigger, as
            // the pink gem does. Nothing is destroyed, nothing is added on
            // a different object, and the collider is therefore guaranteed
            // to sit exactly on the visible shape.
            Collider trigger = golden.GetComponent<Collider>();
            trigger.isTrigger = true;

            // The taught signal: a slow breathing glimmer on the gem, and
            // a few drifting motes along the last stretch of the approach
            // so a curious eye is *led* rather than told.
            GoldenSignal.Glimmer(golden.transform, bodyMat);
            GoldenSignal.Trail(level, spot, parent);

            GoldenStar star = golden.AddComponent<GoldenStar>();
            star.levelIndex = index;
            star.Note = Strings.GoldenNote(level.Name);
            star.body = golden.transform;
        }

        /// The collectible itself: spin, bob, and on touch — the save
        /// flag, a gold burst, the toast, the gift chime. Never the gem
        /// counter.
        ///
        /// [Preserve] IS LOAD-BEARING — do not remove it. This is a
        /// private nested class referenced by nothing in managed code (it
        /// is created only through AddComponent<GoldenStar>()), which
        /// makes it a prime target for IL2CPP's linker. Windows runs Mono
        /// and strips nothing, so the bug was invisible there; on Android
        /// the type was stripped and its OnTriggerEnter never registered
        /// with the physics system. The result was the reported symptom
        /// exactly: the gem RENDERED and its Update ran (it glowed and
        /// bobbed) but the player passed straight through it with no
        /// sound and no pickup. Compare Gem (top-level, publicly
        /// referenced) which never had the problem, and MirrorDoor's
        /// DoorSide (nested, but held in fields) which also survived.
        [UnityEngine.Scripting.Preserve]
        class GoldenStar : MonoBehaviour
        {
            public int levelIndex;
            /// The canon-voice line carried by this gem's spot (see
            /// Strings.GoldenNote). Shown after the unlock line so the
            /// find reads as a discovery with a story, not a pickup.
            public string Note;
            /// The gem object itself (kept named `body` so the spawn code
            /// reads the same as before).
            public Transform body;

            /// Same courtesy the ordinary gems extend (see Gem.cs): once
            /// Pip is close, slide to him. This is why a normal gem is
            /// never missed.
            const float MagnetRadius = 2.2f;
            const float MagnetSpeed = 10f;

            Vector3 basePosition;
            bool taken;
            bool magnetized;

            void Start()
            {
                basePosition = transform.localPosition;
            }

            /// Motion copied from the pink gem, which works: the gem spins
            /// and bobs ITSELF — collider included — and physics follows
            /// fine, because the movement happens every frame and the
            /// rigidbody that triggers it (the player) is a real body
            /// moving under physics. The pickup falls back to the magnet
            /// path the moment Pip is within reach, exactly like Gem.cs.
            void Update()
            {
                if (taken) return;

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
                        transform.position, pip.position,
                        MagnetSpeed * Time.deltaTime);
                }
                else
                {
                    transform.Rotate(Vector3.up, 120f * Time.deltaTime,
                        Space.World);
                    Vector3 pos = basePosition;
                    pos.y += Mathf.Sin(Time.time * ArtLib.HoverBobRate)
                        * 0.22f;
                    transform.localPosition = pos;
                }
            }

            void OnTriggerEnter(Collider other)
            {
                if (taken) return;
                if (other.GetComponentInParent<PlayerController>() == null)
                    return;
                taken = true;

                SaveSystem.SetGoldenFound(levelIndex);
                Fx.Burst(transform.position, ArtLib.Gold * 1.8f, 30);
                Fx.Ring(transform.position, ArtLib.Gold);
                if (UIManager.Instance != null)
                {
                    int bside = -1;
                    for (int i = 0; i < LevelLibrary.Levels.Length; i++)
                        if (LevelLibrary.BSideSourceIndex(i) == levelIndex)
                        {
                            bside = i;
                            break;
                        }
                    // One toast, not two: the second call would simply
                    // overwrite the first, so the story beat rides on the
                    // same line as the unlock — the find and its meaning
                    // arrive together.
                    string line = bside >= 0
                        ? Strings.GoldenUnlocksRemix(
                            LevelLibrary.Levels[bside].Name)
                        : Strings.GoldenFoundLine;
                    if (!string.IsNullOrEmpty(Note))
                        line = line + "  " + Note;
                    UIManager.Instance.ShowStoryToast(line);
                }
                AudioManager.Instance.PlayGift();
                Destroy(gameObject);
            }
        }
    }
}
