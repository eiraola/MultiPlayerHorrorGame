using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using ProjectScare.Core;
using UnityEngine;

namespace ProjectScare.SceneFlow
{
    /// <summary>Owns the full screen fade and is the only writer of the screen covered bus.</summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        private const float OpaqueAlpha = 1f;
        private const float TransparentAlpha = 0f;

        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private ScreenFadeConfig config;
        [SerializeField] private BoolValueBus screenCoveredBus;

        private Tween fadeTween;
        private bool isBlackHeld;

        private void Awake()
        {
            fadeTween = null;
            isBlackHeld = false;
        }

        public void HoldBlack() => isBlackHeld = true;

        public async UniTask FadeToBlackAsync(CancellationToken ct)
        {
            screenCoveredBus.Set(true);
            await FadeAsync(OpaqueAlpha, config.FadeToBlackDuration, ct);
        }

        public async UniTask FadeFromBlackAsync(CancellationToken ct)
        {
            if (isBlackHeld)
                return;

            bool completed = await FadeAsync(TransparentAlpha, config.FadeFromBlackDuration, ct);

            if (completed)
                screenCoveredBus.Set(false);
        }

        /// <returns>False if a newer fade replaced this one before it finished.</returns>
        private async UniTask<bool> FadeAsync(float targetAlpha, float fullDuration, CancellationToken ct)
        {
            fadeTween?.Kill();
            fadeTween = null;

            float remainingPath = Mathf.Abs(targetAlpha - fadeGroup.alpha);
            if (Mathf.Approximately(remainingPath, 0f))
            {
                fadeGroup.alpha = targetAlpha;
                return true;
            }

            Tween tween = CreateFadeTween(targetAlpha, fullDuration * remainingPath);
            fadeTween = tween;
            await tween.ToUniTask(TweenCancelBehaviour.Kill, ct);

            return fadeTween == tween;
        }

        private Tween CreateFadeTween(float targetAlpha, float duration) =>
            fadeGroup.DOFade(targetAlpha, duration)
                .SetEase(config.Ease)
                .SetUpdate(true)
                .SetLink(gameObject);
    }
}
