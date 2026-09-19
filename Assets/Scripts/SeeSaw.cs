using UnityEngine;

namespace GemRush
{
    /// A wooden see-saw plank (The Homecoming): a plank on a pivot that
    /// tips gently toward wherever Pip stands, springing level when he
    /// steps off. Kinematic and deterministic — the tilt follows Pip's
    /// position along the plank with a speed limit, so it reads as weight,
    /// never as randomness. Tip an end down to reach the treat shelves
    /// under it; cross one as a living ramp between shores.
    public class SeeSaw : MonoBehaviour
    {
        public float length = 7f;
        public float maxTilt = 11f;
        /// The plank's tipping axis in world terms ("x" or "z"): the
        /// direction Pip walks to tip it.
        public string axis = "z";

        Rigidbody rb;
        Quaternion restRotation;
        float currentAngle;

        public static SeeSaw Create(Transform parent, SeeSawSpec spec)
        {
            GameObject go = new GameObject("SeeSaw");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = spec.PlatformTop;

            // The pivot post: a small wedge the plank balances on.
            Material stone = ArtLib.Solid(ArtLib.Stone, 0f);
            ArtLib.DecorCube(go.transform, new Vector3(0f, 0.2f, 0f),
                new Vector3(0.7f, 0.4f, 0.7f), Quaternion.identity, stone);

            // The plank: a wooden slab hinged at the pivot's top.
            Material wood = ArtLib.Solid(ArtLib.Wood, 0f);
            GameObject plank = new GameObject("Plank");
            plank.transform.SetParent(go.transform, false);
            plank.transform.localPosition = new Vector3(0f, 0.4f, 0f);

            BoxCollider col = plank.AddComponent<BoxCollider>();
            bool alongX = spec.Axis == "x";
            col.size = alongX
                ? new Vector3(spec.Length, 0.24f, spec.Width)
                : new Vector3(spec.Width, 0.24f, spec.Length);
            ArtLib.DecorCube(plank.transform, Vector3.zero, col.size,
                Quaternion.identity, wood);

            // Edge stripes so the tilting ends read at a glance.
            Material edge = ArtLib.Solid(ArtLib.Gold, 0.2f);
            Vector3 edgeA = alongX
                ? new Vector3(spec.Length * 0.5f - 0.3f, 0.14f, 0f)
                : new Vector3(0f, 0.14f, spec.Length * 0.5f - 0.3f);
            Vector3 edgeB = -edgeA;
            Vector3 edgeSize = alongX
                ? new Vector3(0.6f, 0.04f, spec.Width * 0.7f)
                : new Vector3(spec.Width * 0.7f, 0.04f, 0.6f);
            ArtLib.DecorCube(plank.transform, edgeA, edgeSize,
                Quaternion.identity, edge);
            ArtLib.DecorCube(plank.transform, edgeB, edgeSize,
                Quaternion.identity, edge);

            SeeSaw s = go.AddComponent<SeeSaw>();
            s.length = spec.Length;
            s.maxTilt = spec.MaxTilt;
            s.axis = spec.Axis;
            s.rb = plank.AddComponent<Rigidbody>();
            s.rb.isKinematic = true;
            s.rb.useGravity = false;
            s.rb.interpolation = RigidbodyInterpolation.Interpolate;
            s.restRotation = plank.transform.rotation;
            return s;
        }

        void FixedUpdate()
        {
            // Target tilt from Pip's offset along the tipping axis: stand
            // left of the pivot and the left end sinks. Weight has RANGE —
            // a plank only answers when Pip is aboard (within the plank
            // span plus a step); otherwise it springs level, so distant
            // planks never lean at an empty sky.
            float target = 0f;
            if (GameBootstrap.Player != null)
            {
                Vector3 pivot = transform.position + Vector3.up * 0.4f;
                Vector3 rel = GameBootstrap.Player.transform.position - pivot;
                float along = axis == "x" ? rel.x : rel.z;
                float half = length * 0.5f;
                if (Mathf.Abs(along) <= half + 0.8f &&
                    Mathf.Abs(rel.y) < 4f)
                    target = Mathf.Clamp(along / half, -1f, 1f) * maxTilt;
            }

            currentAngle = Mathf.MoveTowards(currentAngle, target,
                40f * Time.fixedDeltaTime);
            // Player's end sinks: positive X rotation raises the +Z end,
            // so both cases tip away from the sign of the offset.
            Quaternion rotation = axis == "x"
                ? restRotation * Quaternion.Euler(0f, 0f, -currentAngle)
                : restRotation * Quaternion.Euler(-currentAngle, 0f, 0f);
            rb.MoveRotation(rotation);
        }
    }
}
