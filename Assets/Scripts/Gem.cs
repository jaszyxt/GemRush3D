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
        bool magnetized;

        /// Melody gem: when non-zero, collecting this gem plays this note
        /// instead of the regular pickup — gem trails become songs.
        public float noteFrequency;

        /// How close Pip must be before a gem lets go of its perch and
        /// slides toward him: mobile thumbs are imprecise, so near-misses on
        /// the game's core verb should still count.
        const float MagnetRadius = 2.2f;
        const float MagnetSpeed = 10f;

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
            // In the Undercloud, gems ARE the light sources: boost their
            // emission so they read as lanterns in the gloom rather than
            // the same brightness as daylight gems. All gems share one
            // material, so this is one colour write per level load.
            var levelDef = GameManager.Instance != null
                ? GameManager.Instance.CurrentLevelDefinition : null;
            float gemEmission = levelDef != null && levelDef.DarkRealm
                ? 2.4f : 1.6f;
            sharedGemMaterial.SetColor("_EmissionColor",
                ArtLib.GemPink * gemEmission);
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

            // Magnetism: once Pip is close, drop the bob and slide to him.
            if (!magnetized)
            {
                Transform pip = GameBootstrap.Player != null
                    ? GameBootstrap.Player.transform : null;
                if (pip != null &&
                    (pip.position - transform.position).sqrMagnitude
                        < MagnetRadius * MagnetRadius)
                    magnetized = true;
            }

            if (magnetized)
            {
                Transform pip = GameBootstrap.Player.transform;
                transform.position = Vector3.MoveTowards(transform.position,
                    pip.position, MagnetSpeed * Time.deltaTime);
            }
            else
            {
                transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);
                Vector3 pos = basePosition;
                pos.y += Mathf.Sin(Time.time * ArtLib.HoverBobRate + phase) * 0.22f;
                transform.localPosition = pos;
            }

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
            GameManager.Instance.OnGemCollected(!hasNote, transform.position);
            if (hasNote)
            {
                // The trail's own note replaces the generic pickup blip.
                AudioManager.Instance.PlayNote(noteFrequency);
            }
            Fx.Burst(transform.position, ArtLib.GemPink * 1.6f, 18);
            // The "+1" ramps with the live streak: it grows and shifts
            // toward the reward gold as the chain climbs, so mastery is
            // visible, not just audible.
            int streak = AudioManager.CurrentStreak;
            float popScale = Mathf.Min(1f + 0.04f * streak, 1.5f);
            Color popColor = Color.Lerp(ArtLib.GemPink, ArtLib.Gold,
                Mathf.Min(streak, 12) / 12f);
            Fx.Popup(transform.position + Vector3.up * 0.4f, "+1",
                popColor, popScale);
            Destroy(gameObject);
        }
    }
}
