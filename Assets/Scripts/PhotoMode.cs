using UnityEngine;

namespace GemRush
{
    /// Photo-mode camera (photo postcards, DESIGN.md community plan): a
    /// slow, height-breathing orbit around Pip. Runs on the unscaled clock,
    /// so it drifts serenely even while the game is paused for the shot.
    /// Disables the follow camera while active; Begin/End restore it.
    ///
    /// Entry and exit ease the camera between the follow and orbit positions
    /// over ~0.4 s rather than teleporting, so the transition feels like a
    /// natural dolly move rather than a hard cut.
    public class PhotoMode : MonoBehaviour
    {
        public float radius = 8f;
        public float baseHeight = 3.2f;
        public float speed = 0.15f;   // radians per second (~42 s per orbit)

        Transform target;
        CameraFollow follow;

        // Entry ease: the camera interpolates from wherever the follow rig
        // was when the player pressed PHOTO to the first orbit position.
        // Without this, the camera teleports to the orbit circle on the
        // first frame — jarring and visually noisy.
        Vector3 startPos;
        float enterT;
        const float EnterDuration = 0.4f;

        /// Starts an orbit around `photoTarget`, parking the follow camera.
        /// The component lives on the camera itself.
        public static PhotoMode Begin(CameraFollow follow, Transform photoTarget)
        {
            // The follow camera owns the lens, and disabling it freezes
            // whatever FOV it last wrote — a wind-ride hold or a bounce-pad
            // kick would be baked into every postcard. Reset first, then
            // park: photos are always framed at the designed baseline.
            follow.ResetLens();
            follow.enabled = false;
            PhotoMode mode = follow.gameObject.AddComponent<PhotoMode>();
            mode.follow = follow;
            mode.target = photoTarget;
            mode.startPos = follow.transform.position;
            mode.enterT = 0f;
            return mode;
        }

        /// Restores the follow camera. The follow rig's existing smoothing
        /// (positionSmooth = 5) eases the camera back over ~0.3 s instead
        /// of snapping, so the exit reads as a dolly return rather than a
        /// hard cut. SnapToTarget would have teleported — removed.
        public void End()
        {
            if (follow != null)
            {
                follow.enabled = true;
                // SnapToTarget deliberately NOT called: the follow rig's
                // own exp-smoothing takes over from the current orbit
                // position and eases back to the follow position, giving
                // the player a natural transition out of photo mode.
            }
            Destroy(this);
        }

        /// The orbit position at a given time parameter.
        Vector3 OrbitPosition(float t)
        {
            return target.position +
                new Vector3(Mathf.Cos(t) * radius,
                    baseHeight + Mathf.Sin(t * 0.5f) * 0.9f,
                    Mathf.Sin(t) * radius);
        }

        void Update()
        {
            if (target == null) return;
            float t = Time.unscaledTime * speed;

            // During the first 0.4 s, lerp from the follow position to
            // the orbit. After that, follow the orbit directly.
            Vector3 orbitPos = OrbitPosition(t);
            if (enterT < EnterDuration)
            {
                enterT += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(enterT / EnterDuration);
                // Ease-out: the camera decelerates into the orbit rather
                // than accelerating out of the follow — feels like a dolly
                // that arrives gently, not one that slams to a stop.
                float eased = 1f - (1f - k) * (1f - k);
                transform.position = Vector3.Lerp(startPos, orbitPos, eased);
            }
            else
            {
                transform.position = orbitPos;
            }
            transform.LookAt(target.position + Vector3.up * 1.2f);
        }
    }
}
