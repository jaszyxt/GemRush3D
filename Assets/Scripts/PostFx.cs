using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GemRush
{
    /// <summary>
    /// The mood layer: a global URP volume created entirely at runtime
    /// (VolumeProfile is built from code — no asset files, keeping the
    /// project's zero-imported-assets rule). Bloom makes the gems and the
    /// goal portal genuinely luminous; vignette frames the cozy view; the
    /// colour grading extends palette-as-data into post-processing —
    /// Undercloud levels run desaturated and contrastier, daylight levels
    /// get a warm lift.
    /// </summary>
    public static class PostFx
    {
        static Volume volume;
        static ColorAdjustments grading;

        public static void Create(LevelDefinition level)
        {
            if (volume == null)
            {
                GameObject go = new GameObject("PostFX Volume");
                volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

                Bloom bloom = volume.profile.Add<Bloom>(true);
                bloom.intensity.Override(0.85f);
                bloom.threshold.Override(0.95f);
                bloom.scatter.Override(0.75f);
                bloom.tint.Override(new Color(1f, 0.98f, 0.95f));

                Vignette vignette = volume.profile.Add<Vignette>(true);
                vignette.intensity.Override(0.24f);
                vignette.smoothness.Override(0.45f);
                vignette.color.Override(new Color(0f, 0f, 0.04f, 1f));

                grading = volume.profile.Add<ColorAdjustments>(true);
            }
            Apply(level);
        }

        static void Apply(LevelDefinition level)
        {
            if (grading == null) return;
            if (level.DarkRealm)
            {
                grading.saturation.Override(-12f);
                grading.contrast.Override(10f);
            }
            else
            {
                grading.saturation.Override(4f);
                grading.contrast.Override(4f);
            }
        }
    }
}
