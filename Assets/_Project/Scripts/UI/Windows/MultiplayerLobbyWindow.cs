using System.Text;
using ProjectScare.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    public sealed class MultiplayerLobbyWindow : MonoBehaviour
    {
        private const string HostSuffix = " (Host)";

        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text codeText;
        [SerializeField] private Button copyButton;
        [SerializeField] private TMP_Text playersText;
        [SerializeField] private Button leaveButton;
        [SerializeField] private StringValueBus roomNameBus;
        [SerializeField] private StringValueBus joinCodeBus;
        [SerializeField] private StringArrayValueBus playersBus;
        [SerializeField] private EventBus leaveRequestBus;

        private readonly StringBuilder playersBuilder = new();

        private void OnEnable()
        {
            roomNameBus.Changed += ShowRoomName;
            joinCodeBus.Changed += ShowCode;
            playersBus.Changed += ShowPlayers;
            copyButton.onClick.AddListener(CopyCode);
            leaveButton.onClick.AddListener(Leave);

            ShowRoomName(roomNameBus.Value);
            ShowCode(joinCodeBus.Value);
            ShowPlayers(playersBus.Value);
        }

        private void OnDisable()
        {
            roomNameBus.Changed -= ShowRoomName;
            joinCodeBus.Changed -= ShowCode;
            playersBus.Changed -= ShowPlayers;
            copyButton.onClick.RemoveListener(CopyCode);
            leaveButton.onClick.RemoveListener(Leave);
        }

        private void ShowRoomName(string roomName) => roomNameText.text = roomName;

        private void ShowCode(string code)
        {
            codeText.text = code;
            copyButton.interactable = !string.IsNullOrEmpty(code);
        }

        // The host is always the first name, since it joins the room before anyone else.
        private void ShowPlayers(string[] names)
        {
            playersBuilder.Clear();

            for (int i = 0; i < (names?.Length ?? 0); i++)
            {
                if (i > 0)
                    playersBuilder.AppendLine();

                playersBuilder.Append(names[i]);

                if (i == 0)
                    playersBuilder.Append(HostSuffix);
            }

            playersText.SetText(playersBuilder);
        }

        private void CopyCode() => GUIUtility.systemCopyBuffer = joinCodeBus.Value;

        private void Leave()
        {
            // Only once: the window goes away with the scene when the fade ends.
            leaveButton.interactable = false;
            leaveRequestBus.Raise();
        }
    }
}
