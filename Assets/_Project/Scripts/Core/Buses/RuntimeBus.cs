using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProjectScare.Core
{
    /// <summary>
    /// Non-generic base of every databus. It exists because RuntimeInitializeOnLoadMethod
    /// is not allowed on generic classes.
    /// </summary>
    public abstract class RuntimeBus : ScriptableObject
    {
        [Tooltip("Logs every raise in the Editor, to check events that have no listener yet.")]
        [SerializeField] private bool logRaises;

        internal abstract void ResetRuntimeState();

        protected virtual void OnEnable() => ResetRuntimeState();

        [Conditional("UNITY_EDITOR")]
        protected void LogRaise(object payload)
        {
            if (logRaises)
                Debug.Log($"[{name}] {payload}", this);
        }

#if UNITY_EDITOR
        // With domain reload disabled, assets already in memory do not get OnEnable when entering Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLoadedBuses()
        {
            foreach (RuntimeBus bus in Resources.FindObjectsOfTypeAll<RuntimeBus>())
                bus.ResetRuntimeState();
        }
#endif
    }
}
