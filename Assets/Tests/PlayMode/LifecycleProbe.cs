#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UguiFlow.Tests
{
    public sealed class LifecycleProbe : ScreenComponent
    {
        internal static readonly List<(string name, string callback, bool active, bool visible)> Calls = new();

        public bool failOnOpen;
        public bool checkReentrancy;

        protected override void OnOpen()
        {
            Record("Open");
            if (checkReentrancy && Window.Screen.TryClose())
                throw new InvalidOperationException("Closed a window during another transition.");
            if (failOnOpen)
                throw new InvalidOperationException("Probe failed to initialize.");
        }

        protected override void OnShow() => Record("Show");
        protected override void OnHide() => Record("Hide");
        protected override void OnClose() => Record("Close");

        private void Record(string callback) => Calls.Add((name, callback, gameObject.activeInHierarchy, Window.IsVisible));
    }
}
#endif
