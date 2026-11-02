using System;

namespace ProjectScare.Core
{
    public abstract class EventBus<T> : RuntimeBus
    {
        public event Action<T> Raised;

        public void Raise(T payload)
        {
            LogRaise(payload);
            Raised?.Invoke(payload);
        }

        internal override void ResetRuntimeState() => Raised = null;
    }
}
