using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace UguiFlow
{
    internal static class DialogInputSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register() =>
            DialogHost.InstallInputSystemModule = target => target.AddComponent<InputSystemUIInputModule>();
    }
}
