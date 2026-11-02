using System.Collections.Generic;

namespace ProjectScare.UI
{
    /// <summary>Stack of open windows that rejects any operation while a transition is running.</summary>
    public sealed class WindowStack<T> where T : class
    {
        private const int RootOnlyCount = 1;

        private readonly Stack<T> windows = new Stack<T>();

        public T Current => windows.Count > 0 ? windows.Peek() : null;
        public int Count => windows.Count;
        public bool IsTransitioning { get; private set; }

        public bool TryBeginPush(T window, out T previous)
        {
            previous = Current;

            if (IsTransitioning || window == null)
                return false;

            windows.Push(window);
            IsTransitioning = true;
            return true;
        }

        public bool TryBeginPop(out T closing, out T next)
        {
            closing = null;
            next = null;

            if (IsTransitioning || windows.Count <= RootOnlyCount)
                return false;

            closing = windows.Pop();
            next = windows.Peek();
            IsTransitioning = true;
            return true;
        }

        public void EndTransition() => IsTransitioning = false;
    }
}
