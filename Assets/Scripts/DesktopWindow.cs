using System.Runtime.InteropServices;
using UnityEngine;

namespace GemRush
{
    /// Desktop window-rect memory (D8): a resizable window reopens where
    /// the player left it. Size restores through Screen.SetResolution;
    /// position moves through the OS on Windows only (Unity exposes no
    /// portable window-position API). Never runs on mobile and never
    /// fights the borderless-fullscreen default: while the game is in
    /// fullscreen the rect is neither applied nor overwritten.
    public static class DesktopWindow
    {
        const string KeyX = "window.x";
        const string KeyY = "window.y";
        const string KeyW = "window.w";
        const string KeyH = "window.h";

        /// Only the Windows player moves/restores its own window; the
        /// editor window is the developer's, and mobile is always
        /// fullscreen.
        static bool Applies
        {
            get { return Application.platform == RuntimePlatform.WindowsPlayer; }
        }

        public static void Restore()
        {
            if (!Applies || SaveSystem.FullscreenOn) return;
            if (!PlayerPrefs.HasKey(KeyW)) return;

            int w = PlayerPrefs.GetInt(KeyW, 1600);
            int h = PlayerPrefs.GetInt(KeyH, 900);
            // Clamp to something the desktop can actually show — a monitor
            // change between sessions must not restore an invisible window.
            w = Mathf.Clamp(w, 640, Display.main.systemWidth);
            h = Mathf.Clamp(h, 360, Display.main.systemHeight);
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
#if UNITY_STANDALONE_WIN
            RestorePosition();
#endif
        }

        public static void Save()
        {
            if (!Applies) return;
            if (Screen.fullScreenMode != FullScreenMode.Windowed) return;
            PlayerPrefs.SetInt(KeyW, Screen.width);
            PlayerPrefs.SetInt(KeyH, Screen.height);
#if UNITY_STANDALONE_WIN
            SavePosition();
#endif
            PlayerPrefs.Save();
        }

#if UNITY_STANDALONE_WIN
        [DllImport("user32.dll")]
        static extern System.IntPtr GetActiveWindow();
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(System.IntPtr hWnd, out Rect apiRect);
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr after,
            int x, int y, int width, int height, uint flags);

        // SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE — move only.
        const uint MoveOnly = 0x0001 | 0x0004 | 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        static void SavePosition()
        {
            System.IntPtr hwnd = GetActiveWindow();
            if (hwnd == System.IntPtr.Zero) return;
            Rect r;
            if (GetWindowRect(hwnd, out r))
            {
                PlayerPrefs.SetInt(KeyX, r.Left);
                PlayerPrefs.SetInt(KeyY, r.Top);
            }
        }

        static void RestorePosition()
        {
            if (!PlayerPrefs.HasKey(KeyX)) return;
            System.IntPtr hwnd = GetActiveWindow();
            if (hwnd == System.IntPtr.Zero) return;
            SetWindowPos(hwnd, System.IntPtr.Zero,
                PlayerPrefs.GetInt(KeyX, 0), PlayerPrefs.GetInt(KeyY, 0),
                0, 0, MoveOnly);
        }
#endif
    }
}
