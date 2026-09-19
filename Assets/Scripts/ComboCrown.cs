using UnityEngine;

namespace GemRush
{
    /// The visible streak crown: a small gold circlet that pops above Pip's
    /// head when the pickup chain reaches the octave cap and stays while the
    /// chain lives; when the streak resets it fades away softly. Purely
    /// decorative — no collider and no UI, so it can never block a raycast.
    /// AudioManager toggles it from the gem pickup path. The crown parents
    /// to the world, so a level rebuild destroys it — Notify re-creates it
    /// on the new Pip, and a streak carried across a portal is not lost
    /// (respawns teleport rather than rebuild, so those are survived).
    public class ComboCrown : MonoBehaviour
    {
        static ComboCrown instance;

        Transform target;
        Material gold;
        float bob;
        bool showing;

        /// The octave cap on the pickup pitch ladder — mastery made visible.
        const int StreakForCrown = 12;
        const float HeightAbove = 1.25f; // just over Pip's capsule head

        /// Master toggle from the pickup path; 0 (or any sub-cap streak)
        /// fades the crown out if it is showing.
        public static void Notify(int streak)
        {
            bool wanted = streak >= StreakForCrown;
            if (!wanted)
            {
                if (instance != null && instance.showing) instance.FadeOut();
                return;
            }
            if (instance != null && instance.target != null)
            {
                if (!instance.showing) instance.PopIn();
                return;
            }
            if (GameBootstrap.Player == null) return;
            if (instance != null) Object.Destroy(instance.gameObject);
            GameObject go = new GameObject("ComboCrown");
            go.transform.SetParent(GameBootstrap.World != null
                ? GameBootstrap.World.transform : null, false);
            instance = go.AddComponent<ComboCrown>();
            instance.target = GameBootstrap.Player.transform;
            instance.BuildBody();
            instance.PopIn();
        }

        void BuildBody()
        {
            // One fade-enabled gold material for the whole crown, so the
            // soft fade-out is a single color write per frame.
            gold = ArtLib.Solid(ArtLib.Gold, 0.9f);
            ArtLib.SetFade(gold, 1f);
            // Band plus three points — a crown in the game's soft-cube idiom.
            ArtLib.DecorCube(transform, Vector3.zero,
                new Vector3(0.55f, 0.12f, 0.55f), Quaternion.identity, gold);
            for (int i = -1; i <= 1; i++)
            {
                ArtLib.DecorCube(transform, new Vector3(i * 0.17f, 0.14f, 0f),
                    new Vector3(0.09f, 0.2f, 0.09f), Quaternion.identity, gold);
            }
        }

        void PopIn()
        {
            showing = true;
            gameObject.SetActive(true);
            Color c = gold.color;
            c.a = 1f;
            gold.color = c;
            // Land big, settle small — the same overshoot grammar as the
            // win screen's star slam and Pip's squash-and-stretch.
            Transform tr = transform;
            tr.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            ComboCrown self = this;
            Tweener.Value(1.3f, 1f, 0.24f, delegate(float k)
            {
                if (self == null) return;
                tr.localScale = new Vector3(k, k, k);
            });
        }

        void FadeOut()
        {
            showing = false;
            ComboCrown self = this;
            Transform tr = transform;
            Tweener.Value(1f, 0f, 0.35f, delegate(float k)
            {
                if (self == null) return; // world rebuilt mid-fade
                Color c = gold.color;
                c.a = k;
                gold.color = c;
                float s = Mathf.Lerp(0.85f, 1f, k);
                tr.localScale = new Vector3(s, s, s);
            }, delegate
            {
                if (self == null) return;
                gameObject.SetActive(false);
            });
        }

        void LateUpdate()
        {
            if (target == null) return;
            bob += Time.deltaTime;
            transform.position = target.position
                + Vector3.up * (HeightAbove + Mathf.Sin(bob * 2.2f) * 0.07f);
            transform.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);
        }
    }
}
