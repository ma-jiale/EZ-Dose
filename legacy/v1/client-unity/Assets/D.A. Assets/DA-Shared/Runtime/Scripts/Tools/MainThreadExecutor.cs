using System;
using System.Collections.Concurrent;

namespace DA_Assets.Shared
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class EditorMainThreadExecutor
    {
        private static readonly ConcurrentQueue<Action> executionQueue = new ConcurrentQueue<Action>();

        static EditorMainThreadExecutor()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += Update;
#endif
        }

        public static void Invoke(Action action)
        {
            if (action == null)
            {
                return;
            }
            executionQueue.Enqueue(action);
        }

        private static void Update()
        {
            while (executionQueue.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
        }
    }
}