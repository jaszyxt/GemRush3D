using UnityEngine;

namespace GemRush
{
    /// Vibration pulses on Android with amplitude control where the device
    /// supports it (API 26+). The Handheld.Vibrate fallback below is what
    /// makes Unity include android.permission.VIBRATE in the manifest —
    /// without that permission the JNI path throws SecurityException and
    /// vibration silently no-ops. All device access is compiled only into
    /// Android builds and wrapped in try/catch, so this is a silent no-op
    /// everywhere else.
    ///
    /// Grammar (AOSP haptics UX guidance): strength scales TICK < CLICK <
    /// HEAVY_CLICK < DOUBLE_CLICK, effects must be proportional to event
    /// importance, and same-class retriggers inside ~120ms fuse into one
    /// buzz — so pickup ticks are rate-limited here.
    public static class Haptics
    {
        // Amplitudes are 1–255; adjacent steps are >1.4x so they stay
        // distinguishable (Android perception threshold).
        const int AmplitudeLight = 60;
        const int AmplitudeMedium = 120;
        const int AmplitudeHeavy = 200;

        // android.os.VibrationEffect predefined effect ids.
        const int EffectTick = 1;         // lightest
        const int EffectClick = 0;        // midpoint
        const int EffectHeavyClick = 2;   // strong
        const int EffectDoubleClick = 3;  // fanfare / heartbeat

        static float lastEffectTime = -99f;

        public static void Light()
        {
            Pulse(18, AmplitudeLight, EffectTick);
        }

        public static void Medium()
        {
            Pulse(35, AmplitudeMedium, EffectClick);
        }

        public static void Heavy()
        {
            Pulse(60, AmplitudeHeavy, EffectHeavyClick);
        }

        /// Positive fanfare (level won, spare life found): DOUBLE_CLICK's
        /// repeating energy is the point. Bypasses the retrigger cooldown —
        /// it fires at most once per level.
        public static void Fanfare()
        {
            RawPulse(60, AmplitudeHeavy, EffectDoubleClick);
        }

        static void Pulse(int milliseconds, int amplitude, int predefinedEffect)
        {
            // Same-class retrigger cooldown: quick gem-trail ticks shouldn't
            // fuse into one long buzz.
            float now = Time.realtimeSinceStartup;
            if (now - lastEffectTime < 0.12f) return;
            lastEffectTime = now;
            RawPulse(milliseconds, amplitude, predefinedEffect);
        }

        static void RawPulse(int milliseconds, int amplitude, int predefinedEffect)
        {
            if (!SaveSystem.HapticsOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer =
                    new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (AndroidJavaObject activity =
                        unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (AndroidJavaObject vibrator =
                            activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                        {
                            if (vibrator == null) return;
                            if (!VibrateWithEffect(vibrator, milliseconds, amplitude,
                                predefinedEffect))
                            {
                                // Devices without amplitude control (or very old
                                // APIs) still get a usable buzz. This call also
                                // guarantees the VIBRATE permission ships in the
                                // manifest, which the JNI path needs to be allowed
                                // at all.
                                Handheld.Vibrate();
                            }
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // Vibration is optional polish; never let it break gameplay.
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS: Handheld.Vibrate is the one primitive Unity exposes there
            // (a single system-strength buzz). Amplitude gradations are an
            // Android luxury; on iOS every pulse is the same honest buzz.
            if (SaveSystem.HapticsOn) Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static bool VibrateWithEffect(AndroidJavaObject vibrator, int milliseconds,
            int amplitude, int predefinedEffect)
        {
            try
            {
                using (AndroidJavaObject sdkInt =
                    new AndroidJavaClass("android.os.Build$VERSION")
                        .GetStatic<AndroidJavaObject>("SDK_INT"))
                {
                    int api = sdkInt.Call<int>("intValue");
                    using (AndroidJavaClass effectClass =
                        new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        // API 29+: predefined effects (tick/click/heavy click/
                        // double click) are tuned by the device maker and feel
                        // noticeably crisper.
                        if (api >= 29)
                        {
                            using (AndroidJavaObject effect = effectClass.CallStatic<
                                AndroidJavaObject>("createPredefined", predefinedEffect))
                            {
                                if (effect != null)
                                {
                                    vibrator.Call("vibrate", effect);
                                    return true;
                                }
                            }
                        }
                        if (api >= 26)
                        {
                            using (AndroidJavaObject effect = effectClass.CallStatic<
                                AndroidJavaObject>("createOneShot",
                                (long)milliseconds, (int)amplitude))
                            {
                                if (effect != null)
                                {
                                    vibrator.Call("vibrate", effect);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // Fall through to the legacy path below.
            }
            return false;
        }
#endif
    }
}
