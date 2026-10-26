using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlindRunway.EditorTools
{
    // Creates and assigns a URP pipeline asset once. The game only uses primitive default materials,
    // so it also renders under the built-in pipeline if this is never run.
    public static class UrpSetup
    {
        const string Dir = "Assets/_Project/Settings";

        [MenuItem("Blind Runway/Setup URP Pipeline")]
        public static void Setup()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
            {
                Debug.Log("URP already assigned.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/_Project", "Settings");
            var data = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(data, Dir + "/BR_Renderer.asset");
            var urp = UniversalRenderPipelineAsset.Create(data);
            AssetDatabase.CreateAsset(urp, Dir + "/BR_URP.asset");
            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;
            AssetDatabase.SaveAssets();
            Debug.Log("URP pipeline asset created and assigned.");
        }
    }
}
