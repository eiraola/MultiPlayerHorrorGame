using System;
using UnityEngine;

namespace ProjectScare.Core
{
    [CreateAssetMenu(menuName = "ProjectScare/Buses/Event Bus")]
    public class EventBus : RuntimeBus
    {
        public event Action Raised;

        public void Raise()
        {
            LogRaise("raised");
            Raised?.Invoke();
        }

        internal override void ResetRuntimeState() => Raised = null;
    }
}
