using ProjectScare.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    public sealed class MainMenuWindow : MonoBehaviour
    {
        [SerializeField] private WindowNavigator navigator;
        [SerializeField] private WindowTransition hostGameWindow;
        [SerializeField] private WindowTransition joinGameWindow;
        [SerializeField] private WindowTransition settingsWindow;
        [SerializeField] private Button hostGameButton;
        [SerializeField] private Button joinGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private EventBus quitRequestBus;

        private void OnEnable()
        {
            hostGameButton.onClick.AddListener(OpenHostGame);
            joinGameButton.onClick.AddListener(OpenJoinGame);
            settingsButton.onClick.AddListener(OpenSettings);
            exitButton.onClick.AddListener(quitRequestBus.Raise);
        }

        private void OnDisable()
        {
            hostGameButton.onClick.RemoveListener(OpenHostGame);
            joinGameButton.onClick.RemoveListener(OpenJoinGame);
            settingsButton.onClick.RemoveListener(OpenSettings);
            exitButton.onClick.RemoveListener(quitRequestBus.Raise);
        }

        private void OpenHostGame() => navigator.Push(hostGameWindow);

        private void OpenJoinGame() => navigator.Push(joinGameWindow);

        private void OpenSettings() => navigator.Push(settingsWindow);
    }
}
