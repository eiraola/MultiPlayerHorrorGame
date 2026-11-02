using ProjectScare.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    public sealed class JoinGameWindow : MonoBehaviour
    {
        private const float ActiveLabelAlpha = 1f;
        private const float InactiveLabelAlpha = 0.35f;

        [SerializeField] private Button joinGameButton;
        [SerializeField] private TMP_Text joinGameLabel;
        [SerializeField] private EventBus joinRequestedBus;

        private void OnEnable()
        {
            joinGameButton.onClick.AddListener(RequestJoinGame);
            SetJoinButtonActive(true);
        }

        private void OnDisable() => joinGameButton.onClick.RemoveListener(RequestJoinGame);

        private void RequestJoinGame()
        {
            SetJoinButtonActive(false);
            joinRequestedBus.Raise();
        }

        private void SetJoinButtonActive(bool isActive)
        {
            joinGameButton.interactable = isActive;

            // The button's color tint only reaches its background, so the label shows the inactive state itself.
            joinGameLabel.alpha = isActive ? ActiveLabelAlpha : InactiveLabelAlpha;
        }
    }
}
