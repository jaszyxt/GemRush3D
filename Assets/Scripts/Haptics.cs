using UnityEngine;

namespace GemRush
{
    /// Short vibration pulses on Android. All device access is compiled only
    /// into Android builds and wrapped in try/catch, so this is a silent
    /// no-op everywhere else.
    public static class Haptics
    {
        public static void Light()
        {
            Pulse(18);
        }

        public static void Medium()
        {
            Pulse(40);
        }

        static void Pulse(int milliseconds)
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
                            using (AndroidJavaClass effectClass =
                                new AndroidJavaClass("android.os.VibrationEffect"))
                            {
                                using (AndroidJavaObject effect = effectClass.CallStatic<
                                    AndroidJavaObject>("createOneShot",
                                    (long)milliseconds, (int)-1))
                                {
                                    vibrator.Call("vibrate", effect);
                                }
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
    }
}
