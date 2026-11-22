using System.Collections.Generic;
using UnityEngine;

namespace UguiFlow.Samples
{
    [RequireComponent(typeof(FlowScreen), typeof(ThemeScope))]
    public sealed class LifecycleDemo : MonoBehaviour
    {
        [SerializeField] private Window firstWindow;
        [SerializeField] private Window secondWindow;
        [SerializeField] private ThemeDatabase lightTheme;
        [SerializeField] private ThemeDatabase darkTheme;
        [SerializeField] private ThemeView status;
        [SerializeField] private ThemeView callbackLog;

        private readonly Queue<string> callbacks = new();
        private FlowScreen screen;
        private ThemeScope scope;

        private void Awake()
        {
            screen = GetComponent<FlowScreen>();
            scope = GetComponent<ThemeScope>();
            scope.ViewReplaced += FollowView;
            RefreshStatus();
        }

        private void OnDestroy() => scope.ViewReplaced -= FollowView;

        public void Open() => Open(firstWindow);
        public void Replace() => Open(secondWindow);

        public void Hide()
        {
            screen.TryHide();
            RefreshStatus();
        }

        public void Show()
        {
            screen.TryShow();
            RefreshStatus();
        }

        public void Close()
        {
            screen.TryClose();
            RefreshStatus();
        }

        public void ShowLight() => SetTheme(lightTheme);
        public void ShowDark() => SetTheme(darkTheme);

        internal void Record(string title, string callback)
        {
            if (callbacks.Count == 4)
                callbacks.Dequeue();
            callbacks.Enqueue($"{callback}   /   {title}");
            callbackLog.ContentLabel.text = string.Join("\n", callbacks);
        }

        internal void RefreshStatus()
        {
            Window window = screen.CurrentWindow;
            status.ContentLabel.text = window == null
                ? "No window mounted"
                : $"{(window.IsVisible ? "Visible" : "Hidden")}   /   {window.GetComponentInChildren<LifecycleWindowDemo>(true).Title}";
        }

        private void Open(Window prefab)
        {
            if (!screen.TryOpen(prefab, out _, out string error))
                Debug.LogError(error, this);
        }

        private void SetTheme(ThemeDatabase theme)
        {
            if (!scope.TrySetTheme(theme, out string error))
                Debug.LogError(error, this);
        }

        private void FollowView(ThemeView before, ThemeView after)
        {
            if (status == before)
                status = after;
            if (callbackLog == before)
                callbackLog = after;
        }
    }
}
