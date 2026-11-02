using ProjectScare.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    public sealed class HostGameWindow : MonoBehaviour
    {
        [SerializeField] private TMP_InputField gameNameField;
        [SerializeField] private Toggle publicToggle;
        [SerializeField] private Button createButton;
        [SerializeField] private HostGameRequestBus hostRequestedBus;

        private void OnEnable()
        {
            gameNameField.onValueChanged.AddListener(RefreshCreateButton);
            createButton.onClick.AddListener(RequestHostGame);
            RefreshCreateButton(gameNameField.text);
        }

        private void OnDisable()
        {
            gameNameField.onValueChanged.RemoveListener(RefreshCreateButton);
            createButton.onClick.RemoveListener(RequestHostGame);
        }

        private void RefreshCreateButton(string gameName) =>
            createButton.interactable = !string.IsNullOrWhiteSpace(gameName);

        private void RequestHostGame() =>
            hostRequestedBus.Raise(HostGameRequest.Create(gameNameField.text, publicToggle.isOn));
    }
}
