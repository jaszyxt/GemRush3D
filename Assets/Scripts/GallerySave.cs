using System.IO;
using UnityEngine;

namespace GemRush
{
    /// Pushes a saved PNG into the device's photo gallery so it is visible
    /// in the system Photos / Gallery app, not just in the app's private
    /// storage.  Desktop builds are unaffected (no gallery API).
    ///
    /// Android: MediaStore insert via JNI (follows Haptics.cs pattern). Works
    /// on API 29+ without WRITE_EXTERNAL_STORAGE; on the rare API 26-28
    /// device the insert falls back silently (file already safe in
    /// persistentDataPath).
    ///
    /// iOS: a small native plugin at Assets/Plugins/iOS/GallerySave.m writes
    /// the image via PHPhotoLibrary. Returns via UnitySendMessage.
    public static class GallerySave
    {
        /// Add the file at <paramref name="path"/> to the system photo
        /// gallery. Call after a successful write to persistentDataPath —
        /// the gallery push is always a secondary copy.
        public static void AddToGallery(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidAddToGallery(path);
#elif UNITY_IOS && !UNITY_EDITOR
            _GallerySave(path);
#endif
        }

        // ----------------------------------------------------------------
        // iOS native bridge
        // ----------------------------------------------------------------
#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void _GallerySave(string path);

        // Called by the native plugin via UnitySendMessage.
        static void OnSaveSuccess(string _) { }
        static void OnSaveFailed(string error)
        {
            Debug.LogWarning("[GallerySave] iOS gallery save failed: " + error);
        }
#endif

        // ----------------------------------------------------------------
        // Android — MediaStore insert via JNI
        // ----------------------------------------------------------------
#if UNITY_ANDROID && !UNITY_EDITOR
        static void AndroidAddToGallery(string path)
        {
            // Gallery push is optional polish; never let it break gameplay.
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes == null || bytes.Length == 0) return;

                using (var unityPlayer =
                    new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var activity =
                        unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (var resolver =
                            activity.Call<AndroidJavaObject>("getContentResolver"))
                        {
                            InsertViaMediaStore(resolver, path, bytes);
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // Gallery save is optional; the file is already written to
                // persistentDataPath and can be retrieved from there.
            }
        }

        static void InsertViaMediaStore(
            AndroidJavaObject resolver, string path, byte[] png)
        {
            using (var cvClass =
                new AndroidJavaClass("android.content.ContentValues"))
            using (var imagesClass =
                new AndroidJavaClass(
                    "android.provider.MediaStore$Images$Media"))
            {
                AndroidJavaObject values =
                    cvClass.CallStatic<AndroidJavaObject>("__ctor");
                values.Call("put", "mime_type", "image/png");
                values.Call("put", "display_name", Path.GetFileName(path));

                AndroidJavaObject uri = resolver.Call<AndroidJavaObject>(
                    "insert",
                    imagesClass.GetStatic<AndroidJavaObject>(
                        "EXTERNAL_CONTENT_URI"),
                    values);

                if (uri == null) return;

                using (AndroidJavaObject stream =
                    resolver.Call<AndroidJavaObject>("openOutputStream", uri))
                {
                    stream.Call("write", png);
                }

                // Publish: clear pending flag so the gallery picks it up.
                values.Call("clear");
                values.Call("put", "is_pending", 0);
                resolver.Call("update", uri, values, null);
            }
        }
#endif
    }
}
