#if UNITY_EDITOR
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("UguiFlow.Editor")]

namespace UguiFlow
{
    internal static class ValidationQueue
    {
        private static readonly ConcurrentQueue<Object> Pending = new();

        // OnValidate can run on the loading thread. Check Unity objects on the editor thread.
        internal static void Request(Object target) => Pending.Enqueue(target);
        internal static bool TryDequeue(out Object target) => Pending.TryDequeue(out target);
    }
}
#endif
