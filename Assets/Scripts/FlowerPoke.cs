using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// A flower that plays along: when Pip lands nearby it bounces, sheds
    /// a few petals and sings its own pentatonic note — hashed from its
    /// spot, so a given flower always sings the same note and a garden
    /// becomes an instrument. Registered at build time (budgeted per
    /// level), poked only from PlayerController's landing event: nothing
    /// here polls the world.
    public class FlowerPoke : MonoBehaviour
    {
        // The same C-major pentatonic set the melody gems sing.
        static readonly float[] Notes = { 523.25f, 587.33f, 659.25f, 783.99f, 880f };

        const float PokeRadius = 2.5f;     // Pip must land this close (xz)
        const float MaxResponders = 3;     // a chord, never a cacophony
        const float CooldownSeconds = 1.5f;
        const int MaxReactorsPerLevel = 40;

        static readonly List<FlowerPoke> reactors = new List<FlowerPoke>();

        Transform head;
        float note;
        float cooldownUntil;
        float bounce;     // spring scale offset around 1
        float bounceVel;

        static FlowerPoke()
        {
            PlayerController.Landed += OnLanded;
        }

        void Awake()
        {
            // Registration order follows the deterministic build order, so
            // the same level always has the same reactor flowers; the list
            // holds destroyed entries between levels and prunes them here.
            reactors.RemoveAll(r => r == null);
            if (reactors.Count >= MaxReactorsPerLevel) return;
            reactors.Add(this);

            note = Notes[Props.StableHash(transform.position) % Notes.Length];
            if (transform.childCount > 1) head = transform.GetChild(1);
            enabled = false; // the bounce spring only runs while popped
        }

        static void OnLanded(Vector3 point, float impactSpeed)
        {
            // Nearest-first: at most the three closest flowers answer one
            // landing, each on its own cooldown.
            reactors.RemoveAll(r => r == null);
            FlowerPoke best1 = null, best2 = null, best3 = null;
            float d1 = PokeRadius * PokeRadius;
            float d2 = d1, d3 = d1;
            for (int i = 0; i < reactors.Count; i++)
            {
                FlowerPoke r = reactors[i];
                Vector3 flat = point - r.transform.position;
                flat.y = 0f;
                float d = flat.sqrMagnitude;
                if (d > d1 && d > d2 && d > d3) continue;
                if (d < d1)
                {
                    d3 = d2; best3 = best2;
                    d2 = d1; best2 = best1;
                    d1 = d; best1 = r;
                }
                else if (d < d2)
                {
                    d3 = d2; best3 = best2;
                    d2 = d; best2 = r;
                }
                else if (d < d3)
                {
                    d3 = d; best3 = r;
                }
            }
            if (best1 != null) best1.Poke();
            if (best2 != null) best2.Poke();
            if (best3 != null) best3.Poke();
        }

        void Poke()
        {
            if (Time.time < cooldownUntil) return;
            cooldownUntil = Time.time + CooldownSeconds;

            enabled = true;
            bounceVel = 4.5f; // spring kick; settles in ~0.4 s
            Vector3 headPos = head != null ? head.position
                : transform.position + Vector3.up * 0.34f;
            MeshRenderer headRenderer = head != null
                ? head.GetComponent<MeshRenderer>() : null;
            Color petal = headRenderer != null &&
                headRenderer.sharedMaterial != null
                    ? headRenderer.sharedMaterial.color : ArtLib.GemPink;
            Fx.PetalPuff(headPos, petal, 3);
            AudioManager.Instance.PlayNote(note);
        }

        void Update()
        {
            // The shared spring, so a poked flower and Pip's squash are
            // the same material by construction, not by coincidence.
            Tweener.StepSpring(ref bounce, ref bounceVel, 0f,
                Time.deltaTime);
            transform.localScale = new Vector3(
                1f + bounce * 0.5f, 1f + bounce, 1f + bounce * 0.5f);
            if (Mathf.Abs(bounce) < 0.004f && Mathf.Abs(bounceVel) < 0.02f)
            {
                transform.localScale = Vector3.one;
                enabled = false;
            }
        }
    }
}
