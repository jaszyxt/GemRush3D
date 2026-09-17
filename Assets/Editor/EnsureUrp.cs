using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GemRush.EditorTools
{
    /// <summary>
    /// Generates and assigns the URP pipeline assets (pipeline asset +
    /// renderer data) entirely from code, so the project keeps its
    /// "no manual editor setup" rule: nothing is imported or ticked by
    /// hand. The two generated .asset files are committed like
    /// ProjectSettings — they are configuration, not art. If they already
    /// exist this is a no-op, so a future Unity/URP API change can never
    /// break an existing project — only regeneration would need fixing.
    ///
    /// Mobile-tuned: HDR on (bloom needs >1 colors to feed on), 4x MSAA,
    /// Forward renderer (max device compatibility incl. GLES3).
    /// </summary>
    public static class EnsureUrp
    {
        const string AssetPath = "Assets/Settings/GemRushURP.asset";
        const string RendererPath = "Assets/Settings/GemRushURP-Forward.renderer";

        public static string Create()
        {
            if (AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(AssetPath) != null)
            {
                Assign(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(AssetPath));
                return "URP asset already present — (re)assigned.";
            }

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            // Renderer data first: its ctor is internal, CreateInstance
            // sidesteps that (standard trick for programmatic URP setup).
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                RendererPath);
            bool newRenderer = rendererData == null;
            if (newRenderer)
            {
                rendererData = ScriptableObject.CreateInstance("UniversalRendererData")
                    as UniversalRendererData;
                if (rendererData == null)
                    return "FAILED: could not create UniversalRendererData";
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            // Forward rendering mode for maximum device compatibility.
            var so = new SerializedObject(rendererData);
            SetInt(so, "m_RenderingMode", 0); // 0 = Forward
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var apo = new SerializedObject(asset);
            // URP 17 has no Initialize(): the renderer list is a serialized
            // field the pipeline reads when it builds its renderers.
            var list = apo.FindProperty("m_RendererDataList");
            if (list != null)
            {
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            }
            SetInt(apo, "m_DefaultRendererIndex", 0);
            SetBool(apo, "m_SupportsHDR", true);       // bloom feeds on >1 colors
            SetInt(apo, "m_MSAA", 4);                  // cheap at this geometry
            SetFloat(apo, "m_RenderScale", 1f);
            SetFloat(apo, "m_ShadowDistance", 60f);
            SetInt(apo, "m_ShadowCascades", 1);
            apo.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            Assign(asset);
            return "URP assets created at " + AssetPath + " and assigned"
                + (newRenderer ? "" : " (reused existing renderer data)");
        }

        static void Assign(RenderPipelineAsset asset)
        {
            GraphicsSettings.defaultRenderPipeline = asset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
                QualitySettings.renderPipeline = asset;
            Debug.Log("[GemRush] URP pipeline assigned to all quality levels.");
        }

        static void SetBool(SerializedObject o, string prop, bool v)
        { var p = o.FindProperty(prop); if (p != null) p.boolValue = v; }

        static void SetInt(SerializedObject o, string prop, int v)
        { var p = o.FindProperty(prop); if (p != null) p.intValue = v; }

        static void SetFloat(SerializedObject o, string prop, float v)
        { var p = o.FindProperty(prop); if (p != null) p.floatValue = v; }
    }
}
