using UnityEngine;

namespace GemRush
{
    /// The sunstone lantern shrine (The Long Winter). A sleepy sunstone
    /// rests on a little pedestal; walking into it wakes it, and from then
    /// on it hovers beside Pip, its warmth melting every ice gate he passes.
    /// Waking it is permanent for the level — dying never puts the light
    /// out, and neither does restarting from a checkpoint.
    public class Lantern : MonoBehaviour
    {
        /// True once any shrine has been woken this level. Ice gates read
        /// this before checking Pip's warmth range.
        public static bool Lit { get; private set; }

        /// Reset at build time: each winter level builds exactly one shrine,
        /// and the light must start the level asleep.
        public static void ResetForNewLevel()
        {
            Lit = false;
        }

        GameObject flame;
        Material flameMaterial;

        public static void Create(Transform parent, LanternSpec spec)
        {
            ResetForNewLevel();

            GameObject go = new GameObject("Lantern");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = spec.PlatformTop;

            // Pedestal: a small stone plinth with a warm-lit top face.
            Material stone = ArtLib.Solid(ArtLib.Stone, 0f);
            ArtLib.DecorCube(go.transform, new Vector3(0f, 0.45f, 0f),
                new Vector3(0.9f, 0.9f, 0.9f), Quaternion.identity, stone);

            Material sunstone = ArtLib.Solid(ArtLib.Gold, 1.2f);
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(orb.GetComponent<Collider>());
            orb.name = "Sunstone";
            orb.transform.SetParent(go.transform, false);
            orb.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            orb.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            orb.GetComponent<MeshRenderer>().sharedMaterial = sunstone;

            // Trigger volume: walk into the shrine to wake the light.
            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.6f, 2.6f, 2.6f);
            trigger.center = new Vector3(0f, 1.3f, 0f);

            Lantern lantern = go.AddComponent<Lantern>();
            lantern.flame = orb;
            lantern.flameMaterial = sunstone;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Lit) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Lit = true;

            AudioManager.Instance.PlayLantern();
            // Waking the shrine turns the whole level: firm, earned.
            Haptics.Medium();
            Fx.Burst(transform.position + new Vector3(0f, 1.5f, 0f),
                ArtLib.Gold * 1.5f, 24);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowStoryToast(
                    Strings.LanternWake);
        }

        void Update()
        {
            if (!Lit || flame == null) return;

            // The sunstone leaves the pedestal and hovers at Pip's shoulder,
            // bobbing gently — a carried light, not a UI element.
            Transform player = GameBootstrap.Player != null
                ? GameBootstrap.Player.transform : null;
            if (player != null)
            {
                float bob = Mathf.Sin(Time.time * ArtLib.HoverBobRate) * 0.09f;
                flame.transform.position = player.position +
                    new Vector3(0.55f, 1.5f + bob, 0f);
            }
            // A soft warm pulse: same 0.48 Hz family as every other glow.
            float pulse = 1.15f + 0.15f * Mathf.Sin(Time.time * 3f);
            flameMaterial.SetColor("_EmissionColor", ArtLib.Gold * pulse);
        }
    }
}
