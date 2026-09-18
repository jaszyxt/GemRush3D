using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// A frozen doorway on the route (The Long Winter): a translucent ice
    /// wall that stands solid until the carried lantern's warmth melts it.
    /// Never harmful — no damage, no push — it just waits. Melting begins
    /// while <see cref="Lantern.Lit"/> and Pip stands within MeltRadius for
    /// about a second; a melted gate stays melted for the level's lifetime
    /// (deaths only teleport, so no rebuild — and no re-freeze) and leaves
    /// a flat slush remnant. On victory the remnants grow crystal shards in
    /// a wave sweeping out from the portal: the pack's "crystal map"
    /// signature, the melted paths refreezing into where you walked.
    public class IceGate : MonoBehaviour
    {
        public const float MeltRadius = 3.4f;   // XZ reach of the lantern's warmth
        public const float MeltHeight = 3.5f;   // vertical tolerance
        const float MeltSeconds = 1.1f;

        static readonly List<IceGate> all = new List<IceGate>();

        Transform slab;
        Material mat;
        BoxCollider block;
        Vector3 fullScale;
        float melt;          // 0 = solid .. 1 = melted
        bool melted;
        bool meltSoundPlayed;
        float pulseSeed;

        public static void Create(Transform parent, IceGateSpec spec)
        {
            PruneDestroyed(); // gates from a previous world die with it

            GameObject go = new GameObject("IceGate");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = spec.Center;

            Material ice = ArtLib.Solid(ArtLib.IceBlue, 0.35f);
            ArtLib.SetFade(ice, 0.72f);

            Transform slabT = ArtLib.DecorCube(go.transform, Vector3.zero,
                spec.Size, Quaternion.identity, ice).transform;
            // A frosty cap line so the wall reads as ice, not glass.
            ArtLib.DecorCube(go.transform,
                new Vector3(0f, spec.Size.y * 0.5f, 0f),
                new Vector3(spec.Size.x + 0.2f, 0.22f, spec.Size.z + 0.2f),
                Quaternion.identity, ArtLib.Solid(ArtLib.Snow, 0.15f));

            BoxCollider blockCol = go.AddComponent<BoxCollider>();
            blockCol.size = spec.Size;

            IceGate gate = go.AddComponent<IceGate>();
            gate.slab = slabT;
            gate.mat = ice;
            gate.block = blockCol;
            gate.fullScale = spec.Size;
            gate.pulseSeed = Random.value * 10f;
            all.Add(gate);
        }

        /// Clear the level-lifetime registry between world rebuilds (the
        /// old gates die with the old world; their entries must not linger).
        public static void PruneDestroyed()
        {
            all.RemoveAll(g => g == null);
        }

        /// How many melted gates exist — the crystal map only plays when
        /// Pip actually opened the frozen ways.
        public static int MeltedCount()
        {
            int n = 0;
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].melted) n++;
            return n;
        }

        void Update()
        {
            if (melted) return;

            // Idle shimmer: a slow alpha breathe so live ice reads as
            // something, without ever flashing (0.48 Hz family, audited).
            float shimmer = 0.62f + 0.10f *
                Mathf.Sin(Time.time * 3f + pulseSeed);
            Color c = mat.color;
            c.a = Mathf.Lerp(shimmer, 0.34f, melt);
            mat.color = c;

            if (!Lantern.Lit || GameBootstrap.Player == null) return;
            Vector3 pp = GameBootstrap.Player.transform.position;
            Vector2 dxz = new Vector2(pp.x - transform.position.x,
                                      pp.z - transform.position.z);
            if (dxz.magnitude > MeltRadius ||
                Mathf.Abs(pp.y - transform.position.y) > MeltHeight) return;

            if (!meltSoundPlayed)
            {
                meltSoundPlayed = true;
                AudioManager.Instance.PlayMelt();
            }

            melt = Mathf.Min(1f, melt + Time.deltaTime / MeltSeconds);
            float shrink = 1f - melt;
            slab.localScale = new Vector3(
                Mathf.Lerp(0.35f, 1f, shrink) * fullScale.x,
                shrink * fullScale.y,
                fullScale.z);

            if (melt >= 1f)
            {
                melted = true;
                block.enabled = false;
                Fx.Burst(transform.position, ArtLib.IceBlue * 1.4f, 18);
            }
        }

        /// The Long Winter's signature: every melted gate grows one small
        /// crystal shard, in a wave sweeping out from the portal — the
        /// refrozen map of everywhere Pip walked. Visual only; nothing
        /// re-freezes shut.
        public static void TriggerCrystalMap(Transform world, Vector3 portal)
        {
            PruneDestroyed();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null || !all[i].melted) continue;
                CrystalShard shard = all[i].gameObject.AddComponent<CrystalShard>();
                shard.delay = Vector3.Distance(all[i].transform.position,
                    portal) * 0.06f;
            }
        }

        /// One remnant's pop: a slim shard rises out of the slush with the
        /// same overshoot language as the garden blooms.
        class CrystalShard : MonoBehaviour
        {
            public float delay;
            float elapsed;
            Transform shard;

            void Update()
            {
                if (delay > 0f)
                {
                    delay -= Time.deltaTime;
                    return;
                }
                if (shard == null)
                {
                    Material crystal = ArtLib.Solid(ArtLib.PortalCyan, 1.1f);
                    ArtLib.SetFade(crystal, 0.85f);
                    shard = ArtLib.DecorCube(transform,
                        new Vector3(0f, 0.7f, 0f),
                        new Vector3(0.55f, 1.4f, 0.55f),
                        Quaternion.Euler(0f, 45f, 0f), crystal).transform;
                }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.7f);
                float pop = Mathf.Sin(t * Mathf.PI) * 0.35f;
                shard.localScale = new Vector3(0.55f, 1.4f, 0.55f)
                    * Mathf.Lerp(0.05f, 1f, t) * (1f + pop);
                if (t >= 1f) enabled = false;
            }
        }
    }
}
