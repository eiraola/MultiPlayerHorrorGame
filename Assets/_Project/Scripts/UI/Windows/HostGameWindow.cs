using ProjectScare.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    public sealed class HostGameWindow : MonoBehaviour
    {
        private const float ActiveLabelAlpha = 1f;
        private const float InactiveLabelAlpha = 0.35f;

        [SerializeField] private TMP_InputField gameNameField;
        [SerializeField] private Toggle publicToggle;
        [SerializeField] private Button createButton;
        [SerializeField] private TMP_Text createLabel;
        [SerializeField] private HostGameRequestBus hostRequestedBus;

        private bool requestSent;

        private void OnEnable()
        {
            gameNameField.onValueChanged.AddListener(RefreshCreateButton);
            createButton.onClick.AddListener(RequestHostGame);
            requestSent = false;
            RefreshCreateButton(gameNameField.text);
        }

        private void OnDisable()
        {
            gameNameField.onValueChanged.RemoveListener(RefreshCreateButton);
            createButton.onClick.RemoveListener(RequestHostGame);
        }

        private void RefreshCreateButton(string gameName)
        {
            bool canCreate = !requestSent && !string.IsNullOrWhiteSpace(gameName);

            createButton.interactable = canCreate;

            // The button's color tint only reaches its background, so the label shows the inactive state itself.
            createLabel.alpha = canCreate ? ActiveLabelAlpha : InactiveLabelAlpha;
        }

        private void RequestHostGame()
        {
            hostRequestedBus.Raise(HostGameRequest.Create(gameNameField.text, publicToggle.isOn));
            requestSent = true;
            RefreshCreateButton(gameNameField.text);
        }
    }
}
