using DG.Tweening;
using UnityEngine;

namespace ProjectScare.SceneFlow
{
    [CreateAssetMenu(menuName = "ProjectScare/Config/Screen Fade")]
    public sealed class ScreenFadeConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float fadeToBlackDuration = 0.5f;
        [SerializeField, Min(0f)] private float fadeFromBlackDuration = 0.5f;
        [SerializeField] private Ease ease = Ease.Linear;

        public float FadeToBlackDuration => fadeToBlackDuration;
        public float FadeFromBlackDuration => fadeFromBlackDuration;
        public Ease Ease => ease;
    }
}
