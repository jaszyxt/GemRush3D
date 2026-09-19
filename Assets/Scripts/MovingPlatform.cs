using UnityEngine;

namespace GemRush
{
    /// Kinematic platform that glides between its start position and
    /// start + moveOffset on a smooth sine cycle. Exposes its per-frame
    /// velocity so the player controller can carry the rider along.
    public class MovingPlatform : MonoBehaviour
    {
        public Vector3 moveOffset = new Vector3(5f, 0f, 0f);
        public float period = 4f;

        /// Per-frame velocity for the rider carry. Protected setter so
        /// subclasses (AuroraRibbon) drive it from their own motion.
        public Vector3 Velocity { get; protected set; }

        Rigidbody rb;
        Vector3 startPosition;

        void Awake()
        {
            startPosition = transform.position;
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void FixedUpdate()
        {
            Vector3 previous = rb.position;
            float phase = (Mathf.Sin((Time.time / period) * Mathf.PI * 2f
                - Mathf.PI * 0.5f) + 1f) * 0.5f;
            Vector3 next = startPosition + moveOffset * phase;
            rb.MovePosition(next);
            Velocity = (next - previous) / Time.fixedDeltaTime;
        }
    }
}
