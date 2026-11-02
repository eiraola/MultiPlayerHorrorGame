using DG.Tweening;
using UnityEngine;

namespace ProjectScare.UI
{
    [CreateAssetMenu(menuName = "ProjectScare/Config/Window Transition")]
    public sealed class WindowTransitionConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float enterDuration = 0.2f;
        [SerializeField, Min(0f)] private float exitDuration = 0.15f;
        [SerializeField, Min(0f)] private float slideDistance = 40f;
        [SerializeField] private Ease enterEase = Ease.OutCubic;
        [SerializeField] private Ease exitEase = Ease.InCubic;

        public float EnterDuration => enterDuration;
        public float ExitDuration => exitDuration;
        public float SlideDistance => slideDistance;
        public Ease EnterEase => enterEase;
        public Ease ExitEase => exitEase;
    }
}
