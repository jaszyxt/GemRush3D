using UnityEngine;

namespace GemRush
{
    /// A spinning, bobbing collectible crystal. The player has a Rigidbody,
    /// so trigger callbacks fire on this component when they touch it.
    public class Gem : MonoBehaviour
    {
        static Material sharedGemMaterial;

        Vector3 basePosition;
        float phase;
        bool collected;

        /// Melody gem: when non-zero, collecting this gem plays this note
        /// instead of the regular pickup — gem trails become songs.
        public float noteFrequency;

        public static Gem Create(Transform parent, Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Gem";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            if (sharedGemMaterial == null)
                sharedGemMaterial = ArtLib.Solid(ArtLib.GemPink, 1.6f);
            go.GetComponent<MeshRenderer>().sharedMaterial = sharedGemMaterial;

            Collider col = go.GetComponent<Collider>();
            col.isTrigger = true;

            Gem gem = go.AddComponent<Gem>();
            return gem;
        }

        void Awake()
        {
            basePosition = transform.localPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            if (collected) return;
            transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);
            Vector3 pos = basePosition;
            pos.y += Mathf.Sin(Time.time * 2f + phase) * 0.22f;
            transform.localPosition = pos;

            // Shared glow pulse: one material update, all gems breathe together.
            if (sharedGemMaterial != null)
                sharedGemMaterial.SetColor("_EmissionColor",
                    ArtLib.GemPink * (1.3f + 0.5f * Mathf.Sin(Time.time * 3f + phase)));
        }

        void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            collected = true;
            bool hasNote = noteFrequency > 0f;
            GameManager.Instance.OnGemCollected(!hasNote);
            if (hasNote)
            {
                // The trail's own note replaces the generic pickup blip.
                AudioManager.Instance.PlayNote(noteFrequency);
            }
            Fx.Burst(transform.position, ArtLib.GemPink * 1.6f, 18);
            Fx.Popup(transform.position + Vector3.up * 0.4f, "+1",
                new Color(1f, 0.92f, 0.55f));
            Destroy(gameObject);
        }
    }
}
