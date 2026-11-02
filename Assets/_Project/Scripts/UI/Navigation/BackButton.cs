using UnityEngine;
using UnityEngine.UI;

namespace ProjectScare.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class BackButton : MonoBehaviour
    {
        [SerializeField] private WindowNavigator navigator;

        private Button button;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable() => button.onClick.AddListener(navigator.Back);

        private void OnDisable() => button.onClick.RemoveListener(navigator.Back);
    }
}
