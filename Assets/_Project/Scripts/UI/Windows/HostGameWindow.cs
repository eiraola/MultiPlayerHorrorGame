using System.Collections.Generic;
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
        private const string AutomaticRegionLabel = "Automatic";
        private const int AutomaticRegionIndex = 0;

        [SerializeField] private TMP_InputField gameNameField;
        [SerializeField] private Toggle publicToggle;
        [SerializeField] private TMP_Dropdown regionDropdown;
        [SerializeField] private Toggle passwordToggle;
        [SerializeField] private TMP_InputField passwordField;
        [SerializeField] private Button createButton;
        [SerializeField] private TMP_Text createLabel;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private HostGameRequestBus hostRequestedBus;
        [SerializeField] private StringEventBus hostFailedBus;
        [SerializeField] private StringValueBus playerNameBus;
        [SerializeField] private StringArrayValueBus regionsBus;

        private bool requestSent;

        private void OnEnable()
        {
            regionsBus.Changed += RefreshRegionOptions;
            RefreshRegionOptions(regionsBus.Value);

            gameNameField.onValueChanged.AddListener(HandleTextChanged);
            passwordField.onValueChanged.AddListener(HandleTextChanged);
            passwordToggle.onValueChanged.AddListener(HandlePasswordToggled);
            createButton.onClick.AddListener(RequestHostGame);
            hostFailedBus.Raised += HandleHostFailed;
            requestSent = false;
            errorText.text = string.Empty;
            passwordField.SetTextWithoutNotify(string.Empty);
            HandlePasswordToggled(passwordToggle.isOn);
        }

        private void OnDisable()
        {
            regionsBus.Changed -= RefreshRegionOptions;
            gameNameField.onValueChanged.RemoveListener(HandleTextChanged);
            passwordField.onValueChanged.RemoveListener(HandleTextChanged);
            passwordToggle.onValueChanged.RemoveListener(HandlePasswordToggled);
            createButton.onClick.RemoveListener(RequestHostGame);
            hostFailedBus.Raised -= HandleHostFailed;
        }

        private void RefreshRegionOptions(string[] regionIds)
        {
            string selectedRegion = SelectedRegion();
            var options = new List<string> { AutomaticRegionLabel };

            if (regionIds != null)
                options.AddRange(regionIds);

            regionDropdown.ClearOptions();
            regionDropdown.AddOptions(options);

            // Clearing the options resets the dropdown, and reopening the window must keep the player's choice.
            regionDropdown.SetValueWithoutNotify(Mathf.Max(options.IndexOf(selectedRegion), AutomaticRegionIndex));
        }

        private void HandleTextChanged(string _) => RefreshCreateButton();

        private void HandlePasswordToggled(bool usePassword)
        {
            passwordField.interactable = usePassword;
            RefreshCreateButton();
        }

        private void RefreshCreateButton()
        {
            bool hasValidPassword = !passwordToggle.isOn || HostGameRequest.IsValidPassword(passwordField.text);
            bool canCreate = !requestSent && !string.IsNullOrWhiteSpace(gameNameField.text) && hasValidPassword;

            createButton.interactable = canCreate;

            // The button's color tint only reaches its background, so the label shows the inactive state itself.
            createLabel.alpha = canCreate ? ActiveLabelAlpha : InactiveLabelAlpha;
        }

        private void RequestHostGame()
        {
            errorText.text = string.Empty;
            requestSent = true;
            RefreshCreateButton();

            // Raised last so a synchronous failure is not overwritten by the blocked state above.
            hostRequestedBus.Raise(HostGameRequest.Create(
                gameNameField.text,
                publicToggle.isOn,
                SelectedRegion(),
                passwordToggle.isOn ? passwordField.text : null,
                playerNameBus.Value));
        }

        private string SelectedRegion() =>
            regionDropdown.value == AutomaticRegionIndex ? null : regionDropdown.options[regionDropdown.value].text;

        private void HandleHostFailed(string message)
        {
            requestSent = false;
            errorText.text = message;
            RefreshCreateButton();
        }
    }
}
