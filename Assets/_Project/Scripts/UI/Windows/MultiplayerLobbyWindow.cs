using ProjectScare.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    public sealed class MultiplayerLobbyWindow : MonoBehaviour
    {
        [SerializeField] private GameObject codePanel;
        [SerializeField] private TMP_Text codeText;
        [SerializeField] private Button copyButton;
        [SerializeField] private StringValueBus joinCodeBus;

        private void OnEnable()
        {
            // The code is written before this scene loads and only the host has one, so clients see no panel.
            codePanel.SetActive(!string.IsNullOrEmpty(joinCodeBus.Value));
            codeText.text = joinCodeBus.Value;
            copyButton.onClick.AddListener(CopyCode);
        }

        private void OnDisable() => copyButton.onClick.RemoveListener(CopyCode);

        private void CopyCode() => GUIUtility.systemCopyBuffer = joinCodeBus.Value;
    }
}
