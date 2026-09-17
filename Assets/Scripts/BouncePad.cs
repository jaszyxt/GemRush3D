using UnityEngine;

namespace GemRush
{
    /// A springy pad that launches Pip high into the air. Pure playground:
    /// placed away from spinners, always next to something fun to reach.
    public class BouncePad : MonoBehaviour
    {
        public const float LaunchVelocity = 13f;

        Transform padTop;
        Material padMaterial;
        float pulse;

        public static void Create(Transform parent, Vector3 platformTopCenter)
        {
            GameObject pad = new GameObject("BouncePad");
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = platformTopCenter;

            Material mat = ArtLib.Solid(ArtLib.GemPink, 0.55f);

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name = "PadTop";
            Object.Destroy(top.GetComponent<Collider>());
            top.transform.SetParent(pad.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            top.transform.localScale = new Vector3(1.7f, 0.12f, 1.7f);
            top.GetComponent<MeshRenderer>().sharedMaterial = mat;

            GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRing.name = "PadBase";
            Object.Destroy(baseRing.GetComponent<Collider>());
            baseRing.transform.SetParent(pad.transform, false);
            baseRing.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            baseRing.transform.localScale = new Vector3(2.0f, 0.06f, 2.0f);
            baseRing.GetComponent<MeshRenderer>().sharedMaterial =
                ArtLib.Solid(ArtLib.Stone, 0f);

            BoxCollider trigger = pad.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.9f, 1.2f, 1.9f);
            trigger.center = new Vector3(0f, 0.6f, 0f);

            BouncePad bp = pad.AddComponent<BouncePad>();
            bp.padTop = top.transform;
            bp.padMaterial = mat;
        }

        void Update()
        {
            // Idle pulse so pads read as "bouncy" from across the level.
            pulse += Time.deltaTime * 3f;
            if (padTop != null)
                padTop.localScale = new Vector3(1.7f, 0.12f + Mathf.Sin(pulse) * 0.025f, 1.7f);
        }

        void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            player.Launch(LaunchVelocity);

            if (padMaterial != null)
            {
                padMaterial.SetColor("_EmissionColor", ArtLib.GemPink * 2f);
            }
            Vector3 top = transform.localPosition + new Vector3(0f, 0.3f, 0f);
            Fx.Burst(top, ArtLib.GemPink * 1.5f, 14);
            AudioManager.Instance.PlayBounce();
            Haptics.Medium();
        }
    }
}
