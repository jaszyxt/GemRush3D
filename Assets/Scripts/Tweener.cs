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

        class TweenerDriver : MonoBehaviour
        {
            void Update()
            {
                Tweener.Step(Time.unscaledDeltaTime);
            }
        }

        static readonly List<Tween> active = new List<Tween>();
        static TweenerDriver driver;

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
