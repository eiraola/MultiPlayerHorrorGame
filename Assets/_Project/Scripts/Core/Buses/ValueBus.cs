using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectScare.Core
{
    public abstract class ValueBus<T> : RuntimeBus
    {
        [SerializeField] private T initialValue;

        private T currentValue;

        public event Action<T> Changed;

        public T Value => currentValue;

        public void Set(T value)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, value))
                return;

            currentValue = value;
            LogRaise(currentValue);
            Changed?.Invoke(currentValue);
        }

        internal override void ResetRuntimeState()
        {
            currentValue = initialValue;
            Changed = null;
        }
    }
}
