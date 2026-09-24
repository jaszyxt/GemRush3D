using UnityEngine;

namespace GemRush
{
    /// The Rest: after a level is won, the world keeps living and the
    /// camera settles into a slow drift, so a player can simply stay a
    /// while. Nothing is asked of them, nothing is timed, nothing is
    /// rewarded — attaching a reward is exactly what would make it a
    /// task instead of a rest (the cozy-design law: an activity must be
    /// satisfying in itself; when the reward outweighs the gentle
    /// pleasure it becomes extrinsic).
    ///
    /// Research behind it (docs/Fun-Creativity-Directives.md): the
    /// "reflective play" patterns of Slowdowns, Stasis and Stillness —
    /// Animal Crossing's bench, Flower's measurable calm, "it's okay to
    /// slow down". A cozy platformer should be allowed to exhale after
    /// the portal, not only between portals.
    ///
    /// Implementation notes: this never fights the player. It engages
    /// only in the Won state, hands the camera straight back the instant
    /// any input arrives, and respects the same comfort settings as
    /// every other camera motion in the game.
    public class RestBeat : MonoBehaviour
    {
        /// How long after the win before the drift begins — long enough
        /// that the star fanfare owns the moment first.
        const float SettleSeconds = 3.2f;
        /// A full, very slow circle: unhurried by design.
        const float OrbitSeconds = 74f;
        /// Minimum standoff, so the drift has room even when the portal
        /// was reached with the camera close in.
        const float OrbitRadius = 2.6f;

        Camera cam;
        CameraFollow follow;
        Transform target;
        Vector3 home;
        float age;
        float orbitStart;
        float restHeight;
        float radius;
        bool active;

        public static RestBeat Attach(CameraFollow owner)
        {
            if (owner == null) return null;
            RestBeat beat = owner.gameObject.GetComponent<RestBeat>();
            if (beat == null)
                beat = owner.gameObject.AddComponent<RestBeat>();
            beat.follow = owner;
            return beat;
        }

        void LateUpdate()
        {
            // Resolved lazily: BuildWorld attaches this the moment the
            // camera is made, which can precede this component's Start.
            if (cam == null) cam = GetComponent<Camera>();
            if (follow == null) follow = GetComponent<CameraFollow>();

            GameManager gm = GameManager.Instance;
            if (gm == null || cam == null) return;

            // The rest runs on unscaled time so it survives the win sting's
            // hit-stop, which means it also survived a pause: opening
            // Settings or photo mode from the win screen left the camera
            // orbiting behind the panel. timeScale is the honest signal that
            // the world is held, so the rest stands down with it.
            bool held = Time.timeScale <= 0f;
            bool shouldRest = gm.State == GameState.Won
                && !held
                && SaveSystem.ShakeOn
                && GameBootstrap.Player != null;

            if (!shouldRest)
            {
                if (active) Release();
                age = 0f;
                return;
            }

            // The clock runs from the moment the win lands, so the settle
            // window and the drift share one continuous timeline.
            age += Time.unscaledDeltaTime;

            if (!active)
            {
                // Let the star fanfare own the moment first; the drift
                // only begins once the celebration has settled.
                if (age < SettleSeconds) return;

                // Take the camera the same way photo mode does — the
                // follow rig is the only other writer of this transform,
                // so it has to stand down for the drift to exist.
                target = GameBootstrap.Player.transform;
                home = transform.position;
                // Freeze the framing we are coming from: the orbit is a
                // drift around the winning pose, never a re-frame. Height
                // is held (a camera that sinks after a win reads as a
                // fall), and only the horizontal angle travels.
                restHeight = home.y;
                Vector3 from = home - target.position;
                orbitStart = Mathf.Atan2(from.z, from.x);
                radius = new Vector2(from.x, from.z).magnitude;
                // A little extra standoff so the drift has somewhere to
                // go even if the win happened right on top of Pip.
                radius = Mathf.Max(radius, OrbitRadius);
                if (follow != null) follow.enabled = false;
                active = true;
            }

            float k = (age - SettleSeconds) / OrbitSeconds;
            float ang = k * Mathf.PI * 2f;

            // A very slow circle around Pip at the winning height, with a
            // whisper of vertical breath so it reads as alive rather than
            // as a turntable.
            float a = orbitStart + ang;
            Vector3 pos = new Vector3(
                target.position.x + Mathf.Cos(a) * radius,
                restHeight + 0.30f * Mathf.Sin(ang * 0.5f),
                target.position.z + Mathf.Sin(a) * radius);
            transform.position = Vector3.Lerp(transform.position, pos,
                1f - Mathf.Exp(-1.6f * Time.unscaledDeltaTime));

            Vector3 look = (target.position + Vector3.up * 0.9f)
                - transform.position;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(look),
                    1f - Mathf.Exp(-1.4f * Time.unscaledDeltaTime));
        }

        /// Hands the camera back exactly where the follow rig leaves it,
        /// so Next/Replay/Menu is seamless — no snap, and no drift
        /// carrying into the next level.
        void Release()
        {
            active = false;
            age = 0f;
            if (follow != null)
            {
                follow.enabled = true;
                // SnapToTarget deliberately NOT called: the follow rig's
                // own exp-smoothing eases the camera back from the rest
                // orbit position, so the return reads as a dolly move
                // rather than a hard cut (same treatment as PhotoMode).
            }
        }
    }
}
