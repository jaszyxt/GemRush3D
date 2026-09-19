using UnityEngine;

namespace GemRush
{
    /// Photo-mode camera (photo postcards, DESIGN.md community plan): a
    /// slow, height-breathing orbit around Pip. Runs on the unscaled clock,
    /// so it drifts serenely even while the game is paused for the shot.
    /// Disables the follow camera while active; Begin/End restore it.
    public class PhotoMode : MonoBehaviour
    {
        public float radius = 8f;
        public float baseHeight = 3.2f;
        public float speed = 0.15f;   // radians per second (~42 s per orbit)

        Transform target;
        CameraFollow follow;

        /// Starts an orbit around `photoTarget`, parking the follow camera.
        /// The component lives on the camera itself.
        public static PhotoMode Begin(CameraFollow follow, Transform photoTarget)
        {
            follow.enabled = false;
            PhotoMode mode = follow.gameObject.AddComponent<PhotoMode>();
            mode.follow = follow;
            mode.target = photoTarget;
            return mode;
        }

        /// Restores the follow camera exactly where it was.
        public void End()
        {
            if (follow != null)
            {
                follow.enabled = true;
                follow.SnapToTarget();
            }
            Destroy(this);
        }

        void Update()
        {
            if (target == null) return;
            float t = Time.unscaledTime * speed;
            Vector3 position = target.position +
                new Vector3(Mathf.Cos(t) * radius,
                    baseHeight + Mathf.Sin(t * 0.5f) * 0.9f,
                    Mathf.Sin(t) * radius);
            transform.position = position;
            transform.LookAt(target.position + Vector3.up * 1.2f);
        }
    }
}
