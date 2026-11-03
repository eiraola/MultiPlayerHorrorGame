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
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private HostGameRequestBus hostRequestedBus;
        [SerializeField] private StringEventBus hostFailedBus;

        private bool requestSent;

        private void OnEnable()
        {
            gameNameField.onValueChanged.AddListener(RefreshCreateButton);
            createButton.onClick.AddListener(RequestHostGame);
            hostFailedBus.Raised += HandleHostFailed;
            requestSent = false;
            errorText.text = string.Empty;
            RefreshCreateButton(gameNameField.text);
        }

        private void OnDisable()
        {
            gameNameField.onValueChanged.RemoveListener(RefreshCreateButton);
            createButton.onClick.RemoveListener(RequestHostGame);
            hostFailedBus.Raised -= HandleHostFailed;
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
            errorText.text = string.Empty;
            requestSent = true;
            RefreshCreateButton(gameNameField.text);

            // Raised last so a synchronous failure is not overwritten by the blocked state above.
            hostRequestedBus.Raise(HostGameRequest.Create(gameNameField.text, publicToggle.isOn));
        }

        private void HandleHostFailed(string message)
        {
            requestSent = false;
            errorText.text = message;
            RefreshCreateButton(gameNameField.text);
        }
    }
}
