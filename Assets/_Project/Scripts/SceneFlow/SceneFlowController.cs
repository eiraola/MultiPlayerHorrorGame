using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectScare.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectScare.SceneFlow
{
    public sealed class SceneFlowController : MonoBehaviour
    {
        [SerializeField] private StringEventBus sceneRequestBus;
        [SerializeField] private ScreenFader screenFader;

        private string contentSceneName;
        private bool isChangingScene;

        private void Awake()
        {
            contentSceneName = null;
            isChangingScene = false;
        }

        private void OnEnable() => sceneRequestBus.Raised += HandleSceneRequested;

        private void OnDisable() => sceneRequestBus.Raised -= HandleSceneRequested;

        private void HandleSceneRequested(string sceneName)
        {
            if (isChangingScene)
            {
                Debug.LogWarning($"Scene request '{sceneName}' ignored: a scene change is in progress.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' cannot be loaded. Is it in the build list?", this);
                return;
            }

            ChangeSceneAsync(sceneName, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid ChangeSceneAsync(string sceneName, CancellationToken ct)
        {
            isChangingScene = true;

            try
            {
                await screenFader.FadeToBlackAsync(ct);
                await UnloadContentSceneAsync(ct);
                await LoadContentSceneAsync(sceneName, ct);
                await screenFader.FadeFromBlackAsync(ct);
            }
            finally
            {
                isChangingScene = false;
            }
        }

        private async UniTask UnloadContentSceneAsync(CancellationToken ct)
        {
            if (contentSceneName == null)
                return;

            await SceneManager.UnloadSceneAsync(contentSceneName).ToUniTask(cancellationToken: ct);
            contentSceneName = null;
        }

        private async UniTask LoadContentSceneAsync(string sceneName, CancellationToken ct)
        {
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).ToUniTask(cancellationToken: ct);
            contentSceneName = sceneName;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        }
    }
}
