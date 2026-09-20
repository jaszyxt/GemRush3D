using UnityEngine;

namespace GemRush
{
    /// A rotating hazard arm; the player must hop over it as it sweeps past.
    /// Sleeping Guardians variant: with a wakeRadius above zero the guardian
    /// dozes at a slow pace and only spins at full speed while Pip lingers
    /// nearby — flow is rewarded, standing still is safe but boring.
    public class Spinner : MonoBehaviour
    {
        public float degreesPerSecond = 60f;
        public float wakeRadius = 0f;   // 0 = always fully awake
        public float sleepSpeed = 12f;

        // Presence cue for always-awake guardians: how close the player
        // must be, and the floor on how often it may repeat.
        const float PresenceRadius = 11f;
        const float PresenceCooldown = 2.4f;
        float presenceTimer;

        float currentSpeed;
        Transform player;
        Material armMat;
        Color armBase;

        void Update()
        {
            float target = degreesPerSecond;
            if (wakeRadius > 0f)
            {
                if (player == null)
                {
                    PlayerController pc = FindObjectOfType<PlayerController>();
                    if (pc != null) player = pc.transform;
                }
                float dist = player != null
                    ? Vector3.Distance(player.position, transform.position)
                    : float.MaxValue;
                target = dist <= wakeRadius ? degreesPerSecond : sleepSpeed;
            }

            // Ease toward the target pace — waking and dozing are gradual.
            float previousSpeed = currentSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, target,
                90f * Time.deltaTime);
            transform.Rotate(Vector3.up, currentSpeed * Time.deltaTime);

            // A sleeping guardian crossing half-speed on the way up gets a
            // rising growl — the sound of it noticing you. Volume fades
            // with distance so far-off guardians stay quiet.
            if (wakeRadius > 0f &&
                previousSpeed < degreesPerSecond * 0.5f &&
                currentSpeed >= degreesPerSecond * 0.5f &&
                player != null)
            {
                AudioManager.Instance.PlayGuardianWake(
                    AudioManager.Falloff(transform.position, 24f) * 0.9f);
            }

            // Always-awake guardians (wakeRadius 0 — the majority of them)
            // never emit that growl, so a child watching Pip gets no
            // audible cue that an arm is sweeping toward them. Give them a
            // slow repeating presence cue instead: a quiet, distance-faded
            // pulse while the player is close, rate-limited so a spinner
            // can never machine-gun the sound, and silent once they move
            // away. It marks the hazard without nagging.
            if (wakeRadius <= 0f)
            {
                if (player == null)
                {
                    PlayerController pc = FindObjectOfType<PlayerController>();
                    if (pc != null) player = pc.transform;
                }
                if (player != null)
                {
                    float d = Vector3.Distance(player.position,
                        transform.position);
                    if (d <= PresenceRadius)
                    {
                        presenceTimer -= Time.deltaTime;
                        if (presenceTimer <= 0f)
                        {
                            presenceTimer = PresenceCooldown;
                            AudioManager.Instance.PlayGuardianWake(
                                AudioManager.Falloff(transform.position,
                                    24f) * 0.55f);
                        }
                    }
                    else presenceTimer = 0f; // ready the moment they return
                }
            }

            // The arm literally brightens as it wakes.
            if (armMat != null)
            {
                float awake = wakeRadius > 0f
                    ? Mathf.Clamp01(currentSpeed / Mathf.Max(1f, degreesPerSecond))
                    : 1f;
                armMat.SetColor("_EmissionColor",
                    armBase * (0.25f + awake * 0.75f));
            }
        }

        /// Builds the post + arm assembly on the given platform-top position.
        public static void Create(Transform parent, Vector3 platformTopCenter,
            float degreesPerSecond, float wakeRadius = 0f, float sleepSpeed = 12f)
        {
            Material postMat = ArtLib.Solid(ArtLib.Stone, 0f);
            Material armMat = ArtLib.Solid(ArtLib.HazardRed, 0.7f);
            armMat.EnableKeyword("_EMISSION");
            Color baseColor = ArtLib.HazardRed * 0.7f;
            armMat.SetColor("_EmissionColor", baseColor);

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "SpinnerPost";
            post.transform.SetParent(parent, false);
            post.transform.localPosition = platformTopCenter + new Vector3(0f, 1.1f, 0f);
            post.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
            post.GetComponent<MeshRenderer>().sharedMaterial = postMat;

            GameObject pivot = new GameObject("SpinnerPivot");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = platformTopCenter + new Vector3(0f, 1.4f, 0f);

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "SpinnerArm";
            Object.Destroy(arm.GetComponent<Collider>()); // replaced below
            arm.transform.SetParent(pivot.transform, false);
            arm.transform.localPosition = Vector3.zero;
            arm.transform.localScale = new Vector3(9f, 0.7f, 0.9f);
            arm.GetComponent<MeshRenderer>().sharedMaterial = armMat;

            BoxCollider armCollider = arm.AddComponent<BoxCollider>();
            armCollider.size = Vector3.one;
            arm.AddComponent<HazardMarker>();

            Spinner spinner = pivot.AddComponent<Spinner>();
            spinner.degreesPerSecond = degreesPerSecond;
            spinner.wakeRadius = wakeRadius;
            spinner.sleepSpeed = sleepSpeed;
            spinner.currentSpeed = sleepSpeed;
            spinner.armMat = armMat;
            spinner.armBase = baseColor;
        }
    }
}
