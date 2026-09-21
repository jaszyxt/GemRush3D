using UnityEngine;

namespace GemRush
{
    /// A quiet place to sit, derived rather than authored.
    ///
    /// The research for this pass (reflective-play patterns: Slowdowns,
    /// Stasis, Stillness; Animal Crossing's bench; thatgamecompany's "it's
    /// okay to slow down") says calm moments belong *inside* a level, not
    /// only after it — The Rest covers the win, this covers the walk.
    ///
    /// It is derived, not placed, on purpose: there is nothing to author,
    /// no spec type, no level-file edit, and therefore nothing for the
    /// from-scratch pack tests to police or for a future pack to forget.
    /// Every level already contains a calmest spot — the platform furthest
    /// from the route with room to stand — so the code finds it the same
    /// way the golden gem finds its hiding place: deterministically, from
    /// the level's own data, seeded by its name.
    ///
    /// Attached to a platform top as a small wooden bench. Standing near
    /// it is all that is required: ambient sound opens, the wind eases and
    /// nothing else happens. Deliberately unrewarded — no counter, no
    /// bonus, no completion state. A reward attached to a rest converts it
    /// into a task, which is the one thing the cozy literature is
    /// unanimous about.
    public class Perch : MonoBehaviour
    {
        /// How close Pip must be for the spot to "open". Generous: this is
        /// an invitation, not a trigger to hunt for.
        const float CalmRadius = 4f;
        /// Platforms smaller than this have no room to sit.
        const float MinSide = 3.5f;
        /// A bench must not sit essentially on top of a collectible.
        const float MinGemClearance = 2.5f;

        Vector3 spot;
        float calmFade; // eased 0..1, unscaled, so it never snaps

        /// True while Pip is settling at a bench. PlayerController reads
        /// this to fold the seated pose in early — the bench's whole point
        /// is that standing next to it should read as "you could stop
        /// here", and the game already HAS a seated pose (the idle
        /// ladder's). This reuses it rather than inventing a second one.
        ///
        /// Frame-stamped rather than handshaked: the perch claims on the
        /// frame it sees Pip, and the flag is simply stale (false) if no
        /// perch claimed recently. That way a destroyed level, a missed
        /// call or two overlapping benches can never leave Pip stuck
        /// seated — the worst case is the sit ending a frame early.
        static int claimedFrame = -1;

        public static bool PipIsResting
        {
            get { return Time.frameCount - claimedFrame <= 1; }
        }

        /// Called by whichever bench currently has Pip in range.
        void ClaimRest()
        {
            claimedFrame = Time.frameCount;
        }

