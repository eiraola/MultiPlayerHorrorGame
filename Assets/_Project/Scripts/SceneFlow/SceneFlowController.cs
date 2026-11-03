using System;
using System.Collections.Generic;
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
        [SerializeField] private EventBus contentUnloadRequestBus;
        [SerializeField] private ScreenFader screenFader;

        private string contentSceneName;
        private bool isChangingScene;

        private void Awake()
        {
            contentSceneName = null;
            isChangingScene = false;
        }

        private void OnEnable()
        {
            sceneRequestBus.Raised += HandleSceneRequested;
            contentUnloadRequestBus.Raised += HandleContentUnloadRequested;
        }

        private void OnDisable()
        {
            sceneRequestBus.Raised -= HandleSceneRequested;
            contentUnloadRequestBus.Raised -= HandleContentUnloadRequested;
        }

        private void HandleSceneRequested(string sceneName)
        {
            if (isChangingScene)
            {
                Debug.LogWarning($"Scene request '{sceneName}' ignored: a scene change is in progress.", this);
                return;
            }

            if (sceneName == gameObject.scene.name)
            {
                Debug.LogError($"Scene request '{sceneName}' rejected: it is the persistent scene.", this);
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
                if (!await screenFader.FadeToBlackAsync(ct))
                    return;

                await UnloadAllContentScenesAsync(ct);
                await LoadContentSceneAsync(sceneName, ct);
                await screenFader.FadeFromBlackAsync(ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogError($"Scene change to '{sceneName}' failed: {e}", this);
            }
            finally
            {
                isChangingScene = false;
            }
        }

        private void HandleContentUnloadRequested()
        {
            if (isChangingScene)
            {
                Debug.LogWarning("Content unload request ignored: a scene change is in progress.", this);
                return;
            }

            UnloadRequestedContentAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid UnloadRequestedContentAsync(CancellationToken ct)
        {
            isChangingScene = true;

            try
            {
                await UnloadContentSceneAsync(ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogError($"Unloading content scene '{contentSceneName}' failed: {e}", this);
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

        // Also unloads scenes this controller did not load, such as the one Netcode loads for the lobby.
        private async UniTask UnloadAllContentScenesAsync(CancellationToken ct)
        {
            var scenes = new List<Scene>(SceneManager.sceneCount);

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded && scene != gameObject.scene)
                    scenes.Add(scene);
            }

            foreach (Scene scene in scenes)
                await SceneManager.UnloadSceneAsync(scene).ToUniTask(cancellationToken: ct);

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
