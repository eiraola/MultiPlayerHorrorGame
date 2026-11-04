using System;
using System.Collections.Generic;
using ProjectScare.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    // Shows the public rooms of the selected region. The list refreshes when the window opens,
    // when the region changes and on Refresh; there is no polling because the service limits queries.
    public sealed class RoomListView : MonoBehaviour
    {
        private const string AllRegionsLabel = "All regions";
        private const int AllRegionsIndex = 0;
        private const string LoadingMessage = "Loading rooms...";
        private const string EmptyMessage = "No rooms found.";

        [SerializeField] private TMP_Dropdown regionDropdown;
        [SerializeField] private Button refreshButton;
        [SerializeField] private RectTransform rowsParent;
        [SerializeField] private RoomListRow rowPrefab;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private StringArrayValueBus regionsBus;
        [SerializeField] private StringEventBus roomListRequestBus;
        [SerializeField] private RoomListBus roomListBus;
        [SerializeField] private StringEventBus roomListFailedBus;

        private readonly List<RoomListRow> rows = new();
        private bool joinEnabled = true;

        public event Action<RoomSummary> JoinClicked;

        private void OnEnable()
        {
            regionsBus.Changed += RefreshRegionOptions;
            regionDropdown.onValueChanged.AddListener(HandleRegionChanged);
            refreshButton.onClick.AddListener(RequestRooms);
            roomListBus.Raised += ShowRooms;
            roomListFailedBus.Raised += ShowFailure;

            RefreshRegionOptions(regionsBus.Value);
            RequestRooms();
        }

        private void OnDisable()
        {
            regionsBus.Changed -= RefreshRegionOptions;
            regionDropdown.onValueChanged.RemoveListener(HandleRegionChanged);
            refreshButton.onClick.RemoveListener(RequestRooms);
            roomListBus.Raised -= ShowRooms;
            roomListFailedBus.Raised -= ShowFailure;
        }

        // Turned off while a join is running, so a second room cannot be requested at the same time.
        public void SetJoinEnabled(bool enabled)
        {
            joinEnabled = enabled;

            foreach (RoomListRow row in rows)
                row.SetJoinEnabled(enabled);
        }

        private void RefreshRegionOptions(string[] regionIds)
        {
            string selectedRegion = SelectedRegion();
            var options = new List<string> { AllRegionsLabel };

            if (regionIds != null)
                options.AddRange(regionIds);

            regionDropdown.ClearOptions();
            regionDropdown.AddOptions(options);
            regionDropdown.SetValueWithoutNotify(Mathf.Max(options.IndexOf(selectedRegion), AllRegionsIndex));
        }

        private void HandleRegionChanged(int _) => RequestRooms();

        private void RequestRooms()
        {
            refreshButton.interactable = false;
            ClearRows();
            statusText.text = LoadingMessage;

            roomListRequestBus.Raise(SelectedRegion());
        }

        private void ShowRooms(RoomSummary[] rooms)
        {
            refreshButton.interactable = true;
            ClearRows();
            statusText.text = rooms.Length == 0 ? EmptyMessage : string.Empty;

            foreach (RoomSummary room in rooms)
            {
                RoomListRow row = Instantiate(rowPrefab, rowsParent);
                row.Show(room, HandleJoinClicked);
                row.SetJoinEnabled(joinEnabled);
                rows.Add(row);
            }
        }

        private void ShowFailure(string message)
        {
            refreshButton.interactable = true;
            statusText.text = message;
        }

        private void ClearRows()
        {
            foreach (RoomListRow row in rows)
                Destroy(row.gameObject);

            rows.Clear();
        }

        private void HandleJoinClicked(RoomSummary room) => JoinClicked?.Invoke(room);

        private string SelectedRegion() =>
            regionDropdown.value == AllRegionsIndex || regionDropdown.options.Count == 0
                ? null
                : regionDropdown.options[regionDropdown.value].text;
    }
}