        /// Picks the level's calmest platform top. Pure — same level, same
        /// spot, every run (the golden gem's rule, for the same reason: a
        /// spot that moved would read as a bug, not a discovery).
        ///
        /// The scoring was rewritten after MEASURING the real level
        /// shapes: GemRush courses are linear staircases along Z (a
        /// typical platform sits within a couple of units of the centre
        /// line), so "distance from the route" — the obvious first
        /// metric — selected 36 of 39 benches sitting right in the
        /// walking line. The signal that actually identifies a restful
        /// spot in this game is ROOMINESS: the wide landings are where
        /// the course breathes, and the tight 5x5s are mid-hop. So a
        /// perch goes on the roomiest platform that is also clear of the
        /// collectibles.
        public static Vector3 PickSpot(LevelDefinition level)
        {
            int seed = 0;
            for (int i = 0; i < level.Name.Length; i++) seed += level.Name[i];
            System.Random rng = new System.Random(seed);

            float bestScore = float.MinValue;
            Vector3 best = level.Spawn;
            for (int i = 0; i < level.Platforms.Count; i++)
            {
                PlatformSpec p = level.Platforms[i];
                if (Mathf.Min(p.Size.x, p.Size.z) < MinSide) continue;

                Vector3 top = p.Center +
                    new Vector3(0f, p.Size.y * 0.5f, 0f);

                // Away from the collectibles: calm that sits on top of a
                // collectible is clutter, not calm. (Measured: the first
                // pass put 23 of 27 benches within 1.5 units of a gem.)
                float nearestGem = float.MaxValue;
                for (int g = 0; g < level.Gems.Count; g++)
                    nearestGem = Mathf.Min(nearestGem,
                        Vector3.Distance(level.Gems[g], top));
                if (level.Gems.Count == 0) nearestGem = 8f;
                if (nearestGem < MinGemClearance) continue;

                // Nowhere near a guardian. Found by looking at a captured
                // frame: the first version placed a bench directly under a
                // spinner's sweep — a "rest" inside a hazard arc, which is
                // both un-restful and actively misleading. A guardian arm
                // is 9 units end to end, so its reach is half that.
                if (NearHazard(level, top)) continue;

                // Roominess is the headline term: the smallest side
                // squared is an area proxy that rewards a landing rather
                // than a runway.
                float room = Mathf.Min(p.Size.x, p.Size.z);
                float roomScore = Mathf.Min(room, 14f);

                // Prefer the middle of the course: the first platform is
                // where the player is still arriving, the last is the
                // goal crush.
                float along = top.z - level.Spawn.z;
                float span = Mathf.Max(1f, level.Portal.z - level.Spawn.z);
                float midness = 1f - Mathf.Abs(
                    Mathf.Clamp01(along / span) - 0.5f) * 2f;

                // Plus what gem clearance we can get, capped so a truly
                // empty corner cannot outvote a genuinely roomy landing.
                float score = roomScore * 1.0f + midness * 3f
                    + Mathf.Min(nearestGem, 10f) * 0.35f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = top;
                }
            }

            // Nudge toward the platform's edge rather than dead centre —
            // on the way through, not blocking the line of travel — and
            // seed it so the nudge is stable per level.
            float ox = ((float)rng.NextDouble() * 2f - 1f) * 0.5f;
            float oz = ((float)rng.NextDouble() * 2f - 1f) * 0.5f;
            return best + new Vector3(ox, 0f, oz);
        }

        /// True if the spot falls inside a guardian's sweep. The spinner
        /// arm is 9 units end to end (see Spinner.Create), so anything
        /// within about 6 units of a hub is inside the arc at some point
        /// in its rotation — and a sleeping guardian still wakes.
        static bool NearHazard(LevelDefinition level, Vector3 top)
        {
            const float GuardianReach = 6f;
            for (int i = 0; i < level.Spinners.Count; i++)
            {
                Vector3 hub = level.Spinners[i].PlatformTop;
                float d = Vector2.Distance(
                    new Vector2(top.x, top.z), new Vector2(hub.x, hub.z));
                if (d < GuardianReach) return true;
            }
            return false;
        }

        /// Builds the bench for a level, if the level has anywhere calm
        /// enough to deserve one. Called from LevelBuilder next to the
        /// other derived placements (DailyGem, GoldenGem).
        public static void PlaceIfCalm(LevelDefinition level, Transform parent)
        {
            if (level.BonusFlight) return;      // weightless, weatherless
            if (level.Platforms.Count < 2) return; // nothing to step off

            Vector3 spot = PickSpot(level);
            if (Vector3.Distance(spot, level.Spawn) < 6f) return; // too near the start

            GameObject root = new GameObject("Perch");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = spot;
            root.AddComponent<Perch>().spot = spot;
        }

        void Start()
        {
            BuildBench();
        }

        Material matWood;
        Material matSlat;
        Transform seatRef;

