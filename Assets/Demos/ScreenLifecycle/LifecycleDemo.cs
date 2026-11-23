using System.Collections.Generic;
using UnityEngine;

namespace UguiFlow.Samples
{
    [RequireComponent(typeof(Window), typeof(ThemeScope))]
    public sealed class LifecycleDemo : MonoBehaviour
    {
        [SerializeField] private UIComponent notesScreen;
        [SerializeField] private ThemeDatabase lightTheme;
        [SerializeField] private ThemeDatabase darkTheme;
        [SerializeField] private ThemeView status;
        [SerializeField] private ThemeView callbackLog;

        private readonly Queue<string> callbacks = new();
        private Window window;
        private ThemeScope scope;

        private void Awake()
        {
            window = GetComponent<Window>();
            scope = GetComponent<ThemeScope>();
            scope.ViewReplaced += FollowView;
            RefreshStatus();
        }

        private void OnDestroy() => scope.ViewReplaced -= FollowView;

        public void Open()
        {
            if (window.CurrentScreen == null)
                Mount();
        }
        public void ResetScreen() => Mount();

        public void Hide()
        {
            window.TryHide();
            RefreshStatus();
        }

        public void Show()
        {
            window.TryShow();
            RefreshStatus();
        }

        public void Close()
        {
            window.TryClose();
            RefreshStatus();
        }

        public void ShowLight() => SetTheme(lightTheme);
        public void ShowDark() => SetTheme(darkTheme);

        internal void Record(string title, string callback)
        {
            if (callbackLog == null)
                return;

            if (callbacks.Count == 4)
                callbacks.Dequeue();
            callbacks.Enqueue($"{callback}   /   {title}");
            callbackLog.ContentLabel.text = string.Join("\n", callbacks);
        }

        private void LateUpdate() => RefreshStatus();

        private void RefreshStatus()
        {
            UIComponent screen = window.CurrentScreen;
            status.ContentLabel.text = screen == null
                ? "No screen mounted"
                : screen.IsVisible ? "Screen visible" : "Screen hidden";
        }

        private void Mount()
        {
            if (!window.TryOpen(notesScreen, out _, out string error))
                Debug.LogError(error, this);
            RefreshStatus();
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
