using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectScare.EditorTools
{
    [InitializeOnLoad]
    internal static class BootstrapPlayModeStart
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        // The asset database may not be ready yet while the static constructor runs.
        static BootstrapPlayModeStart() => EditorApplication.delayCall += AssignPlayModeStartScene;

        private static void AssignPlayModeStartScene()
        {
            SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);

            if (bootstrapScene == null)
                Debug.LogWarning($"Play mode start scene not set: '{BootstrapScenePath}' was not found.");

            EditorSceneManager.playModeStartScene = bootstrapScene;
        }
    }
}