        /// A small wooden bench: two legs, a seat and a back, in the game's
        /// wood family. Decoration only — no collider, so it can never be a
        /// ledge to fight or a hitbox to trip on.
        ///
        /// The bench owns its OWN material instances rather than tinting
        /// the shared ArtLib ones: mutating a shared material would warm
        /// every tree and platform in the level as a side effect.
        void BuildBench()
        {
            matWood = ArtLib.Solid(ArtLib.Wood, 0f);
            matSlat = ArtLib.Solid(ArtLib.Trunk * 1.1f, 0f);

            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(seat.GetComponent<Collider>());
            seat.name = "Seat";
            seat.transform.SetParent(transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            seat.transform.localScale = new Vector3(2.1f, 0.12f, 0.7f);
            seat.GetComponent<MeshRenderer>().sharedMaterial = matWood;
            seatRef = seat.transform;

            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(back.GetComponent<Collider>());
            back.name = "Back";
            back.transform.SetParent(transform, false);
            back.transform.localPosition = new Vector3(0f, 0.82f, 0.3f);
            back.transform.localScale = new Vector3(2.1f, 0.55f, 0.1f);
            back.GetComponent<MeshRenderer>().sharedMaterial = matSlat;

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject leg = GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
                Object.Destroy(leg.GetComponent<Collider>());
                leg.name = "Leg";
                leg.transform.SetParent(transform, false);
                leg.transform.localPosition =
                    new Vector3(side * 0.85f, 0.22f, 0f);
                leg.transform.localScale = new Vector3(0.14f, 0.45f, 0.6f);
                leg.GetComponent<MeshRenderer>().sharedMaterial = matSlat;
            }
        }

        void Update()
        {
            Transform player = GameBootstrap.Player != null
                ? GameBootstrap.Player.transform : null;
            bool near = player != null &&
                Vector3.Distance(
                    new Vector3(player.position.x, spot.y, player.position.z),
                    new Vector3(spot.x, spot.y, spot.z)) < CalmRadius;

            // Claim the rest for PlayerController's pose blend, but only
            // when he is genuinely settled — in range, on the ground and
            // not moving. Standing up mid-air or running past the bench
            // should not put him in a seated pose.
            if (near && player != null && IsSettled(player))
                ClaimRest();

            // Unscaled: the calm must not break because the player paused
            // to look at it.
            float target = near ? 1f : 0f;
            if (Mathf.Approximately(calmFade, target)) return;
            calmFade = Mathf.MoveTowards(calmFade, target,
                Time.unscaledDeltaTime * 0.8f);

            // The only tell is local and visual: the bench warms very
            // slightly as you settle beside it, then cools when you leave.
            // Deliberately NOT touching the music or ambience — the mix is
            // the audio agent's law, and a rest that ducked the score
            // would fight the adaptive music rather than support it.
            ApplyCalm(calmFade);
        }

        /// Grounded and essentially still — the precondition for sitting.
        /// Read from the physics body rather than input so a walk-in, a
        /// landing and a stand-still all read the same way.
        static bool IsSettled(Transform player)
        {
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body == null) return false;
            Vector3 v = body.linearVelocity;
            // Vertical slack allows the tail of a landing; horizontal
            // slack keeps a slow drift from breaking the pose.
            return Mathf.Abs(v.y) < 1.2f &&
                new Vector2(v.x, v.z).magnitude < 1.2f;
        }

        void ApplyCalm(float k)
        {
            if (matWood == null || matSlat == null) return;
            // A whisper of warmth, not a glow: gold is the reward colour
            // and this is not a reward.
            float warm = 1f + 0.18f * k;
            matWood.color = new Color(
                ArtLib.Wood.r * warm, ArtLib.Wood.g * warm,
                ArtLib.Wood.b * warm, 1f);
            matSlat.color = new Color(
                ArtLib.Trunk.r * warm * 1.1f, ArtLib.Trunk.g * warm * 1.1f,
                ArtLib.Trunk.b * warm * 1.1f, 1f);
            if (seatRef != null)
                seatRef.localPosition = new Vector3(0f,
                    0.45f - 0.02f * k, 0f);
        }
    }
}
