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

        [SerializeField] private TMP_InputField codeField;
        [SerializeField] private TMP_InputField passwordField;
        [SerializeField] private Button joinGameButton;
        [SerializeField] private TMP_Text joinGameLabel;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private JoinGameRequestBus joinRequestedBus;
        [SerializeField] private StringEventBus joinFailedBus;
        [SerializeField] private StringValueBus playerNameBus;

        private bool requestSent;

        private void OnEnable()
        {
            codeField.onValueChanged.AddListener(HandleCodeChanged);
            joinGameButton.onClick.AddListener(RequestJoinGame);
            joinFailedBus.Raised += HandleJoinFailed;
            requestSent = false;
            errorText.text = string.Empty;
            passwordField.SetTextWithoutNotify(string.Empty);
            RefreshJoinButton();
        }

        private void OnDisable()
        {
            codeField.onValueChanged.RemoveListener(HandleCodeChanged);
            joinGameButton.onClick.RemoveListener(RequestJoinGame);
            joinFailedBus.Raised -= HandleJoinFailed;
        }

        private void HandleCodeChanged(string _) => RefreshJoinButton();

        private void RefreshJoinButton()
        {
            bool canJoin = !requestSent && !string.IsNullOrWhiteSpace(codeField.text);

            joinGameButton.interactable = canJoin;

            // The button's color tint only reaches its background, so the label shows the inactive state itself.
            joinGameLabel.alpha = canJoin ? ActiveLabelAlpha : InactiveLabelAlpha;
        }

        private void RequestJoinGame()
        {
            errorText.text = string.Empty;
            requestSent = true;
            RefreshJoinButton();

            // Raised last so a synchronous failure is not overwritten by the blocked state above.
            joinRequestedBus.Raise(JoinGameRequest.Create(codeField.text, passwordField.text, playerNameBus.Value));
        }

        private void HandleJoinFailed(string message)
        {
            requestSent = false;
            errorText.text = message;
            RefreshJoinButton();
        }
    }
}
