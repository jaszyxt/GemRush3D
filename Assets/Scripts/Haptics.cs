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
    public static class Haptics
    {
        // Amplitudes are 1–255; picked to feel distinct through a case.
        const int AmplitudeLight = 72;
        const int AmplitudeMedium = 140;
        const int AmplitudeHeavy = 230;

        public static void Light()
        {
            Pulse(18, AmplitudeLight, 0);
        }

        public static void Medium()
        {
            Pulse(35, AmplitudeMedium, 1);
        }

        public static void Heavy()
        {
            Pulse(60, AmplitudeHeavy, 2);
        }

        static void Pulse(int milliseconds, int amplitude, int predefinedEffect)
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
                        // API 29+: predefined effects (click/tick/heavy click) are
                        // tuned by the device maker and feel noticeably crisper.
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
