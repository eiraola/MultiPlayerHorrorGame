using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectScare.Core;
using UnityEngine;

namespace ProjectScare.UI
{
    public sealed class WindowNavigator : MonoBehaviour
    {
        [SerializeField] private WindowTransition rootWindow;
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private BoolValueBus screenCoveredBus;

        private WindowStack<WindowTransition> stack;

        private void Awake() => stack = new WindowStack<WindowTransition>();

        private void OnEnable()
        {
            screenCoveredBus.Changed += HandleScreenCoveredChanged;
            HandleScreenCoveredChanged(screenCoveredBus.Value);
        }

        private void OnDisable() => screenCoveredBus.Changed -= HandleScreenCoveredChanged;

        public void Push(WindowTransition window)
        {
            if (window == null)
            {
                Debug.LogError("Window push rejected: the window is missing.", this);
                return;
            }

            if (screenCoveredBus.Value)
                return;

            if (stack.TryBeginPush(window, out WindowTransition previous))
                RunTransitionAsync(previous, window, destroyCancellationToken).Forget();
        }

        public void Back()
        {
            if (screenCoveredBus.Value)
                return;

            if (stack.TryBeginPop(out WindowTransition closing, out WindowTransition next))
                RunTransitionAsync(closing, next, destroyCancellationToken).Forget();
        }

        private void HandleScreenCoveredChanged(bool isCovered)
        {
            menuGroup.interactable = !isCovered;

            if (!isCovered && stack.Count == 0)
                Push(rootWindow);
        }

        private async UniTaskVoid RunTransitionAsync(WindowTransition hiding, WindowTransition showing, CancellationToken ct)
        {
            try
            {
                if (hiding != null)
                    await hiding.HideAsync(ct);

                await showing.ShowAsync(ct);
            }
            finally
            {
                stack.EndTransition();
            }
        }
    }
}
