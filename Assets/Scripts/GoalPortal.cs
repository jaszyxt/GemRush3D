using UnityEngine;

namespace GemRush
{
    /// The exit: a glowing arch with a shimmering fill. Walking into the
    /// fill wins the level.
    public class GoalPortal : MonoBehaviour
    {
        Material fillMaterial;
        Color fillColor;

        public static void Create(Transform parent, Vector3 platformTopCenter)
        {
            Material frame = ArtLib.Solid(ArtLib.PortalCyan, 1.4f);

            GameObject portal = new GameObject("GoalPortal");
            portal.transform.SetParent(parent, false);
            portal.transform.localPosition = platformTopCenter;

            // Arch: two pillars + top beam.
            ArtLib.DecorCube(portal.transform, new Vector3(-1.4f, 1.75f, 0f),
                new Vector3(0.5f, 3.5f, 0.5f), Quaternion.identity, frame);
            ArtLib.DecorCube(portal.transform, new Vector3(1.4f, 1.75f, 0f),
                new Vector3(0.5f, 3.5f, 0.5f), Quaternion.identity, frame);
            ArtLib.DecorCube(portal.transform, new Vector3(0f, 3.75f, 0f),
                new Vector3(3.3f, 0.5f, 0.5f), Quaternion.identity, frame);

            // Shimmering fill.
            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "PortalFill";
            Object.Destroy(fill.GetComponent<Collider>());
            fill.transform.SetParent(portal.transform, false);
            fill.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            fill.transform.localScale = new Vector3(2.3f, 3.0f, 0.1f);

            Shader fillShader = Shader.Find("Sprites/Default");
            Material fillMat = fillShader != null
                ? new Material(fillShader)
                : ArtLib.Solid(ArtLib.PortalCyan, 0.5f);
            fillMat.color = new Color(0.2f, 0.9f, 0.95f, 0.45f);
            fill.GetComponent<MeshRenderer>().sharedMaterial = fillMat;

            // Trigger volume.
            BoxCollider trigger = portal.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.4f, 3.4f, 1.6f);
            trigger.center = new Vector3(0f, 1.7f, 0f);

            GoalPortal goal = portal.AddComponent<GoalPortal>();
            goal.fillMaterial = fillMat;
            goal.fillColor = new Color(0.2f, 0.9f, 0.95f, 0.45f);
        }

        /// Distance over which the hum fades in, in world units.
        ///
        /// This was 28, which made the portal a final-approach cue only:
        /// courses run 76-171 units spawn-to-portal, so the hum read exactly
        /// ZERO for most of every level (it also has to clear the audio
        /// manager's 0.005 play threshold before it is audible at all,
        /// pushing the real onset even closer). Widening the reach gives the
        /// player a continuous bearing on the goal from much further out.
        ///
        /// Deliberately NOT raised by scaling the peak volume: proximity maps
        /// linearly onto hum volume, so a longer ramp is automatically
        /// quieter at distance while leaving the near-field exactly as
        /// calibrated (the hum is mixed to sit UNDER the music bed).
        const float CueRange = 120f;

        void Update()
        {
            if (fillMaterial != null)
            {
                float pulse = 0.35f + 0.25f * Mathf.Sin(Time.time * 3f);
                fillColor.a = pulse;
                fillMaterial.color = fillColor;
            }

            // Proximity hum: the goal is heard before it is seen. The
            // manager fades the hum toward whatever we report, so reporting
            // zero while not playing lets it decay away gracefully.
            float proximity = 0f;
            if (GameManager.Instance != null && AudioManager.Instance != null &&
                GameManager.Instance.State == GameState.Playing &&
                GameBootstrap.Player != null)
            {
                float d = Vector3.Distance(GameBootstrap.Player.transform.position,
                    transform.position);
                // Ease-in rather than a straight line: the hum stays a
                // whisper for the first stretch of the course and rises
                // convincingly over the last third, so "getting close" still
                // reads as an event rather than a constant drone.
                float t = Mathf.Clamp01(1f - d / CueRange);
                proximity = t * t;
            }
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetPortalProximity(proximity);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            GameManager.Instance.OnReachGoal();
        }
    }
}
