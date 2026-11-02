using ProjectScare.Core;
using UnityEngine;

namespace ProjectScare.SceneFlow
{
    public sealed class StartupSceneRequester : MonoBehaviour
    {
        [SerializeField] private string startupSceneName = "Menu";
        [SerializeField] private StringEventBus sceneRequestBus;

        private void Start() => sceneRequestBus.Raise(startupSceneName);
    }
}
