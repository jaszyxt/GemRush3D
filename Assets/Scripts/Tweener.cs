using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemRush
{
    /// Minimal tween runner: static entry, one hidden driver object,
    /// ease-out-quad by default. Powers UI and world micro-animations.
    public static class Tweener
    {
        class Tween
        {
            public float t;
            public float duration;
            public float from;
            public float to;
            public Action<float> onUpdate;
            public Action onDone;
        }

        [UnityEngine.Scripting.Preserve]
        class TweenerDriver : MonoBehaviour
        {
            void Update()
            {
                Tweener.Step(Time.unscaledDeltaTime);
            }
        }

        static readonly List<Tween> active = new List<Tween>();
        static TweenerDriver driver;

        /// The game's one spring: underdamped, so a squash or a bounce
        /// passes slightly PAST its target and settles — classic follow-
        /// through. Pip's body and the pokeable flowers both step it, and
        /// they must feel like the same material, so the constants live
        /// here rather than being copied into each caller (they were, and
        /// the copy carried a comment claiming they matched).
        public const float SpringStiffness = 90f;
        public const float SpringDamping = 12f;

        /// Advances a spring by one frame. Semantics: `value` is the
        /// offset from the target (0 = at rest), `velocity` its rate.
        public static void StepSpring(ref float value, ref float velocity,
            float target, float dt)
        {
            velocity += (-SpringStiffness * (value - target)
                - SpringDamping * velocity) * dt;
            value += velocity * dt;
        }

        /// The game's one-growth-pop: a thing appears by scaling up past
        /// its rest size and settling back (the sine bulge peaks mid-way).
        /// The garden bloom, the winter crystal shard and the raindrop
        /// flower all express "something sprouted here", and all three
        /// used to hand-roll this curve with the same 0.7 s and the same
        /// 0.35 amplitude in three separate files — so retuning one would
        /// silently desync the others.
        ///
        /// Returns the eased 0..1 progress; the caller maps it onto its own
        /// scale range, which IS legitimately per-object (a bud and a
        /// crystal shard do not grow to the same size).
        public const float PopSeconds = 0.7f;
        public const float PopBulge = 0.35f;

        public static float PopProgress(float elapsed, out float bulge)
        {
            float t = Mathf.Clamp01(elapsed / PopSeconds);
            bulge = Mathf.Sin(t * Mathf.PI) * PopBulge;
            return t;
        }

        public static void Value(float from, float to, float duration,
            Action<float> onUpdate, Action onDone = null)
        {
            if (driver == null)
            {
                driver = new GameObject("~Tweener").AddComponent<TweenerDriver>();
            }
            Tween tween = new Tween();
            tween.t = 0f;
            tween.duration = Mathf.Max(0.01f, duration);
            tween.from = from;
            tween.to = to;
            tween.onUpdate = onUpdate;
            tween.onDone = onDone;
            active.Add(tween);
        }

        static void Step(float dt)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Tween tween = active[i];
                tween.t += dt;
                float k = Mathf.Clamp01(tween.t / tween.duration);
                float eased = 1f - (1f - k) * (1f - k);
                if (tween.onUpdate != null)
                    tween.onUpdate(Mathf.Lerp(tween.from, tween.to, eased));
                if (k >= 1f)
                {
                    active.RemoveAt(i);
                    if (tween.onDone != null) tween.onDone();
                }
            }
        }
    }
}
