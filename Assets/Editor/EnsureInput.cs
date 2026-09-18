using UnityEditor;
using UnityEngine;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Pins Active Input Handling to "Both" (directives D5/D6): the Input
    /// System package drives gamepads and UI focus navigation, while the
    /// legacy backend keeps serving touch (Input.GetTouch) and keyboard
    /// axes exactly as before — both run side by side. There is no public
    /// API for this setting, so the value is written into
    /// ProjectSettings.asset the same way EnsureShaders pins shaders.
    /// The editor must be restarted once for the native backend switch;
    /// builds pick the value up at build time on their own.
    /// </summary>
    [InitializeOnLoad]
    public static class EnsureInput
    {
        const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        const int Both = 2;

        static EnsureInput()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ProjectSettingsPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[GemRush] Could not load " + ProjectSettingsPath);
                return;
            }

            SerializedObject settings = new SerializedObject(assets[0]);
            SerializedProperty handler = settings.FindProperty("activeInputHandler");
            if (handler == null)
            {
                Debug.LogWarning("[GemRush] activeInputHandler not found.");
                return;
            }
            if (handler.intValue == Both) return; // already correct — quiet no-op

            handler.intValue = Both;
            settings.ApplyModifiedProperties();
            Debug.Log("[GemRush] Active Input Handling set to BOTH (Input System " +
                "gamepad + legacy touch/keyboard). Restart the editor to load " +
                "the new native input backend.");
        }
    }
}
