using System;
using ProjectScare.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    public sealed class RoomListRow : MonoBehaviour
    {
        private const string PasswordSuffix = " (password)";

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text playersText;
        [SerializeField] private TMP_Text ownerText;
        [SerializeField] private TMP_Text regionText;
        [SerializeField] private Button joinButton;

        private RoomSummary room;
        private Action<RoomSummary> joinClicked;

        private void OnEnable() => joinButton.onClick.AddListener(HandleJoinClicked);

        private void OnDisable() => joinButton.onClick.RemoveListener(HandleJoinClicked);

        public void Show(RoomSummary summary, Action<RoomSummary> onJoin)
        {
            room = summary;
            joinClicked = onJoin;

            nameText.text = summary.HasPassword ? summary.Name + PasswordSuffix : summary.Name;
            playersText.text = $"{summary.Players}/{summary.MaxPlayers}";
            ownerText.text = summary.OwnerName;
            regionText.text = summary.Region;
        }

        // A full room stays visible so the player knows it exists, but it cannot be joined.
        public void SetJoinEnabled(bool enabled) => joinButton.interactable = enabled && !room.IsFull;

        private void HandleJoinClicked() => joinClicked?.Invoke(room);
    }
}
