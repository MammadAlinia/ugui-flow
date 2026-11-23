#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UguiFlow.Tests
{
    public sealed class LifecycleProbe : UIComponent
    {
        internal static readonly List<(string name, string callback, bool active, bool visible)> Calls = new();

        public bool failOnOpen;
        public bool checkReentrancy;

        protected override void OnInitialize()
        {
            Record("Initialize");
            if (checkReentrancy && GetComponentInParent<Window>(true).TryClose())
                throw new InvalidOperationException("Closed a window during another transition.");
            if (failOnOpen)
                throw new InvalidOperationException("Probe failed to initialize.");
        }

        protected override void OnShow() => Record("Show");
        protected override void OnHide() => Record("Hide");
        protected override void OnDispose() => Record("Destroy");

        private void Record(string callback) => Calls.Add((name, callback, gameObject.activeInHierarchy, IsVisible));
    }
}
#endif
