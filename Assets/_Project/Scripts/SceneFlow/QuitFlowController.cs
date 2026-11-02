using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectScare.Core;
using UnityEngine;

namespace ProjectScare.SceneFlow
{
    /// <summary>Makes the Exit button and the operating system close request share the same fade out.</summary>
    public sealed class QuitFlowController : MonoBehaviour
    {
        [SerializeField] private EventBus quitRequestBus;
        [SerializeField] private ScreenFader screenFader;

        private bool isQuitStarted;
        private bool isQuitApproved;

        private void Awake()
        {
            isQuitStarted = false;
            isQuitApproved = false;
        }

        private void OnEnable()
        {
            Application.wantsToQuit += HandleWantsToQuit;
            quitRequestBus.Raised += BeginQuit;
        }

        private void OnDisable()
        {
            Application.wantsToQuit -= HandleWantsToQuit;
            quitRequestBus.Raised -= BeginQuit;
        }

        private bool HandleWantsToQuit()
        {
            if (isQuitApproved)
                return true;

            BeginQuit();
            return false;
        }

        private void BeginQuit()
        {
            if (isQuitStarted)
                return;

            isQuitStarted = true;
            QuitAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid QuitAsync(CancellationToken ct)
        {
            try
            {
                screenFader.HoldBlack();
                await screenFader.FadeToBlackAsync(ct);

                // The fade can finish synchronously when the screen is already black,
                // and Quit must not run inside the wantsToQuit callback that started it.
                await UniTask.Yield(ct);
            }
            finally
            {
                isQuitApproved = true;
                CloseApplication();
            }
        }

        private static void CloseApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}
