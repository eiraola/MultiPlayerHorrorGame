using ProjectScare.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    public sealed class JoinGameWindow : MonoBehaviour
    {
        [SerializeField] private Button joinGameButton;
        [SerializeField] private EventBus joinRequestedBus;

        private void OnEnable() => joinGameButton.onClick.AddListener(joinRequestedBus.Raise);

        private void OnDisable() => joinGameButton.onClick.RemoveListener(joinRequestedBus.Raise);
    }
}
