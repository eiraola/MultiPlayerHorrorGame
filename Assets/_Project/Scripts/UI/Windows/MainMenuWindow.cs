using ProjectScare.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    public sealed class MainMenuWindow : MonoBehaviour
    {
        private const string PlayerNamePrefsKey = "PlayerName";
        private const float ActiveLabelAlpha = 1f;
        private const float InactiveLabelAlpha = 0.35f;

        [SerializeField] private WindowNavigator navigator;
        [SerializeField] private WindowTransition hostGameWindow;
        [SerializeField] private WindowTransition joinGameWindow;
        [SerializeField] private WindowTransition settingsWindow;
        [SerializeField] private TMP_InputField playerNameField;
        [SerializeField] private Button hostGameButton;
        [SerializeField] private TMP_Text hostGameLabel;
        [SerializeField] private Button joinGameButton;
        [SerializeField] private TMP_Text joinGameLabel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private EventBus quitRequestBus;
        [SerializeField] private StringValueBus playerNameBus;

        private void Awake() => playerNameField.text = PlayerPrefs.GetString(PlayerNamePrefsKey, string.Empty);

        private void OnEnable()
        {
            playerNameField.onValueChanged.AddListener(HandlePlayerNameChanged);
            hostGameButton.onClick.AddListener(OpenHostGame);
            joinGameButton.onClick.AddListener(OpenJoinGame);
            settingsButton.onClick.AddListener(OpenSettings);
            exitButton.onClick.AddListener(quitRequestBus.Raise);
            HandlePlayerNameChanged(playerNameField.text);
        }

        private void OnDisable()
        {
            playerNameField.onValueChanged.RemoveListener(HandlePlayerNameChanged);
            hostGameButton.onClick.RemoveListener(OpenHostGame);
            joinGameButton.onClick.RemoveListener(OpenJoinGame);
            settingsButton.onClick.RemoveListener(OpenSettings);
            exitButton.onClick.RemoveListener(quitRequestBus.Raise);
        }

        private void HandlePlayerNameChanged(string rawName)
        {
            string playerName = PlayerName.Normalize(rawName);
            bool hasName = PlayerName.IsValid(playerName);

            PlayerPrefs.SetString(PlayerNamePrefsKey, playerName);
            playerNameBus.Set(playerName);

            SetButtonAvailable(hostGameButton, hostGameLabel, hasName);
            SetButtonAvailable(joinGameButton, joinGameLabel, hasName);
        }

        private static void SetButtonAvailable(Button button, TMP_Text label, bool isAvailable)
        {
            button.interactable = isAvailable;

            // The button's color tint only reaches its background, so the label shows the inactive state itself.
            label.alpha = isAvailable ? ActiveLabelAlpha : InactiveLabelAlpha;
        }

        private void OpenHostGame() => navigator.Push(hostGameWindow);

        private void OpenJoinGame() => navigator.Push(joinGameWindow);

        private void OpenSettings() => navigator.Push(settingsWindow);
    }
}
