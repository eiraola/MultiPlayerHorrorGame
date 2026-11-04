using System.Text;
using ProjectScare.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    /// <summary>
    /// LobbyState empties the buses in its OnDestroy. While the scene unloads this window can still be
    /// subscribed with some references already destroyed, so every bus handler checks the ones it uses:
    /// an exception here would also stop LobbyState from emptying the remaining buses.
    /// </summary>
    public sealed class MultiplayerLobbyWindow : MonoBehaviour
    {
        private const string HostSuffix = " (Host)";
        private const string ReadySuffix = " - Listo";

        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text codeText;
        [SerializeField] private Button copyButton;
        [SerializeField] private TMP_Text playersText;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Toggle readyToggle;
        [SerializeField] private Button startButton;

        [Tooltip("Only the text of the buttons is visible, so this is what shows that Start is disabled.")]
        [SerializeField] private TMP_Text startButtonText;

        [SerializeField] private Color startTextEnabledColor = Color.white;
        [SerializeField] private Color startTextDisabledColor = Color.gray;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private StringValueBus roomNameBus;
        [SerializeField] private StringValueBus joinCodeBus;
        [SerializeField] private LobbyPlayerEntryArrayValueBus playersBus;
        [SerializeField] private BoolValueBus localReadyBus;
        [SerializeField] private BoolValueBus isHostBus;
        [SerializeField] private BoolValueBus canStartBus;
        [SerializeField] private IntValueBus countdownBus;
        [SerializeField] private EventBus startRequestBus;
        [SerializeField] private EventBus leaveRequestBus;

        private readonly StringBuilder playersBuilder = new();

        private void OnEnable()
        {
            roomNameBus.Changed += ShowRoomName;
            joinCodeBus.Changed += ShowCode;
            playersBus.Changed += ShowPlayers;
            isHostBus.Changed += ShowStartButton;
            canStartBus.Changed += ShowStartAvailability;
            countdownBus.Changed += ShowCountdown;
            copyButton.onClick.AddListener(CopyCode);
            leaveButton.onClick.AddListener(Leave);
            startButton.onClick.AddListener(RequestStart);
            readyToggle.onValueChanged.AddListener(SetLocalReady);

            ShowRoomName(roomNameBus.Value);
            ShowCode(joinCodeBus.Value);
            ShowPlayers(playersBus.Value);
            ShowStartButton(isHostBus.Value);
            ShowStartAvailability(canStartBus.Value);
            ShowCountdown(countdownBus.Value);
            readyToggle.SetIsOnWithoutNotify(localReadyBus.Value);
        }

        private void OnDisable()
        {
            roomNameBus.Changed -= ShowRoomName;
            joinCodeBus.Changed -= ShowCode;
            playersBus.Changed -= ShowPlayers;
            isHostBus.Changed -= ShowStartButton;
            canStartBus.Changed -= ShowStartAvailability;
            countdownBus.Changed -= ShowCountdown;
            copyButton.onClick.RemoveListener(CopyCode);
            leaveButton.onClick.RemoveListener(Leave);
            startButton.onClick.RemoveListener(RequestStart);
            readyToggle.onValueChanged.RemoveListener(SetLocalReady);
        }

        private void ShowRoomName(string roomName)
        {
            if (roomNameText == null)
                return;

            roomNameText.text = roomName;
        }

        private void ShowCode(string code)
        {
            if (codeText == null || copyButton == null)
                return;

            codeText.text = code;
            copyButton.interactable = !string.IsNullOrEmpty(code);
        }

        // The host is always the first name, since it joins the room before anyone else.
        private void ShowPlayers(LobbyPlayerEntry[] players)
        {
            if (playersText == null)
                return;

            playersBuilder.Clear();

            for (int i = 0; i < (players?.Length ?? 0); i++)
            {
                if (i > 0)
                    playersBuilder.AppendLine();

                playersBuilder.Append(players[i].Name);

                if (i == 0)
                    playersBuilder.Append(HostSuffix);

                if (players[i].IsReady)
                    playersBuilder.Append(ReadySuffix);
            }

            playersText.SetText(playersBuilder);
        }

        private void ShowStartButton(bool isHost)
        {
            if (startButton == null)
                return;

            startButton.gameObject.SetActive(isHost);
        }

        private void ShowStartAvailability(bool canStart)
        {
            if (startButton == null || startButtonText == null)
                return;

            startButton.interactable = canStart;
            startButtonText.color = canStart ? startTextEnabledColor : startTextDisabledColor;
        }

        private void ShowCountdown(int countdown)
        {
            if (countdownText == null)
                return;

            countdownText.text = countdown > LobbyRules.NoCountdown ? countdown.ToString() : string.Empty;

            // Never re-enabled here: Leave also disables its button, and that has to last.
            if (countdown == LobbyRules.CountdownFinished)
            {
                readyToggle.interactable = false;
                leaveButton.interactable = false;
            }
        }

        private void SetLocalReady(bool isReady) => localReadyBus.Set(isReady);

        private void RequestStart() => startRequestBus.Raise();

        private void CopyCode() => GUIUtility.systemCopyBuffer = joinCodeBus.Value;

        private void Leave()
        {
            // Only once: the window goes away with the scene when the fade ends.
            leaveButton.interactable = false;
            leaveRequestBus.Raise();
        }
    }
}
