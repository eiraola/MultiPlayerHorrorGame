using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace ProjectScare.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WindowTransition : MonoBehaviour
    {
        private const float VisibleAlpha = 1f;
        private const float HiddenAlpha = 0f;

        [SerializeField] private WindowTransitionConfig config;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector2 restPosition;
        private Tween transitionTween;

        private Vector2 SlidOutPosition => restPosition + Vector2.left * config.SlideDistance;

        // Windows are saved inactive, so this first runs inside the first ShowAsync.
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = (RectTransform)transform;
            restPosition = rectTransform.anchoredPosition;
            transitionTween = null;
        }

        public async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            canvasGroup.interactable = false;
            canvasGroup.alpha = HiddenAlpha;
            rectTransform.anchoredPosition = SlidOutPosition;

            await PlayAsync(VisibleAlpha, restPosition, config.EnterDuration, config.EnterEase, ct);

            canvasGroup.interactable = true;
        }

        public async UniTask HideAsync(CancellationToken ct)
        {
            canvasGroup.interactable = false;

            await PlayAsync(HiddenAlpha, SlidOutPosition, config.ExitDuration, config.ExitEase, ct);

            gameObject.SetActive(false);
        }

        private async UniTask PlayAsync(float alpha, Vector2 position, float duration, Ease ease, CancellationToken ct)
        {
            transitionTween?.Kill();

            // The ease goes on each tween: on the sequence it would stack on top of their default ease.
            transitionTween = DOTween.Sequence()
                .Join(canvasGroup.DOFade(alpha, duration).SetEase(ease))
                .Join(rectTransform.DOAnchorPos(position, duration).SetEase(ease))
                .SetUpdate(true)
                .SetLink(gameObject);

            // Cancelling the await stops the caller from touching this object once the scene is unloaded.
            await transitionTween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, ct);

            transitionTween = null;
        }
    }
}
